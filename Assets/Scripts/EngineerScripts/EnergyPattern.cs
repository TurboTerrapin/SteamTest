/*
    EnergyPattern.cs
    - Handles enabling/disabling energy pattern display
    - Handles shifting between ship/probe/tractor beam configuration
    Contributor(s): Jake Schott
    Last Updated: 9/1/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnergyPattern : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 1.0f; //how long it takes to turn on/off the energy pattern display
    private static float SHIFT_TIME = 0.5f; //how long it takes to shift between configurations
    private static float MAX_POWER_CONSUMPTION = 0.5f; //equates to 5 circles

    private string[] CONTROL_NAMES = { "ENERGY PATTERN POWER", "ENERGY PATTERN SHIFTER" };
    private List<string> CONTROL_DESCS = new List<string>() { "ENABLE", "DISABLE", "SHIFT DOWN", "SHIFT UP" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6, 2, 0 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public GameObject energy_pattern_dial;
    public GameObject energy_pattern_slider;
    public GameObject energy_pattern_selection_display;
    public GameObject energy_pattern_indicator_display;

    private EnergyPatternManager energy_pattern_manager;
    private Vector3 energy_pattern_slider_initial_pos;
    private Vector3 energy_pattern_slider_final_pos = new Vector3(7.677f, -0.2442f, -8.2511f);

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private bool display_enabled = false;
    private int currently_viewing = 0; //goes ship/probe/tractor beam
    private Coroutine energy_pattern_power_coroutine = null;
    private Coroutine energy_pattern_shift_coroutine = null;

    private List<string> ray_targets = new List<string> { "energy_pattern_power", "energy_pattern_configuration" };

    private static HUDInfo hud_info = null;
    private void Start()
    {
        energy_pattern_manager = GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EnergyPatternManager>();
        energy_pattern_slider_initial_pos = energy_pattern_slider.transform.localPosition;

        hud_info = new HUDInfo(CONTROL_NAMES[0]);

        //power on list
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        //configuration list
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[1], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[2], false, true));

        hud_info.setButtons(BUTTON_LISTS[0], 6);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);

        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[1], 7);

        if (index == 0)
        {
            hud_info.setButtons(BUTTON_LISTS[0], 6);
        }

        return hud_info;
    }

    private void displayAdjustment()
    {
        energy_pattern_manager.updateDisplay(display_enabled, currently_viewing);
    }

    private void handlePowerConsumptionChange()
    {
        if (display_enabled == true)
        {
            transform.GetComponent<PowerControl>().power_manager.controlPowerChange(2, this.GetType().Name, MAX_POWER_CONSUMPTION);
        }
        else
        {
            transform.GetComponent<PowerControl>().power_manager.controlPowerChange(2, this.GetType().Name, 0.0f);
        }
    }

    IEnumerator powerChange()
    {
        bool enabling = !display_enabled;
        if (enabling == false)
        {
            display_enabled = false;
            handlePowerConsumptionChange();
            displayAdjustment();
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

            energy_pattern_dial.transform.localRotation =
                Quaternion.Euler(energy_pattern_dial.transform.localEulerAngles.x,
                            energy_pattern_dial.transform.localEulerAngles.y,
                            Mathf.Lerp(180.0f, 90.0f, switch_percentage));

            yield return null;
        }

        if (enabling == true)
        {
            display_enabled = true;
            handlePowerConsumptionChange();
            displayAdjustment();
            BUTTON_LISTS[0][0].updateDesc(CONTROL_DESCS[1]);
            BUTTON_LISTS[1][0].updateInteractable(currently_viewing < 2);
            BUTTON_LISTS[1][1].updateInteractable(currently_viewing > 0);
        }
        else
        {
            BUTTON_LISTS[0][0].updateDesc(CONTROL_DESCS[0]);
        }
        BUTTON_LISTS[0][0].updateInteractable(true);

        energy_pattern_power_coroutine = null;
    }

    IEnumerator shiftChange()
    {
        Vector3 start_pos = energy_pattern_slider.transform.localPosition;
        Vector3 dest_pos = Vector3.Lerp(energy_pattern_slider_initial_pos, energy_pattern_slider_final_pos, currently_viewing / 2.0f);
        float anim_time = SHIFT_TIME;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            energy_pattern_slider.transform.localPosition = Vector3.Lerp(start_pos, dest_pos, 1.0f - (anim_time / SHIFT_TIME));

            yield return null;
        }

        displayAdjustment();

        BUTTON_LISTS[1][0].untoggle();
        BUTTON_LISTS[1][1].untoggle();
        BUTTON_LISTS[1][0].updateInteractable(currently_viewing < 2 && display_enabled);
        BUTTON_LISTS[1][1].updateInteractable(currently_viewing > 0 && display_enabled);

        energy_pattern_shift_coroutine = null;
    }

    public bool getDisplayEnabled()
    {
        return display_enabled;
    }

    public int getCurrentlyViewing()
    {
        return currently_viewing;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int target_index = ray_targets.IndexOf(current_target.name);
        if (energy_pattern_power_coroutine == null && energy_pattern_shift_coroutine == null)
        {
            if (target_index == 0) //check power
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
                {
                    BUTTON_LISTS[0][0].toggle(0.2f);
                    BUTTON_LISTS[1][0].updateInteractable(false);
                    BUTTON_LISTS[1][1].updateInteractable(false);
                    transmitEnergyPatternPowerChangeRPC(display_enabled);
                }
            }
            else //check shifter
            {
                if (display_enabled == true)
                {
                    if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs) && currently_viewing < 2)
                    {
                        BUTTON_LISTS[1][0].toggle();
                        BUTTON_LISTS[1][1].updateInteractable(false);
                        transmitEnergyPatternShiftChangeRPC(currently_viewing + 1);
                    }
                    else if (ControlScript.checkInputIndex(CONTROL_INDEXES[2], inputs) && currently_viewing > 0)
                    {
                        BUTTON_LISTS[1][1].toggle();
                        BUTTON_LISTS[1][0].updateInteractable(false);
                        transmitEnergyPatternShiftChangeRPC(currently_viewing - 1);
                    }
                }
            }
        }
    }

    public void resetToDefault()
    {
        currently_viewing = 0;
        energy_pattern_slider.transform.localPosition = energy_pattern_slider_initial_pos;
        displayAdjustment();
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float starting_rotation = energy_pattern_dial.transform.localRotation.eulerAngles.z;

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            energy_pattern_dial.transform.localRotation =
                Quaternion.Euler(energy_pattern_dial.transform.localEulerAngles.x,
                                 energy_pattern_dial.transform.localEulerAngles.y,
                                 Mathf.Lerp(starting_rotation, 180.0f, 1.0f - (anim_time / power_off_time)));

            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        energy_pattern_selection_display.SetActive(true);
        energy_pattern_indicator_display.SetActive(true);
        BUTTON_LISTS[0][0].updateInteractable(true);
        BUTTON_LISTS[1][0].updateInteractable(false);
        BUTTON_LISTS[1][0].updateInteractable(false);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        energy_pattern_selection_display.SetActive(false);
        energy_pattern_indicator_display.SetActive(false);
        display_enabled = false;
        displayAdjustment();
        BUTTON_LISTS[0][0].updateInteractable(false);
        BUTTON_LISTS[0][0].updateDesc(CONTROL_DESCS[0]);
        BUTTON_LISTS[1][0].updateInteractable(false);
        BUTTON_LISTS[1][1].updateInteractable(false);
        if (energy_pattern_power_coroutine != null)
        {
            StopCoroutine(energy_pattern_power_coroutine);
            energy_pattern_power_coroutine = null;
        }

        //return energy pattern dial to off
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEnergyPatternPowerChangeRPC(bool de)
    {
        display_enabled = de;
        if (energy_pattern_power_coroutine != null)
        {
            StopCoroutine(energy_pattern_power_coroutine);
        }
        energy_pattern_power_coroutine = StartCoroutine(powerChange());
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEnergyPatternShiftChangeRPC(int cv)
    {
        currently_viewing = cv;
        if (energy_pattern_shift_coroutine != null)
        {
            StopCoroutine(energy_pattern_shift_coroutine);
        }
        energy_pattern_shift_coroutine = StartCoroutine(shiftChange());
    }
}
