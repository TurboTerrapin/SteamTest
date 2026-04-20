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
    [SerializeField]
    private GameObject LobbyUITemplate = null;

    private List<GameObject> LobbyObjects = new List<GameObject>();
    private Coroutine LobbyCheckCoroutine = null;

    private void OnEnable()
    {
        JoinableLobbyList.SetActive(true);

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

    //Called by FriendJoinWithButton
    public void StopCheckingLobbies()
    {
        //Stop checking lobbies
        ResetCoroutines();
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
        JoinableLobbyList.SetActive(false);
        CampaignOptions.SetActive(false);

        target.SetActive(true);
    }
}