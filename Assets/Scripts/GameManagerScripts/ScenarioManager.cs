/*
    ScenarioManager.cs
    - Handles loading and transitioning of scenarios
    Contributor(s): John Aylward, Jake Schott
    Last Updated: 3/22/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioManager : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static int[] COUNTDOWN_TIME = new int[] { 900, 720, 600, 360 }; //how long each round lasts before scenario failure
    public const int BOUNDARY_SIZE = 5000; //diamater of boundary circle, referenced by PilotingSystem, NavigationMap, ProximityMap
    public const int BOUNDARY_ALTITUDE = 100; //how high/low the ship can go in either direction
    public const int START_DIST_OFFSET = 500; //how far back the ship starts in the entrance path
    public const int DIST_TO_ENDPOINT = 200; //how far into the exit path until endpoint reached
    public const float PATH_SIZE = 10.0f; //for entrance/exit paths, degrees of the boundary, does not reflect on NavigationMap so be careful!

    //different reasons for why a scenario ended
    public enum EndCondition
    {
        ReachedEndpoint = 0,
        LeftBoundary = 1,
        ShipDestroyed = 2,
        TimeRanOut = 3,
        SelfDestructed = 4
    }

    //contains info for a spawn location at the start of a scenario
    private struct OccupiedSpawnLocation
    {
        private Vector3 spawn_position;
        private float spawn_radius;
        private bool infinitely_tall;

        public OccupiedSpawnLocation(Vector3 p, float r, bool it)
        {
            spawn_position = p;
            spawn_radius = r;
            infinitely_tall = it;
        }

        public Vector3 getSpawnPosition()
        {
            return spawn_position;
        }

        public float getSpawnRadius()
        {
            return spawn_radius;
        }

        public bool getInfinitelyTall()
        {
            return infinitely_tall;
        }
    }

    public GameObject player_manager; 
    public GameObject scenario_transitioner;
    public GameObject failure_handler;

    private ShipInventory ship_inventory;
    private ScenarioCountdown scenario_countdown;
    private ScenarioMap scenario_map;
    private PowerManager power_manager;
    private PowerControl power_control;
    private LightsManager lights_manager;
    private BackgroundAnimator background_animator;
    private Coroutine countdown_coroutine;
    private GameObject scenario_handler;

    private List<OccupiedSpawnLocation> occupied_spawn_locations = new List<OccupiedSpawnLocation>();
    private bool endpoint_reached = false;
    private bool game_over = false;
    private int scenario_number = 0;
    private int game_difficulty = -1; //assigned by LoadHandler, goes easy, medium, hard, expert (0-3)

    //entrance/exit channel info
    private Vector2 entrance_position;
    private float entrance_rotation;
    private Vector2 exit_position;
    private float exit_rotation;

    private void Awake()
    {
        ship_inventory = GameObject.FindGameObjectWithTag("Spaceship").GetComponent<ShipInventory>();
        scenario_countdown = ReferenceAssistor.Instance.module_handlers[2].GetComponent<ScenarioCountdown>();
        scenario_map = ReferenceAssistor.Instance.module_handlers[2].GetComponent<ScenarioMap>();
        power_manager = ReferenceAssistor.Instance.power_manager;
        power_control = ReferenceAssistor.Instance.module_handlers[4].GetComponent<PowerControl>();
        lights_manager = GameObject.Find("LightsManager").GetComponent<LightsManager>();
        background_animator = GameObject.Find("BackgroundAnimator").GetComponent<BackgroundAnimator>();
        game_difficulty = GameObject.Find("LobbyHandler").GetComponent<LobbyHandler>().getDifficulty();
    }

    public int getDifficulty()
    {
        return game_difficulty;
    }

    public void forceSpawnLocation(Vector3 location, float radius, bool infinitely_tall)
    {
        occupied_spawn_locations.Add(new OccupiedSpawnLocation(location, radius, infinitely_tall));
    }

    public Vector3 getSpawnLocation(float radius, bool infinitely_tall)
    {
        Vector3 location_to_insert = Vector3.zero;
        bool successful_insertion = false;

        while (successful_insertion == false)
        {
            Vector2 x_and_z = Random.insideUnitCircle * ((ScenarioManager.BOUNDARY_SIZE - 200.0f) * 0.5f);

            float x_coordinate = x_and_z.x;
            float y_coordinate = Random.Range(-(ScenarioManager.BOUNDARY_ALTITUDE + 20.0f), ScenarioManager.BOUNDARY_ALTITUDE + 20.0f);
            float z_coordinate = x_and_z.y + ScenarioManager.BOUNDARY_SIZE * 0.5f;

            location_to_insert =
                new Vector3(x_coordinate, y_coordinate, z_coordinate);

            successful_insertion = true;

            foreach (OccupiedSpawnLocation existing_location in occupied_spawn_locations)
            {
                float necessary_buffer = existing_location.getSpawnRadius() + radius;
                if (existing_location.getInfinitelyTall() == true || infinitely_tall == true)
                {
                    if (Vector2.Distance(new Vector2(existing_location.getSpawnPosition().x, existing_location.getSpawnPosition().z), new Vector2(location_to_insert.x, location_to_insert.z)) < necessary_buffer)
                    {
                        successful_insertion = false;
                        break;
                    }
                }
                else
                {
                    if (Vector3.Distance(existing_location.getSpawnPosition(), location_to_insert) < necessary_buffer)
                    {
                        successful_insertion = false;
                        break;
                    }
                }
            }
        }

        occupied_spawn_locations.Add(new OccupiedSpawnLocation(location_to_insert, radius, infinitely_tall));
        return location_to_insert;
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
    private void spawnCollectibleItem(bool utility)
    {
        Transform world_root = GameObject.FindGameObjectWithTag("WorldRoot").transform;

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
        Vector3 spawn_location = getSpawnLocation(5.0f, false);
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

    //called when start of scenario transition
    public string loadNewScenario()
    {
        endpoint_reached = false;
        scenario_number += 1;
        if (SceneManager.GetActiveScene().name != "RedLightGreenLight") 
        {
            SceneSwapper.Instance.ChangeScene("RedLightGreenLight", scenario_number);
            return "RedLightGreenLight";
        }
        else
        {
            SceneSwapper.Instance.ChangeScene("CollectibleTest", scenario_number);
            return "CollectibleTest";
        }
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
        occupied_spawn_locations.Clear();

        //assign the piloting system the new WorldRoot
        GameObject.FindGameObjectWithTag("Spaceship").GetComponent<ShipController>().assignWorldRoot(GameObject.FindGameObjectWithTag("WorldRoot"));
        
        //generate new entrance/exit path locations and angles
        generatePaths();

        //reset transmission frequencies
        ReferenceAssistor.Instance.module_handlers[1].GetComponent<TransmissionHandler>().resetFrequencies();

        //check for a scenario script and handle any sort of scenario prep (ex. starting an energy pattern, spawning cheeseballs)
        scenario_handler = GameObject.FindWithTag("ScenarioHandler");
        IScenario scenario_script = getScenarioScript();
        if (scenario_script != null)
        {
            scenario_script.initiateScenario();
        }

        //spawn collectibles
        spawnCollectibleItem(true);
        spawnCollectibleItem(false);
    }

    //only run by host, called by PlayerManager.startScenarioRPC()
    public void startScenario()
    {
        enableScenarioTimer();
        GameObject.Find("PowerHandler").GetComponent<PowerRegulator>().initializePowerRegulator();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<EngineCoolantSupply>().initializeEngineTemperatureIncreaser();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<ComputerRegulator>().initializeComputerRegulator();
        ReferenceAssistor.Instance.module_handlers[4].GetComponent<PrefixCodeManager>().initiatePrefixCodeManager();
    }

    IEnumerator scenarioCountdown()
    {
        int time_remaining = COUNTDOWN_TIME[getDifficulty()];
        countdownUpdateRPC(time_remaining);
        while (time_remaining > 0)
        {
            yield return new WaitForSeconds(1.0f);
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
            handleTransitionRPC(scenario_number);
            return;
        }

        game_over = true;
        string failure_report_message = "";

        //failure conditions
        if (reason == EndCondition.TimeRanOut)
        {
            failure_report_message = "Stolen ship designated SEACC-3002 was apprehended and recovered after long-range scanners intercepted its signal at the conclusion of the periodic 6-minute reset window.";
        }
        else if (reason == EndCondition.LeftBoundary)
        {
            failure_report_message = "Stolen ship designated SEACC-3002 mistakenly left long-range scanner dead zone and was immediately identified and apprehended. Four crew members were found alive and have been arrested.";
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

        handleFailureRPC(scenario_number, failure_report_message);
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

        //if host, position the ship to entrance position and let the network sync the transform
        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject.FindGameObjectWithTag("Spaceship").GetComponent<PilotingSystem>().SetPaths(entrance_position, entrance_rotation, exit_position, exit_rotation);
            GameObject.FindGameObjectWithTag("Spaceship").GetComponent<PilotingSystem>().PlaceShip(entrance_position, ent_rot);
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
        ReferenceAssistor.Instance.module_handlers[1].GetComponent<TransmissionHandler>().resetFrequencies();
        ReferenceAssistor.Instance.module_handlers[1].GetComponent<TorpedoBaySelector>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<EnergyPattern>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<PhaserFrequency>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<AuxiliaryPower>().resetAuxiliaryPower();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<EngineCoolantSupply>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<TorpedoLoader>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<CargoEjectLoader>().resetToDefault();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<ComputerRegulator>().resetToDefault();

        //destroy probe (if exists)
        ReferenceAssistor.Instance.module_handlers[1].GetComponent<ProbeController>().damageProbe(9999.9f);
    }

    [Rpc(SendTo.Everyone)]
    private void handleTransitionRPC(int sn)
    {
        //prepare to load next scenario
        GameObject.FindGameObjectWithTag("PlayerManager").GetComponent<PlayerManager>().resetPlayersReady();

        //power down all stations and reset certain controls (power will be restored later)
        controlResetHelper();

        //mute audio during scene transition
        GameObject.Find("AudioManager").GetComponent<AudioManager>().MuteAudio();

        //stop checking for controls/seats
        PrimaryScript.Instance.deactivate(true, false);

        //show transition
        scenario_transitioner.GetComponent<TransitionHandler>().ShowTransition(sn);

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
    private void handleFailureRPC(int sn, string frm)
    {
        //mute audio
        GameObject.Find("AudioManager").GetComponent<AudioManager>().MuteAudio();

        //stop checking for controls/seats
        PrimaryScript.Instance.deactivate(false, true);

        //display death screen using scenario number sn and death message frm
        failure_handler.GetComponent<FailureHandler>().displayDeathScreen(player_manager.GetComponent<PlayerManager>().getPlayerNames(), sn, frm);
    }

    //used to update the boundary expiration timer in engineer position
    [Rpc(SendTo.Everyone)]
    private void countdownUpdateRPC(int time_remaining)
    {
        scenario_countdown.displayCountdownAdjustment(time_remaining);
    }
}