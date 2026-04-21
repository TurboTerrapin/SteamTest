/*
    PlayerManager.cs
    - Handles loading and managing of players
    - Handles when a player quits to take them back to the TitleScreen
    Contributor(s): Jake Schott
    Last Updated: 4/20/2026
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
    public GameObject players_holder;
    public GameObject audio_manager;
    public GameObject scenario_transitioner;

    private GameObject local_player;
    private LoadHandler load_handler;
    private LobbyHandler lobby_handler;

    private Dictionary<ulong, GameObject> player_prefabs = new Dictionary<ulong, GameObject>(); //key steam ID, value player prefab
    private Dictionary<ulong, bool> players_ready = new Dictionary<ulong, bool>(); //key steam ID, value ready or not
    private bool game_initialized = false;

    //---------------------------------------------------------------------------------------//
    //----------------------------------INITIAL LOAD-IN--------------------------------------//
    //---------------------------------------------------------------------------------------//

    private void Awake()
    {
        lobby_handler = GameObject.Find("LobbyHandler").GetComponent<LobbyHandler>();
        List<ulong> player_steam_ids = lobby_handler.getPlayerSteamIDsInLobby();
        for (int i = 0; i < player_steam_ids.Count; i++)
        {
            if (player_steam_ids[i] != 0) //ignore empty slots
            {
                players_ready.Add(player_steam_ids[i], false);
            }
        }
    }

    //called by LoadHandler after BridgeEnvironment is loaded in
    public void addPlayer(GameObject client_player, LoadHandler lh)
    {
        local_player = client_player;
        local_player.GetComponent<NetworkObject>().TrySetParent(players_holder.transform);
        load_handler = lh;

        individualBridgeEnvironmentLoadedRPC(SteamClient.SteamId);

        if (NetworkManager.Singleton.IsHost == true)
        {
            StartCoroutine(waitForOthers());
        }
    }

    //returns how many players have loaded in current scene
    private int getNumReadyPlayers()
    {
        int num_ready_players = 0;
        List<ulong> player_steam_ids = lobby_handler.getPlayerSteamIDsInLobby();
        for (int i = 0; i < player_steam_ids.Count; i++)
        {
            if (player_steam_ids[i] != 0 && players_ready[player_steam_ids[i]] == true)
            {
                num_ready_players++;
            }
        }
        return num_ready_players;
    }

    //reset ready players
    public void resetReadyPlayers()
    {
        List<ulong> player_steam_ids = lobby_handler.getPlayerSteamIDsInLobby();
        for (int i = 0; i < player_steam_ids.Count; i++)
        {
            if (players_ready.ContainsKey(player_steam_ids[i]) == true)
            {
                players_ready[player_steam_ids[i]] = false;
            }
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
        while (getNumReadyPlayers() < minimum_players)
        {
            yield return null;
        }

        //parent all players to spaceship
        foreach (GameObject plr in GameObject.FindGameObjectsWithTag("Player"))
        {
            NetworkObject plr_no = plr.GetComponent<NetworkObject>();
            if (plr_no != null)
            {
                plr_no.TrySetParent(players_holder.transform, true);
            }
        }

        //ensure all players are on the same page
        collectiveBridgeEnvironmentLoadedRPC();
    }

    //called when ONE player is done loading into BridgeEnvironment for the first time
    [Rpc(SendTo.Everyone)]
    private void individualBridgeEnvironmentLoadedRPC(ulong steam_id)
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            players_ready[steam_id] = true;
        }
    }

    //called when EVERYONE in the lobby is done loading into BridgeEnvironment for the first time
    [Rpc(SendTo.Everyone)]
    private void collectiveBridgeEnvironmentLoadedRPC()
    {
        //store every player
        foreach (GameObject plr in GameObject.FindGameObjectsWithTag("Player"))
        {
            ulong player_steam_id = lobby_handler.getPlayerSteamID(plr.GetComponent<NetworkObject>().OwnerClientId);
            int player_index = lobby_handler.getPlayerIndex(player_steam_id);
            plr.name = lobby_handler.getPlayerNamesInLobby()[player_index] + "_" + lobby_handler.getPlayerSteamIDsInLobby()[player_index];
            player_prefabs.Add(player_steam_id, plr);
        }

        //position local player
        local_player.transform.localPosition = spawn_points.transform.GetChild(lobby_handler.getPlayerIndex(lobby_handler.getPlayerSteamID(NetworkManager.Singleton.LocalClientId))).localPosition;

        //reset ready player counter to 0 to prepare for scenario load instead of BridgeEnvironment load
        resetReadyPlayers();
        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().intializeScenarioDatabase();
            GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().loadNewScenario();
        }
    }

    //called only at the start of the game
    [Rpc(SendTo.Everyone)]
    private void unlockPlayersRPC()
    {
        foreach (GameObject plr in player_prefabs.Values)
        {
            if (plr != null)
            {
                unfreezePlayer(plr);
            }
        }
        PrimaryScript.Instance.unlockPlayer(local_player);
        load_handler.endLoad(true);
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
        List<string> to_destroy = new List<string>() { "Origin", "EventSystem", "GameManagerScripts", "PlayerUICanvas", "LobbyHandler" };
        foreach (string d in to_destroy)
        {
            GameObject attempt_to_destroy = GameObject.Find(d);
            if (attempt_to_destroy != null)
            {
                GameObject.Destroy(attempt_to_destroy);
            }
        }
    }

    //called by PauseMenuController and FailureHandler
    public static void leaveGame()
    {
        GameObject.Destroy(NetworkManager.Singleton.gameObject);
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

    //called by FailureHandler.cs
    public void freezeAllPlayers()
    {
        foreach (GameObject plr in GameObject.FindGameObjectsWithTag("Player"))
        {
            freezePlayer(plr);
        }
    }

    public void freezePlayer(ulong steam_id)
    {
        if (player_prefabs.ContainsKey(steam_id) == false)
        {
            return;
        }
        freezePlayer(player_prefabs[steam_id]);
    }

    public void unfreezePlayer(ulong steam_id)
    {
        if (player_prefabs.ContainsKey(steam_id) == false)
        {
            return;
        }
        unfreezePlayer(player_prefabs[steam_id]);
    }

    //---------------------------------------------------------------------------------------//
    //----------------------------------USEFUL INFORMATION-----------------------------------//
    //---------------------------------------------------------------------------------------//

    //returns the player prefab of the local client
    public GameObject getLocalPlayer()
    {
        return local_player;
    }

    //---------------------------------------------------------------------------------------//
    //------------------------------------SCENARIO LOADING-----------------------------------//
    //---------------------------------------------------------------------------------------//

    //called by LoadHandler.cs
    public void signifyScenarioLoaded()
    {
        scenarioLoadedRPC(SteamClient.SteamId);
    }

    //when paths are generated, ship is relocated into entrance path, thus requiring an update to ship screens
    public void handleShipRepositioning()
    {
        float ship_rotation = GameObject.FindGameObjectWithTag("Spaceship").transform.rotation.eulerAngles.y;
        string current_heading = FlyingInstruments.getRoundedDegreeReading(ship_rotation + 90.0f);
        string target_heading = GameObject.FindGameObjectWithTag("Spaceship").GetComponent<PilotingSystem>().GetTargetHeading();

        ReferenceAssistor.Instance.module_handlers[0].GetComponent<FlyingInstruments>().updateAltimeterScreen();
        ReferenceAssistor.Instance.module_handlers[0].GetComponent<FlyingInstruments>().updateCourseHeadingScreen(ship_rotation, current_heading);
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<ScenarioMap>().updateAltitude();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<ScenarioMap>().updateShipLocation();
        ReferenceAssistor.Instance.module_handlers[2].GetComponent<ScenarioMap>().updateShipOrientation(ship_rotation, current_heading, target_heading);
    }

    [Rpc(SendTo.Everyone)]
    private void startScenarioRPC()
    {
        //if host, begin the scenario
        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().startScenario();
        }

        //end transition (whether looking at the cinematic shot or load screen)
        scenario_transitioner.GetComponent<TransitionHandler>().EndTransition();

        //end load (whether looking at the cinematic shot or load screen)
        load_handler.endLoad(false);

        //reactivate control/seat checking
        PrimaryScript.Instance.activate();

        //reactivate camera
        local_player.transform.GetComponent<CameraMove>().ReactivateCamera();
        
        //update screens to account for ship's new location/rotation in newly-generated entrance path
        handleShipRepositioning();

        //unmute audio that was muted during scenario transition
        GameObject.Find("AudioManager").GetComponent<AudioManager>().UnmuteAudio();
    }

    //fired when a client's AsyncOperation for loading a scenario (not BridgeEnvironment) is complete
    [Rpc(SendTo.Everyone)]
    private void scenarioLoadedRPC(ulong steam_id)
    {
        players_ready[steam_id] = true;

        //if host, check if all players are ready
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (getNumReadyPlayers() >= NetworkManager.Singleton.ConnectedClientsIds.Count)
            {
                resetReadyPlayers();
                GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().prepScenario(!game_initialized);
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