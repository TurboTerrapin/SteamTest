/*
    PowerAllocation.cs
    - Handles inputs for power allocation
    - Moves dials
    Contributor(s): Jake Schott
    Last Updated: 8/31/2025
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerAllocation : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float TURN_TIME = 0.2f; //how long it takes to move the dial in either direction

    private string[] CONTROL_NAMES = new string[] { "PILOT", "TACTICIAN", "ENGINEER", "CAPTAIN" };
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[] { new List<Button>(), new List<Button>(), new List<Button>(), new List<Button>() };

    private bool is_powered = false;

    private List<string> ray_targets = new List<string> { "power_allocation_pilot", "power_allocation_tactician", "power_allocation_engineer", "power_allocation_captain" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        }

        hud_info = new HUDInfo(CONTROL_NAMES[0] + " POWER ALLOCATION");
        hud_info.setButtons(BUTTON_LISTS[0], 7);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);

        hud_info.setTitle(CONTROL_NAMES[index] + " POWER ALLOCATION");
        hud_info.setButtons(BUTTON_LISTS[index], 7);

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

    }

    public void powerOff(int position, float time)
    {

    }
}
