/*
    ShieldStrength.cs
    - Handles allocating power to each of the shields and showing their damage
    - Moves dials
    Contributor(s): Jake Schott
    Last Updated: 10/23/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShieldStrength : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float ADJUST_TIME = 0.4f;

    private string[] CONTROL_NAMES = new string[] { "FORWARD", "PORT", "STARBOARD", "AFT" };
    private static string INFO_MESSAGE = "Use shield batteries to adjust shield strength. Does not enable/disable shields (pilot position).";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 2, 0 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[] { new List<Button>(), new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject shield_strength_display;
    public GameObject shield_indicators; //on the ship overview screen
    public List<GameObject> shield_strength_switches;

    private bool is_powered = false;
    private int available_units = 10;
    private int[] shield_strengths = new int[4] { 10, 10, 10, 10 };
    private Coroutine[] shield_strength_adjustment_coroutines = new Coroutine[4] { null, null, null, null };

    private List<string> ray_targets = new List<string> { "shield_strength_forward", "shield_strength_port", "shield_strength_starboard", "shield_strength_aft" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        }

        hud_info = new HUDInfo(CONTROL_NAMES[0] + " SHIELD STRENGTH");
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

    public float getShieldStrength(int location)
    {
        return (shield_strengths[location]);
    }

    //helper method used to deal with the blue shield strength bars
    private void barChange(GameObject bar, bool enabled)
    {
        float a = 1.0f;
        if (enabled == false)
        {
            a = 0.04f;
        }
        bar.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
    }

    private void displayAdjustment(int index)
    {
        //adjust bars
        for (int i = 0; i < 10; i++)
        {
            barChange(shield_strength_display.transform.GetChild(0).GetChild(index).GetChild(i).gameObject, i < shield_strengths[index]);
        }

        float shield_strength_percentage = (shield_strengths[index] / 10.0f);

        //adjust dots on ship overview screen (even if they're visible or not)
        foreach (Transform dot in shield_indicators.transform.GetChild(index))
        {
            dot.GetComponent<RectTransform>().sizeDelta = new Vector2(0.002f + (shield_strength_percentage * 0.008f), 0.002f + (shield_strength_percentage * 0.008f));
            dot.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.1f + (shield_strength_percentage * 0.9f));
        }
    }

    private void highlightSection(int index, float a)
    {
        shield_strength_display.transform.GetChild(1).GetChild(index).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
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

            if (i == 0)
            {
                highlightSection(index, 1.0f);
                displayAdjustment(index);
            }
        }

        highlightSection(index, 0.2f);

        BUTTON_LISTS[index][0].untoggle();
        BUTTON_LISTS[index][1].untoggle();
        BUTTON_LISTS[index][0].updateInteractable(shield_strengths[index] > 0 && is_powered);
        BUTTON_LISTS[index][1].updateInteractable(shield_strengths[index] < 10 && available_units > 0 && is_powered);

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
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && shield_strengths[target_index] > 0) //decrease
            {
                BUTTON_LISTS[target_index][0].toggle();
                BUTTON_LISTS[target_index][1].updateInteractable(false);
                available_units += 1;
                transmitShieldStrengthChangeRPC(target_index, shield_strengths[target_index] - 1, available_units);
            }
            else if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs) && shield_strengths[target_index] < 10 && available_units > 0) //increase
            {
                BUTTON_LISTS[target_index][1].toggle();
                BUTTON_LISTS[target_index][0].updateInteractable(false);
                available_units -= 1;
                transmitShieldStrengthChangeRPC(target_index, shield_strengths[target_index] + 1, available_units);
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
            BUTTON_LISTS[i][1].updateInteractable(shield_strengths[i] < 10 && available_units > 0);
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        shield_strength_display.SetActive(false);
        for (int i = 0; i < 4; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(false);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitShieldStrengthChangeRPC(int index, int new_allocation, int units_remaining)
    {
        bool is_increasing = (new_allocation > shield_strengths[index]);

        shield_strengths[index] = new_allocation;
        available_units = units_remaining;
        if (shield_strength_adjustment_coroutines[index] != null)
        {
            StopCoroutine(shield_strength_adjustment_coroutines[index]);
        }
        shield_strength_adjustment_coroutines[index] = StartCoroutine(handleShieldStrengthChange(index, is_increasing));
    }
}