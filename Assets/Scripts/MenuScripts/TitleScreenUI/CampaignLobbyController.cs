using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CampaignLobbyController : NetworkBehaviour
{
    public GameObject CampaignOptions;
    public GameObject CampaignLobby;
    public GameObject FriendListBox;
    public GameObject FriendsLabel;
    public GameObject NoFriendsOnlineLabel;

    [SerializeField]
    private GameObject FriendUITemplate = null;
    private List<GameObject> FriendObjects = new List<GameObject>();
    
    public List<TextMeshProUGUI> JoinedPlayersList = new List<TextMeshProUGUI>();
    private Coroutine YieldForLobbyCoroutine = null;

    void OnEnable()
    {
        CheckForLobbyUpdates();
        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().connectNetworkManager();
        if (YieldForLobbyCoroutine != null)
        {
            StopCoroutine(YieldForLobbyCoroutine);
        }
        StartCoroutine(YieldForLobby());
    }

    //updates list of names 
    private void UpdateLobbyList()
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            JoinedPlayersList[0].text = SteamClient.Name;
        }
        if (GameNetworkManager.Instance.currentLobby == null)
        {
            return;
        }

        //clear names
        for (int i = 0; i < 4; i++)
        {
            JoinedPlayersList[i].text = "";
        }
        //add however many friends are in the lobby
        IEnumerable<Friend> lobbyMembers = GameNetworkManager.Instance.currentLobby.Value.Members;
        for (int i = 0; i < lobbyMembers.Count<Friend>(); i++)
        {
            if (i != (int)NetworkManager.Singleton.LocalClientId)
            {
                JoinedPlayersList[i].text = lobbyMembers.ElementAt<Friend>(i).Name;
            }
            else
            {
                JoinedPlayersList[i].text = SteamClient.Name;
            }
        }
    }

    IEnumerator YieldForLobby()
    {
        while (GameNetworkManager.Instance.currentLobby == null)
        {
            yield return null;
        }
        UpdateLobbyList();
        UpdateFriendsList();

        YieldForLobbyCoroutine = null;
    }

    private void UpdateFriendsList()
    {
        //clear existing friend entries
        foreach (GameObject friend in FriendObjects)
        {
            Destroy(friend.gameObject);
        }
        FriendObjects.Clear();

        //get invitable friends
        List<Friend> invitableFriends = CheckFriends.GetOnlineFriendsNotInAnyLobby();

        //display invitable friends
        foreach (Friend friend in invitableFriends)
        {
            GameObject friendObject = Instantiate<GameObject>(FriendUITemplate, FriendListBox.transform);
            friendObject.GetComponent<FriendInviteWithButton>().SetFriend(friend);
            FriendObjects.Add(friendObject.gameObject);
        }

        //if no friends, make it known
        NoFriendsOnlineLabel.SetActive(invitableFriends.Count == 0);
        FriendsLabel.GetComponent<TMP_Text>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        if (invitableFriends.Count == 0)
        {
            FriendsLabel.GetComponent<TMP_Text>().color = new Color(1.0f, 1.0f, 1.0f, 0.2f);
        }
    }

    private void OnLobbyChange(NetworkManager manager, ConnectionEventData eventData)
    {
        UpdateFriendsList();
        UpdateLobbyList();
    }

    //fires whenever SteamFriends detects a change in any friend's state
    private Action<Friend> OnFriendChange()
    {
        UpdateFriendsList();
        UpdateLobbyList();
        return friend => { };
    }

    public void CheckForLobbyUpdates()
    {
        UpdateLobbyList();
        //listen for future updates to the lobby
        NetworkManager.Singleton.OnConnectionEvent += OnLobbyChange;
        SteamFriends.OnPersonaStateChange += OnFriendChange();
    }

    public void HandleXButtonClick()
    {
        //do not listen for future updates to the lobby
        NetworkManager.Singleton.OnConnectionEvent -= OnLobbyChange;
        SteamFriends.OnPersonaStateChange -= OnFriendChange();
        GameNetworkManager.Instance.currentLobby.Value.Leave();
        SwitchTo(CampaignOptions);
    }

    public void HandleEngageButtonClick()
    {
        //do not listen for future updates to the lobby
        NetworkManager.Singleton.OnConnectionEvent -= OnLobbyChange;
        SteamFriends.OnPersonaStateChange -= OnFriendChange();
        //lock the lobby once game starts
        GameNetworkManager.Instance.currentLobby.Value.SetInvisible();
        GameNetworkManager.Instance.currentLobby.Value.SetJoinable(false);
        SceneSwapper.Instance.ChangeSceneClientRPC("BridgeEnvironment");
    }

    private void SwitchTo(GameObject target)
    {
        CampaignOptions.SetActive(false);
        CampaignLobby.SetActive(false);

        target.SetActive(true);
    }
}
