using Steamworks;
using UnityEngine;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    public GameObject MainMenu;
    public GameObject CampaignMenu;
    //public GameObject LogsScreen;
    public GameObject CustomizationMenu;
    public GameObject SettingsMenu;

    private void Start()
    {
        GameObject LoadHandler = GameObject.Find("LoadHandler");
        if (LoadHandler != null)
        {
            LoadHandler.GetComponent<LoadHandler>().endLoad(false);
        }
    }

    public void HandleCampaignButtonClick()
    {
        SwitchTo(CampaignMenu);
    }

    public void HandleLogsButtonClick()
    {
        //SwitchTo(LogsScreen);
    }

    public void HandleCustomizeButtonClick()
    {
        SwitchTo(CustomizationMenu);
    }

    public void HandleSettingsButtonClick()
    {
        SwitchTo(SettingsMenu);
    }

    public void HandleQuitButtonClick()
    {
        Application.Quit();
    }

    private void SwitchTo(GameObject target)
    {
        MainMenu.SetActive(false);
        CampaignMenu.SetActive(false);
        //LogsScreen.SetActive(false);
        CustomizationMenu.SetActive(false);
        SettingsMenu.SetActive(false);

        target.SetActive(true);
    }
}
