using UnityEngine;

public static class SceneData
{
    public static string targetUI = null;
}

public class PauseMenuController : MonoBehaviour
{
    public GameObject PauseMenu;
    public GameObject ControlsMenu;
    public GameObject SettingsMenu;

    public void HandleResumeButtonClick()
    {
        PauseMenu.SetActive(false);
        PrimaryScript.Instance.unpause();
    }

    public void HandleControlsButtonClick()
    {
        SwitchTo(ControlsMenu);
    }

    public void HandleSettingsButtonClick()
    {
        SwitchTo(SettingsMenu);
    }

    public void HandleQuitButtonClick()
    {
        PlayerManager.leaveGame();
    }

    private void SwitchTo(GameObject target)
    {
        PauseMenu.SetActive(false);
        ControlsMenu.SetActive(false);
        SettingsMenu.SetActive(false);

        target.SetActive(true);
    }
}
