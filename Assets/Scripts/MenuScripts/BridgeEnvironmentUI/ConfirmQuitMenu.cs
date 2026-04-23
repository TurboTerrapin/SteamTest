using UnityEngine;

public class ConfirmQuit : MonoBehaviour
{
    public GameObject PauseMenu;
    public GameObject ConfirmQuitMenu;
    public void HandleYesButtonClick()
    {
        PlayerManager.leaveGame();
    }

    public void HandleNoButtonClick()
    {
        SwitchTo(PauseMenu);
    }

    private void SwitchTo(GameObject target)
    {
        PauseMenu.SetActive(false);
        ConfirmQuitMenu.SetActive(false);

        target.SetActive(true);
    }
}
