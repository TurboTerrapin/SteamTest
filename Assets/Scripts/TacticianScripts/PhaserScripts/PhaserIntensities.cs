/*
    PhaserTemperatures.cs
    - Moves phaser sliders
    - Adjusts phaser intensity screens next to sliders
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PhaserIntensities : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 0.25f;
    private static float MAX_POWER_CONSUMPTION = 0.2f; //equates to 2 circles
    private static Vector3 PHASER_SLIDE_DIRECTION = new Vector3(0.0f, 0.1078f, 0.2626f);

    private string[] CONTROL_NAMES = new string[] { "LONG-RANGE PHASER", "SHORT-RANGE PHASERS" };
    private static string INFO_MESSAGE = "Adjusts the intensity of the corresponding phasers to increase or decrease damage.";
    private List<string> CONTROL_DESCS = new List<string> { "REDUCE", "ENERGIZE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public List<GameObject> phaser_display_displays = null;
    public List<GameObject> phaser_sliders = null;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private PhaserActivators phaser_activators;
    private float[] phaser_intensities = new float[2] { 0.0f, 0.0f };
    private Vector3[] phaser_slider_initial_positions = new Vector3[2];
    private Vector3[] phaser_slider_final_positions = new Vector3[2];

    private List<string> ray_targets = new List<string> { "long_range_phasers", "short_range_phasers" };

    private static HUDInfo hud_info = null;
    private void Start()
    {
        phaser_activators = GetComponent<PhaserActivators>();

        hud_info = new HUDInfo(CONTROL_NAMES[0], true);

        for (int i = 0; i <= 1; i++)
        {
            //set buttons
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false)); //increase button

            //set positions
            phaser_slider_initial_positions[i] = phaser_sliders[i].transform.localPosition;
            phaser_slider_final_positions[i] = phaser_sliders[i].transform.localPosition + PHASER_SLIDE_DIRECTION;
        }

        hud_info.setButtons(BUTTON_LISTS[0], 7);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 7);
        hud_info.setInfo(INFO_MESSAGE);

        return hud_info;
    }

    public float[] getPhaserTemperatures()
    {
        return phaser_intensities;
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
            BUTTON_LISTS[index][0].updateInteractable(phaser_intensities[index] > 0.0f);
            BUTTON_LISTS[index][1].updateInteractable(phaser_intensities[index] < 1.0f);
        }
    }

    private void handlePowerConsumptionChange()
    {
        float consumed_power = 0.0f;
        for (int i = 0; i < 2; i++)
        {
            consumed_power += (phaser_intensities[i] * 0.5f * MAX_POWER_CONSUMPTION);
        }
        ReferenceAssistor.Instance.power_manager.controlPowerChange(1, this.GetType().Name, consumed_power);
        hud_info.setPowerConsumption(consumed_power);
    }

    private void displayAdjustment(int index)
    {
        //move physical slider
        phaser_sliders[index].transform.localPosition = Vector3.Lerp(phaser_slider_initial_positions[index], phaser_slider_final_positions[index], phaser_intensities[index]);

        //adjust screen
        Color phaser_color = new Color(0.0f, 0.84f, 1.0f);
        if (index == 1)
        {
            phaser_color = new Color(1.0f, 0.47f, 0.0f);
        }
        float tmp_pwr = phaser_intensities[index];
        for (int i = 0; i <= 19; i++)
        {
            tmp_pwr = phaser_intensities[index] - (0.05f * i);
            float a = tmp_pwr / 0.05f;
            phaser_display_displays[index].transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(phaser_color.r, phaser_color.g, phaser_color.b, a);
        }
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int index = ray_targets.IndexOf(current_target.name);

        //make sure phaser is currently active
        bool[] phasers_enabled = phaser_activators.getActivePhasers();
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
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs) && phaser_intensities[index] < 1.0f) //E to increment
        {
            phaser_direction += 1;
        }
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && phaser_intensities[index] > 0.0f)  //Q to decrement
        {
            phaser_direction -= 1;
        }
        if (phaser_direction != 0)
        {
            if (phaser_direction > 0)
            {
                phaser_intensities[index] = Mathf.Max(0.0f, phaser_intensities[index] + dt * MOVE_SPEED);
            }
            else
            {
                phaser_intensities[index] = Mathf.Min(1.0f, phaser_intensities[index] - dt * MOVE_SPEED);
            }
            BUTTON_LISTS[index][0].updateInteractable(phaser_intensities[index] > 0.0f);
            BUTTON_LISTS[index][1].updateInteractable(phaser_intensities[index] < 1.0f);
            transmitPhaserTemperatureAdjustmentRPC(index, phaser_intensities[index]);
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float[] start_temps = new float[2] { 0.0f, 0.0f };
        for (int i = 0; i < 2; i++)
        {
            start_temps[i] = phaser_intensities[i];
        }

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            for (int i = 0; i < 2; i++)
            {
                phaser_intensities[i] = Mathf.Lerp(start_temps[i], 0.0f, 1.0f - (anim_time / power_off_time));
                displayAdjustment(i);
            }
            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        for (int i = 0; i < 2; i++)
        {
            phaser_display_displays[i].SetActive(true);
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(false);
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        for (int i = 0; i < 2; i++)
        {
            phaser_display_displays[i].SetActive(false);
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(false);
        }
        hud_info.setPowerConsumption(0.0f);

        //return intensities to 0
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitPhaserTemperatureAdjustmentRPC(int index, float phsr_percent)
    {
        phaser_intensities[index] = phsr_percent;
        handlePowerConsumptionChange();
        displayAdjustment(index);
    }
}