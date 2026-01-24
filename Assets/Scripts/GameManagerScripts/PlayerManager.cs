/*
    PlayerManager.cs
    - Handles loading and managing of players
    - Handles when a player quits to take them back to the TitleScreen
    Contributor(s): Jake Schott
    Last Updated: 9/6/2025
*/

using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static int MINIMUM_PLAYERS = -1; //if -1, will default to how many players are in the game
    private static float LOAD_IN_DELAY = 1.5f; //how long it takes after all players have their scenes loaded to actually unlock them

    public GameObject spawn_points;
    public GameObject audio_manager;
    public GameObject scenario_transitioner;

    private GameObject local_player;
    private LoadHandler load_handler;
    private int num_starting_players = 0; //how many players are at the start of the game
    private string[] player_prefab_names = new string[4] { "", "", "", "" };
    private string[] player_steam_names = new string[4] { "", "", "", "" };
    private ulong[] player_steam_ids = new ulong[4];
    private GameObject[] player_prefabs = new GameObject[4] { null, null, null, null };

    private bool game_initialized = false;
    private int players_ready = 0;

    //---------------------------------------------------------------------------------------//
    //----------------------------------INITIAL LOAD-IN--------------------------------------//
    //---------------------------------------------------------------------------------------//

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            num_starting_players = NetworkManager.ConnectedClients.Count;
        }
    }

    //called by LoadHandler after BridgeEnvironment is loaded in
    public void addPlayer(GameObject this_player, LoadHandler lh)
    {
        local_player = this_player;
        load_handler = lh;

        individualBridgeEnvironmentLoadedRPC(SteamClient.Name, SteamClient.SteamId, local_player.GetComponent<NetworkObject>().OwnerClientId);

        if (NetworkManager.Singleton.IsHost == true)
        {
            StartCoroutine(waitForOthers());
        }
    }

    //only run by the host
    IEnumerator waitForOthers()
    {
        int minimum_players = MINIMUM_PLAYERS;
        if (minimum_players < 0)
        {
            minimum_players = NetworkManager.Singleton.ConnectedClients.Count;
        }

        //wait until MINIMUM_PLAYERS have loaded in
        while (players_ready < minimum_players)
        {
            yield return null;
        }

        //parent all players to spaceship
        foreach (GameObject plr in GameObject.FindGameObjectsWithTag("Player"))
        {
            NetworkObject plr_no = plr.GetComponent<NetworkObject>();
            if (plr_no != null)
            {
                plr_no.TrySetParent(GameObject.FindGameObjectWithTag("Spaceship").transform, true);
            }
        }

        //ensure all players are on the same page
        //------------------------------------//
        collectiveBridgeEnvironmentLoadedRPC(
                      player_steam_names[0], 
                      player_steam_ids[0],
                      player_steam_names[1],
                      player_steam_ids[1],
                      player_steam_names[2],
                      player_steam_ids[2],
                      player_steam_names[3],
                      player_steam_ids[3]
                      );
    }

    //called when ONE player is done loading into BridgeEnvironment for the first time
    [Rpc(SendTo.Everyone)]
    private void individualBridgeEnvironmentLoadedRPC(string plr_steam_name, ulong plr_steam_id, ulong plr_game_id)
    {
        //record Steam name (ex. EPICJAKEISCOOL)
        player_steam_names[plr_game_id] = plr_steam_name;
        //record Steam user ID (ex. 13590185091)
        player_steam_ids[plr_game_id] = plr_steam_id;
        //record player prefab name (ex. EPICJAKEISCOOL_13590185091)
        player_prefab_names[plr_game_id] = plr_steam_name + "_" + plr_steam_id;

        if (NetworkManager.Singleton.IsHost == true)
        {
            players_ready++;
        }
    }

    //called when EVERYONE in the lobby is done loading into BridgeEnvironment for the first time
    [Rpc(SendTo.Everyone)]
    private void collectiveBridgeEnvironmentLoadedRPC(string plr_a_steam_name, ulong plr_a_steam_id, string plr_b_steam_name, ulong plr_b_steam_id, string plr_c_steam_name, ulong plr_c_steam_id, string plr_d_steam_name, ulong plr_d_steam_id)
    {
        player_steam_names[0] = plr_a_steam_name;
        player_steam_ids[0] = plr_a_steam_id;
        player_steam_names[1] = plr_b_steam_name;
        player_steam_ids[1] = plr_b_steam_id;
        player_steam_names[2] = plr_c_steam_name;
        player_steam_ids[2] = plr_c_steam_id;
        player_steam_names[3] = plr_d_steam_name;
        player_steam_ids[3] = plr_d_steam_id;

        //prepare player prefab names
        for (int i = 0; i < 4; i++)
        {
            player_prefab_names[i] = player_steam_names[i] + "_" + player_steam_ids[i];
        }

        //position local player
        local_player.transform.localPosition = spawn_points.transform.GetChild(getPlayerIndex()).localPosition;

        foreach (GameObject plr in GameObject.FindGameObjectsWithTag("Player"))
        {
            NetworkObject plr_no = plr.GetComponent<NetworkObject>();
            if (plr_no != null)
            {
                int index = (int)plr_no.OwnerClientId;
                if (index < 4)
                {
                    player_prefabs[index] = plr;
                    player_prefabs[index].name = player_prefab_names[index];
                }
            }
        }
        players_ready = 0;
        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().loadNewScenario();
        }
    }

    //called only at the start of the game
    [Rpc(SendTo.Everyone)]
    private void unlockPlayersRPC()
    {
        for (int i = 0; i < 4; i++)
        {
            if (player_prefabs[i] != null)
            {
                unfreezePlayer(i);

            }
        }
        ControlScript.Instance.unlockPlayer(local_player);
        load_handler.endLoad();
        audio_manager.GetComponent<AudioManager>().InitializeAudio();
        if (NetworkManager.Singleton.IsHost == true)
        {
            startScenarioRPC();
        }
        handleShipRepositioning();
    }

    //---------------------------------------------------------------------------------------//
    //--------------------------------RESTART/QUIT HANDLING----------------------------------//
    //---------------------------------------------------------------------------------------//

    public static void clearDontDestroyOnLoads()
    {
        List<string> to_destroy = new List<string>() { "Origin", "EventSystem", "GameManagerScripts", "PlayerUICanvas" };
        foreach (string d in to_destroy)
        {
            GameObject.Destroy(GameObject.Find(d));
        }
    }

    //called by PauseMenuController and FailureHandler
    public static void leaveGame()
    {
        GameNetworkManager.Instance.currentLobby.Value.Leave();
        GameObject.Destroy(GameObject.Find("NetworkManager"));
        clearDontDestroyOnLoads();
        SceneManager.LoadScene("TitleScreen", LoadSceneMode.Single);
        SceneData.targetUI = "MainMenu";
        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().startLoad();
    }

    private void freezePlayer(GameObject plr)
    {
        plr.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        plr.transform.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        plr.transform.GetComponent<CapsuleCollider>().excludeLayers = LayerMask.NameToLayer("Everything");
        plr.transform.GetComponent<Rigidbody>().excludeLayers = LayerMask.NameToLayer("Everything");
        plr.transform.GetComponent<Rigidbody>().useGravity = false;
    }

    private void unfreezePlayer(GameObject plr)
    {
        plr.GetComponent<CapsuleCollider>().excludeLayers = LayerMask.GetMask("None");
        plr.GetComponent<Rigidbody>().excludeLayers = LayerMask.GetMask("None");
        plr.GetComponent<Rigidbody>().useGravity = true;
    }

    //called by FailureHandler
    public void freezeAllPlayers()
    {
        foreach (GameObject plr in GameObject.FindGameObjectsWithTag("Player"))
        {
            freezePlayer(plr);
        }
    }

    public void freezePlayer(int index)
    {
        if (index > player_prefabs.Length || player_prefabs[index] == null)
        {
            return;
        }
        freezePlayer(player_prefabs[index]);
    }

    public void unfreezePlayer(int index)
    {
        if (index > player_prefabs.Length || player_prefabs[index] == null)
        {
            return;
        }
        unfreezePlayer(player_prefabs[index]);
    }

    //---------------------------------------------------------------------------------------//
    //----------------------------------USEFUL INFORMATION-----------------------------------//
    //---------------------------------------------------------------------------------------//

    //returns how many players there were at the start of the game (ideally should always be 4)
    public int getNumStartingPlayers()
    {
        return num_starting_players;
    }

    //returns the 0-3 index of the player with respect to the lobby (0 = host)
    public int getPlayerIndex()
    {
        if (local_player == null)
        {
            return -1;
        }
        if (local_player.GetComponent<NetworkObject>() != null)
        {
            return (int)local_player.GetComponent<NetworkObject>().OwnerClientId;
        }
        for (int i = 0; i < player_steam_names.Length; i++)
        {
            if (player_steam_names[i] == SteamClient.Name)
            {
                return i;
            }
        }
        return -1;
    }

    //returns the player prefab of the local client
    public GameObject getLocalPlayer()
    {
        return local_player;
    }

    //returns a string table of the player Steam usernames corresponding to their order of when they joined (0 = host)
    public string[] getPlayerNames()
    {
        return player_steam_names;
    }

    //---------------------------------------------------------------------------------------//
    //------------------------------------SCENARIO LOADING-----------------------------------//
    //---------------------------------------------------------------------------------------//

    //called by ScenarioManager
    public void resetPlayersReady()
    {
        players_ready = 0;
    }

    //called by LoadHandler
    public void signifyScenarioLoaded()
    {
        scenarioLoadedRPC();
    }

    //when paths are generated, ship is relocated into entrance path, thus requiring an update to ship screens
    public void handleShipRepositioning()
    {
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<PilotNavigation>().updateAltimeterScreen();
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<PilotNavigation>().updateCourseHeadingScreen();
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerMap>().updateAltitude();
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerMap>().updateShipLocation();
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerMap>().updateShipOrientation();
    }

    [Rpc(SendTo.Everyone)]
    private void startScenarioRPC()
    {
        //if host, begin the scenario (timer)
        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().startScenario();
        }

        //end transition (whether looking at the cinematic shot or load screen)
        scenario_transitioner.GetComponent<TransitionHandler>().EndTransition();

        //end load (whether looking at the cinematic shot or load screen)
        load_handler.endLoad();

        //reactivate control/seat checking
        ControlScript.Instance.reactivate();

        //reactivate camera
        local_player.transform.GetComponent<CameraMove>().reactivateCamera();
        
        //update screens to account for ship's new location/rotation in newly-generated entrance path
        handleShipRepositioning();

        //unmute audio that was muted during scenario transition
        GameObject.Find("AudioManager").GetComponent<AudioManager>().UnmuteAudio();
    }

    //fired when a client's AsyncOperation for loading a scene is complete
    [Rpc(SendTo.Everyone)]
    private void scenarioLoadedRPC()
    {
        players_ready++;

        //if host, check if all players are ready
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (players_ready >= num_starting_players)
            {
                resetPlayersReady();
                GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().prepScenario(game_initialized);
                if (game_initialized == false)
                {
                    //wait LOAD_IN_DELAY
                    StartCoroutine(unlockPlayersDelay(true));
                }
                else
                {
                    //wait LOAD_IN_DELAY
                    StartCoroutine(unlockPlayersDelay(false));
                }
            }
        }
    }

    //only run by host
    IEnumerator unlockPlayersDelay(bool initial_load)
    {
        yield return new WaitForSeconds(LOAD_IN_DELAY);
        if (initial_load == true)
        {
            game_initialized = true;
            unlockPlayersRPC(); //only run once at the start of the game
        }
        else
        {
            GameObject transition_canvas = scenario_transitioner.GetComponent<TransitionHandler>().TransitionCanvas;
            while (transition_canvas.activeSelf == true) //ensure that host's transition is done before starting new scenario
            {
                yield return null;
            }
            startScenarioRPC();
        }
    }
}