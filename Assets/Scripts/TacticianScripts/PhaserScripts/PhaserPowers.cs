/*
    PhaserPowers.cs
    - Determines whether phasers are enabled or not
    Contributor(s): Jake Schott
    Last Updated: 6/30/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PhaserPowers : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.2f;
    private static float ENABLE_TIME = 1.0f;

    private string CONTROL_NAME = "PHASER POWER SWITCHES";
    private List<string> CONTROL_DESCS = new List<string> {"LONG-RANGE", "SHORT-RANGE LEFT", "SHORT-RANGE RIGHT"};
    private List<int> CONTROL_INDEXES = new List<int>() {7, 8, 9};
    private List<Button> BUTTONS = new List<Button>();

    public List<GameObject> phaser_switches = null;
    public List<GameObject> phaser_coverups = null;
    public GameObject phaser_switch_canvas;
    private PhaserTemperatures phaser_temperatures;

    private Coroutine[] phaser_switch_coroutines = {null, null, null};
    private bool[] phaser_is_enabled = {false, false, false};

    private static HUDInfo hud_info = null;
    private void Start()
    {
        phaser_temperatures = transform.GetComponent<PhaserTemperatures>();

        hud_info = new HUDInfo(CONTROL_NAME);

        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], true, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], true, true));

        hud_info.setButtons(BUTTONS);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
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

            float lever_angle = Mathf.Lerp(-112f, -68f, switch_time / SWITCH_TIME);
            float charge_fill = charge_time / ENABLE_TIME;
            if (increasing == true)
            {
                lever_angle = Mathf.Lerp(-112f, -68f, 1.0f - (switch_time / SWITCH_TIME));
                charge_fill = 1.0f - (charge_time / ENABLE_TIME);
            }

            phaser_switch_canvas.transform.GetChild(2 + (2 * index)).gameObject.GetComponent<UnityEngine.UI.Image>().fillAmount = charge_fill;
            phaser_switches[index].transform.localRotation =
                Quaternion.Euler(lever_angle, 
                                 0.0f,
                                 90.0f);

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
        }

        BUTTONS[index].updateInteractable(true);

        phaser_switch_coroutines[index] = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        for (int i = 0; i <= 2; i++)
        {
            if (phaser_switch_coroutines[i] == null)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    BUTTONS[i].toggle(0.2f);
                    transmitPhaserPowerRPC(i, phaser_is_enabled[i]);
                }
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
