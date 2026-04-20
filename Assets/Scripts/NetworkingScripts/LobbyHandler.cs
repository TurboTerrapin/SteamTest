/*
    LobbyHandler.cs
    - Handles RPCs that pertain to lobby functions, ex. load initiation, difficulty handling
    - Keeps track of who is actually in and connected in the lobby
    - Handles 
    Contributor(s): Jake Schott
    Last Updated: 4/20/2026
*/

using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;

public class LobbyHandler : NetworkBehaviour
{
    //CLASS CONSTANTS
    public const int DEFAULT_DIFFICULTY = 0; //Easy

    private int difficulty = -1;
    private List<string> player_names = new List<string>() { "", "", "", "" };
    private bool[] player_connecteds = new bool[] { false, false, false, false };

    private void Awake()
    {
        gameObject.name = "LobbyHandler";
        if (NetworkManager.Singleton.IsHost == true)
        {
            player_names[0] = SteamClient.Name;
            player_connecteds[0] = false;
            SteamMatchmaking.OnLobbyMemberJoined += onLobbyChange;
            SteamMatchmaking.OnLobbyMemberLeave += onLobbyChange;
            SteamMatchmaking.OnLobbyCreated += onLobbyCreated;
        }
        else
        {
            player_names[0] = GameNetworkManager.Instance.currentLobby.Value.Owner.Name;
            player_connecteds[0] = true;
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

    //called by host when restarting a game or when engage is clicked
    public void startLoadForAllPlayers()
    {
        allPlayersLoadRPC(); //triggers below RPC
    }

    //resizes the list of player names, eliminating gaps (only called by host)
    private void rebuildLobbyList()
    {
        List<string> copied_names = new List<string>();
        List<bool> copied_connections = new List<bool>();
        for (int i = 0; i < 4; i++)
        {
            if (player_names[i] != "")
            {
                copied_names.Add(player_names[i]);
                copied_connections.Add(player_connecteds[i]);
            }
        }
        for (int i = 0; i < 4; i++)
        {
            if (i < copied_names.Count)
            {
                player_names[i] = copied_names[i];
                player_connecteds[i] = copied_connections[i];
            }
            else
            {
                player_names[i] = "";
                player_connecteds[i] = false;
            }
        }
    }

    //called when a client is connected to the NetworkManager lobby
    private void onClientConnect(ulong id)
    {
        if (id == NetworkManager.Singleton.LocalClientId)
        {
            lobbyConnectionUpdateRPC(SteamClient.Name);
        }
    }

    //called when the host's created Steam lobby comes back with a result
    private void onLobbyCreated(Result r, Lobby l)
    {
        if (r == Result.OK)
        {
            player_connecteds[0] = true;
            rebuildLobbyList();
            lobbyUpdateRPC(player_names[1], player_names[2], player_names[3], player_connecteds[1], player_connecteds[2], player_connecteds[3]);
        }
    }

    //called on lobby member join or leaving (only run by host)
    private void onLobbyChange(Lobby l, Friend f)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        if (player_names.Contains(f.Name) == true) //remove player from list
        {
            player_names[player_names.IndexOf(f.Name)] = "";
        }
        else //add player to list
        {
            for (int i = 0; i < 4; i++)
            {
                if (player_names[i] == "")
                {
                    player_names[i] = f.Name;
                    player_connecteds[i] = false;
                    break;
                }
            }
        }
        rebuildLobbyList();
        lobbyUpdateRPC(player_names[1], player_names[2], player_names[3], player_connecteds[1], player_connecteds[2], player_connecteds[3]);
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
    private void lobbyConnectionUpdateRPC(string player_name)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        //find index of connected player
        for (int i = 0; i < 4; i++)
        {
            if (player_names[i] == player_name)
            {
                player_connecteds[i] = true;
                rebuildLobbyList();
                lobbyUpdateRPC(player_names[1], player_names[2], player_names[3], player_connecteds[1], player_connecteds[2], player_connecteds[3]);
                return;
            }
        }
    }

    //called by host when a non-host player joins/leaves or connects/disconnects from the lobby
    [Rpc(SendTo.Everyone)]
    private void lobbyUpdateRPC(string p2, string p3, string p4, bool c2, bool c3, bool c4)
    {
        //player_names[0] = GameNetworkManager.Instance.currentLobby.value.Owner.name;
        player_names[1] = p2;
        player_names[2] = p3;
        player_names[3] = p4;
        //player_connecteds[0] = true;
        player_connecteds[1] = c2;
        player_connecteds[2] = c3;
        player_connecteds[3] = c4;
        GameObject campaign_lobby = GameObject.Find("CampaignLobby");
        if (campaign_lobby != null)
        {
            campaign_lobby.GetComponent<CampaignLobbyController>().OnLobbyChange();
        }
    }

    //called by host when change in difficulty or need to push current difficulty to newly-joined player
    [Rpc(SendTo.Everyone)]
    private void updateDifficultyRPC(int new_difficulty)
    {
        difficulty = new_difficulty;

        GameObject campaign_lobby = GameObject.Find("CampaignLobby");
        if (campaign_lobby != null)
        {
            campaign_lobby.GetComponent<CampaignLobbyController>().DisplayDifficultyChange(new_difficulty);
        }
    }
}
