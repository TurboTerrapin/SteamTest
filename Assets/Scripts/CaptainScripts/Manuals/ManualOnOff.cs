/*
    ManualOnOff.cs
    - Used to turn on and off both manuals
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ManualOnOff : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.5f;
    private static float MAX_POWER_CONSUMPTION = 0.6f; //6 circles, 3 per manual

    private string[] CONTROL_NAMES = new string[] { "SHIP MANUAL", "COMMUNICATIONS MANUAL" };
    public static List<string> INFO_MESSAGES = new List<string>() { "Information resource on general operations (ship functions, procedures, anomaly analysis).", "Information resource on communications (universal communicator and indirect signaling)." };
    private List<string> CONTROL_DESCS = new List<string> { "TURN ON", "TURN OFF" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public List<GameObject> target_colliders = null; //goes ship_manual_on_off, ship_manual_selector, communications_manual_on_off, communications_manual_selector
    public List<GameObject> power_switches = null;
    private float[] power_switch_angles = new float[2] { 295.0f, 295.0f };
    private Component[] manuals = new Component[2];

    private Coroutine[] power_change_coroutine = new Coroutine[] { null, null };

    private List<string> ray_targets = new List<string> { "ship_manual_on_off", "communications_manual_on_off" };

    private static HUDInfo hud_info = null;
    private void Start()
    {
        manuals[0] = GetComponent<ShipManual>();
        manuals[1] = GetComponent<CommunicationsManual>();

        hud_info = new HUDInfo(CONTROL_NAMES[0], true);

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        hud_info.setButtons(BUTTON_LISTS[0], 6);
        hud_info.setInfo(INFO_MESSAGES[0]);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 6);
        hud_info.setInfo(INFO_MESSAGES[index]);
        hud_info.setPowerConsumption(getManualPowerConsumption(index));
        return hud_info;
    }

    public float getManualPowerConsumption(int index)
    {
        float consumed_power = 0.0f;
        Manual m = (Manual)manuals[index];
        if (m.getCurrentlyEnabled() == true || m.getCurrentlyAnimating() == true)
        {
            consumed_power += (MAX_POWER_CONSUMPTION / 2);
        }
        return consumed_power;
    }

    private void handlePowerConsumptionChange()
    {
        float consumed_power = 0.0f;
        for (int i = 0; i < 2; i++)
        {
            consumed_power += getManualPowerConsumption(i);
        }
        ReferenceAssistor.Instance.power_manager.controlPowerChange(3, this.GetType().Name, consumed_power);
    }

    public void reactivate(int index)
    {
        Manual curr_manual = (Manual)manuals[index];
        bool ce = curr_manual.getCurrentlyEnabled();
        if (ce == true)
        {
            BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[1]);
        }
        else
        {
            BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[0]);
        }

        BUTTON_LISTS[index][0].updateInteractable(curr_manual.getIsPowered());
    }

    //called by ShipManual, CommunicationsManual
    public void disableManual(int index, float time)
    {
        if (power_change_coroutine[index] != null)
        {
            StopCoroutine(power_change_coroutine[index]);
            power_change_coroutine[index] = null;
        }

        power_change_coroutine[index] = StartCoroutine(switchReturn(index, time));

        BUTTON_LISTS[index][0].updateInteractable(false);
        BUTTON_LISTS[index][0].untoggle();
        BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[0]);

        if (index == 0)
        {
            GetComponent<ShipManual>().powerSwitch(false, 0);
            target_colliders[0].SetActive(true);
            target_colliders[1].SetActive(false);

        }
        else
        {
            GetComponent<CommunicationsManual>().powerSwitch(false);
            target_colliders[2].SetActive(true);
            target_colliders[3].SetActive(false);
        }
    }

    //called by disableManual, returns switch to default position
    IEnumerator switchReturn(int index, float time)
    {
        float switch_time = time;
        float starting_rotation = power_switch_angles[index];

        //flip switch
        while (switch_time > 0)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            switch_time = Mathf.Max(0.0f, switch_time - dt);

            power_switches[index].transform.localRotation =
                Quaternion.Euler(Mathf.Lerp(starting_rotation, 295.0f, 1.0f - (switch_time / time)), 0f, 90f);

            yield return null;
        }

        power_switch_angles[index] = 295.0f;

        power_change_coroutine[index] = null;
    }

    IEnumerator powerChangeAdjustment(bool to_switch_to, int msg, int manual_index)
    {
        float switch_time = SWITCH_TIME;

        //flip switch
        while (switch_time > 0)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            switch_time = Mathf.Max(0.0f, switch_time - dt);

            float lever_angle = Mathf.Lerp(295f, 245f, switch_time / SWITCH_TIME);

            if (to_switch_to == true)
            {
                lever_angle = Mathf.Lerp(295f, 245f, 1.0f - (switch_time / SWITCH_TIME));
            }

            power_switch_angles[manual_index] = lever_angle;
            power_switches[manual_index].transform.localRotation =
                Quaternion.Euler(lever_angle, 0f, 90f);


            yield return null;
        }

        if (manual_index == 0) //ShipManual
        {
            GetComponent<ShipManual>().powerSwitch(to_switch_to, msg);
            target_colliders[0].SetActive(!to_switch_to);
            target_colliders[1].SetActive(to_switch_to);
        }
        else //CommunicationsManual
        {
            GetComponent<CommunicationsManual>().powerSwitch(to_switch_to);
            target_colliders[2].SetActive(!to_switch_to);
            target_colliders[3].SetActive(to_switch_to);
        }

        handlePowerConsumptionChange();

        power_change_coroutine[manual_index] = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int manual_index = ray_targets.IndexOf(current_target.name);

        if (power_change_coroutine[manual_index] == null)
        {
            Manual curr_manual = (Manual)manuals[manual_index];
            
            if (curr_manual.getIsPowered() == false)
            {
                return;
            }

            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && curr_manual.getCurrentlyAnimating() == false)
            {
                BUTTON_LISTS[manual_index][0].toggle(0.2f);
                int welcome_message = transform.GetComponent<ShipManual>().pickWelcomeMessage();
                bool currently_enabled = curr_manual.getCurrentlyEnabled();

                transmitManualPowerChangeRPC(!currently_enabled, welcome_message, manual_index);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitManualPowerChangeRPC(bool to_switch_to, int msg, int manual_index)
    {
        if (power_change_coroutine[manual_index] != null)
        {
            StopCoroutine(power_change_coroutine[manual_index]);
        }
        power_change_coroutine[manual_index] = StartCoroutine(powerChangeAdjustment(to_switch_to, msg, manual_index));
    }
}