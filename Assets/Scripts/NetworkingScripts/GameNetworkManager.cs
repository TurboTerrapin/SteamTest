/*
    GameNetworkManager.cs
    - Handles interfacing between Steam lobbies and NetworkManager lobbies
    - Handles connecting/disconnecting as host and client
    - Communicates with LoadHandler for connecting/disconnect screens
    Contributor(s): John Aylward, Jake Schott
    Last Updated: 4/20/2026
*/

using System.Collections;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; } = null;

    private FacepunchTransport transport = null;

    public Lobby? currentLobby { get; private set; } = null;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        //Debug.Log(SteamClient.RestartAppIfNecessary(480));
    }

    private void Start()
    {
        transport = GetComponent<FacepunchTransport>();

        SteamMatchmaking.OnLobbyCreated += SteamMatchmaking_OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += SteamMatchmaking_OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += SteamMatchmaking_OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyGameCreated += SteamMatchmaking_OnLobbyGameCreated;
        SteamUser.OnSteamServersDisconnected += SteamUser_OnSteamServersDisconnected;
        SteamFriends.OnGameLobbyJoinRequested += SteamFriends_OnGameLobbyJoinRequested;
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyCreated -= SteamMatchmaking_OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= SteamMatchmaking_OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= SteamMatchmaking_OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= SteamMatchmaking_OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyGameCreated -= SteamMatchmaking_OnLobbyGameCreated;
        SteamUser.OnSteamServersDisconnected -= SteamUser_OnSteamServersDisconnected;
        SteamFriends.OnGameLobbyJoinRequested -= SteamFriends_OnGameLobbyJoinRequested;

        Disconnect();
    }

    //Ensure you disconnect if you hard quit
    private void OnApplicationQuit()
    {
        Disconnect();
    }

    //Called when joining through Steam invite or clicking on lobby "JOIN" button in join screen
    private async void AttemptJoin(Lobby lobbyToJoin)
    {
        //Can only join games from TitleScreen scene
        if (SceneManager.GetActiveScene().name != "TitleScreen")
        {
            Debug.Log("Failed to join lobby (cannot join from an active session)");
            return;
        }

        //Can't leave as host of a session with at least one other player
        if (NetworkManager.Singleton.IsHost == true && (currentLobby != null && currentLobby.Value.MemberCount > 1))
        {
            Debug.Log("Failed to join lobby (cannot leave as host with a non-empty session)");
            return;
        }

        //If in a lobby, make sure you are not joining lobby your are already in
        if (currentLobby != null)
        {
            if (lobbyToJoin.Owner.Name == currentLobby.Value.Owner.Name) //Already in lobby trying to join
            {
                Debug.Log("Failed to join lobby (already in lobby)");
                return;
            }
        }

        //Tell Steam to join lobby
        RoomEnter joinedLobby = await lobbyToJoin.Join();
        if (joinedLobby != RoomEnter.Success)
        {
            Debug.Log("Failed to join lobby (unknown issue)");
        }
        else
        {
            currentLobby = lobbyToJoin;
            Debug.Log("Successfully joined lobby owned by " + lobbyToJoin.Owner.Name);
        }
    }

    //Called by Steam when joining through Steam app
    private void SteamFriends_OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
    {
        AttemptJoin(lobby);
    }

    //Called by FriendJoinWithButton.cs
    public void JoinUsingButton(Lobby lobby)
    {
        AttemptJoin(lobby);
    }

    //Called when Steam lobby creation comes back successfully
    private void SteamMatchmaking_OnLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId steamId)
    {
        Debug.Log("Lobby created successfully");
    }

    //Called when a member of current Steam lobby has left the lobby
    private void SteamMatchmaking_OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        Debug.Log(friend.Name + " has left the lobby");
    }

    //Called when a member joins current Steam lobby
    private void SteamMatchmaking_OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        Debug.Log(friend.Name + " has joined the lobby");
    }

    //Called on successful joining of a lobby
    private void SteamMatchmaking_OnLobbyEntered(Lobby lobby)
    {
        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().linkNetworkManager();

        if (NetworkManager.Singleton.IsHost == true)
        {
            if (lobby.IsOwnedBy(SteamClient.SteamId) == false)
            {
                Debug.Log("Stopping host");
                GameObject.Find("LoadHandler").GetComponent<LoadHandler>().startConnecting();
                NetworkManager.Singleton.Shutdown();
                StartCoroutine(YieldForNetworkManagerShutdown());
            }
        }
        else
        {
            GameObject.Find("LoadHandler").GetComponent<LoadHandler>().startConnecting();
            StartClient(currentLobby.Value.Owner.Id);
        }
    }

    //Called when a host joins a lobby which requires NetworkManager shutdown, followed by initializing client
    private IEnumerator YieldForNetworkManagerShutdown()
    {
        while (NetworkManager.Singleton.ShutdownInProgress == true)
        {
            yield return null;
        }
        StartClient(currentLobby.Value.Owner.Id);
    }

    //Called when lobby creation attempt returns successful or not
    private void SteamMatchmaking_OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK)
        {
            Debug.Log("Lobby creation failed");
        }
        else
        {
            Debug.Log("Lobby created by " + lobby.Owner.Name);
            lobby.SetPublic();
            lobby.SetJoinable(true);
            lobby.SetGameServer(lobby.Owner.Id);
        }
    }

    //Called when connection to Steam is loss for whatever reason, interpreted as internet disconnect
    private void SteamUser_OnSteamServersDisconnected()
    {
        Debug.Log("Steam connection lost");
        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().displayLostConnection("Connection interrupted.");
    }

    //Used to link client updates
    private void LinkNetworkManagerEvents()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
    }

    //Called by CampaignOptionsController.cs when creating a lobby (if one does not already exist)
    public async void StartHost(int maxMembers)
    {
        LinkNetworkManagerEvents();
        NetworkManager.Singleton.StartHost();
        currentLobby = await SteamMatchmaking.CreateLobbyAsync(maxMembers);
    }

    //Called when Steam says lobby has been joined and not host 
    public void StartClient(SteamId id)
    {
        LinkNetworkManagerEvents();
        transport.targetSteamId = id;
        if (NetworkManager.Singleton.StartClient() == true)
        {
            Debug.Log("Client started");
        }
        else
        {
            Debug.Log("Failed to start client");
        }
    }

    //Leaves Steam lobby, shuts down NetworkManager, and unlinks client events
    public void Disconnect()
    {
        Debug.Log("Disconnected");
        currentLobby?.Leave();
        currentLobby = null;
        if (NetworkManager.Singleton == null)
        {
            return;
        }
        NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
        NetworkManager.Singleton.Shutdown(true);
    }

    //Called when a client connects to the NetworkManager lobby
    private void Singleton_OnClientConnectedCallback(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId) //someone else connected
        {
            Debug.Log("Client " + clientId + " connected"); 
        }
        else //we connected
        {
            if (clientId != 0) //only end connecting animation if not host
            {
                GameObject.Find("LoadHandler").GetComponent<LoadHandler>().endConnecting();
            }
        }
    }

    //Called when a client disconnects from the NetworkManager lobby
    private void Singleton_OnClientDisconnectCallback(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost == false) 
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.Log("Host has disconnected");
                Disconnect();
                GameObject.Find("LoadHandler").GetComponent<LoadHandler>().displayLostConnection("The host has disconnected.");
            }
        }
    }
}