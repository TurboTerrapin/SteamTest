/*
    PhaserPowers.cs
    - Determines whether phasers are enabled or not
    Contributor(s): Jake Schott
    Last Updated: 9/1/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PhaserPowers : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.2f; //how long it takes for the switch to be flipped
    private static float ENABLE_TIME = 1.0f; //how long it takes for the phaser to charge/uncharge
    private static float MAX_POWER_CONSUMPTION = 0.3f; //equates to 3 circles

    private List<string> CONTROL_NAMES = new List<string>() { "LONG-RANGE PHASER", "SHORT-RANGE LEFT PHASER", "SHORT-RANGE RIGHT PHASER" };
    private List<string> CONTROL_DESCS = new List<string> {"ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    public List<GameObject> phaser_switches = null;
    public List<GameObject> phaser_coverups = null;
    public GameObject phaser_switch_display;
    private PhaserTemperatures phaser_temperatures;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine;
    private Coroutine[] phaser_switch_coroutines = {null, null, null};
    private bool[] phaser_is_enabled = {false, false, false};

    private List<string> ray_targets = new List<string> { "long_range_power", "short_range_left_power", "short_range_right_power" };

    private static HUDInfo hud_info = null;
    private void Start()
    {
        phaser_temperatures = transform.GetComponent<PhaserTemperatures>();

        hud_info = new HUDInfo(CONTROL_NAMES[0]);

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[2].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        hud_info.setButtons(BUTTON_LISTS[0], 6);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);

        if (index != 0)
        {
            hud_info.setButtons(BUTTON_LISTS[index], 6);
        }
        else
        {
            hud_info.setButtons(BUTTON_LISTS[index]);
        }

        return hud_info;
    }

    public bool[] getActivePhasers()
    {
        return phaser_is_enabled;
    }

    private void handlePowerConsumptionChange()
    {
        float consumed_power = 0.0f;
        for (int i = 0; i < 3; i++)
        {
            if (phaser_is_enabled[i] == true)
            {
                consumed_power += (MAX_POWER_CONSUMPTION / 3);
            }
        }
        transform.GetComponent<PowerControl>().power_manager.controlPowerChange(1, this.GetType().Name, consumed_power);
    }

    IEnumerator switchPhaser(int index)
    {
        bool increasing = true;

        //disable phasers
        if (phaser_is_enabled[index] == true)
        {
            phaser_coverups[index].SetActive(true);
            phaser_is_enabled[index] = false;
            handlePowerConsumptionChange();
            increasing = false;
            if (index == 0)
            {
                phaser_temperatures.changeInPower(0, false);
            }
            else
            {
                phaser_temperatures.changeInPower(index, phaser_is_enabled[1] == true || phaser_is_enabled[2] == true);
            }
        }

        float switch_time = SWITCH_TIME;
        float charge_time = ENABLE_TIME;

        //flip switch, fill meter
        while (charge_time > 0)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            charge_time = Mathf.Max(0.0f, charge_time - dt);
            switch_time = Mathf.Max(0.0f, switch_time - dt);

            float lever_angle = Mathf.Lerp(210f, 285f, switch_time / SWITCH_TIME);
            float charge_fill = charge_time / ENABLE_TIME;
            if (increasing == true)
            {
                lever_angle = Mathf.Lerp(210f, 285f, 1.0f - (switch_time / SWITCH_TIME));
                charge_fill = 1.0f - (charge_time / ENABLE_TIME);
            }

            phaser_switch_display.transform.GetChild(1 + (index)).GetChild(0).gameObject.GetComponent<UnityEngine.UI.Image>().fillAmount = charge_fill;
            phaser_switches[index].transform.localRotation =
                Quaternion.Euler(lever_angle, 
                                 0.0f,
                                 0.0f);

            if (switch_time <= 0.0f)
            {
                BUTTON_LISTS[index][0].untoggle();
            }

            yield return null;
        }

        //enable phasers
        if (increasing == true)
        {
            phaser_coverups[index].SetActive(false);
            phaser_is_enabled[index] = true;
            handlePowerConsumptionChange();
            if (index == 0)
            {
                phaser_temperatures.changeInPower(0, true);
            }
            else
            {
                phaser_temperatures.changeInPower(1, true);
            }
            BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[1]);
        }
        else 
        {
            BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[0]);
        }
        BUTTON_LISTS[index][0].updateInteractable(true);

        phaser_switch_coroutines[index] = null;
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float[] starting_rotations = new float[3] { 0.0f, 0.0f, 0.0f };
        for (int i = 0; i < 3; i++)
        {
            if (phaser_switch_coroutines[i] != null)
            {
                StopCoroutine(phaser_switch_coroutines[i]);
                phaser_switch_coroutines[i] = null;
            }
            phaser_switch_display.transform.GetChild(1 + i).GetChild(0).gameObject.GetComponent<UnityEngine.UI.Image>().fillAmount = 0.0f;
            phaser_is_enabled[i] = false;

            BUTTON_LISTS[i][0].updateDesc(CONTROL_DESCS[0]);
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][0].untoggle();

            starting_rotations[i] = phaser_switches[i].transform.localRotation.eulerAngles.x;
        }

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            //turn switches
            for (int i = 0; i < 3; i++)
            {
                phaser_switches[i].transform.localRotation =
                    Quaternion.Euler(Mathf.Lerp(starting_rotations[i], 210.0f, 1.0f - (anim_time / power_off_time)),
                                     0.0f,
                                     0.0f);
            }

            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        phaser_switch_display.SetActive(true);
        BUTTON_LISTS[0][0].updateInteractable(true);
        BUTTON_LISTS[1][0].updateInteractable(true);
        BUTTON_LISTS[2][0].updateInteractable(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        phaser_switch_display.SetActive(false);

        //return phasers to 0
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

        int index = ray_targets.IndexOf(current_target.name);

        if (phaser_switch_coroutines[index] == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTON_LISTS[index][0].toggle();
                transmitPhaserPowerRPC(index, phaser_is_enabled[index]);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitPhaserPowerRPC(int index, bool is_enabled)
    {
        phaser_is_enabled[index] = is_enabled;
        if (phaser_switch_coroutines[index] != null)
        {
            StopCoroutine(phaser_switch_coroutines[index]);
        }
        phaser_switch_coroutines[index] = StartCoroutine(switchPhaser(index));
    }
}
