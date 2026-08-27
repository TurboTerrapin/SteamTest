/*
    PhaserIntensities.cs
    - Moves phaser sliders
    - Adjusts phaser intensity screens next to sliders
    Contributor(s): Jake Schott
    Last Updated: 8/26/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PhaserIntensities : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 0.35f;
    private static float[] MAX_POWER_CONSUMPTION_BY_INDEX = new float[] { 0.2f, 0.4f };
    private static Vector3 PHASER_SLIDE_DIRECTION = new Vector3(0.0f, 0.031f, 0.082f);

    private string[] CONTROL_NAMES = new string[] { "LONG-RANGE PHASER", "SHORT-RANGE PHASERS" };
    private static string INFO_MESSAGE = "Adjusts the intensity of the corresponding phasers to adjust damage and firing rate.";
    private List<string> CONTROL_DESCS = new List<string> { "REDUCE", "ENERGIZE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public List<GameObject> phaser_intensity_displays = null;
    public List<GameObject> phaser_sliders = null;
    private UnityEngine.UI.RawImage[,] phaser_intensity_bars = new UnityEngine.UI.RawImage[2,20];
    private Phasers phasers;

    private float[] phaser_intensities = new float[2] { 0.0f, 0.0f };
    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;

    private List<string> ray_targets = new List<string> { "long_range_intensity", "short_range_intensity" };

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private void Start()
    {
        phasers = GetComponent<Phasers>();
        hud_info = new HUDInfo(CONTROL_NAMES[0], MAX_POWER_CONSUMPTION_BY_INDEX[0]);

        for (int i = 0; i < 2; i++)
        {
            //set buttons
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false)); //increase button

            for (int k = 0; k < 20; k++)
            {
                phaser_intensity_bars[i, k] = phaser_intensity_displays[i].transform.GetChild(k).GetComponent<UnityEngine.UI.RawImage>();
            }
        }

        hud_info.setInfo(INFO_MESSAGE);
        hud_info.setButtons(BUTTON_LISTS[0], 7);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 7);
        hud_info.setPowerConsumption(getPowerConsumptionByIndex(index));
        hud_info.setMaxPowerConsumption(MAX_POWER_CONSUMPTION_BY_INDEX[index]);
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

    public float[] getPhaserIntensities()
    {
        return phaser_intensities;
    }

    public void onPhaserActivationChange()
    {
        handlePowerConsumptionChange();
    }

    private float getPowerConsumptionByIndex(int index)
    {
        float power_consumption = 0.0f;
        if (phaser_intensities[index] > 0.0f)
        {
            power_consumption = (MAX_POWER_CONSUMPTION_BY_INDEX[index] * 0.5f) + (MAX_POWER_CONSUMPTION_BY_INDEX[index] * 0.5f * phaser_intensities[index]);
        }
        for (int i = 0; i < 4; i++)
        {
            if (Mathf.Abs(power_consumption - (i * 0.1f)) < 0.001f)
            {
                return (i * 0.1f);
            }
        }
        return power_consumption;
    }

    private void handlePowerConsumptionChange()
    {
        float consumed_power = getPowerConsumptionByIndex(0);
        consumed_power += getPowerConsumptionByIndex(1);
        ReferenceAssistor.Instance.power_manager.controlPowerChange(1, this.GetType().Name, consumed_power);
    }

    public void adjustPhaserColor(int index, Color new_color)
    {
        for (int i = 0; i < 20; i++)
        {
            new_color.a = phaser_intensity_bars[index, i].color.a;
            phaser_intensity_bars[index, i].color = new_color;
        }
    }

    private void displayAdjustment(int index)
    {
        //move physical slider
        phaser_sliders[index].transform.localPosition = Vector3.Lerp(Vector3.zero, PHASER_SLIDE_DIRECTION, phaser_intensities[index]);

        //adjust screen
        Color phaser_color = phaser_intensity_bars[index, 0].color;
        float tmp_pwr = phaser_intensities[index];
        for (int i = 0; i < 20; i++)
        {
            tmp_pwr = phaser_intensities[index] - (0.05f * i);
            float a = Mathf.Max(0.08f, tmp_pwr / 0.05f);
            phaser_color.a = a;
            phaser_intensity_bars[index, i].color = phaser_color;
        }
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int index = ray_targets.IndexOf(current_target.name);
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
            float new_phaser_intensity;
            if (phaser_direction > 0)
            {
                new_phaser_intensity = Mathf.Min(1.0f, phaser_intensities[index] + dt * MOVE_SPEED);
            }
            else
            {
                new_phaser_intensity = Mathf.Max(0.0f, phaser_intensities[index] - dt * MOVE_SPEED);
            }
            transmitPhaserIntensityAdjustmentRPC(index, new_phaser_intensity);
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

        phasers.updatePhasers();

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        for (int i = 0; i < 2; i++)
        {
            phaser_intensity_displays[i].SetActive(true);
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(true);
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        for (int i = 0; i < 2; i++)
        {
            phaser_intensity_displays[i].SetActive(false);
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
    private void transmitPhaserIntensityAdjustmentRPC(int index, float phaser_prcnt)
    {
        bool update_necessary = (phaser_prcnt == 0.0f || phaser_intensities[index] == 0.0f);
        phaser_intensities[index] = phaser_prcnt;
        BUTTON_LISTS[index][0].updateInteractable(phaser_intensities[index] > 0.0f);
        BUTTON_LISTS[index][1].updateInteractable(phaser_intensities[index] < 1.0f);
        handlePowerConsumptionChange();
        displayAdjustment(index);
        hud_info.setPowerConsumption(getPowerConsumptionByIndex(index));
        if (update_necessary == true)
        {
            phasers.updatePhasers();
        }
    }
}