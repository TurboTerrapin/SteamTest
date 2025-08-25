/*
    TorpedoPower.cs
    - Moves torpedo power levers
    - Adjusts torpedo power screens
    Contributor(s): Jake Schott
    Last Updated: 8/23/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TorpedoPower : NetworkBehaviour, IControllable, IPowerable
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

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
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
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false)); //increase button

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
        float tmp_pwr = power_levels[index];
        for (int i = 0; i <= 19; i++)
        {
            tmp_pwr = power_levels[index] - (0.05f * i);
            float a = tmp_pwr / 0.05f;
            //do both sides
            for (int x = 0; x <= 1; x++)
            {
                information_containers[index].transform.GetChild(x).GetChild(1).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.73f, 1.0f, a);
            }
        }

        //update lit indicators
        if (is_powered == true)
        {
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

    }
    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

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
            //enable lit indicators
            information_containers[i].transform.GetChild(2).gameObject.GetComponent<Renderer>().material = unlit_green;
            information_containers[i].transform.GetChild(3).gameObject.GetComponent<Renderer>().material = lit_red;
            //enable icons
            information_containers[i].transform.GetChild(4).GetChild(0).GetChild(1).gameObject.SetActive(true);
            //enable bar displays
            information_containers[i].transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
            information_containers[i].transform.GetChild(1).GetChild(1).gameObject.SetActive(true);
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
            //disable lit indicators
            information_containers[i].transform.GetChild(2).gameObject.GetComponent<Renderer>().material = unlit_green;
            information_containers[i].transform.GetChild(3).gameObject.GetComponent<Renderer>().material = unlit_red;
            //disable icons
            information_containers[i].transform.GetChild(4).GetChild(0).GetChild(1).gameObject.SetActive(false);
            //disable bar displays
            information_containers[i].transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
            information_containers[i].transform.GetChild(1).GetChild(1).gameObject.SetActive(false);
        }

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
        displayAdjustment(index);
    }
}
