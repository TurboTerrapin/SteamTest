/*
    TorpedoPower.cs
    - Moves torpedo power levers
    - Adjusts torpedo power screens
    Contributor(s): Jake Schott
    Last Updated: 5/15/2025
*/

using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class TorpedoPower : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 0.25f;

    private string[] CONTROL_NAMES = new string[] {"FORWARD TORPEDO POWER", "PORT TORPEDO POWER", "STARBOARD TORPEDO POWER", "AFT TORPEDO POWER"};
    private List<string> CONTROL_DESCS = new List<string> {"REDUCE", "ENERGIZE"};
    private List<int> CONTROL_INDEXES = new List<int>() {4, 5};
    private List<Button>[] BUTTON_LISTS = new List<Button>[4] {new List<Button>(), new List<Button>(), new List<Button>(), new List<Button>()};

    public Material lit_red;
    public Material unlit_red;
    public Material lit_green;
    public Material unlit_green;

    public List<GameObject> power_levers = null;
    public List<GameObject> information_containers = null; //contains screens and indicators

    private float[] power_levels = new float[] { 0.0f, 0.0f, 0.0f, 0.0f };
    private Vector3[] initial_positions = new Vector3[4]; //handle starting position (0% power)
    private Vector3[] final_positions = new Vector3[4]; //handle starting position (0% power)
    private Vector3 final_lever_direction = new Vector3(0.0842f, 0.0308f, 0f); //handle final position (100% power)

    private List<string> ray_targets = new List<string> {"forward_torpedo_power", "port_torpedo_power", "starboard_torpedo_power", "aft_torpedo_power"};

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAMES[0]);

        for (int i = 0; i <= 3; i++)
        {
            //set buttons
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], true, false)); //increase button

            //set positions
            initial_positions[i] = power_levers[i].transform.localPosition;
            final_positions[i] = power_levers[i].transform.localPosition + final_lever_direction;
        }

        hud_info.setButtons(BUTTON_LISTS[0]);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index]);

        return hud_info;
    }
    private void displayAdjustment(int index)
    {
        //move physical lever
        power_levers[index].transform.localPosition =
            new Vector3(Mathf.Lerp(initial_positions[index].x, final_positions[index].x, power_levels[index]),
                        Mathf.Lerp(initial_positions[index].y, final_positions[index].y, power_levels[index]),
                        Mathf.Lerp(initial_positions[index].z, final_positions[index].z, power_levels[index]));

        //update bars on screen
        //update bars on screen
        float tmp_pwr = power_levels[index];
        for (int i = 0; i <= 19; i++)
        {
            tmp_pwr = power_levels[index] - (0.05f * i);
            float a = tmp_pwr / 0.05f;
            //do both sides
            for (int x = 0; x <= 1; x++)
            {
                information_containers[index].transform.GetChild(x).GetChild(1 + i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.73f, 1.0f, a);
            }
        }

        //update lit indicators
        if (power_levels[index] >= 1.0f)
        {
            information_containers[index].transform.GetChild(2).gameObject.GetComponent<Renderer>().material = lit_green;
            information_containers[index].transform.GetChild(3).gameObject.GetComponent<Renderer>().material = unlit_red;
        }
        else
        {
            information_containers[index].transform.GetChild(2).gameObject.GetComponent<Renderer>().material = unlit_green;
            information_containers[index].transform.GetChild(3).gameObject.GetComponent<Renderer>().material = lit_red;
        }
    }
    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int index = ray_targets.IndexOf(current_target.name);

        int power_direction = 0;
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs) && power_levels[index] < 1.0f ) //E to increment
        {
            power_direction += 1;
        }
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && power_levels[index] > 0.0f)  //Q to decrement
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
            if (power_levels[index] <= 0)
            {
                BUTTON_LISTS[index][0].updateInteractable(false);
            }
            else
            {
                BUTTON_LISTS[index][0].updateInteractable(true);
            }
            if (power_levels[index] >= 1f)
            {
                BUTTON_LISTS[index][1].updateInteractable(false);
            }
            else
            {
                BUTTON_LISTS[index][1].updateInteractable(true);
            }
            transmitTorpedoPowerAdjustmentRPC(index, power_levels[index]);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTorpedoPowerAdjustmentRPC(int index, float trpdo_percent)
    {
        power_levels[index] = trpdo_percent;
        displayAdjustment(index);
    }
}
