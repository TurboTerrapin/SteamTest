using Steamworks;
using TMPro;
using UnityEngine;

public class SteamConnectionTest : MonoBehaviour
{
    public GameObject SteamConnectionScreen;

    public GameObject TitleScreenCanvas;

    void Start()
    {
        if (!SteamClient.IsValid || !SteamClient.IsLoggedOn)
        {
            SteamConnectionScreen.SetActive(true);
        }
        else
        {
            TitleScreenCanvas.SetActive(true);
        }
    }
    public void HandleQuitButtonClick()
    {
        Application.Quit();
    }
}
