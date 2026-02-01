/*
    ThreatDetectors.cs
    - Used to detect incoming phaser fire or torpedoes
    - Call adjustTorpedoWarning(true) or adjustPhaserWarning(true) if targeting torpedoes or phasers
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ThreatDetectors : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.5f;
    private static float MAX_POWER_CONSUMPTION = 0.2f; //equates to 2 circles (1 per detector)
    private static float FLASH_ANIMATION_SPEED = 1.0f;
    private static Color BLUE = new Color(0.0f, 0.84f, 1.0f);
    private static Color RED = new Color(1.0f, 0.0f, 0.0f);

    private string[] CONTROL_NAMES = new string[] { "TORPEDO DETECTOR", "PHASER DETECTOR" };
    private static string INFO_MESSAGE = "When active, flashes red and makes a noise when targeted or attacked by ";
    private static string[] INFO_MESSAGE_ENDINGS = new string[] { "torpedoes.", "phasers." };
    private List<string> CONTROL_DESCS = new List<string>() { "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public GameObject detector_switches;
    public GameObject threat_detectors_display;
    public AudioSource threat_detector_alert_sound;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private bool[] threat_detected = new bool[2] { false, false };
    private bool[] detector_is_enabled = new bool[2] { false, false };
    private float[] detector_switch_percentage = new float[2] { 0.0f, 0.0f };
    private Coroutine flashing_animation_coroutine = null;
    private Coroutine[] detector_switch_coroutines = new Coroutine[] { null, null };

    private List<string> ray_targets = new List<string> { "torpedo_detector", "phaser_detector" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAMES[0], true);
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        hud_info.setButtons(BUTTON_LISTS[0]);
        hud_info.setInfo(INFO_MESSAGE + INFO_MESSAGE_ENDINGS[0]);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index]);
        hud_info.setInfo(INFO_MESSAGE + INFO_MESSAGE_ENDINGS[index]);
        return hud_info;
    }

    private void handlePowerConsumptionChange()
    {
        float consumed_power = 0.0f;
        for (int i = 0; i < 2; i++)
        {
            if (detector_is_enabled[i] == true)
            {
                consumed_power += (MAX_POWER_CONSUMPTION / 2.0f);
            }
        }
        ReferenceAssistor.Instance.power_manager.controlPowerChange(1, this.GetType().Name, consumed_power);
        hud_info.setPowerConsumption(consumed_power);
    }

    //resets threat detection to default (none)
    public void resetToDefault()
    {
        for (int i = 0; i < 2; i++)
        {
            threat_detected[i] = false;
        }
    }

    //used when a torpedo is imminently being fired on the ship or no longer targeted
    public void adjustTorpedoWarning(bool alert)
    {
        threat_detected[0] = alert;
        startThreatAlertSound();
    }

    //used when phasers are imminently being fired on the ship or no longer targeted
    public void adjustPhaserWarning(bool alert)
    {
        threat_detected[1] = alert;
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
            if (threat_detected[i] == true && detector_switch_percentage[i] == 1.0f)
            {
                threat_detector_alert_sound.Play();
                return;
            }
        }
    }

    //helper method for screen animation
    private void displayFlashAnimationAdjustment(int index, bool active, float norm_a, float alert_a)
    {
        bool alert = threat_detected[index];
        if (active == false)
        {
            alert = false;
            norm_a = 0.2f;
        }

        if (alert == true)
        {
            foreach (Transform t in threat_detectors_display.transform.GetChild(index))
            {
                t.GetComponent<UnityEngine.UI.RawImage>().color = new Color(RED.r, RED.g, RED.b, alert_a);
            }
        }
        else
        {
            foreach (Transform t in threat_detectors_display.transform.GetChild(index))
            {
                t.GetComponent<UnityEngine.UI.RawImage>().color = new Color(BLUE.r, BLUE.g, BLUE.b, norm_a);
            }
        }
    }

    //handles flashing animation on screen
    IEnumerator flashAnimation()
    {
        float normal_alpha = 0.0f;
        float alert_alpha = 0.0f;
        float normal_elapsed_time = 0.0f;
        float alert_elapsed_time = 0.0f;
        while (true)
        {
            normal_elapsed_time += (Time.deltaTime * FLASH_ANIMATION_SPEED);
            alert_elapsed_time += (Time.deltaTime * FLASH_ANIMATION_SPEED * 6.0f);
            normal_alpha = Mathf.Lerp(0.2f, 1.0f, Mathf.PingPong(normal_elapsed_time, 1.0f));
            alert_alpha = Mathf.Lerp(0.2f, 1.0f, Mathf.PingPong(alert_elapsed_time, 1.0f));

            for (int i = 0; i < 2; i++)
            {
                displayFlashAnimationAdjustment(i, detector_switch_percentage[i] == 1.0f, normal_alpha, alert_alpha);
            }

            yield return null;
        }
    }

    //helper method that disables the flash animation
    private void disableFlashAnimation()
    {
        StopCoroutine(flashing_animation_coroutine);
        flashing_animation_coroutine = null;
        threat_detector_alert_sound.Stop();
    }

    IEnumerator switchDetector(int index, bool to_switch_to)
    {
        GameObject current_switch = detector_switches.transform.GetChild(index).gameObject;
        float starting_switch_rotation = current_switch.transform.localRotation.eulerAngles.z;
        float desired_switch_rotation = 90.0f;

        detector_is_enabled[index] = to_switch_to;
        handlePowerConsumptionChange();

        if (to_switch_to == true)
        {
            desired_switch_rotation = 180.0f;
        }
        else
        {
            if (flashing_animation_coroutine != null)
            {
                //stop sound if no active threat detection
                if ((detector_is_enabled[0] == false || threat_detected[0] == false) && (detector_is_enabled[1] == false || threat_detected[1] == false))
                {
                    threat_detector_alert_sound.Stop();
                }
                //end flash animation if neither are flashing
                if (detector_is_enabled[0] == false && detector_is_enabled[1] == false)
                {
                    disableFlashAnimation();
                    displayFlashAnimationAdjustment(index, false, 0.0f, 0.0f);
                }
            }
        }

        float anim_time = SWITCH_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            detector_switch_percentage[index] = anim_time / SWITCH_TIME;
            if (to_switch_to == true)
            {
                detector_switch_percentage[index] = 1.0f - detector_switch_percentage[index];
            }

            //turn switch
            current_switch.transform.localRotation =
                Quaternion.Euler(248.0f, 0.0f, Mathf.Lerp(starting_switch_rotation, desired_switch_rotation, 1.0f - (anim_time / SWITCH_TIME)));

            yield return null;
        }

        if (to_switch_to == true)
        {
            BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[1]);
            if (flashing_animation_coroutine == null)
            {
                flashing_animation_coroutine = StartCoroutine(flashAnimation());
            }
            //start sound if active threat detection
            startThreatAlertSound();
        }
        else
        {
            BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[0]);
        }

        BUTTON_LISTS[index][0].updateInteractable(is_powered);

        detector_switch_coroutines[index] = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int index = ray_targets.IndexOf(current_target.name);

        if (detector_switch_coroutines[index] == null && is_powered == true)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTON_LISTS[index][0].toggle(0.2f);
                BUTTON_LISTS[index][0].updateInteractable(false);
                transmitThreatDetectorSwitchRPC(index, detector_is_enabled[index]);
            }
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float[] starting_rotations = new float[2] { 0.0f, 0.0f };
        for (int i = 0; i < 2; i++)
        {
            starting_rotations[i] = Mathf.Lerp(90.0f, 180.0f, detector_switch_percentage[i]);
         
            if (detector_switch_coroutines[i] != null)
            {
                StopCoroutine(detector_switch_coroutines[i]);
                detector_switch_coroutines[i] = null;
            }
            BUTTON_LISTS[i][0].updateDesc(CONTROL_DESCS[0]);
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][0].untoggle();
            detector_is_enabled[i] = false;
            detector_switch_percentage[i] = 0.0f;
            displayFlashAnimationAdjustment(i, false, 0.0f, 0.0f);
        }

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            //turn switches
            for (int i = 0; i < 2; i++)
            {
                detector_switches.transform.GetChild(i).localRotation =
                    Quaternion.Euler(248.0f, 0.0f, Mathf.Lerp(starting_rotations[i], 90.0f, 1.0f - (anim_time / power_off_time)));
            }

            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int pos)
    {
        is_powered = true;
        threat_detectors_display.SetActive(true);
        for (int i = 0; i < 2; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(true);
        }
    }

    public void powerOff(int pos, float time)
    {
        is_powered = false;
        threat_detectors_display.SetActive(false);
        hud_info.setPowerConsumption(0.0f);

        if (flashing_animation_coroutine != null)
        {
            disableFlashAnimation();
        }

        //turn off both detectors
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitThreatDetectorSwitchRPC(int index, bool is_enabled)
    {
        detector_is_enabled[index] = is_enabled;
        if (detector_switch_coroutines[index] != null)
        {
            StopCoroutine(detector_switch_coroutines[index]);
        }
        detector_switch_coroutines[index] = StartCoroutine(switchDetector(index, !is_enabled));
    }
}