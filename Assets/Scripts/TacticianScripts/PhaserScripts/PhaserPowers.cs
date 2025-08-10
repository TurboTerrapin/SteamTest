/*
    PhaserPowers.cs
    - Determines whether phasers are enabled or not
    Contributor(s): Jake Schott
    Last Updated: 7/23/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PhaserPowers : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.2f; //how long it takes for the switch to be flipped
    private static float ENABLE_TIME = 1.0f; //how long it takes for the phaser to charge/uncharge

    private List<string> CONTROL_NAMES = new List<string>() { "LONG-RANGE PHASER", "SHORT-RANGE LEFT PHASER", "SHORT-RANGE RIGHT PHASER" };
    private List<string> CONTROL_DESCS = new List<string> {"ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    public List<GameObject> phaser_switches = null;
    public List<GameObject> phaser_coverups = null;
    public GameObject phaser_switch_canvas;
    private PhaserTemperatures phaser_temperatures;

    private Coroutine[] phaser_switch_coroutines = {null, null, null};
    private bool[] phaser_is_enabled = {false, false, false};

    private List<string> ray_targets = new List<string> { "long_range_power", "short_range_left_power", "short_range_right_power" };

    private static HUDInfo hud_info = null;
    private void Start()
    {
        phaser_temperatures = transform.GetComponent<PhaserTemperatures>();

        hud_info = new HUDInfo(CONTROL_NAMES[0]);

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, true));
        BUTTON_LISTS[2].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, true));

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

    IEnumerator switchPhaser(int index)
    {
        bool increasing = true;

        //disable phasers
        if (phaser_is_enabled[index] == true)
        {
            phaser_coverups[index].SetActive(true);
            phaser_is_enabled[index] = false;
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

            float lever_angle = Mathf.Lerp(-150f, -75f, switch_time / SWITCH_TIME);
            float charge_fill = charge_time / ENABLE_TIME;
            if (increasing == true)
            {
                lever_angle = Mathf.Lerp(-150f, -75f, 1.0f - (switch_time / SWITCH_TIME));
                charge_fill = 1.0f - (charge_time / ENABLE_TIME);
            }

            phaser_switch_canvas.transform.GetChild(2 + (index)).GetChild(0).gameObject.GetComponent<UnityEngine.UI.Image>().fillAmount = charge_fill;
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

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
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
