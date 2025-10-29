/*
    EmergencyLights.cs
    - Handles inputs for emergency lights
    - Moves slider
    - Enables/disables emergency lights using LightsManager
    Contributor(s): Jake Schott
    Last Updated: 10/23/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class EmergencyLights : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 1.25f;
    private static float MAX_POWER_CONSUMPTION = 0.2f; //equates to 2 circles

    private string CONTROL_NAME = "EMERGENCY LIGHTS";
    private static string INFO_MESSAGE = "Enables/disables the emergency lights. Useful in situations where the main lights are malfunctioning.";
    private List<string> CONTROL_DESCS = new List<string> { "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject slider;
    public GameObject display_canvas; //used to display the bars beneath the handle
    public LightsManager lights_manager;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private bool emergency_lights_enabled = false;
    private Coroutine emergency_lights_switch_coroutine = null;
    private Vector3 initial_pos; //handle starting position (disabled)
    private Vector3 final_pos = new Vector3(0.0334f, 0.01277f, 0.0f); //handle final position (enabled)

    private static HUDInfo hud_info = null;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME, true);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true)); //enable button
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);

        initial_pos = slider.transform.localPosition; //sets the initial position
        final_pos = initial_pos + final_pos;
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    public bool getEmergencyLightsEnabled()
    {
        return emergency_lights_enabled;
    }
    private void displayAdjustment(float fill_percentage)
    {
        //update screen
        display_canvas.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount = fill_percentage;
    }

    IEnumerator lightSwitch()
    {
        bool enabling = !emergency_lights_enabled;
        if (enabling == false)
        {
            emergency_lights_enabled = false;
            transform.GetComponent<PowerControl>().power_manager.controlPowerChange(3, this.GetType().Name, 0.0f);
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

            slider.transform.localPosition =
                new Vector3(Mathf.Lerp(initial_pos.x, final_pos.x, switch_percentage),
                            Mathf.Lerp(initial_pos.y, final_pos.y, switch_percentage),
                            Mathf.Lerp(initial_pos.z, final_pos.z, switch_percentage));

            displayAdjustment(switch_percentage);

            yield return null;
        }

        if (enabling == true)
        {
            emergency_lights_enabled = true;
            transform.GetComponent<PowerControl>().power_manager.controlPowerChange(3, this.GetType().Name, MAX_POWER_CONSUMPTION);
            hud_info.setPowerConsumption(MAX_POWER_CONSUMPTION);
            BUTTONS[0].updateDesc(CONTROL_DESCS[1]);
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
        displayAdjustment(0.0f);
        Vector3 start_pos = slider.transform.localPosition;
        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            slider.transform.localPosition = Vector3.Lerp(start_pos, initial_pos, 1.0f - (anim_time / power_off_time));
            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        BUTTONS[0].updateInteractable(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
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

        //turn off lights
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

        if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //click to enable/disable
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

        if (emergency_lights_enabled == false)
        {
            lights_manager.enableEmergencyLights();
        }
        else
        {
            lights_manager.disableEmergencyLights();
        }

        emergency_lights_switch_coroutine = StartCoroutine(lightSwitch());
    }
}
