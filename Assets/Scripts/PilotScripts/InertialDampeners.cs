/*
    InertialDampeners.cs
    - Handles inertial dampeners
    - When enabled, increase acceleration rates for thrusters and impulse throttle
    - Each one has an equal, 33% effect on both thrusters and impulse throttle (all three enabled means 100% effect)
    Contributor(s): Jake Schott
    Last Updated: 10/21/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InertialDampeners : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.5f;
    private static float MAX_POWER_CONSUMPTION = 0.3f; //equates to 3 circles (1 per dampener)

    private string[] CONTROL_NAMES = new string[] { "PRIMARY INERTIAL DAMPENER", "SECONDARY INERTIAL DAMPENER", "TERTIARY INERTIAL DAMPENER" };
    private static string INFO_MESSAGE = "Increases ship acceleration for thrusters and impulse throttle. Each dampener contributes 33% to maximum acceleration effect.";
    private List<string> CONTROL_DESCS = new List<string>() { "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject dampener_switches;
    public GameObject dampener_display;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private bool[] dampener_is_enabled = new bool[3] { false, false, false };
    private float[] dampener_enabled_percentage = new float[3] { 0.0f, 0.0f, 0.0f };
    private Coroutine fill_bar_coroutine = null;
    private Coroutine[] dampener_switch_coroutines = new Coroutine[] { null, null, null };

    private List<string> ray_targets = new List<string> { "primary_inertial_dampener", "secondary_inertial_dampener", "tertiary_inertial_dampener" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAMES[0], true);
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[2].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        hud_info.setButtons(BUTTON_LISTS[0], 6);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 6);

        return hud_info;
    }

    private float getFillBarValue()
    {
        float modifier = 0.0f;
        for (int i = 0; i <= 2; i++)
        {
            if (dampener_is_enabled[i] == true)
            {
                modifier += 1.0f;
            }
        }
        return (modifier / 3.0f);
    }

    private float getInertialDampenerModifierValue()
    {
        float modifier = 0.0f;
        for (int i = 0; i < 3; i++)
        {
            if (dampener_enabled_percentage[i] >= 1.0f)
            {
                modifier += 1.0f;
            }
        }
        return (modifier / 3.0f);
    }

    private void adjustInertialDampenerModifiers()
    {
        float modifier = getInertialDampenerModifierValue();
        transform.GetComponent<ImpulseThrottle>().adjustInertialDampenerModifier(modifier);
        transform.GetComponent<HorizontalThrusters>().adjustInertialDampenerModifier(modifier);
        transform.GetComponent<VerticalThrusters>().adjustInertialDampenerModifier(modifier);
    }

    private void handlePowerConsumptionChange()
    {
        float consumed_power = 0.0f;
        for (int i = 0; i < 3; i++)
        {
            if (dampener_is_enabled[i] == true)
            {
                consumed_power += (MAX_POWER_CONSUMPTION / 3.0f);
            }
        }
        transform.GetComponent<PowerControl>().power_manager.controlPowerChange(0, this.GetType().Name, consumed_power);
        hud_info.setPowerConsumption(consumed_power);
    }

    IEnumerator adjustFillBar()
    {
        UnityEngine.UI.Image fill_bar = dampener_display.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>();

        float start_fill = fill_bar.fillAmount;
        float dest_fill = getFillBarValue();

        float anim_time = SWITCH_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            fill_bar.fillAmount = Mathf.Lerp(start_fill, dest_fill, 1.0f - (anim_time / SWITCH_TIME));
            yield return null;
        }

        fill_bar_coroutine = null;
    }

    IEnumerator switchDampener(int index, bool to_switch_to)
    {
        GameObject current_switch = dampener_switches.transform.GetChild(index).gameObject;
        float starting_switch_rotation = current_switch.transform.localRotation.eulerAngles.z;
        float desired_switch_rotation = 90.0f;

        dampener_is_enabled[index] = to_switch_to;
        handlePowerConsumptionChange();

        if (to_switch_to == true)
        {
            desired_switch_rotation = 180.0f;
        }

        if (fill_bar_coroutine != null)
        {
            StopCoroutine(fill_bar_coroutine);
        }
        fill_bar_coroutine = StartCoroutine(adjustFillBar());

        float anim_time = SWITCH_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            dampener_enabled_percentage[index] = anim_time / SWITCH_TIME;
            if (to_switch_to == true)
            {
                dampener_enabled_percentage[index] = 1.0f - dampener_enabled_percentage[index];
            }

            //turn switch
            current_switch.transform.localRotation = 
                Quaternion.Euler(-113.0f, 0.0f, Mathf.Lerp(starting_switch_rotation, desired_switch_rotation, 1.0f - (anim_time / SWITCH_TIME)));

            yield return null;
        }

        adjustInertialDampenerModifiers();

        if (to_switch_to == true)
        {
            BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[1]);
        }
        else
        {
            BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[0]);
        }

        BUTTON_LISTS[index][0].updateInteractable(is_powered);

        dampener_switch_coroutines[index] = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int index = ray_targets.IndexOf(current_target.name);

        if (dampener_switch_coroutines[index] == null && is_powered == true)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTON_LISTS[index][0].toggle(0.2f);
                BUTTON_LISTS[index][0].updateInteractable(false);
                transmitInertialDampenerRPC(index, dampener_is_enabled[index]);
            }
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float[] starting_rotations = new float[3] { 0.0f, 0.0f, 0.0f };
        for (int i = 0; i < 3; i++)
        {
            starting_rotations[i] = Mathf.Lerp(90.0f, 180.0f, dampener_enabled_percentage[i]);

            if (dampener_switch_coroutines[i] != null)
            {
                StopCoroutine(dampener_switch_coroutines[i]);
                dampener_switch_coroutines[i] = null;
            }
            BUTTON_LISTS[i][0].updateDesc(CONTROL_DESCS[0]);
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][0].untoggle();
            dampener_is_enabled[i] = false;
            dampener_enabled_percentage[i] = 0.0f;
        }
        dampener_display.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = 0.0f;

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            //turn switches
            for (int i = 0; i < 3; i++)
            {
                dampener_switches.transform.GetChild(i).localRotation =
                    Quaternion.Euler(-113.0f, 0.0f, Mathf.Lerp(starting_rotations[i], 90.0f, 1.0f - (anim_time / power_off_time)));
            }

            yield return null;
        }

        adjustInertialDampenerModifiers();

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        BUTTON_LISTS[0][0].updateInteractable(true);
        BUTTON_LISTS[1][0].updateInteractable(true);
        BUTTON_LISTS[2][0].updateInteractable(true);
        dampener_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        dampener_display.SetActive(false);
        hud_info.setPowerConsumption(0.0f);

        //turn off all inertial dampeners
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitInertialDampenerRPC(int index, bool is_enabled)
    {
        dampener_is_enabled[index] = is_enabled;
        if (dampener_switch_coroutines[index] != null)
        {
            StopCoroutine(dampener_switch_coroutines[index]);
        }
        dampener_switch_coroutines[index] = StartCoroutine(switchDampener(index, !is_enabled));
    }
}