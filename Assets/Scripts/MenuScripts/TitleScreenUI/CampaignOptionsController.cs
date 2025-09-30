using UnityEngine;
using Steamworks;

public class CampaignOptionsController : MonoBehaviour
{
    public GameObject CampaignOptions;
    public GameObject CampaignLobby;
    public GameObject JoinCampaignMenu;
    public GameObject MainMenu;

    public void HandleHostGameButtonClick()
    {
        //only start host if haven't done so already
        if (GameNetworkManager.Instance.currentLobby.HasValue == false)
        {
            //SteamMatchmaking.CreateLobbyAsync(4);
            GameNetworkManager.Instance.StartHost(4);
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
