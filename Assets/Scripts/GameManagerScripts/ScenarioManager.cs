/*
    ScenarioManager.cs
    - Handles loading and transitioning of scenarios
    Contributor(s): John Aylward, Jake Schott, Henryk Musial
    Last Updated: 6/22/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

//contains info for an off-limits spawn location (ex. RLGL spectacle) at the start of a scenario
public struct OffLimitsSpawnLocation
{
    public Vector3 position;
    public float radius;

    public OffLimitsSpawnLocation(Vector3 position, float radius)
    {
        this.position = position;
        this.radius = radius;
    }
}

public class ScenarioManager : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static int[] COUNTDOWN_TIME = new int[] { 900, 720, 600, 360 }; //how long each round lasts before scenario failure based on difficulty
    private static int[][] SCENARIO_SEQUENCE = new int[][] //denotes number of scenarios and scenario tier (1-3) to reach Deep Space Five
    {
        new int[]{ 1, 1, 2, 2, 3 }, //easy
        new int[]{ 1, 2, 2, 3, 2, 3}, //medium
        new int[]{ 2, 2, 1, 2, 1, 2, 3, 3}, //hard
        new int[]{ 2, 2, 3, 2, 2, 3, 2, 3, 2, 3} //expert
    };
    private static string[][] SCENARIO_POSSIBILITIES = new string[][]
    {
        new string[]{ "RedLightGreenLight", "Minefield", "BlackAndWhite", "Historian", "SinisterSymphony", "PartyMode", "GuardiansOfPeace" }, //tier 1 scenarios
        new string[]{ "Indestructibles", "Wreckage", "IntergalacticZoo", "PurpleAlert" }, //tier 2 scenarios
        new string[]{ "Temple", "BlackHole", "InvisibleEnemy", "MiserableMeevils", "ZybokProtocol" } //tier 3 scenarios
    };
    private static int[] OBTAINABLE_COLLECTIBLE_ITEMS = new int[] { 8, 6, 4, 2 }; //how many random collectibles spawn inside the boundary per scenario based on difficulty
    public const int BOUNDARY_SIZE = 5000; //diamater of boundary circle, referenced by PilotingSystem, NavigationMap, ProximityMap
    public const int BOUNDARY_ALTITUDE = 100; //how high/low the ship can go in either direction
    public const int START_DIST_OFFSET = 600; //how far back the ship starts in the entrance path
    public const int DIST_TO_ENDPOINT = 200; //how far into the exit path until endpoint reached
    public const float PATH_SIZE = 10.0f; //for entrance/exit paths, degrees of the boundary, does not reflect on NavigationMap so be careful!
    private static int MAX_SPAWN_LOCATION_SEARCH_ATTEMPTS = 30;

    //different reasons for why a scenario ended
    public enum EndCondition
    {
        ReachedEndpoint,
        LeftBoundary,
        ShipDestroyed,
        TimeRanOut,
        SelfDestructed
    }

    public GameObject player_manager; 
    public GameObject scenario_transitioner;
    public GameObject failure_handler;

    private GameObject scenario_handler;
    private LobbyHandler lobby_handler;
    private ShipInventory ship_inventory;
    private ScenarioCountdown scenario_countdown;
    private ScenarioMap scenario_map;
    private PowerManager power_manager;
    private PowerControl power_control;
    private LightsManager lights_manager;
    private BackgroundAnimator background_animator;
    private Coroutine countdown_coroutine = null;

    private List<Vector3> spawn_locations = new List<Vector3>();
    private bool endpoint_reached = false;
    private bool game_over = false;
    private List<string>[] already_defeated_scenarios = new List<string>[]
    {
        new List<string>(), //tier 1 scenarios
        new List<string>(), //tier 2 scenarios
        new List<string>() //tier 3 scenarios
    };
    private List<string> implemented_scenarios = new List<string>();
    private int num_scenarios_defeated = 0;
    private int current_scenario_index = -1;
    private int countdown_time_to_add = 0; //for controls/scenarios that want to add countdown time
    private int game_difficulty = -1; //assigned by LobbyHandler, goes easy, medium, hard, expert (0-3)

    //entrance/exit channel info
    private Vector2 entrance_position;
    private float entrance_rotation;
    private Vector2 exit_position;
    private float exit_rotation;

    private void Awake()
    {
        lobby_handler = GameObject.Find("LobbyHandler").GetComponent<LobbyHandler>();
        ship_inventory = ReferenceAssistor.Instance.spaceship.GetComponent<ShipInventory>();
        scenario_countdown = ReferenceAssistor.Instance.module_handlers[2].GetComponent<ScenarioCountdown>();
        scenario_map = ReferenceAssistor.Instance.module_handlers[2].GetComponent<ScenarioMap>();
        power_manager = ReferenceAssistor.Instance.power_manager;
        power_control = ReferenceAssistor.Instance.module_handlers[4].GetComponent<PowerControl>();
        lights_manager = GameObject.Find("LightsManager").GetComponent<LightsManager>();
        background_animator = GameObject.Find("BackgroundAnimator").GetComponent<BackgroundAnimator>();
        game_difficulty = lobby_handler.getDifficulty();

        for (int tier = 0; tier < 3; tier++)
        {
            for (int scenario = 0; scenario < SCENARIO_POSSIBILITIES[tier].Length; scenario++)
            {
                for (int scene = 0; scene < SceneManager.sceneCountInBuildSettings; scene++)
                {
                    if (SceneUtility.GetScenePathByBuildIndex(scene).Contains(SCENARIO_POSSIBILITIES[tier][scenario]) == true)
                    {
                        implemented_scenarios.Add(SCENARIO_POSSIBILITIES[tier][scenario]);
                        break;
                    }
                }
            }
        }
    }

    public int getDifficulty()
    {
        return game_difficulty;
    }

    public int getCurrentScenarioIndex()
    {
        return current_scenario_index;
    }

    public bool getGameOver()
    {
        return game_over;
    }

    //returns a list of coordinates of length num_points that is of at least min_distance from each other and not intersecting with off-limits locations
    public List<Vector3> generateSpawnLocations(float min_distance, int num_points, List<OffLimitsSpawnLocation> off_limits_locations)
    {
        float spawn_area_radius = BOUNDARY_SIZE * 0.45f;
        float spawn_area_height = BOUNDARY_ALTITUDE * 2.0f;
        Vector3 world_root_center = new Vector3(0.0f, 0.0f, ScenarioManager.BOUNDARY_SIZE * 0.5f);

        //reset spawn_locations to new list that includes the number of collectible items to be spawned as well
        num_points += OBTAINABLE_COLLECTIBLE_ITEMS[game_difficulty];
        spawn_locations = new List<Vector3>(num_points);

        float cell_size = min_distance / Mathf.Sqrt(3.0f); //grid cell size = minDistance / sqrt(3)

        //any two points in adjacent cells are within minDistance
        int grid_width = Mathf.CeilToInt((2.0f * spawn_area_radius) / cell_size);
        int grid_height = Mathf.CeilToInt(spawn_area_height / cell_size);

        //initialize 3D grid array to store indices of points and populate (-1 for empty cell)
        int[,,] grid = new int[grid_width, grid_height, grid_width];
        for (int i = 0; i < grid_width; i++)
        {
            for (int j = 0; j < grid_height; j++)
            {
                for (int k = 0; k < grid_width; k++)
                {
                    grid[i, j, k] = -1;
                }
            }
        }

        //helper to get grid cell coordinates for a point
        Vector3Int getGridCoordinate(Vector3 point) 
        {
            //convert from world coordinates (centered at 0) to grid indices
            float x = point.x + spawn_area_radius; //shift so min is 0
            float y = point.y + spawn_area_height / 2.0f;
            float z = point.z + spawn_area_radius;

            int xi = Mathf.FloorToInt(x / cell_size);
            int yi = Mathf.FloorToInt(y / cell_size);
            int zi = Mathf.FloorToInt(z / cell_size);
            return new Vector3Int(Mathf.Clamp(xi, 0, grid_width - 1), Mathf.Clamp(yi, 0, grid_height - 1), Mathf.Clamp(zi, 0, grid_width - 1));
        }

        //helper to test if point is too close to any existing point
        bool isValid(Vector3 point) 
        {
            Vector3Int coordinate = getGridCoordinate(point);

            //check cells in a 3 x 3 x 3 neighborhood
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int nx = coordinate.x + dx;
                        int ny = coordinate.y + dy;
                        int nz = coordinate.z + dz;

                        if (nx < 0 || nx >= grid_width || ny < 0 || ny >= grid_height || nz < 0 || nz >= grid_width) //neighbor index is outside of grid bounds
                        {
                            continue; //skip
                        }

                        int idx = grid[nx, ny, nz];

                        if (idx != -1 && Vector3.Distance(point, spawn_locations[idx]) < min_distance) //cell is occupied and fails euclidean distance check
                        {
                            return false; //point is invalid
                        }
                    }
                }
            }

            if (off_limits_locations != null)
            {
                for (int i = 0; i < off_limits_locations.Count; i++)
                {
                    float required_distance = off_limits_locations[i].radius + min_distance;
                    if (Vector3.Distance(point, off_limits_locations[i].position) < required_distance)
                    {
                        return false; //point is invalid
                    }
                }
            }

            return true; //valid point
        }

        //generate spawn points
        for (int i = 0; i < num_points; i++)
        {
            bool found = false;

            for (int attempt = 0; attempt < MAX_SPAWN_LOCATION_SEARCH_ATTEMPTS; attempt++)
            {
                //calculate a random point within the volume of a cylinder 
                float angle = Random.Range(0f, 2.0f * Mathf.PI); //random angle between 0 & 360 deg
                float r = spawn_area_radius * Mathf.Sqrt(Random.Range(0f, 1.0f)); //random radius sqrt for uniform distribution on circle 

                //convert polar xz coords to cartesian
                float x = r * Mathf.Cos(angle);
                float z = r * Mathf.Sin(angle);

                float y = Random.Range(-spawn_area_height / 2.0f, spawn_area_height / 2.0f); //random height 

                Vector3 candidate_point = new Vector3(x, y, z);

                if (isValid(candidate_point))
                {
                    spawn_locations.Add(candidate_point);
                    Vector3Int coord = getGridCoordinate(candidate_point);
                    grid[coord.x, coord.y, coord.z] = spawn_locations.Count - 1; //store point index
                    found = true;
                    break;
                }
            }

            //fallback just in case
            if (!found)
            {
                //generate a point without a min distance check
                float angle = Random.Range(0f, 2.0f * Mathf.PI);
                float r = spawn_area_radius * Mathf.Sqrt(Random.Range(0f, 1.0f));
                float x = r * Mathf.Cos(angle);
                float z = r * Mathf.Sin(angle);
                float y = Random.Range(-spawn_area_height / 2.0f, spawn_area_height / 2.0f);
                spawn_locations.Add(new Vector3(x, y, z));
            }
        }

        //add world_root_center
        for (int i = 0; i < spawn_locations.Count; i++)
        {
            spawn_locations[i] += world_root_center;
        }

        //return the locations that are not occupied by the collectibles (0-OBTAINBLE_COLLECTIBLE_ITEMS[game_difficulty]) to be spawned
        List<Vector3> free_locations = new List<Vector3>(num_points);
        for (int i = OBTAINABLE_COLLECTIBLE_ITEMS[game_difficulty]; i < num_points; i++)
        {
            free_locations.Add(spawn_locations[i]);
        }
        return free_locations;
    }

    //called by generatePathLocation() and PilotingSystem.CalculatePoint()
    public static Vector2 getBoundaryPointFromAngle(float ang)
    {
        Vector2 return_point = new Vector2(0.0f, 0.0f);
        float path_slope = Mathf.Tan(Mathf.Deg2Rad * ang);
        return_point.x = ((BOUNDARY_SIZE * 0.5f) * (BOUNDARY_SIZE * 0.5f)) / (1.0f + (path_slope * path_slope));
        return_point.x = Mathf.Sqrt(return_point.x);
        return_point.y = return_point.x * path_slope;
        return return_point;
    }

    //called by generatePaths() to generate the points for the entrance/exit channels
    private Vector2 generatePathLocation()
    {
        float path_angle = Random.Range(0.0f, 15.0f);
        Vector2 path_point = getBoundaryPointFromAngle(path_angle);
        //determine if the path point will be above or below the midline of the circle/boundary
        if (Random.Range(0,2) == 0)
        {
            path_point.y *= -1;
        }
        return path_point;
    }

    //called by prepScenario()
    private void spawnCollectibleItem(int location_index, bool utility)
    {
        Transform world_root = ReferenceAssistor.Instance.world_root.transform;

        int item_index = 0;
        if (utility == true)
        {
            item_index = Random.Range(0, 4);
        }
        else
        {
            item_index = Random.Range(4, 10);
        }

        GameObject collectible_item = GameObject.Instantiate(ReferenceAssistor.Instance.collectible_items[item_index], world_root);
        collectible_item.transform.localRotation = Random.rotation;
        Vector3 spawn_location = spawn_locations[location_index];
        collectible_item.transform.localPosition = spawn_location;
        collectible_item.GetComponent<CollectibleItem>().setSerialNumber(ship_inventory.generateSerialNumber());
        collectible_item.GetComponent<Collider>().excludeLayers = LayerMask.GetMask("None");

        collectible_item.GetComponent<NetworkObject>().SynchronizeTransform = true;
        collectible_item.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
        collectible_item.GetComponent<NetworkObject>().TrySetParent(world_root);
    }

    //sets entrance/exit channel points and rotations
    public void generatePaths()
    {
        entrance_position = generatePathLocation();
        entrance_position.x *= -1.0f;
        entrance_rotation = Random.Range(-10.0f, 10.0f);
        exit_position = generatePathLocation();
        exit_rotation = Random.Range(-10.0f, 10.0f);
        setNewPathsRPC(entrance_position, entrance_rotation, exit_position, exit_rotation);
    }

    //called when all players have loaded in at the very start
    public void intializeScenarioDatabase()
    {
        foreach (Component c in GetComponents<IScenarioInitialization>())
        {
            if (c as IScenarioInitialization != null)
            {
                IScenarioInitialization isi = (IScenarioInitialization)c;
                isi.initializeDatabaseInformation();
            }
        }
    }

    //returns scenario name (ex. "RedLightGreenLight") of a randomly-selected eligible scenario in given tier (SCENARIO_POSSIBILITIES)
    private string identifyNextScenario(int tier)
    {
        List<string> possible_scenarios = new List<string>();
        for (int i = 0; i < SCENARIO_POSSIBILITIES[tier].Length; i++)
        {
            if (already_defeated_scenarios[tier].Contains(SCENARIO_POSSIBILITIES[tier][i]) == false && implemented_scenarios.Contains(SCENARIO_POSSIBILITIES[tier][i]) == true)
            {
                possible_scenarios.Add(SCENARIO_POSSIBILITIES[tier][i]);
            }
        }
        if (possible_scenarios.Count == 0)
        {
            return "";
        }
        return possible_scenarios[Random.Range(0, possible_scenarios.Count)];
    }

    //gets scenario index from name
    private int getScenarioIndexFromName(string name)
    {
        int scenario_index = -1;
        for (int tier = 0; tier < 3; tier++)
        {
            for (int index = 0; index < SCENARIO_POSSIBILITIES[tier].Length; index++)
            {
                scenario_index++;
                if (SCENARIO_POSSIBILITIES[tier][index].CompareTo(name) == 0)
                {
                    return scenario_index;
                }
            }
        }
        return -1;
    }

    //called when start of scenario transition
    public void loadNewScenario()
    {
        endpoint_reached = false;

        string next_scenario = "RedLightGreenLight"; //used for override for testing (blank means obey sequence and random)

        if (next_scenario.CompareTo("") == 0)
        {
            int tier_to_select = SCENARIO_SEQUENCE[game_difficulty][num_scenarios_defeated] - 1; //-1 to make it index nicely 0-2 instead of 1-3
            if (already_defeated_scenarios[tier_to_select].Count == SCENARIO_POSSIBILITIES[tier_to_select].Length)
            {
                already_defeated_scenarios[tier_to_select].Clear();
            }
            next_scenario = identifyNextScenario(tier_to_select);
        }

        current_scenario_index = getScenarioIndexFromName(next_scenario);
        if (current_scenario_index == -1)
        {
            next_scenario = SCENARIO_POSSIBILITIES[0][0]; //default to RLGL
            current_scenario_index = 0; //default to RLGL
        }

        NetworkManager.Singleton.SceneManager.LoadScene(next_scenario, LoadSceneMode.Single);
    }

    //called by PlayerManager.scenarioLoadedRPC() when all players have loaded the scenario scene
    public void prepScenario(bool first_scenario)
    {
        //initialize inventory if first scenario
        if (first_scenario == true)
        {
            ship_inventory.GetComponent<ShipInventory>().initializeInventory();
        }

        powerAllStationsRPC();

        //clear spawn locations
        spawn_locations.Clear();
        
        //generate new entrance/exit path locations and angles
        generatePaths();

        //reset transmission frequencies
        ReferenceAssistor.Instance.module_handlers[1].GetComponent<FrequencyAdjuster>().resetFrequencies();

        //check for a scenario script and handle any sort of scenario prep (ex. starting an energy pattern, spawning cheeseballs)
        scenario_handler = GameObject.FindWithTag("ScenarioHandler");
        IScenario scenario_script = getScenarioScript();
        if (scenario_script != null)
        {
            scenario_script.initiateScenario();
        }

        //ensure spawn locations created for obtainable collectibles
        if (spawn_locations.Count == 0)
        {
            generateSpawnLocations(25.0f, 0, null);
        }

        //spawn collectibles
        for (int i = 0; i < OBTAINABLE_COLLECTIBLE_ITEMS[game_difficulty]; i++) 
        {
            spawnCollectibleItem(i, i >= OBTAINABLE_COLLECTIBLE_ITEMS[game_difficulty] / 2);
        }
    }

    //only run by host, called by PlayerManager.startScenarioRPC()
    public void startScenario()
    {
        enableScenarioTimer();
        ReferenceAssistor.Instance.power_manager.GetComponent<PowerRegulator>().initializePowerRegulator();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<EngineCoolantSupply>().initializeEngineTemperatureIncreaser();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<ComputerRegulator>().initializeComputerRegulator();
        ReferenceAssistor.Instance.module_handlers[4].GetComponent<PrefixCodeManager>().initiatePrefixCodeManager();
    }

    //called by SignalJammer.cs
    public void addCountdownTime(int time_to_add)
    {
        countdown_time_to_add += time_to_add;
    }

    IEnumerator scenarioCountdown()
    {
        int time_remaining = COUNTDOWN_TIME[getDifficulty()];
        countdownUpdateRPC(time_remaining);
        while (time_remaining > 0)
        {
            yield return new WaitForSeconds(1.0f);
            time_remaining += countdown_time_to_add;
            countdown_time_to_add = 0;
            time_remaining--;
            countdownUpdateRPC(time_remaining);
        }

        yield return new WaitForSeconds(2.0f); //2-second courtesy

        countdown_coroutine = null;

        endScenario(EndCondition.TimeRanOut);
    }

    private void enableScenarioTimer()
    {
        disableScenarioTimer();
        countdown_coroutine = StartCoroutine(scenarioCountdown());
    }

    private void disableScenarioTimer()
    {
        if (countdown_coroutine != null)
        {
            StopCoroutine(countdown_coroutine);
            countdown_coroutine = null;
        }
    }

    //returns the IScenario script component attached to ScenarioHandler as the first component beneath NetworkObject (if it exists)
    private IScenario getScenarioScript()
    {
        if (scenario_handler != null)
        {
            Component scenario_script_component = scenario_handler.GetComponentAtIndex(2);
            if (scenario_script_component != null)
            {
                IScenario scenario_script = (IScenario)scenario_script_component;
                if (scenario_script != null)
                {
                    return scenario_script;
                }
            }
        }
        return null;
    }

    //called when a scenario ends by PilotingSystem, ShipHealth, a scenario, or this
    public void endScenario(EndCondition reason)
    {
        //only run if host
        if (NetworkManager.IsHost == false)
        {
            return; 
        }

        disableScenarioTimer();

        //check if already did game over or reached endpoint
        if (game_over == true || endpoint_reached == true)
        {
            return;
        }

        if (reason == EndCondition.ReachedEndpoint) //only success condition is to reach endpoint
        {
            endpoint_reached = true;
            num_scenarios_defeated++;
            handleTransitionRPC(current_scenario_index, scenario_transitioner.GetComponent<TransitionHandler>().GetTransitionOption(), num_scenarios_defeated / (1.0f * SCENARIO_SEQUENCE[game_difficulty].Length));
            return;
        }

        game_over = true;
        string failure_report_message = "";

        //failure conditions
        if (reason == EndCondition.TimeRanOut)
        {
            failure_report_message = "Stolen ship designated SEACC-3002 was apprehended and recovered after long-range scanners intercepted its signal at the conclusion of the periodic " + (COUNTDOWN_TIME[getDifficulty()] / 60).ToString() + "-minute reset window.";
        }
        else if (reason == EndCondition.LeftBoundary)
        {
            string[] crew_members = new string[4] { "One crew member was found alive and has been", "Two crew members were found alive and have been",  "Three crew members were found alive and have been", "Four crew members were found alive and have been" };
            failure_report_message = "Stolen ship designated SEACC-3002 mistakenly left long-range scanner dead zone and was immediately identified and apprehended. " + crew_members[lobby_handler.getNumberOfPlayersInNetworkManagerLobby() - 1] + " arrested.";
        }
        else if (reason == EndCondition.SelfDestructed)
        {
            failure_report_message = "Debris of stolen ship designated SEACC-3002 was found after apparent self-destruction. No survivors found and ship has been sent to SEACC authority for further investigation.";
        }
        else if (reason == EndCondition.ShipDestroyed)
        {
            failure_report_message = "Stolen ship designated SEACC-3002 was discovered adrift in space with severe hull damage. No survivors found and ship has been deemed unsalvageable due to irreparable damage.";

            IScenario scenario_script = getScenarioScript();
            if (scenario_script != null)
            {
                failure_report_message = scenario_script.getDeathMessage();
            }
        }

        //reparent all players to prepare for reparenting later on should they restart
        foreach (GameObject plr in GameObject.FindGameObjectsWithTag("Player"))
        {
            plr.transform.parent = GameObject.Find("NetworkManager").transform.parent;
        }

        //destroy seats
        GameObject.FindGameObjectWithTag("SeatHandler").GetComponent<SeatManager>().destroySeats();

        //turn off power
        ReferenceAssistor.Instance.power_manager.totalShutdown(false);

        handleFailureRPC(num_scenarios_defeated / (1.0f * SCENARIO_SEQUENCE[game_difficulty].Length), failure_report_message, (reason == EndCondition.TimeRanOut || reason == EndCondition.LeftBoundary));
    }

    //ensures every player has the same entrance/exit path locations and rotations
    [Rpc(SendTo.Everyone)]
    private void setNewPathsRPC(Vector2 ent_pos, float ent_rot, Vector2 exit_pos, float exit_rot)
    {
        entrance_position = ent_pos;
        entrance_rotation = ent_rot;
        exit_position = exit_pos;
        exit_rotation = exit_rot;

        scenario_map.updatePathLocations(entrance_position, entrance_rotation, exit_position, exit_rotation);
        ReferenceAssistor.Instance.spaceship.GetComponent<ShipMovement>().SetPaths(entrance_position, entrance_rotation, exit_position, exit_rotation);

        //if host, position the ship to entrance position and let the network sync the transform
        if (NetworkManager.Singleton.IsHost == true)
        {
            ReferenceAssistor.Instance.spaceship.GetComponent<ShipMovement>().PlaceShip(entrance_position, ent_rot);
        }
    }

    //called by handleTransitionRPC(), used to reset certain controls for next scenario
    private void controlResetHelper()
    {
        //resets PowerManager, PowerAllocation, and PowerRegulator
        power_manager.resetPowerManager();

        //reset BackgroundAnimator
        background_animator.enableAllScreens();

        //power down all stations
        for (int i = 0; i < 4; i++)
        {
            power_manager.disableStation(i);
        }

        //reset lights
        lights_manager.resetLights();

        //reset certain controls
        ReferenceAssistor.Instance.module_handlers[0].GetComponent<SpatialCompositionAnalyzer>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[0].GetComponent<TractorBeamOptions>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[0].GetComponent<DirectionalShifter>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[1].GetComponent<ThreatDetectors>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[1].GetComponent<ProximityMapOptions>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[1].GetComponent<LongRangeDirection>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[1].GetComponent<FrequencyAdjuster>().resetFrequencies();
        ReferenceAssistor.Instance.module_handlers[1].GetComponent<TorpedoBaySelector>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<EnergyPattern>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<PhaserFrequency>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<PhaserHeat>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<AuxiliaryPower>().resetAuxiliaryPower();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<EngineCoolantSupply>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<TorpedoLoader>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<CargoEject>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<ComputerRegulator>().resetToDefault();

        //destroy probe (if exists)
        ReferenceAssistor.Instance.module_handlers[1].GetComponent<ProbeController>().damageProbe(9999.9f);
    }

    [Rpc(SendTo.Everyone)]
    private void handleTransitionRPC(int defeated_scenario_index, int transition_option, float percent_to_DSF)
    {
        //update logs
        LogMenuController.OnScenarioBeaten(defeated_scenario_index);

        //prepare to load next scenario
        ReferenceAssistor.Instance.player_manager.resetReadyPlayers();

        //power down all stations and reset certain controls (power will be restored later)
        controlResetHelper();

        //reset and mute audio
        ReferenceAssistor.Instance.audio_manager.DeactivateComputerVoice();
        ReferenceAssistor.Instance.audio_manager.MuteAudio();
        ReferenceAssistor.Instance.audio_manager.ResetToDefault();

        //stop checking for controls/seats
        PrimaryScript.Instance.deactivate(true, false);
        ReferenceAssistor.Instance.player_manager.getLocalPlayer().GetComponent<CameraMove>().ResetCameraEffects();

        //show transition
        scenario_transitioner.GetComponent<TransitionHandler>().ShowTransition(transition_option, OverviewTracker.getStarDate(percent_to_DSF), OverviewTracker.getDistanceToDSF(percent_to_DSF));

        //update overview screen in back of bridge
        ReferenceAssistor.Instance.module_handlers[4].GetComponent<OverviewTracker>().updateOverviewDisplay(percent_to_DSF);

        //if host, begin to load the next scenario
        if (NetworkManager.Singleton.IsHost == true)
        {
            loadNewScenario();
        } 
    }

    [Rpc(SendTo.Everyone)]
    private void powerAllStationsRPC()
    {
        for (int i = 0; i < 4; i++)
        {
            power_manager.powerStation(i);
            power_control.turnDial(i, true);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void handleFailureRPC(float percent_to_DSF, string failure_message, bool caught)
    {
        //set game over to true
        game_over = true;

        //mute audio
        ReferenceAssistor.Instance.audio_manager.MuteAudio();

        //stop checking for controls/seats
        PrimaryScript.Instance.deactivate(false, true);

        //display death screen using scenario number sn and death message frm
        failure_handler.GetComponent<FailureHandler>().displayDeathScreen(lobby_handler.getPlayerNamesInLobby(), lobby_handler.getPlayerSteamIDsInLobby(), OverviewTracker.getStarDate(percent_to_DSF), failure_message, caught);
    }

    //used to update the boundary expiration timer in engineer position
    [Rpc(SendTo.Everyone)]
    private void countdownUpdateRPC(int time_remaining)
    {
        scenario_countdown.displayCountdownAdjustment(time_remaining);
    }
}