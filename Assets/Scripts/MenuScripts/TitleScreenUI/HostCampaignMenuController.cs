using System.Collections.Generic;
using System.Linq;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class HostCampaignMenuController : MonoBehaviour
{
    public GameObject HostCampaignMenu;
    public GameObject CampaignMenu;

    public List<TextMeshProUGUI> JoinedPlayersList = new List<TextMeshProUGUI>();

    //updates list of names 
    private void UpdateLobbyList()
    {
        JoinedPlayersList[0].text = SteamClient.Name;
        if (GameNetworkManager.Instance.currentLobby == null)
        {
            return;
        }
        //clear non-host entries
        for (int i = 1; i < 4; i++)
        {
            JoinedPlayersList[i].text = "";
        }
        //add however many friends are in the lobby
        IEnumerable<Friend> lobby_members = GameNetworkManager.Instance.currentLobby.Value.Members;
        for (int i = 1; i < lobby_members.Count<Friend>(); i++)
        {
            JoinedPlayersList[i].text = lobby_members.ElementAt<Friend>(i).Name;
        }
    }

    private void OnLobbyChange(NetworkManager manager, ConnectionEventData eventData)
    {
        UpdateLobbyList();
    }

    public void CheckForLobbyUpdates()
    {
        UpdateLobbyList();
        //listen for future updates to the lobby
        NetworkManager.Singleton.OnConnectionEvent += OnLobbyChange;
    }

    public void HandleXButtonClick()
    {
        //do not listen for future updates to the lobby
        NetworkManager.Singleton.OnConnectionEvent -= OnLobbyChange;
        SwitchTo(CampaignMenu);
    }

    public void HandleEngageButtonClick()
    {
        //do not listen for future updates to the lobby
        NetworkManager.Singleton.OnConnectionEvent -= OnLobbyChange;
        //lock the lobby once game starts
        GameNetworkManager.Instance.currentLobby.Value.SetJoinable(false);
        SceneSwapper.Instance.ChangeSceneClientRPC("BridgeEnvironment");
    }

    private void SwitchTo(GameObject target)
    {
        HostCampaignMenu.SetActive(false);
        CampaignMenu.SetActive(false);

        target.SetActive(true);
    }
}
