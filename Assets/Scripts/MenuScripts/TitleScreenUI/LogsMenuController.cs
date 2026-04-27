using TMPro;
using UnityEngine;

public class LogsMenuController : MonoBehaviour
{
    public GameObject LogsMenu;
    public GameObject MainMenu;

    public GameObject Page1;
    public GameObject Page2;

    public GameObject NextPageButton;
    public GameObject PreviousPageButton;

    public TMP_Text PageNumberText;

    void Start()
    {
        Page1.SetActive(true);
        NextPageButton.SetActive(true);

        Page2.SetActive(false);
        PreviousPageButton.SetActive(false);
    }
    public void HandleNextPageButtonClick()
    {
        Page1.SetActive(false);
        NextPageButton.SetActive(false);

        Page2.SetActive(true);
        PreviousPageButton.SetActive(true);
        PageNumberText.text = "2";
    }

    public void HandlePreviousPageButtonClick()
    {
        Page2.SetActive(false);
        PreviousPageButton.SetActive(false);

        Page1.SetActive(true);
        NextPageButton.SetActive(true);
        PageNumberText.text = "1";
    }

    public void HandleXButtonClick()
    {
        LogsMenu.SetActive(false);
        MainMenu.SetActive(true);
    }
}
