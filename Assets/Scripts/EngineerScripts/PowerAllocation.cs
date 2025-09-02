/*
    PowerAllocation.cs
    - Handles inputs for power allocation
    - Moves dials
    Contributor(s): Jake Schott
    Last Updated: 8/31/2025
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    public List<GameObject> allocation_dials;
    public List<GameObject> position_icon_displays;
    public List<GameObject> power_screen_displays; //the screen that shows the power allocation AND consumption
    public List<GameObject> allocation_circle_displays; //the circular screens around each dial
    public GameObject info_display;
    private GameObject units_counter;
    private GameObject units_circle_collection;

    private bool is_powered = false;
    private int available_units = 24;
    private int[] allocated_units = new int[4] { 0, 0, 0, 0 };
    private Coroutine[] allocation_adjustment_coroutines = new Coroutine[4] { null, null, null, null };

    private List<string> ray_targets = new List<string> { "power_allocation_pilot", "power_allocation_tactician", "power_allocation_engineer", "power_allocation_captain" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        units_counter = info_display.transform.GetChild(0).gameObject;
        units_circle_collection = info_display.transform.GetChild(1).gameObject;

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

    //helper method used to deal with the blue power allocation circles
    private void circleChange(GameObject circle, bool enabled)
    {
        float a = 1.0f;
        if (enabled == false)
        {
            a = 0.2f;
        }
        circle.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
        circle.transform.GetChild(0).gameObject.SetActive(!enabled);
    }

    private void displayAdjustment(int index)
    {
        //adjust available power units circles on header display
        for (int i = 0; i <= 23; i++)
        {
            circleChange(units_circle_collection.transform.GetChild(i).gameObject, i < available_units);
        }

        //adjust available power units counter on header display
        string units_left = available_units.ToString();
        if (units_left.Length < 2)
        {
            units_left = "0" + units_left;
        }
        units_counter.GetComponent<TMP_Text>().text = units_left;

        //adjust circle around the dial
        for (int i = 0; i <= 9; i++)
        {
            circleChange(allocation_circle_displays[index].transform.GetChild(i).gameObject, i < allocated_units[index]);
            circleChange(power_screen_displays[index].transform.GetChild(i + 1).gameObject.transform.GetChild(1).gameObject, i < allocated_units[index]);
        }
    }

    //turns the dial and calls displayAdjustment()
    IEnumerator handleAllocationChange(int index)
    {
        float initial_rotation = allocation_dials[index].transform.localRotation.eulerAngles.z;
        float destination_rotation = 359.9f * (allocated_units[index] / 10.0f);

        if (initial_rotation > destination_rotation)
        {
            displayAdjustment(index);
        }

        float anim_time = TURN_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            allocation_dials[index].transform.localRotation =
                Quaternion.Euler(-54.0f, -45.0f, Mathf.Lerp(initial_rotation, destination_rotation, 1.0f - (anim_time / TURN_TIME)));
            
            yield return null;
        }

        if (initial_rotation < destination_rotation)
        {
            displayAdjustment(index);
        }

        BUTTON_LISTS[index][0].untoggle();
        BUTTON_LISTS[index][1].untoggle();
        BUTTON_LISTS[index][0].updateInteractable(allocated_units[index] > 0 && is_powered);
        BUTTON_LISTS[index][1].updateInteractable(allocated_units[index] < 10 && available_units > 0 && is_powered);

        allocation_adjustment_coroutines[index] = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int target_index = ray_targets.IndexOf(current_target.name);
        if (allocation_adjustment_coroutines[target_index] == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && allocated_units[target_index] > 0) //decrease
            {
                BUTTON_LISTS[target_index][0].toggle(TURN_TIME);
                BUTTON_LISTS[target_index][1].updateInteractable(false);
                available_units += 1;
                transmitAllocationChangeRPC(target_index, allocated_units[target_index] - 1, available_units);
            }
            else if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs) && allocated_units[target_index] < 10 && available_units > 0) //increase
            {
                BUTTON_LISTS[target_index][1].toggle(TURN_TIME);
                BUTTON_LISTS[target_index][0].updateInteractable(false);
                available_units -= 1;
                transmitAllocationChangeRPC(target_index, allocated_units[target_index] + 1, available_units);
            }
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        info_display.SetActive(true);
        for (int i = 0; i < 4; i++)
        {
            position_icon_displays[i].SetActive(true);
            allocation_circle_displays[i].SetActive(true);
            BUTTON_LISTS[i][0].updateInteractable(allocated_units[i] > 0);
            BUTTON_LISTS[i][1].updateInteractable(allocated_units[i] < 10);
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        info_display.SetActive(false);
        for (int i = 0; i < 4; i++)
        {
            position_icon_displays[i].SetActive(false);
            allocation_circle_displays[i].SetActive(false);
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(false);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitAllocationChangeRPC(int index, int new_allocation, int units_remaining)
    {
        allocated_units[index] = new_allocation;
        available_units = units_remaining;
        if (allocation_adjustment_coroutines[index] != null)
        {
            StopCoroutine(allocation_adjustment_coroutines[index]);
        }
        allocation_adjustment_coroutines[index] = StartCoroutine(handleAllocationChange(index));
    }
}
