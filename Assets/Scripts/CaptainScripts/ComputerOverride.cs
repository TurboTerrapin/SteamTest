/*
    ComputerOverride.cs
    - Handles color switches in captain position
    Contributor(s): Jake Schott
    Last Updated: 1/12/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ComputerOverride : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.2f; //how long the switch takes to be flipped
    private static float COOLDOWN_TIME = 0.2f; //how long until it can be switched again after being switched
    private static string[] COLOR_NAMES = { "RED", "YELLOW", "DARK BLUE", "WHITE", "LIGHT BLUE", "GREEN", "PURPLE", "ORANGE" };
    private static float MAX_POWER_CONSUMPTION = 0.2f; //equates to 2 circles

    private string CONTROL_NAME = "OVERRIDE SWITCH ";
    private static string INFO_MESSAGE = "Enables/disables computer override based on corresponding color for internal operations.";
    private List<string> CONTROL_DESCS = new List<string>() { "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[8];

    public List<GameObject> override_displays = null;
    public GameObject override_switches;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private bool[] enabled_overrides = new bool[8] { false, false, false, false, false, false, false, false };
    private Coroutine[] override_switch_coroutines = new Coroutine[8] { null, null, null, null, null, null, null, null };

    private List<string> ray_targets = new List<string> { "override_switch_a1", "override_switch_a2", "override_switch_a3", "override_switch_a4", "override_switch_b1", "override_switch_b2", "override_switch_b3", "override_switch_b4" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME + COLOR_NAMES[0], true);

        for (int i = 0; i < 8; i++)
        {
            BUTTON_LISTS[i] = new List<Button>();
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        }

        hud_info.setButtons(BUTTON_LISTS[0], 6);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAME + COLOR_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 6);

        return hud_info;
    }

    private void handlePowerConsumptionChange()
    {
        float consumed_power = 0.0f;
        for (int i = 0; i < 8; i++)
        {
            if (enabled_overrides[i] == true)
            {
                consumed_power += (MAX_POWER_CONSUMPTION / 8);
            }
        }
        transform.GetComponent<PowerControl>().power_manager.controlPowerChange(3, this.GetType().Name, consumed_power);
        hud_info.setPowerConsumption(consumed_power);
    }

    private void displayAdjustment(int index)
    {
        //adjust corresponding color indicator
        Color c = override_displays[index / 4].transform.GetChild(index % 4).GetComponent<UnityEngine.UI.RawImage>().color;
        c.a = 0.2f;
        if (enabled_overrides[index] == true)
        {
            c.a = 1.0f;
        }
        override_displays[index / 4].transform.GetChild(index % 4).GetComponent<UnityEngine.UI.RawImage>().color = c;
        override_displays[index / 4].transform.GetChild(index % 4).GetChild(1).gameObject.SetActive(enabled_overrides[index]);
    }

    IEnumerator overrideChange(int override_to_change)
    {
        bool disabling = enabled_overrides[override_to_change];
        
        if (disabling == true)
        {
            enabled_overrides[override_to_change] = false; //disable override
            handlePowerConsumptionChange();
            displayAdjustment(override_to_change);
        }

        float anim_time = SWITCH_TIME;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            float switch_percentage = anim_time / SWITCH_TIME;
            if (disabling == false)
            {
                switch_percentage = 1.0f - switch_percentage;
            }

            override_switches.transform.GetChild(override_to_change).localRotation =
                Quaternion.Euler(Mathf.Lerp(320.0f, 260.0f, switch_percentage), -90.0f, 180.0f);

            yield return null;
        }
        BUTTON_LISTS[override_to_change][0].untoggle();

        if (disabling == false)
        {
            enabled_overrides[override_to_change] = true; //enable override
            handlePowerConsumptionChange();
            displayAdjustment(override_to_change);
        }

        yield return new WaitForSeconds(COOLDOWN_TIME);

        if (disabling == false)
        {
            BUTTON_LISTS[override_to_change][0].updateDesc(CONTROL_DESCS[1]);
        }
        else
        {
            BUTTON_LISTS[override_to_change][0].updateDesc(CONTROL_DESCS[0]);
        }
        BUTTON_LISTS[override_to_change][0].updateInteractable(is_powered);

        override_switch_coroutines[override_to_change] = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int index = ray_targets.IndexOf(current_target.name);

        if (override_switch_coroutines[index] == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTON_LISTS[index][0].toggle();
                transmitOverrideChangeRPC(index, enabled_overrides[index]);
            }
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float[] starting_rotations = new float[8] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
        for (int i = 0; i < 8; i++)
        {
            if (override_switch_coroutines[i] != null)
            {
                StopCoroutine(override_switch_coroutines[i]);
                override_switch_coroutines[i] = null;
            }
            enabled_overrides[i] = false;
            displayAdjustment(i);

            BUTTON_LISTS[i][0].updateDesc(CONTROL_DESCS[0]);
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][0].untoggle();

            starting_rotations[i] = override_switches.transform.GetChild(i).localRotation.eulerAngles.x;
        }

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            //turn switches
            for (int i = 0; i < 8; i++)
            {
                override_switches.transform.GetChild(i).localRotation =
                    Quaternion.Euler(Mathf.Lerp(starting_rotations[i], 320.0f, 1.0f - (anim_time / power_off_time)), -90.0f, 180.0f);
            }

            yield return null;
        }

        power_loss_coroutine = null;
    }


    public void powerOn(int position)
    {
        is_powered = true;
        override_displays[0].SetActive(true);
        override_displays[1].SetActive(true);
        for (int i = 0; i < 8; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(true);
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        override_displays[0].SetActive(false);
        override_displays[1].SetActive(false);
        hud_info.setPowerConsumption(0.0f);

        //return override switches to off
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitOverrideChangeRPC(int index, bool is_enabled)
    {
        enabled_overrides[index] = is_enabled;
        if (override_switch_coroutines[index] != null)
        {
            StopCoroutine(override_switch_coroutines[index]);
        }
        override_switch_coroutines[index] = StartCoroutine(overrideChange(index));
    }
}