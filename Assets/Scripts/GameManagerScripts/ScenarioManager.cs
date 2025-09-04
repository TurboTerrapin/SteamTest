/*
    ScenarioManager.cs
    - Handles loading and transitioning of scenarios
    Contributor(s): John Aylward, Jake Schott
    Last Updated: 8/27/2025
*/

using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioManager : NetworkBehaviour
{
    //CLASS CONSTANTS
    public const int COUNTDOWN_TIME = 360; //how long each round lasts before scenario failure
    public const int BOUNDARY_SIZE = 5000; //diamater of boundary circle, referenced by PilotingSystem, EngineerMap
    public const int BOUNDARY_ALTITUDE = 100; //how high/low the ship can go in either direction
    public const int START_DIST_OFFSET = 500; //how far back the ship starts in the entrance path
    public const int DIST_TO_ENDPOINT = 200; //how far into the exit path until endpoint reached
    public const float PATH_SIZE = 10.0f; //for entrance/exit paths, degrees of the boundary, does not reflect on EngineerMap so be careful!

    //different reasons for why a scenario ended
    public enum EndCondition
    {
        ReachedEndpoint = 0,
        LeftBoundary = 1,
        ShipDestroyed = 2,
        TimeRanOut = 3
    }

    private enum Difficulty
    {
        Random,
        Easy,
        Medium,
        Hard,
        Specific
    }

    public GameObject player_manager; 
    public GameObject scenario_transitioner;
    public GameObject failure_handler;

    private EngineerScenarioCountdown scenario_countdown;
    private EngineerMap engineer_map;
    private Coroutine countdown_coroutine;
    private GameObject scenario_handler;

    private bool endpoint_reached = false;
    private bool game_over = false;
    private int scenario_number = 0;

    //entrance/exit channel info
    private Vector2 entrance_position;
    private float entrance_rotation;
    private Vector2 exit_position;
    private float exit_rotation;

    private void Start()
    {
        scenario_countdown = GameObject.FindWithTag("SensorHandler").GetComponent<EngineerScenarioCountdown>();
        engineer_map = GameObject.FindWithTag("SensorHandler").GetComponent<EngineerMap>();
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

    public string loadNewScenario()
    {
        endpoint_reached = false;
        scenario_number += 1;
        if (SceneManager.GetActiveScene().name == "Cheeseballs")
        {
            SceneSwapper.Instance.ChangeScene("RedLightGreenLight", scenario_number);
            return "RedLightGreenLight";
        }
        else
        {
            SceneSwapper.Instance.ChangeScene("Cheeseballs", scenario_number);
            return "Cheeseballs";
        }
    }

    public void prepScenario(bool enable_stations)
    {
        if (enable_stations == true)
        {
            powerAllStationsRPC();
        }
        GameObject.FindGameObjectWithTag("Spaceship").GetComponent<ShipController>().assignWorldRoot(GameObject.FindGameObjectWithTag("WorldRoot"));
        generatePaths();
        scenario_handler = GameObject.FindWithTag("ScenarioHandler");
        IScenario scenario_script = getScenarioScript();
        if (scenario_script != null)
        {
            scenario_script.initiateScenario();
        }
    }

    //only run by host
    public void startScenario()
    {
        enableScenarioTimer();
    }

    IEnumerator scenarioCountdown()
    {
        int time_remaining = COUNTDOWN_TIME;
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
            failure_report_message = "Stolen ship designated NCC-3002 was apprehended and recovered after long-range scanners intercepted its signal at the conclusion of the periodic 6-minute reset window.";
        }
        else if (reason == EndCondition.LeftBoundary)
        {
            failure_report_message = "Stolen ship designated NCC-3002 mistakenly left long-range scanner dead zone and was immediately identified and apprehended. Four crew members were found alive and have been arrested.";
        }
        else if (reason == EndCondition.ShipDestroyed)
        {
            failure_report_message = "Stolen ship designated NCC-3002 was discovered adrift in space with severe hull damage. No survivors found and ship has been deemed unsalvageable due to irreparable damage.";

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


    /*
     *         if (difficulty == Difficulty.Random)
        {
            SceneSwapper.Instance.ChangeSceneRandom();
        }
        else if (difficulty == Difficulty.Easy)
        {
            SceneSwapper.Instance.ChangeScenarioEasy();
        }
        else if (difficulty == Difficulty.Medium)
        {
            SceneSwapper.Instance.ChangeScenarioMedium();
        }
        else if (difficulty == Difficulty.Hard)
        {
            SceneSwapper.Instance.ChangeScenarioHard();
        }
        else if (difficulty == Difficulty.Specific)
        {
            SceneSwapper.Instance.ChangeScene(specificSceneName, specificSceneNum);
        }
    */

    [Rpc(SendTo.Everyone)]
    private void setNewPathsRPC(Vector2 ent_pos, float ent_rot, Vector2 exit_pos, float exit_rot)
    {
        entrance_position = ent_pos;
        entrance_rotation = ent_rot;
        exit_position = exit_pos;
        exit_rotation = exit_rot;

        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject.FindGameObjectWithTag("Spaceship").GetComponent<PilotingSystem>().SetPaths(entrance_position, entrance_rotation, exit_position, exit_rotation);
            GameObject.FindGameObjectWithTag("Spaceship").GetComponent<PilotingSystem>().PlaceShip(entrance_position, ent_rot);
        }
        engineer_map.updatePathLocations(entrance_position, entrance_rotation, exit_position, exit_rotation);
    }

    [Rpc(SendTo.Everyone)]
    private void handleTransitionRPC(int sn)
    {
        //prepare to load next scenario
        GameObject.FindGameObjectWithTag("PlayerManager").GetComponent<PlayerManager>().resetPlayersReady();

        //power down all stations
        for (int i = 0; i < 4; i++)
        {
            GameObject.Find("PowerHandler").GetComponent<PowerManager>().disableStation(i);
        }
        GameObject.Find("AudioManager").GetComponent<AudioManager>().MuteAudio();
        ControlScript.Instance.deactivate(true, false);
        scenario_transitioner.GetComponent<TransitionHandler>().ShowTransition(sn);

        //reset certain controls
        GameObject.Find("SensorHandler").GetComponent<EnergyPatternManager>().clearAllPatterns();

        foreach (GameObject probe in GameObject.FindGameObjectsWithTag("Probe"))
        {
            probe.GetComponent<Probe>().damageProbe(9999.9f);
        }

        if (NetworkManager.Singleton.IsHost == true)
        {
            loadNewScenario();
        } 
    }

    [Rpc(SendTo.Everyone)]
    private void powerAllStationsRPC()
    {
        PowerManager power_manager = GameObject.Find("PowerHandler").GetComponent<PowerManager>();
        PowerControl power_control = GameObject.FindGameObjectWithTag("ControlHandler").GetComponent<PowerControl>();
        for (int i = 0; i < 4; i++)
        {
            power_manager.powerStation(i);
            power_control.turnDial(i, true);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void handleFailureRPC(int sn, string frm)
    {
        GameObject.Find("AudioManager").GetComponent<AudioManager>().MuteAudio();
        ControlScript.Instance.deactivate(false, true);
        failure_handler.GetComponent<FailureHandler>().displayDeathScreen(player_manager.GetComponent<PlayerManager>().getPlayerNames(), sn, frm);
    }

    [Rpc(SendTo.Everyone)]
    private void countdownUpdateRPC(int time_remaining)
    {
        scenario_countdown.displayCountdownAdjustment(time_remaining);
    }
}
