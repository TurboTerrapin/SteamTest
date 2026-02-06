/*
    TorpedoPowers.cs
    - Moves torpedo power levers
    - Adjusts torpedo power screens
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TorpedoPowers : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 0.35f;
    private static float MAX_POWER_CONSUMPTION = 0.4f; //0.4 means 4 circles
    private static Vector3 FINAL_LEVER_DIRECTION = new Vector3(0.0842f, 0.0308f, 0f); //handle final position (100% power)

    private string[] CONTROL_NAMES = new string[] { "FORWARD TORPEDO POWER", "PORT TORPEDO POWER", "STARBOARD TORPEDO POWER", "AFT TORPEDO POWER" };
    private static string INFO_MESSAGE = "Handles power control on the corresponding torpedo bay. Greater power improves damage capability.";
    private List<string> CONTROL_DESCS = new List<string> { "REDUCE", "ENERGIZE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[4] { new List<Button>(), new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject torpedo_power_levers;
    public GameObject torpedo_power_glasses;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private float[] power_levels = new float[] { 0.0f, 0.0f, 0.0f, 0.0f };
    private Vector3[] initial_positions = new Vector3[4]; //handle starting position (0% power)
    private Vector3[] final_positions = new Vector3[4]; //handle starting position (0% power)

    private List<string> ray_targets = new List<string> { "forward_torpedo_power", "port_torpedo_power", "starboard_torpedo_power", "aft_torpedo_power" };

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAMES[0], true);

        for (int i = 0; i <= 3; i++)
        {
            //set buttons
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false)); //increase button

            //set positions
            initial_positions[i] = torpedo_power_levers.transform.GetChild(i).localPosition;
            final_positions[i] = torpedo_power_levers.transform.GetChild(i).localPosition + FINAL_LEVER_DIRECTION;
        }

        hud_info.setButtons(BUTTON_LISTS[0], 7);
        hud_info.setInfo(INFO_MESSAGE);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 7);

        return hud_info;
    }

    private void handlePowerConsumptionChange()
    {
        float consumed_power = 0.0f;
        for (int i = 0; i < 4; i++)
        {
            consumed_power += (power_levels[i] * 0.25f * MAX_POWER_CONSUMPTION);
        }
        ReferenceAssistor.Instance.power_manager.controlPowerChange(1, this.GetType().Name, consumed_power);
        hud_info.setPowerConsumption(consumed_power);
    }

    private void displayAdjustment(int index)
    {
        //move physical lever
        torpedo_power_levers.transform.GetChild(index).localPosition = Vector3.Lerp(initial_positions[index], final_positions[index], power_levels[index]);

        //update bars on screen
        float tmp_pwr = power_levels[index];
        for (int i = 0; i <= 19; i++)
        {
            tmp_pwr = power_levels[index] - (0.05f * i);
            float a = tmp_pwr / 0.05f;
            //do both sides
            for (int x = 1; x <= 2; x++)
            {
                torpedo_power_glasses.transform.GetChild(index).GetChild(x).GetChild(1).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, Mathf.Max(0.04f, a));
            }
        }

        //update text
        torpedo_power_glasses.transform.GetChild(index).GetChild(0).GetChild(1).GetChild(1).GetComponent<TMP_Text>().SetText(Mathf.RoundToInt(power_levels[index] * 100.0f).ToString());
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int index = ray_targets.IndexOf(current_target.name);

        int power_direction = 0;
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs) && power_levels[index] < 1.0f) //E to increment
        {
            power_direction += 1;
        }
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && power_levels[index] > 0.0f)  //Q to decrement
        {
            power_direction -= 1;
        }
        if (power_direction != 0)
        {
            if (power_direction > 0)
            {
                power_levels[index] = Mathf.Max(0.0f, power_levels[index] + dt * MOVE_SPEED);
            }
            else
            {
                power_levels[index] = Mathf.Min(1.0f, power_levels[index] - dt * MOVE_SPEED);
            }

            BUTTON_LISTS[index][0].updateInteractable(power_levels[index] > 0.0f);
            BUTTON_LISTS[index][1].updateInteractable(power_levels[index] < 1.0f);

            transmitTorpedoPowerAdjustmentRPC(index, power_levels[index]);
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float[] start_powers = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f };
        for (int i = 0; i < 4; i++)
        {
            start_powers[i] = power_levels[i];
        }

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            for (int i = 0; i < 4; i++)
            {
                power_levels[i] = Mathf.Lerp(start_powers[i], 0.0f, 1.0f - (anim_time / power_off_time));
                displayAdjustment(i);
            }
            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        for (int i = 0; i < 4; i++)
        {
            //enable buttons
            BUTTON_LISTS[i][0].updateInteractable(power_levels[i] > 0.0f);
            BUTTON_LISTS[i][1].updateInteractable(power_levels[i] < 1.0f);
            //enable icons
            torpedo_power_glasses.transform.GetChild(i).GetChild(0).GetChild(1).gameObject.SetActive(true);
            //enable bar displays
            torpedo_power_glasses.transform.GetChild(i).GetChild(1).GetChild(1).gameObject.SetActive(true);
            torpedo_power_glasses.transform.GetChild(i).GetChild(2).GetChild(1).gameObject.SetActive(true);
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        for (int i = 0; i < 4; i++)
        {
            //disable buttons
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(false);
            //disable icons
            torpedo_power_glasses.transform.GetChild(i).GetChild(0).GetChild(1).gameObject.SetActive(false);
            //disable bar displays
            torpedo_power_glasses.transform.GetChild(i).GetChild(1).GetChild(1).gameObject.SetActive(false);
            torpedo_power_glasses.transform.GetChild(i).GetChild(2).GetChild(1).gameObject.SetActive(false);
        }
        hud_info.setPowerConsumption(0.0f);

        //return torpedo levers to 0
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTorpedoPowerAdjustmentRPC(int index, float trpdo_percent)
    {
        power_levels[index] = trpdo_percent;
        handlePowerConsumptionChange();
        displayAdjustment(index);
    }
}