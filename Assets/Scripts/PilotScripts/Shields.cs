/*
    Shields.cs
    - Handles enabling/disabling of shields
    Contributor(s): Jake Schott
    Last Updated: 7/7/2025
*/

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Shields : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.25f; //how long the switch takes to be flipped
    private static float CHANGE_TIME = 3.0f; //how long it takes for the shield adjustment to take place

    private List<string> CONTROL_NAMES = new List<string>() { "FORWARD SHIELDS", "PORT SHIELDS", "STARBOARD SHIELDS", "AFT SHIELDS" };
    private List<string> CONTROL_DESCS = new List<string>() { "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[4] { new List<Button>(), new List<Button>(), new List<Button>(), new List<Button>() };

    public List<GameObject> shield_switches = null;
    public GameObject pilot_shield_display;
    public GameObject engineer_shield_display;

    private bool[] enabled_shields = new bool[4] { false, false, false, false };
    private Coroutine[] shield_switch_coroutines = new Coroutine[4] { null, null, null, null };

    private List<string> ray_targets = new List<string> { "forward_shields_switch", "port_shields_switch", "starboard_shields_switch", "aft_shields_switch" };

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, true));
        BUTTON_LISTS[2].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, true));
        BUTTON_LISTS[3].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, true));
        hud_info.setButtons(BUTTON_LISTS[0]);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index]);

        return hud_info;
    }

    IEnumerator shieldChange(int shield_to_change, bool to_change_to)
    {
        //start by flipping the switch
        float anim_time = SWITCH_TIME;
        float starting_rotation = -25.0f;
        float desired_rotation = -110.0f;
        if (to_change_to == false)
        {
            starting_rotation = -110.0f;
            desired_rotation = -25.0f;
            enabled_shields[shield_to_change] = false; //disable shields
        }
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            shield_switches[shield_to_change].transform.localRotation =
                Quaternion.Euler(Mathf.Lerp(desired_rotation, starting_rotation, anim_time / SWITCH_TIME),
                                 90.0f,
                                 0.0f);

            yield return null;
        }
        BUTTON_LISTS[shield_to_change][0].untoggle();

        //animate the displays
        anim_time = CHANGE_TIME;
        float starting_shield_percentage = 1.0f;
        float desired_shield_percentage = 0.0f;
        if (to_change_to == true)
        {
            starting_shield_percentage = 0.0f;
            desired_shield_percentage = 1.0f;
        }
        Transform p_current_section = pilot_shield_display.transform.GetChild(shield_to_change + 1); //pilot display
        Transform e_current_section = engineer_shield_display.transform.GetChild(shield_to_change); //engineer display
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            float current_shield_percentage = Mathf.Lerp(desired_shield_percentage, starting_shield_percentage, anim_time / CHANGE_TIME);
            for (int i = 0; i <= 3; i++)
            {
                bool is_visible = (current_shield_percentage <= 0.25f * (3 - i));
                p_current_section.GetChild(i + 1).GetChild(0).gameObject.SetActive(is_visible);
            }
            pilot_shield_display.transform.GetChild(shield_to_change + 1).GetChild(0).GetChild(2).gameObject.SetActive(current_shield_percentage == 1.0f);

            for (int i = 0; i <= e_current_section.childCount - 1; i++)
            {
                bool is_visible = (current_shield_percentage > (1.0f / e_current_section.childCount) * (e_current_section.childCount - 1 - i));
                e_current_section.GetChild(i).gameObject.SetActive(is_visible);
            }

            yield return null;
        }

        //reset
        if (to_change_to == false)
        {
            BUTTON_LISTS[shield_to_change][0].updateDesc(CONTROL_DESCS[0]);
        }
        else
        {
            enabled_shields[shield_to_change] = true; //enable shields
            BUTTON_LISTS[shield_to_change][0].updateDesc(CONTROL_DESCS[1]);
        }
        BUTTON_LISTS[shield_to_change][0].updateInteractable(true);

        shield_switch_coroutines[shield_to_change] = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int index = ray_targets.IndexOf(current_target.name);
        
        if (shield_switch_coroutines[index] == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTON_LISTS[index][0].toggle();
                transmitShieldChangeRPC(index, !enabled_shields[index]);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitShieldChangeRPC(int index, bool is_enabled)
    {
        if (shield_switch_coroutines[index] != null)
        {
            StopCoroutine(shield_switch_coroutines[index]);
        }
        shield_switch_coroutines[index] = StartCoroutine(shieldChange(index, is_enabled));
    }
}