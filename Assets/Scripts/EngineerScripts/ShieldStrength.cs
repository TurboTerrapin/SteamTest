/*
    ShieldStrength.cs
    - Handles allocating shield battery to each of the four ship sections
    - Flips the switches
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShieldStrength : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float ADJUST_TIME = 0.15f;
    private static float MAX_POWER_CONSUMPTION = 0.4f; //equates to 4 circles

    private string[] CONTROL_NAMES = new string[] { "FORWARD", "PORT", "STARBOARD", "AFT" };
    private static string INFO_MESSAGE = "Use shield batteries from ship inventory to adjust shield strength in forward, port, starboard, and aft sections.";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[] { new List<Button>(), new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject shield_strength_display;
    public GameObject shield_indicators; //on the ship overview screen
    public List<GameObject> shield_strength_switches;
    private ShipInventory ship_inventory;

    private bool is_powered = false;
    private int[] shield_strengths = new int[4] { 0, 0, 0, 0 };
    private Stack<string>[] shield_strength_serial_nums = new Stack<string>[4];
    private Coroutine[] shield_strength_adjustment_coroutines = new Coroutine[4] { null, null, null, null };

    private List<string> ray_targets = new List<string> { "shield_strength_forward", "shield_strength_port", "shield_strength_starboard", "shield_strength_aft" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
            shield_strength_serial_nums[i] = new Stack<string>();
        }

        ship_inventory = GameObject.FindGameObjectWithTag("Spaceship").GetComponent<ShipInventory>();

        hud_info = new HUDInfo(CONTROL_NAMES[0] + " SHIELD STRENGTH", true);
        hud_info.setButtons(BUTTON_LISTS[0], 7);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);

        hud_info.setTitle(CONTROL_NAMES[index] + " SHIELD STRENGTH");
        hud_info.setButtons(BUTTON_LISTS[index], 7);

        return hud_info;
    }

    public void onInventoryChange(int available_batteries)
    {
        if (is_powered == false)
        {
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (shield_strength_adjustment_coroutines[i] == null)
            {
                BUTTON_LISTS[i][0].updateInteractable(shield_strengths[i] > 0);
                BUTTON_LISTS[i][1].updateInteractable(shield_strengths[i] < 5 && available_batteries > 0);
            }
        }

        float a = 1.0f;
        if (available_batteries <= 0)
        {
            a = 0.08f;
        }

        shield_strength_display.transform.GetChild(3).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
        shield_strength_display.transform.GetChild(4).GetComponent<TMP_Text>().color = new Color(0.0f, 0.84f, 1.0f, a);
        if (available_batteries == 1)
        {
            shield_strength_display.transform.GetChild(4).GetComponent<TMP_Text>().SetText("1 BATTERY AVAILABLE");
        }
        else
        {
            shield_strength_display.transform.GetChild(4).GetComponent<TMP_Text>().SetText(available_batteries + " BATTERIES AVAILABLE");
        }
    }

    public float getShieldStrength(int location)
    {
        return (shield_strengths[location]);
    }

    public float getPowerConsumption()
    {
        float total_consumption = 0.0f;
        for (int i = 0; i < 4; i++)
        {
            total_consumption += getShieldStrength(i) * (MAX_POWER_CONSUMPTION / 20.0f);
        }
        return total_consumption;
    }

    //helper method used to deal with the blue shield strength bars
    private void barChange(GameObject bar, bool enabled)
    {
        float a = 1.0f;
        if (enabled == false)
        {
            a = 0.08f;
        }
        bar.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
    }

    private void displayAdjustment(int index)
    {
        //adjust bars
        for (int i = 0; i < 5; i++)
        {
            barChange(shield_strength_display.transform.GetChild(0).GetChild(index).GetChild(i).gameObject, i < shield_strengths[index]);
        }

        //adjust arrow
        shield_strength_display.transform.GetChild(0).GetChild(index).GetChild(6).GetChild(0).gameObject.SetActive(shield_strengths[index] > 0);

        //adjust percentage
        float a = 0.08f;
        if (shield_strengths[index] > 0)
        {
            a = 1.0f;
        }
        shield_strength_display.transform.GetChild(0).GetChild(index).GetChild(5).GetComponent<TMP_Text>().color = new Color(0.0f, 0.84f, 1.0f, a);
        shield_strength_display.transform.GetChild(0).GetChild(index).GetChild(5).GetComponent<TMP_Text>().SetText((shield_strengths[index] * 20).ToString() + "%");

        float shield_strength_percentage = (shield_strengths[index] / 5.0f);

        //adjust dots on ship overview screen
        foreach (Transform dot in shield_indicators.transform.GetChild(index))
        {
            dot.GetComponent<RectTransform>().sizeDelta = new Vector2(0.002f + (shield_strength_percentage * 0.008f), 0.002f + (shield_strength_percentage * 0.008f));
            dot.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.1f + (shield_strength_percentage * 0.9f));
        }

        //adjust power consumption
        hud_info.setPowerConsumption(getPowerConsumption());
        ReferenceAssistor.Instance.power_manager.controlPowerChange(2, this.GetType().Name, getPowerConsumption());
    }

    //turns the dial and calls displayAdjustment()
    IEnumerator handleShieldStrengthChange(int index, bool increase)
    {
        float destination_rotation = -74.0f;
        if (increase == false)
        {
            destination_rotation = -34.0f;
        }

        float anim_time = ADJUST_TIME;
        for (int i = 0; i <= 1; i++)
        {
            float half_time = ADJUST_TIME * 0.5f;
            float curr_time = half_time;

            while (curr_time > 0.0f)
            {
                curr_time = Mathf.Max(0.0f, curr_time - Time.deltaTime);

                float switch_percentage = 1.0f - (curr_time / half_time);
                if (i == 1)
                {
                    switch_percentage = (curr_time / half_time);
                }

                shield_strength_switches[index].transform.localRotation = Quaternion.Euler(Mathf.Lerp(-54.0f, destination_rotation, switch_percentage), 315.0f, 0.0f);

                yield return null;
            }

            if (i == 0 && is_powered == true)
            {
                displayAdjustment(index);
            }
        }

        BUTTON_LISTS[index][0].untoggle();
        BUTTON_LISTS[index][1].untoggle();

        for (int i = 0; i < 4; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(shield_strengths[i] > 0 && is_powered);
            BUTTON_LISTS[i][1].updateInteractable(shield_strengths[i] < 5 && ship_inventory.getItemQuantity(0, 2) > 0 && is_powered);
        }

        shield_strength_adjustment_coroutines[index] = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int target_index = ray_targets.IndexOf(current_target.name);
        if (shield_strength_adjustment_coroutines[target_index] == null)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && shield_strengths[target_index] > 0) //decrease
            {
                BUTTON_LISTS[target_index][0].toggle();
                BUTTON_LISTS[target_index][1].updateInteractable(false);

                transmitShieldStrengthChangeRPC(target_index, shield_strengths[target_index] - 1);
            }
            else if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs) && shield_strengths[target_index] < 5 && ship_inventory.getItemQuantity(0, 2) > 0) //increase
            {
                BUTTON_LISTS[target_index][1].toggle();
                BUTTON_LISTS[target_index][0].updateInteractable(false);

                transmitShieldStrengthChangeRPC(target_index, shield_strengths[target_index] + 1);
            }
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        shield_strength_display.SetActive(true);
        for (int i = 0; i < 4; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(shield_strengths[i] > 0);
            BUTTON_LISTS[i][1].updateInteractable(shield_strengths[i] < 5 && ship_inventory.getItemQuantity(0, 2) > 0);
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        shield_strength_display.SetActive(false);
        Stack<string> shield_battery_serial_nums = new Stack<string>();
        for (int i = 0; i < 4; i++)
        {
            while (shield_strength_serial_nums[i].Count > 0)
            {
                shield_battery_serial_nums.Push(shield_strength_serial_nums[i].Pop());
            }
            shield_strengths[i] = 0;
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(false);
            displayAdjustment(i);
        }
        //handle inventory adjustment
        if (NetworkManager.Singleton.IsHost == true)
        {
            ship_inventory.addItems(0, 2, shield_battery_serial_nums);
        }
        hud_info.setPowerConsumption(0.0f);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitShieldStrengthChangeRPC(int index, int new_allocation)
    {
        bool is_increasing = (new_allocation > shield_strengths[index]);

        shield_strengths[index] = new_allocation;

        //handle inventory adjustment
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (is_increasing == true)
            {
                shield_strength_serial_nums[index].Push(ship_inventory.removeItem(0, 2));
            }
            else
            {
                ship_inventory.addItem(0, 2, shield_strength_serial_nums[index].Pop());
            }
        }

        if (shield_strength_adjustment_coroutines[index] != null)
        {
            StopCoroutine(shield_strength_adjustment_coroutines[index]);
        }
        shield_strength_adjustment_coroutines[index] = StartCoroutine(handleShieldStrengthChange(index, is_increasing));
    }
}