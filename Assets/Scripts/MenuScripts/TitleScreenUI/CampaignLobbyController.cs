using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CampaignLobbyController : MonoBehaviour
{
    //CLASS CONSTANTS
    private static int DEFAULT_DIFFICULTY = 0; //Easy

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
    private Coroutine YieldForLobbyCoroutine = null;

    private void OnEnable()
    {
        DisplayDifficultyChange(DEFAULT_DIFFICULTY);
        HandleDifficultyChange();
        DeactivateEngageButton();
        DeactivateDifficultyGroup();
        CheckForLobbyUpdates();
        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().connectNetworkManager();
        if (YieldForLobbyCoroutine != null)
        {
            StopCoroutine(YieldForLobbyCoroutine);
        }
        StartCoroutine(YieldForLobby());
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
            Color c = entry.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color;
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
        //Used for temporary period where screen is displayed but lobby is not yet created
        if (NetworkManager.Singleton.IsHost == true)
        {
            JoinedPlayersList[0].text = SteamClient.Name;
        }

        //Only update list if a lobby exists
        if (GameNetworkManager.Instance.currentLobby == null)
        {
            return;
        }

        //Clear names
        for (int i = 0; i < 4; i++)
        {
            JoinedPlayersList[i].text = "";
        }

        //Add however many friends are in the lobby
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

        //Activate/deactive engage button
        if (NetworkManager.Singleton.IsHost == true)
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

    //waits for lobby to exist and then updates lobby/friends list
    IEnumerator YieldForLobby()
    {
        while (GameNetworkManager.Instance.currentLobby == null)
        {
            yield return null;
        }
        UpdateLobbyList();
        UpdateFriendsList();
        HandleDifficultyChange();

        YieldForLobbyCoroutine = null;
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
        List<Friend> invitableFriends = CheckFriends.GetOnlineFriendsNotInAnyLobby();

        //Display invitable friends
        foreach (Friend friend in invitableFriends)
        {
            GameObject friendObject = Instantiate<GameObject>(FriendUITemplate, FriendListBox.transform);
            friendObject.GetComponent<FriendInviteWithButton>().SetFriend(friend);
            FriendObjects.Add(friendObject.gameObject);
        }

        //If no friends, make it known
        NoFriendsOnlineLabel.SetActive(invitableFriends.Count == 0);
        FriendsLabel.GetComponent<TMP_Text>().color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        if (invitableFriends.Count == 0)
        {
            FriendsLabel.GetComponent<TMP_Text>().color = new Color(1.0f, 1.0f, 1.0f, 0.2f);
        }
    }

    //Runs on changes to the lobby
    private void OnLobbyChange(NetworkManager manager, ConnectionEventData eventData)
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

    //Links to several events
    public void CheckForLobbyUpdates()
    {
        UpdateLobbyList();
        //Listen for future updates to the lobby
        NetworkManager.Singleton.OnConnectionEvent += OnLobbyChange;
        SteamFriends.OnPersonaStateChange += OnFriendChange();
    }

    //Unlinks several events
    public void HandleXButtonClick()
    {
        //Do not listen for future updates to the lobby
        NetworkManager.Singleton.OnConnectionEvent -= OnLobbyChange;
        SteamFriends.OnPersonaStateChange -= OnFriendChange();
        GameNetworkManager.Instance.currentLobby.Value.Leave();
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

        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().updateDifficulty(difficultyIndex);
    }

    public void HandleEngageButtonClick()
    {
        //Do not listen for future updates to the lobby
        NetworkManager.Singleton.OnConnectionEvent -= OnLobbyChange;
        SteamFriends.OnPersonaStateChange -= OnFriendChange();
        //Lock the lobby once game starts
        GameNetworkManager.Instance.currentLobby.Value.SetInvisible();
        GameNetworkManager.Instance.currentLobby.Value.SetJoinable(false);
        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().startLoadForAllPlayers();
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