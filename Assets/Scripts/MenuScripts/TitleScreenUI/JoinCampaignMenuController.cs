using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class JoinCampaignMenuController : MonoBehaviour
{
    private const float UPDATE_DELAY = 1.0f;

    public GameObject JoinCampaign;
    public GameObject JoinableLobbyList;
    public GameObject CampaignOptions;
    public GameObject CampaignLobby;
    public GameObject LobbyListBox;
    public GameObject NoLobbiesFoundLabel;
    public GameObject Connecting;
    [SerializeField]
    private GameObject LobbyUITemplate = null;

    private List<GameObject> LobbyObjects = new List<GameObject>();
    private Coroutine LobbyCheckCoroutine = null;
    private Coroutine ConnectingCoroutine = null;

    void OnEnable()
    {
        JoinableLobbyList.SetActive(true);
        Connecting.SetActive(false);

        ResetCoroutines();
        LobbyCheckCoroutine = StartCoroutine(LobbyCheck());
    }

    private void ResetCoroutines()
    {
        if (LobbyCheckCoroutine != null)
        {
            StopCoroutine(LobbyCheckCoroutine);
            LobbyCheckCoroutine = null;
        }
        if (ConnectingCoroutine != null)
        {
            StopCoroutine(ConnectingCoroutine);
            ConnectingCoroutine = null;
        }
    }

    //Checks possible lobbies every UPDATE_DELAY
    IEnumerator LobbyCheck()
    {
        while (true)
        {
            UpdateJoinableLobbiesList();
            yield return new WaitForSeconds(UPDATE_DELAY);
        }   
    }

    //Handles the ... animation for connecting
    IEnumerator ConnectingAnimation()
    {
        TMP_Text connectingText = Connecting.transform.GetChild(1).GetComponent<TMP_Text>();
        while (true)
        {
            string elipse = "";
            for (int i = 0; i < 4; i++)
            {
                connectingText.SetText("CONNECTING" + elipse);
                yield return new WaitForSeconds(0.25f);
                elipse += ".";
            }
            yield return null;
        }
    }

    //Fired when connected to the lobby attempting to join
    public Action<ulong> OnLobbyJoin()
    {
        return handleJoin => {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnLobbyJoin();
            ResetCoroutines();
            SwitchTo(CampaignLobby);
        };
    }

    //Called by FriendJoinWithButton
    public void ConnectToLobby()
    {
        //Stop checking lobbies
        ResetCoroutines();

        //Switch to connecting box
        JoinableLobbyList.SetActive(false);
        Connecting.SetActive(true);

        ConnectingCoroutine = StartCoroutine(ConnectingAnimation());

        //Listen for connection update
        NetworkManager.Singleton.OnClientConnectedCallback += OnLobbyJoin();
    }

    public void UpdateJoinableLobbiesList()
    {
        //Clear existing lobby entries
        foreach (GameObject lobby in LobbyObjects)
        {
            Destroy(lobby.gameObject);
        }
        LobbyObjects.Clear();

        //Get invitable friends
        List<Lobby> joinableLobbies = CheckLobbies.GetPrivateLobbies();

        //Display invitable friends
        foreach (Lobby lobby in joinableLobbies)
        {
            GameObject lobbyObject = Instantiate<GameObject>(LobbyUITemplate, LobbyListBox.transform);
            lobbyObject.GetComponent<FriendJoinWithButton>().SetLobby(lobby, this);
            LobbyObjects.Add(lobbyObject.gameObject);
        }

        //If no lobbies, make it known
        NoLobbiesFoundLabel.SetActive(joinableLobbies.Count == 0);
    }

    public void HandleXButtonClick()
    {
        ResetCoroutines();
        SwitchTo(CampaignOptions);
    }

    private void SwitchTo(GameObject target)
    {
        JoinCampaign.SetActive(false);
        Connecting.SetActive(false);
        JoinableLobbyList.SetActive(false);
        CampaignOptions.SetActive(false);

        target.SetActive(true);
    }
}