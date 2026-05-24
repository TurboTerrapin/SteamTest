using System;
using System.Collections.Generic;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CampaignLobbyController : MonoBehaviour
{
    public GameObject CampaignOptions;
    public GameObject CampaignLobby;
    public GameObject FriendListBox;
    public GameObject FriendsLabel;
    public GameObject NoFriendsOnlineLabel;
    public GameObject DifficultyToggleGroup;
    public GameObject EngageButton;

    [SerializeField]
    private GameObject FriendUITemplate = null;
    private List<GameObject> FriendObjects = new List<GameObject>();
    
    public List<TextMeshProUGUI> JoinedPlayersList = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> JoinedNumbersList = new List<TextMeshProUGUI>();

    private LobbyHandler LobbyHandler;

    private void OnEnable()
    {
        LobbyHandler = GameObject.Find("LobbyHandler").GetComponent<LobbyHandler>();
        if (NetworkManager.Singleton.IsHost == true)
        {
            LobbyHandler.updateDifficulty(LobbyHandler.DEFAULT_DIFFICULTY);
        }
        DeactivateEngageButton();
        DeactivateDifficultyGroup();
        CheckForLobbyUpdates();
        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().linkNetworkManager();
    }

    //Used by ActivateEngageButton() and DeactivateEngageButton()
    private void EngageButtonAlphaHelper(float a)
    {
        //Fade button
        UnityEngine.Color buttonColor = EngageButton.GetComponent<UnityEngine.UI.Image>().color;
        EngageButton.GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(buttonColor.r, buttonColor.g, buttonColor.b, a);
        //Fade button text (ENGAGE)
        EngageButton.transform.GetChild(1).GetComponent<TMP_Text>().color = new UnityEngine.Color(1.0f, 1.0f, 1.0f, a);
        //Fade button border
        UnityEngine.Color borderColor = EngageButton.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color;
        EngageButton.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new UnityEngine.Color(borderColor.r, borderColor.g, borderColor.b, a);
    }

    //Used by ActivateDifficultyGroup() and DeactivateDifficultyGroup()
    private void DifficultyGroupHelper(float a, bool active)
    {
        foreach (Transform entry in DifficultyToggleGroup.transform)
        {
            //Make toggle interactable
            entry.GetComponent<UnityEngine.UI.Toggle>().interactable = active;

            //Recolor border
            UnityEngine.Color c = entry.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color;
            c.a = a;
            entry.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = c;

            //Recolor checkbox
            c = entry.transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.Image>().color;
            c.a = a;
            entry.transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.Image>().color = c;

            //Recolor difficulty label
            c = entry.transform.GetChild(1).GetComponent<TMP_Text>().color;
            c.a = a;
            entry.transform.GetChild(1).GetComponent<TMP_Text>().color = c;

            //Recolor recommendation label
            c = entry.transform.GetChild(2).GetComponent<TMP_Text>().color;
            c.a = a;
            entry.transform.GetChild(2).GetComponent<TMP_Text>().color = c;
        }
    }

    private void ActivateEngageButton()
    {
        //Make button interactable
        EngageButton.GetComponent<UnityEngine.UI.Button>().interactable = true;
        //Unfade button
        EngageButtonAlphaHelper(1.0f);
    }

    private void DeactivateEngageButton()
    {
        //Make button uninteractable
        EngageButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
        //Fade button
        EngageButtonAlphaHelper(0.2f);
    }

    private void ActivateDifficultyGroup()
    {
        DifficultyGroupHelper(1.0f, true);
    }

    private void DeactivateDifficultyGroup()
    {
        DifficultyGroupHelper(0.2f, false);
    }

    //Used to communicate difficulty changes across clients
    public void DisplayDifficultyGroupChange(int difficulty)
    {
        DifficultyToggleGroup.transform.GetChild(difficulty).GetComponent<UnityEngine.UI.Toggle>().isOn = true;
    }

    //Updates list of names in lobby
    private void UpdateLobbyList()
    {
        //Add however many friends are in the lobby
        List<string> lobbyMembers = LobbyHandler.getPlayerNamesInLobby();
        bool[] lobbyConnections = LobbyHandler.getPlayersConnectedInLobby();
        for (int i = 0; i < 4; i++)
        {
            float a = 0.2f;
            if (lobbyConnections[i] == true)
            {
                a = 1.0f;
            }
            JoinedPlayersList[i].text = lobbyMembers[i];
            JoinedPlayersList[i].color = new UnityEngine.Color(1.0f, 1.0f, 1.0f, a);
            JoinedNumbersList[i].color = new UnityEngine.Color(1.0f, 1.0f, 1.0f, a);
        }

        //Only update list if a lobby exists
        if (GameNetworkManager.Instance.currentLobby == null)
        {
            return;
        }

        //Activate/deactive engage button
        if (NetworkManager.Singleton.IsHost == true && LobbyHandler.getPlayerNamesInLobby().Count == NetworkManager.Singleton.ConnectedClientsIds.Count)
        {
            ActivateEngageButton();
            ActivateDifficultyGroup();
        }
        else
        {
            DeactivateEngageButton();
            DeactivateDifficultyGroup();
        }
    }

    //Clears friend invite entries and repopulates with friends not in a DSF lobby already
    private void UpdateFriendsList()
    {
        //Clear existing friend entries
        foreach (GameObject friend in FriendObjects)
        {
            Destroy(friend.gameObject);
        }
        FriendObjects.Clear();

        //Get invitable friends
        List<Friend> invitableFriends = CheckFriends.GetOnlineFriendsNotInSameLobby();

        //Display invitable friends
        foreach (Friend friend in invitableFriends)
        {
            GameObject friendObject = Instantiate<GameObject>(FriendUITemplate, FriendListBox.transform);
            friendObject.GetComponent<FriendInviteWithButton>().SetFriend(friend);
            FriendObjects.Add(friendObject.gameObject);
        }

        //If no friends, make it known
        NoFriendsOnlineLabel.SetActive(invitableFriends.Count == 0);
        FriendsLabel.GetComponent<TMP_Text>().color = new UnityEngine.Color(1.0f, 1.0f, 1.0f, 1.0f);
        if (invitableFriends.Count == 0)
        {
            FriendsLabel.GetComponent<TMP_Text>().color = new UnityEngine.Color(1.0f, 1.0f, 1.0f, 0.2f);
        }
    }

    //Runs on changes to the lobby
    public void OnLobbyChange()
    {
        UpdateFriendsList();
        UpdateLobbyList();
        if (NetworkManager.Singleton.IsHost == true)
        {
            HandleDifficultyChange();
        }
    }
    
    //Fires whenever SteamFriends detects a change in any friend's state (maybe?)
    private Action<Friend> OnFriendChange()
    {
        return handleFriendChange => {
            UpdateFriendsList();
            UpdateLobbyList();
        };
    }

    //Links to friend change event
    public void CheckForLobbyUpdates()
    {
        UpdateLobbyList();
        //Listen for future updates to the lobby
        SteamFriends.OnPersonaStateChange += OnFriendChange();
    }

    //Unlinks several events
    public void HandleXButtonClick()
    {
        //Do not listen for future updates to the lobby
        SteamFriends.OnPersonaStateChange -= OnFriendChange();
        GameNetworkManager.Instance.Disconnect();
        SwitchTo(CampaignOptions);
    }

    public void DisplayDifficultyChange(int difficulty)
    {
        DifficultyToggleGroup.transform.GetChild(difficulty).GetComponent<UnityEngine.UI.Toggle>().isOn = true;
    }

    public void HandleDifficultyChange()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        int difficultyIndex = -1;
        for (int i = 0; i < 4; i++)
        {
            if (DifficultyToggleGroup.transform.GetChild(i).GetComponent<UnityEngine.UI.Toggle>().isOn == true)
            {
                difficultyIndex = i;
                break;
            }
        }

        LobbyHandler.updateDifficulty(difficultyIndex);
    }

    public void HandleEngageButtonClick()
    {
        //Do not listen for future updates to the lobby
        SteamFriends.OnPersonaStateChange -= OnFriendChange();
        //Lock the lobby once game starts
        GameNetworkManager.Instance.currentLobby.Value.SetInvisible();
        GameNetworkManager.Instance.currentLobby.Value.SetJoinable(false);
        LobbyHandler.startLoadForAllPlayers();
        CharacterCustomization[] players = GameObject.FindObjectsByType<CharacterCustomization>(FindObjectsSortMode.InstanceID);
        foreach (CharacterCustomization c in players)
        {
            c.SyncCustomizationRPC();
        }
        SceneSwapper.Instance.ChangeScene("BridgeEnvironment", 0);
    }

    private void SwitchTo(GameObject target)
    {
        CampaignOptions.SetActive(false);
        CampaignLobby.SetActive(false);

        target.SetActive(true);
    }
}