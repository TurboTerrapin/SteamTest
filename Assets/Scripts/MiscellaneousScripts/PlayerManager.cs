/*
    PlayerManager.cs
    - Handles loading and managing of players
    Contributor(s): Jake Schott
    Last Updated: 8/25/2025
*/

using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static int MINIMUM_PLAYERS = -1; //if -1, will default to how many players are in the game
    private static float LOAD_IN_DELAY = 1.0f; //how long it takes after all players have their scenes loaded to actually unlock them

    public GameObject spawn_points;
    public GameObject audio_manager;

    private GameObject local_player;
    private LoadHandler load_handler;
    private string[] player_names = new string[4];
    private GameObject[] player_prefabs = new GameObject[4] { null, null, null, null };

    private int players_ready = 0;

    //called by LoadHandler after scene is loaded in
    public void addPlayer(GameObject this_player, LoadHandler lh)
    {
        local_player = this_player;
        load_handler = lh;

        doneLoadingRPC(local_player.name, local_player.GetComponent<NetworkObject>().OwnerClientId);

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

        //at this point, load the first scenario and stuff
        //------------------------------------//
        endLoadingRPC(player_names[0], player_names[1], player_names[2], player_names[3]);
    }

    [Rpc(SendTo.Everyone)]
    private void doneLoadingRPC(string plr_name, ulong plr_id)
    {
        player_names[plr_id] = plr_name;
        if (NetworkManager.Singleton.IsHost == true)
        {
            players_ready++;
        }
    }

    //called by waitForOthers after minimum players have loaded in
    [Rpc(SendTo.Everyone)]
    private void endLoadingRPC(string plr_a, string plr_b, string plr_c, string plr_d)
    {
        player_names[0] = plr_a;
        player_names[1] = plr_b;
        player_names[2] = plr_c;
        player_names[3] = plr_d;
        foreach (GameObject plr in GameObject.FindGameObjectsWithTag("Player"))
        {
            NetworkObject plr_no = plr.GetComponent<NetworkObject>();
            if (plr_no != null)
            {
                int index = (int)plr_no.OwnerClientId;
                if (index < 4)
                {
                    player_prefabs[index] = plr;
                    player_prefabs[index].name = player_names[index];
                    player_prefabs[index].transform.position = spawn_points.transform.GetChild(index).position;
                }
            }
        }
        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().generatePaths();
            int players_loaded = 0;
            for (int i = 0; i < 4; i++)
            {
                if (player_prefabs[i] != null)
                {
                    players_loaded++;
                }
            }
            if (players_loaded > 1)
            {
                //only yield if there is more than one player in the game
                StartCoroutine(unlockPlayersDelay());
            }
            else
            {
                unlockPlayersRPC();
            }
        }
    }

    IEnumerator unlockPlayersDelay()
    {
        yield return new WaitForSeconds(LOAD_IN_DELAY);
        unlockPlayersRPC();
    }

    [Rpc(SendTo.Everyone)]
    private void unlockPlayersRPC()
    {
        for (int i = 0; i < 4; i++)
        {
            if (player_prefabs[i] != null)
            {
                player_prefabs[i].GetComponent<CapsuleCollider>().excludeLayers = LayerMask.GetMask("None");
                player_prefabs[i].GetComponent<Rigidbody>().excludeLayers = LayerMask.GetMask("None");
                player_prefabs[i].GetComponent<Rigidbody>().useGravity = true;
            }
        }
        ControlScript.Instance.unlockPlayer(local_player);
        load_handler.endLoad();
        audio_manager.GetComponent<AudioManager>().initializeAudio();
        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().initializeScenarioManager();
        }
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<PilotNavigation>().updateAltimeterScreen();
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<PilotNavigation>().updateCourseHeadingScreen();
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerMap>().updateAltitude();
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerMap>().updateShipLocation();
        GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerMap>().updateShipOrientation();
    }
}