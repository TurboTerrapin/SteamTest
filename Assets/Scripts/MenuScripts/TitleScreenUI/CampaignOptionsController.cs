using UnityEngine;
using Unity.Netcode;

public class CampaignOptionsController : MonoBehaviour
{
    public GameObject CampaignOptions;
    public GameObject CampaignLobby;
    public GameObject JoinCampaignMenu;
    public GameObject MainMenu;
    public GameObject LobbyHandler;

    public void HandleHostGameButtonClick()
    {
        //only start host if haven't done so already
        if (GameNetworkManager.Instance.currentLobby.HasValue == false)
        {
            //SteamMatchmaking.CreateLobbyAsync(4);
            GameNetworkManager.Instance.StartHost(4);
            GameObject LH = GameObject.Instantiate(LobbyHandler);
            LH.name = "LobbyHandler";
            LH.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            GameNetworkManager.Instance.currentLobby.Value.Join();
        }

        SwitchTo(CampaignLobby);
    }

    public void HandleJoinGameButtonClick()
    {
        SwitchTo(JoinCampaignMenu);
    }

    public void HandleBackButtonClick()
    {
        SwitchTo(MainMenu);
    }

    private void SwitchTo(GameObject target)
    {
        CampaignLobby.SetActive(false);
        CampaignOptions.SetActive(false);
        JoinCampaignMenu.SetActive(false);
        MainMenu.SetActive(false);

        target.SetActive(true);
    }
}
