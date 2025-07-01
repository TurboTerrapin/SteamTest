/*
    PhaserTemperatures.cs
    - Moves phaser sliders
    - Adjusts phaser temperature screens next to sliders
    Contributor(s): Jake Schott
    Last Updated: 6/30/2025
*/

using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class PhaserTemperatures : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 0.1f;

    private string[] CONTROL_NAMES = new string[] { "LONG-RANGE PHASERS", "SHORT-RANGE PHASERS"};
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>()};

    public List<GameObject> phaser_display_canvases = null;
    public List<GameObject> phaser_sliders = null;
    private PhaserPowers phaser_powers;
    private float[] phaser_temperatures = new float[2] { 0.0f, 0.0f };
    private Vector3[] phaser_slider_initial_positions = new Vector3[2];
    private Vector3[] phaser_slider_final_positions = new Vector3[2];
    private Vector3 phaser_slide_direction = new Vector3(0.0f, 0.1078f, 0.2626f);

    private List<string> ray_targets = new List<string> {"long_range_phasers", "short_range_phasers"};

    private static HUDInfo hud_info = null;
    private void Start()
    {
        phaser_powers = transform.GetComponent<PhaserPowers>();

        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        
        for (int i = 0; i <= 1; i++)
        {
            //set buttons
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false)); //increase button

            //set positions
            phaser_slider_initial_positions[i] = phaser_sliders[i].transform.localPosition;
            phaser_slider_final_positions[i] = phaser_sliders[i].transform.localPosition + phaser_slide_direction;
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

    public float[] getPhaserTemperatures()
    {
        return phaser_temperatures;
    }

    public void changeInPower(int index, bool new_power)
    {
        if (index == 2)
        {
            index = 1;
        }
        if (new_power == false)
        {
            BUTTON_LISTS[index][0].updateInteractable(false);
            BUTTON_LISTS[index][1].updateInteractable(false);
        }
        else
        {
            BUTTON_LISTS[index][0].updateInteractable(phaser_temperatures[index] > 0.0f);
            BUTTON_LISTS[index][1].updateInteractable(phaser_temperatures[index] < 1.0f);
        }
    }

    private void displayAdjustment(int index)
    {
        //move physical slider
        phaser_sliders[index].transform.localPosition =
            new Vector3(Mathf.Lerp(phaser_slider_initial_positions[index].x, phaser_slider_final_positions[index].x, phaser_temperatures[index]),
                        Mathf.Lerp(phaser_slider_initial_positions[index].y, phaser_slider_final_positions[index].y, phaser_temperatures[index]),
                        Mathf.Lerp(phaser_slider_initial_positions[index].z, phaser_slider_final_positions[index].z, phaser_temperatures[index]));

        //adjust screen
        int power_as_int = (int)(phaser_temperatures[index] * 100.0f);
        if (power_as_int < 100)
        {
            int position = (power_as_int / 5);
            if (index == 0)
            {
                phaser_display_canvases[index].transform.GetChild(position + 1).gameObject.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0f, 0.5f + 0.34f * ((19 - position) / 20.0f), 1f, (0.2f * (power_as_int % 5)));
            }
            else
            {
                phaser_display_canvases[index].transform.GetChild(position + 1).gameObject.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.5f + (0.5f * (position / 20.0f)), 0.5f, 0f, (0.2f * (power_as_int % 5)));
            }
        }
    }
    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int index = ray_targets.IndexOf(current_target.name);

        //make sure phaser is currently active
        bool[] phasers_enabled = phaser_powers.getActivePhasers();
        if (index == 0)
        {
            if (phasers_enabled[0] == false)
            {
                //phaser is disabled
                return;
            }
        }
        else
        {
            if (phasers_enabled[1] == false && phasers_enabled[2] == false)
            {
                //phaser is disabled
                return;
            }
        }

        int phaser_direction = 0;
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs) && phaser_temperatures[index] < 1.0f) //E to increment
        {
            phaser_direction += 1;
        }
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && phaser_temperatures[index] > 0.0f)  //Q to decrement
        {
            phaser_direction -= 1;
        }
        if (phaser_direction != 0)
        {
            if (phaser_direction > 0)
            {
                phaser_temperatures[index] = Mathf.Max(0.0f, phaser_temperatures[index] + dt * MOVE_SPEED);
            }
            else
            {
                phaser_temperatures[index] = Mathf.Min(1.0f, phaser_temperatures[index] - dt * MOVE_SPEED);
            }
            BUTTON_LISTS[index][0].updateInteractable(phaser_temperatures[index] > 0.0f);
            BUTTON_LISTS[index][1].updateInteractable(phaser_temperatures[index] < 1.0f);
            transmitPhaserTemperatureAdjustmentRPC(index, phaser_temperatures[index]);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitPhaserTemperatureAdjustmentRPC(int index, float phsr_percent)
    {
        phaser_temperatures[index] = phsr_percent;
        displayAdjustment(index);
    }
}
