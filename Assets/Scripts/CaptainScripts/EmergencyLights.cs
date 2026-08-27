/*
    EmergencyLights.cs
    - Handles inputs for emergency lights
    - Moves slider
    - Enables/disables emergency lights using LightsManager
    Contributor(s): Jake Schott
    Last Updated: 8/25/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class EmergencyLights : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 1.0f;
    private static float MAX_POWER_CONSUMPTION = 0.2f; //equates to 2 circles

    private string CONTROL_NAME = "EMERGENCY LIGHTS";
    private static string INFO_MESSAGE = "Enables/disables the emergency lights. Useful in situations where the main lights are malfunctioning.";
    private List<string> CONTROL_DESCS = new List<string> { "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject emergency_lights_dial;
    public GameObject emergency_lights_display;
    public LightsManager lights_manager;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private bool emergency_lights_enabled = false;
    private Coroutine emergency_lights_switch_coroutine = null;

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public GameObject ik_target = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME, MAX_POWER_CONSUMPTION);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true)); //enable button
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }
    public Transform getIKTarget(GameObject current_target)
    {
        return ik_target.transform;
    }
    public AnimatorHandler.HandInteractionType getHandInteractionType()
    {
        return hand_interaction_type;
    }
    public float getHandPose()
    {
        return hand_pose;
    }
    public bool getRightHandFlip()
    {
        return does_right_hand_flip;
    }
    
    public Vector3 getRightHandOffset()
    {
        return right_hand_offset;
    }
    public float getLerpSpeed()
    {
        return lerp_speed;
    }
    public bool getEmergencyLightsEnabled()
    {
        return emergency_lights_enabled;
    }

    private void displayAdjustment(float fill_percentage)
    {
        //update switch light
        if (emergency_lights_enabled == true)
        {
            emergency_lights_dial.transform.GetChild(0).GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_neon;
        }
        else
        {
            emergency_lights_dial.transform.GetChild(0).GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_neon;
        }

        //update display
        Color c = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        if (fill_percentage == 0.0f)
        {
            c.a = 0.2f;
        }
        for (int i = 0; i < 4; i++)
        {
            emergency_lights_display.transform.GetChild(i).GetChild(0).gameObject.SetActive((i / 4.0f) >= (fill_percentage));
            emergency_lights_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = c;
        }
    }

    IEnumerator lightSwitch()
    {
        bool enabling = !emergency_lights_enabled;
        if (enabling == false)
        {
            emergency_lights_enabled = false;
            ReferenceAssistor.Instance.power_manager.controlPowerChange(3, this.GetType().Name, 0.0f);
            hud_info.setPowerConsumption(0.0f);
        }

        float anim_time = SWITCH_TIME;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            float switch_percentage = anim_time / SWITCH_TIME;
            if (enabling == true)
            {
                switch_percentage = 1.0f - switch_percentage;
            }

            emergency_lights_dial.transform.localRotation =
                Quaternion.Euler(emergency_lights_dial.transform.localEulerAngles.x, emergency_lights_dial.transform.localEulerAngles.y, Mathf.Lerp(0.0f, 90.0f, switch_percentage));

            displayAdjustment(switch_percentage);

            yield return null;
        }

        if (enabling == true)
        {
            emergency_lights_enabled = true;
            ReferenceAssistor.Instance.power_manager.controlPowerChange(3, this.GetType().Name, MAX_POWER_CONSUMPTION);
            hud_info.setPowerConsumption(MAX_POWER_CONSUMPTION);
            BUTTONS[0].updateDesc(CONTROL_DESCS[1]);
            displayAdjustment(1.0f);
        }
        else
        {
            BUTTONS[0].updateDesc(CONTROL_DESCS[0]);
        }
        BUTTONS[0].updateInteractable(is_powered);

        emergency_lights_switch_coroutine = null;
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float starting_rotation = emergency_lights_dial.transform.localRotation.eulerAngles.z;
        displayAdjustment(0.0f);
        lights_manager.setEmergencyLights(false);

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            emergency_lights_dial.transform.localRotation =
                Quaternion.Euler(emergency_lights_dial.transform.localEulerAngles.x, 
                                 emergency_lights_dial.transform.localEulerAngles.y, 
                                 Mathf.Lerp(starting_rotation, 0.0f, 1.0f - (anim_time / power_off_time)));

            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        emergency_lights_display.SetActive(true);
        BUTTONS[0].updateInteractable(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        emergency_lights_display.SetActive(false);
        emergency_lights_enabled = false;
        BUTTONS[0].updateInteractable(false);
        BUTTONS[0].untoggle();
        BUTTONS[0].updateDesc(CONTROL_DESCS[0]);  
        if (emergency_lights_switch_coroutine != null)
        {
            StopCoroutine(emergency_lights_switch_coroutine);
            emergency_lights_switch_coroutine = null;
        }
        hud_info.setPowerConsumption(0.0f);

        //return switch to default, turn off lights
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //click to enable/disable
        {
            if (emergency_lights_switch_coroutine == null)
            {
                BUTTONS[0].toggle(0.2f);
                BUTTONS[0].updateInteractable(false);
                transmitEmergencyLightAdjustmentRPC(emergency_lights_enabled);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEmergencyLightAdjustmentRPC(bool el)
    {
        emergency_lights_enabled = el;
        if (emergency_lights_switch_coroutine != null)
        {
            StopCoroutine(emergency_lights_switch_coroutine);
        }

        lights_manager.setEmergencyLights(!emergency_lights_enabled);

        emergency_lights_switch_coroutine = StartCoroutine(lightSwitch());
    }
}