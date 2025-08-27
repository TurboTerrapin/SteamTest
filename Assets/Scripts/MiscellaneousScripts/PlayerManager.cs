/*
    PlayerManager.cs
    - Handles loading and managing of players
    - Handles when a player quits to take them back to the TitleScreen
    Contributor(s): Jake Schott
    Last Updated: 8/25/2025
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
    private static float LOAD_IN_DELAY = 1.0f; //how long it takes after all players have their scenes loaded to actually unlock them

    public GameObject spawn_points;
    public GameObject audio_manager;

    private GameObject local_player;
    private LoadHandler load_handler;
    private int num_starting_players = 0; //how many players are at the start of the game (should always be 4 but for testing purposes may be less than that number)
    private string[] player_prefab_names = new string[4] { "", "", "", "" };
    private string[] player_steam_names = new string[4] { "", "", "", "" };
    private ulong[] player_steam_ids = new ulong[4];
    private GameObject[] player_prefabs = new GameObject[4] { null, null, null, null };

    private bool game_initialized = false;
    private int players_ready = 0;
    private Coroutine scenario_load_coroutine = null;

    //called by LoadHandler after scene is loaded in
    public void addPlayer(GameObject this_player, LoadHandler lh)
    {
        local_player = this_player;
        load_handler = lh;

        doneLoadingRPC(SteamClient.Name, SteamClient.SteamId, local_player.GetComponent<NetworkObject>().OwnerClientId);

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

        //ensure all players are on the same page
        //------------------------------------//
        endLoadingRPC(player_steam_names[0], 
                      player_steam_ids[0],
                      player_steam_names[1],
                      player_steam_ids[1],
                      player_steam_names[2],
                      player_steam_ids[2],
                      player_steam_names[3],
                      player_steam_ids[3]
                      );
    }

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

    //called by FailureHandler
    public void freezeAllPlayers()
    {
        for (int i = 0; i < 4; i++)
        {
            if (player_prefabs[i] != null)
            {
                player_prefabs[i].transform.GetComponent<CapsuleCollider>().excludeLayers = LayerMask.GetMask("Everything");
                player_prefabs[i].transform.GetComponent<Rigidbody>().excludeLayers = LayerMask.GetMask("Everything");
                player_prefabs[i].transform.GetComponent<Rigidbody>().useGravity = false;
            }
        }
    }

    //returns how many players there were at the start of the game (ideally should always be 4)
    public int getNumStartingPlayers()
    {
        return num_starting_players;
    }

    //returns the 0-3 index of the player with respect to the lobby (0 = host)
    public int getPlayerIndex()
    {
        return (int)local_player.GetComponent<NetworkObject>().OwnerClientId;
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

    public void handleShipRepositioning()
    {
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<PilotNavigation>().updateAltimeterScreen();
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<PilotNavigation>().updateCourseHeadingScreen();
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerMap>().updateAltitude();
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerMap>().updateShipLocation();
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerMap>().updateShipOrientation();
    }

    [Rpc(SendTo.Everyone)]
    private void doneLoadingRPC(string plr_steam_name, ulong plr_steam_id, ulong plr_game_id)
    {
        //record Steam name (ex. EPICJAKEISCOOL)
        player_steam_names[plr_game_id] = plr_steam_name;
        //record Steam user ID (ex. 13590185091)
        player_steam_ids[plr_game_id] = plr_steam_id;
        //record player prefab name (ex. EPICJAKEISCOOL_13590185091)
        player_prefab_names[plr_game_id] = plr_steam_name + "_" + plr_steam_id;

        //set parent
        GameObject.Find(player_prefab_names[plr_game_id]).transform.parent = GameObject.Find("Spaceship").transform;

        if (NetworkManager.Singleton.IsHost == true)
        {
            players_ready++;
        }
    }

    //called by waitForOthers after minimum players have loaded in
    [Rpc(SendTo.Everyone)]
    private void endLoadingRPC(string plr_a_steam_name, ulong plr_a_steam_id, string plr_b_steam_name, ulong plr_b_steam_id, string plr_c_steam_name, ulong plr_c_steam_id, string plr_d_steam_name, ulong plr_d_steam_id)
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

        foreach (GameObject plr in GameObject.FindGameObjectsWithTag("Player"))
        {
            NetworkObject plr_no = plr.GetComponent<NetworkObject>();
            if (plr_no != null)
            {
                int index = (int)plr_no.OwnerClientId;
                if (index < 4)
                {
                    num_starting_players++;
                    player_prefabs[index] = plr;
                    player_prefabs[index].name = player_prefab_names[index];
                    player_prefabs[index].transform.position = spawn_points.transform.GetChild(index).position;
                }
            }
        }
        if (NetworkManager.Singleton.IsHost == true)
        {
            string scenario_to_load = GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().loadNewScenario();
            int players_loaded = 0;
            for (int i = 0; i < 4; i++)
            {
                if (player_prefabs[i] != null)
                {
                    players_loaded++;
                }
            }

            loadScenarioRPC(scenario_to_load);
        }
    }

    IEnumerator yieldForScenarioLoad(string scenario_to_load)
    {
        while (SceneManager.GetActiveScene().name != scenario_to_load)
        {
            yield return null;
        }
        scenarioLoadedRPC();
    }

    //only run by host
    IEnumerator unlockPlayersDelay(bool initial_load)
    {
        yield return new WaitForSeconds(LOAD_IN_DELAY);
        if (initial_load == true)
        {
            game_initialized = true;
            unlockPlayersRPC();
        }
        else
        {
            GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().startScenario();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void loadScenarioRPC(string scenario_to_load)
    {
        if (scenario_load_coroutine != null)
        {
            StopCoroutine(scenario_load_coroutine);
        }
        scenario_load_coroutine = StartCoroutine(yieldForScenarioLoad(scenario_to_load));
    }

    [Rpc(SendTo.Everyone)]
    private void scenarioLoadedRPC()
    {
        players_ready++;
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (players_ready >= num_starting_players)
            {
                GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().prepScenario();
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

    [Rpc(SendTo.Everyone)]
    private void unlockPlayersRPC()
    {
        for (int i = 0; i < 4; i++)
        {
            if (player_prefabs[i] != null)
            {
                player_prefabs[i].transform.parent = GameObject.Find("Spaceship").transform;
                player_prefabs[i].GetComponent<CapsuleCollider>().excludeLayers = LayerMask.GetMask("None");
                player_prefabs[i].GetComponent<Rigidbody>().excludeLayers = LayerMask.GetMask("None");
                player_prefabs[i].GetComponent<Rigidbody>().useGravity = true;
            }
        }
        ControlScript.Instance.unlockPlayer(local_player);
        load_handler.endLoad();
        audio_manager.GetComponent<AudioManager>().InitializeAudio();
        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().startScenario();
        }
        handleShipRepositioning();
    }
}