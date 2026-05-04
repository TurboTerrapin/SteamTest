/*
    PowerAllocation.cs
    - Handles inputs for power allocation
    - Moves dials
    Contributor(s): Jake Schott
    Last Updated: 5/3/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using static AnimatorHandler;

public class PowerAllocation : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float TURN_TIME = 0.2f; //how long it takes to move the dial in either direction
    private static int MAX_ALLOCATION_UNITS = 24; //don't change this number

    private string[] CONTROL_NAMES = new string[] { "PILOT", "TACTICIAN", "ENGINEER", "CAPTAIN" };
    private static string INFO_MESSAGE = "Controls the power allocation (circles) for the corresponding position to prevent overconsumption and power loss.";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[] { new List<Button>(), new List<Button>(), new List<Button>(), new List<Button>() };

    public List<GameObject> allocation_dials;
    public List<GameObject> position_icons;
    public List<GameObject> power_screen_displays; //the screen that shows the power allocation AND consumption
    public List<GameObject> allocation_circle_displays; //the circular screens around each dial
    public GameObject info_display;

    private PowerManager power_manager;
    private GameObject units_counter;
    private GameObject units_circle_collection;

    private bool is_powered = false;
    private int available_units = MAX_ALLOCATION_UNITS;
    private int[] allocated_units = new int[4] { 0, 0, 0, 0 };
    private Coroutine[] allocation_adjustment_coroutines = new Coroutine[4] { null, null, null, null };

    private List<string> ray_targets = new List<string> { "power_allocation_pilot", "power_allocation_tactician", "power_allocation_engineer", "power_allocation_captain" };

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    public float lerp_speed = 5f;

    private void Start()
    {
        power_manager = ReferenceAssistor.Instance.power_manager;
        units_counter = info_display.transform.GetChild(0).gameObject;
        units_circle_collection = info_display.transform.GetChild(1).gameObject;

        for (int i = 0; i < 4; i++)
        {
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        }

        hud_info = new HUDInfo(CONTROL_NAMES[0] + " POWER ALLOCATION");
        hud_info.setButtons(BUTTON_LISTS[0], 7);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);

        hud_info.setTitle(CONTROL_NAMES[index] + " POWER ALLOCATION");
        hud_info.setButtons(BUTTON_LISTS[index], 7);

        return hud_info;
    }
    public Transform getIKTarget(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        return IK_targets[index].transform;
    }
    public AnimatorHandler.HandInteractionType getHandInteractionType()
    {
        return hand_interaction_type;
    }
    public float getHandPose()
    {
        return hand_pose;
    }
    public bool getRightHandFlip()
    {
        return does_right_hand_flip;
    }
    
    public Vector3 getRightHandOffset()
    {
        return right_hand_offset;
    }
    public float getLerpSpeed()
    {
        return lerp_speed;
    }
    public float getPowerAllocation(int position)
    {
        return (allocated_units[position] * 0.1f);
    }

    //called by ScenarioManager.controlResetHelper()
    public void resetToDefaultAllocation(int[] allocation_values)
    {
        if (allocation_values.Length != 4)
        {
            return;
        }

        int total_units = 0;
        for (int i = 0; i < 4; i++)
        {
            if (allocation_values[i] < 0 || allocation_values[i] > 10)
            {
                Debug.Log("ATTEMPTED TO ASSIGN MORE THAN 10 UNITS OR LESS THAN 0 TO A POSITION");
                return;
            }
            total_units += allocation_values[i];
        }

        if (total_units > 24)
        {
            Debug.Log("ATTEMPTED TO ASSIGN MORE THAN AVAILABLE UNITS!");
            return;
        }

        available_units = 24 - total_units;

        //display new values
        for (int i = 0; i < 4; i++)
        {
            //stop adjustment if currently happening
            if (allocation_adjustment_coroutines[i] != null)
            {
                StopCoroutine(allocation_adjustment_coroutines[i]);
                allocation_adjustment_coroutines[i] = null;
            }

            //display new value
            allocated_units[i] = allocation_values[i];
            allocation_dials[i].transform.localRotation =
                Quaternion.Euler(-54.0f, -45.0f, 359.9f * (allocation_values[i] / 10.0f));
            displayAdjustment(i);
        }
    }

    //helper method used to deal with the blue power allocation circles
    private void circleChange(GameObject circle, bool enabled)
    {
        float a = 1.0f;
        if (enabled == false)
        {
            a = 0.2f;
        }
        Color circle_color = circle.GetComponent<UnityEngine.UI.RawImage>().color;
        circle.GetComponent<UnityEngine.UI.RawImage>().color = new Color(circle_color.r, circle_color.g, circle_color.b, a);
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
    IEnumerator handleAllocationChange(int index, int new_allocation)
    {
        float initial_rotation = allocation_dials[index].transform.localRotation.eulerAngles.z;
        float destination_rotation = 359.9f * (allocated_units[index] / 10.0f);

        if (initial_rotation > destination_rotation)
        {
            displayAdjustment(index);
            power_manager.allocationChange(index, new_allocation * 0.1f);
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
            power_manager.allocationChange(index, new_allocation * 0.1f);
        }

        BUTTON_LISTS[index][0].untoggle();
        BUTTON_LISTS[index][1].untoggle();
        for (int i = 0; i < 4; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(allocated_units[i] > 0 && is_powered);
            BUTTON_LISTS[i][1].updateInteractable(allocated_units[i] < 10 && available_units > 0 && is_powered);
        }

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
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && allocated_units[target_index] > 0) //decrease
            {
                BUTTON_LISTS[target_index][0].toggle(TURN_TIME);
                BUTTON_LISTS[target_index][1].updateInteractable(false);
                available_units += 1;
                transmitAllocationChangeRPC(target_index, allocated_units[target_index] - 1, available_units);
            }
            else if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs) && allocated_units[target_index] < 10 && available_units > 0) //increase
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
        //first pass
        if (power_screen_displays[0].transform.parent.gameObject.activeSelf == false)
        {
            power_screen_displays[0].transform.parent.gameObject.SetActive(true);
            return;
        }

        //second pass
        is_powered = true;
        info_display.SetActive(true);
        for (int i = 0; i < 4; i++)
        {
            position_icons[i].SetActive(true);
            allocation_circle_displays[i].SetActive(true);
            BUTTON_LISTS[i][0].updateInteractable(allocated_units[i] > 0);
            BUTTON_LISTS[i][1].updateInteractable(allocated_units[i] < 10 && available_units > 0);
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        power_screen_displays[0].transform.parent.gameObject.SetActive(false);
        info_display.SetActive(false);
        for (int i = 0; i < 4; i++)
        {
            position_icons[i].SetActive(false);
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
        allocation_adjustment_coroutines[index] = StartCoroutine(handleAllocationChange(index, new_allocation));
    }
}