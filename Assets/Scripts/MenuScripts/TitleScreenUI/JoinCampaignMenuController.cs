using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

public class JoinCampaignMenuController : MonoBehaviour
{
    private const float UPDATE_DELAY = 1.0f;

    public GameObject JoinCampaignList;
    public GameObject CampaignOptions;
    public GameObject LobbyListBox;
    public GameObject NoLobbiesFoundLabel;

    [SerializeField]
    private GameObject LobbyUITemplate = null;
    private List<GameObject> LobbyObjects = new List<GameObject>();
    private Coroutine LobbyCheckCoroutine = null;

    void OnEnable()
    {
        if (LobbyCheckCoroutine != null)
        {
            StopCoroutine(LobbyCheckCoroutine);
        }
        LobbyCheckCoroutine = StartCoroutine(LobbyCheck());
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
            lobbyObject.GetComponent<FriendJoinWithButton>().SetLobby(lobby);
            LobbyObjects.Add(lobbyObject.gameObject);
        }

        //If no lobbies, make it known
        NoLobbiesFoundLabel.SetActive(joinableLobbies.Count == 0);
    }

    public void HandleXButtonClick()
    {
        if (LobbyCheckCoroutine != null)
        {
            StopCoroutine(LobbyCheckCoroutine);
            LobbyCheckCoroutine = null;
        }
        SwitchTo(CampaignOptions);
    }

    private void SwitchTo(GameObject target)
    {
        JoinCampaignList.SetActive(false);
        CampaignOptions.SetActive(false);

        target.SetActive(true);
    }
}
