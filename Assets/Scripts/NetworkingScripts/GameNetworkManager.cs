using System.Collections;
using System.Collections.Generic;
using Netcode.Transports.Facepunch;
using NUnit.Framework;
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
        SteamMatchmaking.OnLobbyInvite += SteamMatchmaking_OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += SteamMatchmaking_OnLobbyGameCreated;
        SteamFriends.OnGameLobbyJoinRequested += SteamFriends_OnGameLobbyJoinRequested;
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyCreated -= SteamMatchmaking_OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= SteamMatchmaking_OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= SteamMatchmaking_OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= SteamMatchmaking_OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= SteamMatchmaking_OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= SteamMatchmaking_OnLobbyGameCreated;
        SteamFriends.OnGameLobbyJoinRequested -= SteamFriends_OnGameLobbyJoinRequested;

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnServerStarted -= Singleton_OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    //Called when joining through Steam invite or clicking on lobby invite button
    private async void AttemptJoin(Lobby lobbyToJoin)
    {
        if (SceneManager.GetActiveScene().name != "TitleScreen")
        {
            Debug.Log("Failed to join lobby (cannot join from an active session)");
            return;
        }

        if (NetworkManager.Singleton.IsHost == true && (currentLobby != null && currentLobby.Value.MemberCount > 1))
        {
            Debug.Log("Failed to join lobby (cannot leave as host with a non-empty session)");
            return;
        }

        if (currentLobby != null)
        {
            if (lobbyToJoin.Owner.Name == currentLobby.Value.Owner.Name) //Already in lobby trying to join
            {
                Debug.Log("Failed to join lobby (already in lobby)");
                return;
            }
        }

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

    private void SteamMatchmaking_OnLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId steamId)
    {
        Debug.Log("Lobby created successfully");
    }

    private void SteamMatchmaking_OnLobbyInvite(Friend friend, Lobby lobby)
    {
        Debug.Log("Invite from " + friend.Name);
    }

    private void SteamMatchmaking_OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        Debug.Log(friend.Name + " has left the lobby");
    }

    private void SteamMatchmaking_OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        Debug.Log(friend.Name + " has joined the lobby");
    }

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

    private IEnumerator YieldForNetworkManagerShutdown()
    {
        while (NetworkManager.Singleton.ShutdownInProgress == true)
        {
            yield return null;
        }
        StartClient(currentLobby.Value.Owner.Id);
    }

    private void SteamMatchmaking_OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK)
        {
            Debug.Log("Lobby was not created");
            return;
        }
        else
        {
            lobby.SetPublic();
            lobby.SetJoinable(true);
            lobby.SetGameServer(lobby.Owner.Id);
            Debug.Log("Lobby created by " + lobby.Owner.Name);
        }
    }

    private void LinkNetworkManagerEvents()
    {
        NetworkManager.Singleton.OnServerStarted += Singleton_OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
    }

    public async void StartHost(int maxMembers)
    {
        LinkNetworkManagerEvents();
        NetworkManager.Singleton.StartHost();
        currentLobby = await SteamMatchmaking.CreateLobbyAsync(maxMembers);
    }

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

    public void Disconnect()
    {
        currentLobby?.Leave();
        currentLobby = null;
        if (NetworkManager.Singleton == null)
        {
            return;
        }
        if (NetworkManager.Singleton.IsHost == true)
        {
            NetworkManager.Singleton.OnServerStarted -= Singleton_OnServerStarted;
        }
        else
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
        }
        NetworkManager.Singleton.Shutdown(true);
        Debug.Log("Disconnected");
    }

    private void Singleton_OnClientDisconnectCallback(ulong clientId)
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
    }

    private void Singleton_OnClientConnectedCallback(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Client " + clientId + " connected");
        }
        else
        {
            if (clientId != 0) //only end connecting if not host
            {
                GameObject.Find("LoadHandler").GetComponent<LoadHandler>().endConnecting();
            }
        }

    }

    private void Singleton_OnServerStarted()
    {
        Debug.Log("Host started");
    }
}