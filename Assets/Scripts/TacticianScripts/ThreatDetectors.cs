/*
    ThreatDetectors.cs
    - Used to detect incoming phaser fire or torpedoes
    - Call adjustTorpedoWarning(true) or adjustPhaserWarning(true) if targeting torpedoes or phasers
    Contributor(s): Jake Schott
    Last Updated: 6/25/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ThreatDetectors : NetworkBehaviour, IDescribable, IPowerable
{
    //CLASS CONSTANTS
    private static float FLASH_ANIMATION_SPEED = 1.0f;
    private static Color BLUE = new Color(0.0f, 0.84f, 1.0f);
    private static Color RED = new Color(1.0f, 0.0f, 0.0f);

    private string[] CONTROL_NAMES = new string[] { "TORPEDO DETECTOR", "PHASER DETECTOR" };
    private static string INFO_MESSAGE = "When active, flashes red and makes a noise when targeted or attacked by ";
    private static string[] INFO_MESSAGE_ENDINGS = new string[] { "torpedoes.", "phasers." };

    public GameObject threat_detectors_display;
    public GameObject lit_indicators;
    public AudioSource threat_detector_alert_sound;

    private bool[] threat_detection_override = new bool[2] { false, false };
    private float[] threat_detection_times = new float[2] { 0.0f, 0.0f };
    private Coroutine flashing_animation_coroutine = null;

    private List<string> ray_targets = new List<string> { "torpedo_detector", "phaser_detector" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        hud_info.setInfo(INFO_MESSAGE + INFO_MESSAGE_ENDINGS[0]);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setInfo(INFO_MESSAGE + INFO_MESSAGE_ENDINGS[index]);
        return hud_info;
    }

    //resets threat detection to default (none)
    public void resetToDefault()
    {
        for (int i = 0; i < 2; i++)
        {
            threat_detection_override[i] = false;
            threat_detection_times[i] = 0.0f;
        }
    }

    //used when a torpedo is imminently being fired on the ship or no longer targeted
    public void adjustTorpedoWarningOverride(bool alert)
    {
        threat_detection_override[0] = alert;
        startThreatAlertSound();
    }

    //used when phaser(s) are imminently being fired on the ship or no longer targeted
    public void adjustPhaserWarningOverride(bool alert)
    {
        threat_detection_override[1] = alert;
        startThreatAlertSound();
    }

    //used when a torpedo is imminently being fired on the ship or no longer targeted
    public void adjustTorpedoWarningTime(float time)
    {
        threat_detection_times[0] = Mathf.Max(threat_detection_times[0], time);
        startThreatAlertSound();
    }

    //used when phaser(s) are imminently being fired on the ship or no longer targeted
    public void adjustPhaserWarningTime(float time)
    {
        threat_detection_times[1] = Mathf.Max(threat_detection_times[1], time);
        startThreatAlertSound();
    }

    //helper method
    private void startThreatAlertSound()
    {
        if (threat_detector_alert_sound.isPlaying == true)
        {
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            //start sound if active threat detection
            if (threat_detection_override[i] == true || threat_detection_times[i] > 0.0f)
            {
                threat_detector_alert_sound.Play();
                return;
            }
        }
    }

    //helper method for screen animation
    private bool displayFlashAnimationAdjustment(int index, float norm_a, float alert_a)
    {
        bool alert = (threat_detection_override[index] || threat_detection_times[index] > 0.0f);

        if (alert == true)
        {
            lit_indicators.transform.GetChild(index).GetChild(0).gameObject.SetActive(alert_a < 0.6f);
            lit_indicators.transform.GetChild(index).GetChild(1).gameObject.SetActive(false);
            lit_indicators.transform.GetChild(index).GetChild(2).gameObject.SetActive(alert_a >= 0.6f);

            Color c = new Color(RED.r, RED.g, RED.b, alert_a);
            foreach (Transform t in threat_detectors_display.transform.GetChild(index))
            {
                t.GetComponent<UnityEngine.UI.RawImage>().color = c;
            }
        }
        else
        {
            lit_indicators.transform.GetChild(index).GetChild(0).gameObject.SetActive(false);
            lit_indicators.transform.GetChild(index).GetChild(1).gameObject.SetActive(true);
            lit_indicators.transform.GetChild(index).GetChild(2).gameObject.SetActive(false);

            Color c = new Color(BLUE.r, BLUE.g, BLUE.b, norm_a);
            foreach (Transform t in threat_detectors_display.transform.GetChild(index))
            {
                t.GetComponent<UnityEngine.UI.RawImage>().color = c;
            }
        }

        return alert;
    }

    //handles flashing animation on screen
    IEnumerator flashAnimation()
    {
        float normal_alpha = 0.0f;
        float alert_alpha = 0.0f;
        float normal_elapsed_time = 0.0f;
        float alert_elapsed_time = 0.0f;
        bool alert_in_effect;
        while (true)
        {
            for (int i = 0; i < 2; i++)
            {
                threat_detection_times[i] = Mathf.Max(0.0f, threat_detection_times[i] - Time.deltaTime);
            }

            normal_elapsed_time += (Time.deltaTime * FLASH_ANIMATION_SPEED);
            alert_elapsed_time += (Time.deltaTime * FLASH_ANIMATION_SPEED * 6.0f);
            normal_alpha = Mathf.Lerp(0.2f, 1.0f, Mathf.PingPong(normal_elapsed_time, 1.0f));
            alert_alpha = Mathf.Lerp(0.2f, 1.0f, Mathf.PingPong(alert_elapsed_time, 1.0f));

            alert_in_effect = false;
            for (int i = 0; i < 2; i++)
            {
                alert_in_effect = (alert_in_effect || displayFlashAnimationAdjustment(i, normal_alpha, alert_alpha));
            }

            if (alert_in_effect == false) 
            {
                threat_detector_alert_sound.Stop();
            }

            yield return null;
        }
    }

    //helper method that disables the flash animation
    private void disableFlashAnimation()
    {
        if (flashing_animation_coroutine == null)
        {
            return;
        }
        StopCoroutine(flashing_animation_coroutine);
        flashing_animation_coroutine = null;
        threat_detector_alert_sound.Stop();
    }

    public void powerOn(int pos)
    {
        threat_detectors_display.SetActive(true);
        if (flashing_animation_coroutine == null)
        {
            flashing_animation_coroutine = StartCoroutine(flashAnimation());
        }
    }

    public void powerOff(int pos, float time)
    {
        threat_detectors_display.SetActive(false);
        disableFlashAnimation();
        for (int i = 0; i < 2; i++)
        {
            lit_indicators.transform.GetChild(i).GetChild(0).gameObject.SetActive(true);
            lit_indicators.transform.GetChild(i).GetChild(1).gameObject.SetActive(false);
            lit_indicators.transform.GetChild(i).GetChild(2).gameObject.SetActive(false);
        }
    }
}