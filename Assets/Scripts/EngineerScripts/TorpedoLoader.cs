/*
    TorpedoLoader.cs
    - Handles the loading of torpedoes 
    Contributor(s): Jake Schott
    Last Updated: 9/19/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TorpedoLoader : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS

    private string[] CONTROL_NAMES = new string[] { "TORPEDO TYPE SELECTOR", "TORPEDO BAY SELECTOR", "TORPEDO BAY LOADER" };
    private List<string> CONTROL_DESCS = new List<string> { "SELECT LEFT", "SELECT RIGHT", "SHIFT LEFT", "SHIFT RIGHT", "LOAD" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5, 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject torpedo_selection_display;
    public GameObject torpedo_direction_display;
    public GameObject torpedo_selection_switch;
    public GameObject torpedo_direction_slider;
    public GameObject torpedo_confirmation_switch;

    private bool is_powered = false;

    private List<string> ray_targets = new List<string> { "torpedo_loader_selection_switch", "torpedo_loader_direction_slider", "torpedo_loader_confirm_switch" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));

        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[1], false, true));

        BUTTON_LISTS[2].Add(new Button(CONTROL_DESCS[4], CONTROL_INDEXES[2], false, true));

        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        hud_info.setButtons(BUTTON_LISTS[0], 7);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);

        hud_info.setTitle(CONTROL_NAMES[index]);

        if (index < 2)
        {
            hud_info.setButtons(BUTTON_LISTS[index], 7);
        }
        else
        {
            hud_info.setButtons(BUTTON_LISTS[index], 6);
        }

        return hud_info;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;

    }

    public void powerOff(int position, float time)
    {
        is_powered = false;

    }
}