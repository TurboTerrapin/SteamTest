/*
    LobbyHandler.cs
    - Handles RPCs that pertain to lobby functions, ex. load initiation, difficulty handling
    - Keeps track of who is actually in and connected in the lobby
    Contributor(s): Jake Schott
    Last Updated: 4/29/2026
*/

using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using UnityEngine.SceneManagement;
using System.Collections;

public class LobbyHandler : NetworkBehaviour
{
    //CLASS CONSTANTS
    public const int DEFAULT_DIFFICULTY = 0; //easy
    private static float HEARTBEAT_LENGTH = 5.0f; //time in seconds that a client can go without getting a ping from host before it disconnects

    private int difficulty = -1;
    private List<ulong> player_steam_ids = new List<ulong>() { 0, 0, 0, 0 };
    private Dictionary<ulong, ulong> player_client_ids = new Dictionary<ulong, ulong>(); //key client ID, value steam ID
    private List<string> player_names = new List<string>() { "", "", "", "" };
    private bool[] player_connecteds = new bool[] { false, false, false, false };
    private Coroutine heartbeat_coroutine = null;

    private void Awake()
    {
        gameObject.name = "LobbyHandler";
        if (NetworkManager.Singleton.IsHost == true)
        {
            player_steam_ids[0] = SteamClient.SteamId;
            player_client_ids.Add(0, SteamClient.SteamId);
            player_names[0] = SteamClient.Name;
            player_connecteds[0] = false;
            SteamMatchmaking.OnLobbyMemberJoined += onLobbyChange;
            SteamMatchmaking.OnLobbyMemberLeave += onLobbyChange;
            SteamMatchmaking.OnLobbyCreated += onLobbyCreated;
            heartbeat_coroutine = StartCoroutine(heartbeatSender()); //send out heartbeat pings
        }
        else
        {
            player_steam_ids[0] = GameNetworkManager.Instance.currentLobby.Value.Owner.Id;
            player_names[0] = GameNetworkManager.Instance.currentLobby.Value.Owner.Name;
            player_connecteds[0] = true;
            heartbeat_coroutine = StartCoroutine(heartbeatChecker()); //track reception of heartbeat pings
        }
        NetworkManager.Singleton.OnClientConnectedCallback += onClientConnect;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            SteamMatchmaking.OnLobbyMemberJoined -= onLobbyChange;
            SteamMatchmaking.OnLobbyMemberLeave -= onLobbyChange;
            SteamMatchmaking.OnLobbyCreated -= onLobbyCreated;
        }
        NetworkManager.Singleton.OnClientConnectedCallback -= onClientConnect;
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    //called by CampaignLobbyController.cs when checking difficulty boxes
    public void updateDifficulty(int new_difficulty)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        updateDifficultyRPC(new_difficulty);
    }

    //returns game difficulty (0-3, easy, medium, hard, or expert)
    public int getDifficulty()
    {
        return difficulty;
    }

    //returns list of steam IDs in order of joining
    public List<ulong> getPlayerSteamIDsInLobby()
    {
        return player_steam_ids;
    }

    //returns list of players in lobby in order of joining
    public List<string> getPlayerNamesInLobby()
    {
        return player_names;
    }

    //returns list of currently connected in lobby in order of joining
    public bool[] getPlayersConnectedInLobby()
    {
        return player_connecteds;
    }

    //returns 0-3 index of player by name
    public int getPlayerIndex(ulong steam_id)
    {
        for (int i = 0; i < 4; i++)
        {
            if (steam_id == player_steam_ids[i])
            {
                return i;
            }
        }
        return 0;
    }

    //returns steam ID corresponding to client ID
    public ulong getPlayerSteamID(ulong client_id)
    {
        if (player_client_ids.ContainsKey(client_id) == false)
        {
            return 0;
        }
        return player_client_ids[client_id];
    }

    //called by host when restarting a game or when engage is clicked
    public void startLoadForAllPlayers()
    {
        //send out client IDs as a one-time RPC if in lobby
        if (SceneManager.GetActiveScene().name == "TitleScreen")
        {
            ulong[] client_ids = new ulong[3] { 0, 0, 0 };
            foreach (KeyValuePair<ulong, ulong> id_match in player_client_ids)
            {
                if (id_match.Key != 0 && player_steam_ids.Contains(id_match.Value) == true)
                {
                    client_ids[player_steam_ids.IndexOf(id_match.Value) - 1] = id_match.Key;
                }
            }
            lobbyFinalizedRPC(client_ids[0], client_ids[1], client_ids[2]);
        }

        allPlayersLoadRPC(); //triggers below RPC
    }

    //resizes the list of player names, eliminating gaps (only called by host)
    private void rebuildLobbyList()
    {
        List<ulong> copied_steam_ids = new List<ulong>();
        List<bool> copied_connections = new List<bool>();
        for (int i = 0; i < 4; i++)
        {
            if (player_steam_ids[i] != 0)
            {
                copied_steam_ids.Add(player_steam_ids[i]);
                copied_connections.Add(player_connecteds[i]);
            }
        }
        for (int i = 0; i < 4; i++)
        {
            if (i < copied_steam_ids.Count)
            {
                player_steam_ids[i] = copied_steam_ids[i];
                player_connecteds[i] = copied_connections[i];
            }
            else
            {
                player_steam_ids[i] = 0;
                player_connecteds[i] = false;
            }
        }
    }

    //called when a client is connected to the NetworkManager lobby
    private void onClientConnect(ulong id)
    {
        if (id == NetworkManager.Singleton.LocalClientId)
        {
            lobbyConnectionUpdateRPC(SteamClient.SteamId, NetworkManager.Singleton.LocalClientId);
        }
    }

    //called when the host's created Steam lobby comes back with a result
    private void onLobbyCreated(Result r, Lobby l)
    {
        if (r == Result.OK)
        {
            player_connecteds[0] = true;
            rebuildLobbyList();
            lobbyUpdateRPC(player_steam_ids[1], player_steam_ids[2], player_steam_ids[3], player_connecteds[1], player_connecteds[2], player_connecteds[3]);
        }
    }

    //called on lobby member join or leaving (only run by host)
    private void onLobbyChange(Lobby l, Friend f)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        if (player_steam_ids.Contains(f.Id) == true) //remove player from list
        {
            //remove from client dictionary
            foreach (KeyValuePair<ulong, ulong> id_match in player_client_ids)
            {
                if (id_match.Value == f.Id)
                {
                    player_client_ids.Remove(id_match.Key);
                    break;
                }
            }

            //remove from steam list
            player_steam_ids[player_steam_ids.IndexOf(f.Id)] = 0;
        }
        else //add player to list
        {
            for (int i = 1; i < 4; i++)
            {
                if (player_steam_ids[i] == 0)
                {
                    player_steam_ids[i] = f.Id;
                    player_connecteds[i] = false;
                    break;
                }
            }
        }
        rebuildLobbyList();
        lobbyUpdateRPC(player_steam_ids[1], player_steam_ids[2], player_steam_ids[3], player_connecteds[1], player_connecteds[2], player_connecteds[3]);
    }

    //called when loading into the start of a game (there is a waiting period when the host loads into BridgeEnvironment compared to clients)
    [Rpc(SendTo.Everyone)]
    private void allPlayersLoadRPC()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            GameObject.Find("LoadHandler").GetComponent<LoadHandler>().startLoad();
        }
    }

    //called by a client when they are connected to the lobby which gets sent to the host and relayed back to the other clients
    [Rpc(SendTo.Everyone)]
    private void lobbyConnectionUpdateRPC(ulong steam_id, ulong client_id)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        //add to or update dictionary
        if (player_client_ids.ContainsKey(client_id) == false)
        {
            player_client_ids.Add(client_id, steam_id);
        }
        else
        {
            player_client_ids[client_id] = steam_id;
        }

        //find index of connected player
        for (int i = 0; i < 4; i++)
        {
            if (player_steam_ids[i] == steam_id)
            {
                player_connecteds[i] = true;
                rebuildLobbyList();
                lobbyUpdateRPC(player_steam_ids[1], player_steam_ids[2], player_steam_ids[3], player_connecteds[1], player_connecteds[2], player_connecteds[3]);
                return;
            }
        }
    }

    //called by host when a non-host player joins/leaves or connects/disconnects from the lobby
    [Rpc(SendTo.Everyone)]
    private void lobbyUpdateRPC(ulong p2, ulong p3, ulong p4, bool c2, bool c3, bool c4)
    {
        //player_steam_ids[0] = GameNetworkManager.Instance.currentLobby.value.Owner.id;
        //player_connecteds[0] = true;
        player_steam_ids[1] = p2;
        player_connecteds[1] = c2;
        player_steam_ids[2] = p3;
        player_connecteds[2] = c3;
        player_steam_ids[3] = p4;
        player_connecteds[3] = c4;

        //get/assign names of current lobby members (excluding host which should be set in stone)
        for (int i = 1; i < 4; i++)
        {
            if (player_steam_ids[i] != 0)
            {
                player_names[i] = new Friend(player_steam_ids[i]).Name;
            }
            else
            {
                player_names[i] = "";
            }
        }

        //trigger visual lobby update if in TitleScreen
        GameObject campaign_lobby = GameObject.Find("CampaignLobby");
        if (campaign_lobby != null)
        {
            campaign_lobby.GetComponent<CampaignLobbyController>().OnLobbyChange();
        }

        //if host, check for seat occupants
        if (NetworkManager.Singleton.IsHost == true && SceneManager.GetActiveScene().name != "TitleScreen")
        {
            GameObject.FindGameObjectWithTag("SeatHandler").GetComponent<SeatManager>().checkForMissingSeats();
        }
    }

    //called by host on lobby finalized to assign steam IDs to client IDs
    [Rpc(SendTo.Everyone)]
    public void lobbyFinalizedRPC(ulong client_id1, ulong client_id2, ulong client_id3)
    {
        //skip if host, already cached client IDs
        if (NetworkManager.Singleton.IsHost == true)
        {
            return;
        }

        //if client, cache client IDs based on Steam IDs
        ulong[] client_ids = new ulong[4] { 0, client_id1, client_id2, client_id3 };
        for (int i = 0; i < 4; i++)
        {
            if (player_steam_ids[i] != 0)
            {
                player_client_ids.Add(client_ids[i], player_steam_ids[i]);
            }
        }
    }

    //called by host when change in difficulty or need to push current difficulty to newly-joined player
    [Rpc(SendTo.Everyone)]
    private void updateDifficultyRPC(int new_difficulty)
    {
        difficulty = new_difficulty;
        Debug.Log("Difficulty changed to " +  difficulty);

        GameObject campaign_lobby = GameObject.Find("CampaignLobby");
        GameObject failure_handler = GameObject.Find("FailureHandler");
     
        if (campaign_lobby != null)
        {
            campaign_lobby.GetComponent<CampaignLobbyController>().DisplayDifficultyChange(new_difficulty);
        }

        if (failure_handler != null)
        {
            failure_handler.GetComponent<FailureHandler>().DisplayDifficulty(new_difficulty);
        }      
    }

    IEnumerator heartbeatChecker()
    {
        yield return new WaitForSeconds(HEARTBEAT_LENGTH);
        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().displayLostConnection("The host has disconnected.");
        GameNetworkManager.Instance.Disconnect();
        heartbeat_coroutine = null;
    }

    IEnumerator heartbeatSender()
    {
        float half_heartbeat = HEARTBEAT_LENGTH * 0.5f;
        while (true)
        {
            yield return new WaitForSeconds(half_heartbeat);
            heartbeatRPC();
        }
    }

    //called by host every HEARTBEAT_TIME to let the players know that a connection is still active
    [Rpc(SendTo.Everyone)]
    private void heartbeatRPC()
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            return;
        }

        if (heartbeat_coroutine != null)
        {
            StopCoroutine(heartbeat_coroutine);
        }
        heartbeat_coroutine = StartCoroutine(heartbeatChecker());
    }
}