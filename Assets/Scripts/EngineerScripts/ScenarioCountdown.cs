/*
    ScenarioCountdown.cs
    - Handles scenario countdown timer visual in engineer position (boundary expiration)
    Contributor(s): Jake Schott
    Last Updated: 6/11/2026
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScenarioCountdown : MonoBehaviour, IPowerable
{
    public GameObject countdown_display;
    public List<AudioClip> countdown_notifications;

    public void displayCountdownAdjustment(int total_seconds)
    {
        if (total_seconds == 180)
        {
            ReferenceAssistor.Instance.audio_manager.AddNotification(1, countdown_notifications[0]);
        }
        else if (total_seconds == 60)
        {
            ReferenceAssistor.Instance.audio_manager.AddNotification(1, countdown_notifications[1]);
        }
        else if (total_seconds == 15)
        {
            ReferenceAssistor.Instance.audio_manager.AddNotification(3, countdown_notifications[2]);
        }
        else if (total_seconds < 11 && total_seconds > 0)
        {
            ReferenceAssistor.Instance.audio_manager.AddNotification(3, countdown_notifications[2 + total_seconds]);
        }

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
        countdown_display.transform.GetChild(1).GetComponent<TMP_Text>().SetText(to_display);

        //recolor
        Color to_change_to = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        if (total_seconds <= 60)
        {
            to_change_to = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }
        countdown_display.transform.GetChild(0).GetComponent<TMP_Text>().color = to_change_to;
        countdown_display.transform.GetChild(1).GetComponent<TMP_Text>().color = to_change_to;
        to_change_to.a = 0.08f;
        countdown_display.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().color = to_change_to;
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