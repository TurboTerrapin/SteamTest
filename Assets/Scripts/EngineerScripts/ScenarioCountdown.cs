/*
    ScenarioCountdown.cs
    - Handles scenario countdown timer visual in engineer position (boundary expiration)
    Contributor(s): Jake Schott
    Last Updated: 2/1/2026
*/

using TMPro;
using UnityEngine;

public class ScenarioCountdown : MonoBehaviour, IPowerable
{
    public GameObject countdown_display;

    public void displayCountdownAdjustment(int total_seconds)
    {
        //set text
        string to_display = "";
        int minutes = total_seconds / 60;
        int seconds = total_seconds % 60;
        to_display += minutes.ToString() + ":";
        if (seconds < 10)
        {
            to_display += "0" + seconds;
        }
        else
        {
            to_display += seconds.ToString();
        }
        countdown_display.transform.GetChild(2).GetComponent<TMP_Text>().SetText(to_display);

        //adjust progress bar
        countdown_display.transform.GetChild(3).GetComponent<UnityEngine.UI.Image>().fillAmount = (1.0f * total_seconds / ScenarioManager.COUNTDOWN_TIME);

        //recolor
        Color to_change_to = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        if (total_seconds <= 60)
        {
            to_change_to = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }
        for (int i = 0; i < countdown_display.transform.GetChild(0).childCount; i++)
        {
            countdown_display.transform.GetChild(0).GetChild(i).GetComponent<UnityEngine.UI.Image>().color = to_change_to;
        }
        countdown_display.transform.GetChild(1).GetComponent<TMP_Text>().color = to_change_to;
        countdown_display.transform.GetChild(2).GetComponent<TMP_Text>().color = to_change_to;
        countdown_display.transform.GetChild(3).GetComponent<UnityEngine.UI.Image>().color = to_change_to;
    }

    public void powerOn(int position)
    {
        countdown_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        countdown_display.SetActive(false);
    }
}