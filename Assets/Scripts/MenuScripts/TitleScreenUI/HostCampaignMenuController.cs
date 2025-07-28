using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Steamworks;

public class HostCampaignMenuController : MonoBehaviour
{
    public GameObject HostCampaignMenu;
    public GameObject CampaignMenu;

    public List<TextMeshProUGUI> JoinedPlayersList = new List<TextMeshProUGUI>();
    public float timer = 1.8f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 2f)
        {
            timer = 0f;
            int i = 0;
            foreach (Friend player in GameNetworkManager.Instance.currentLobby.Value.Members)
            {
                Debug.Log(player.Name);
                JoinedPlayersList[i].text = player.Name;
                i++;
            }
        }
    }

    public void HandleXButtonClick()
    {
        SwitchTo(CampaignMenu);
    }

    public void HandleEngageButtonClick()
    {
        SceneSwapper.Instance.ChangeSceneRPC("BridgeEnvironment", 0);
    }

    private void SwitchTo(GameObject target)
    {
        HostCampaignMenu.SetActive(false);
        CampaignMenu.SetActive(false);

        target.SetActive(true);
    }
}
