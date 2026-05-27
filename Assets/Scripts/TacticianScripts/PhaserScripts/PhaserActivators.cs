/*
    PhaserActivators.cs
    - Determines whether phasers are enabled or not
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PhaserActivators : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.2f; //how long it takes for the switch to be flipped
    private static float ENABLE_TIME = 1.0f; //how long it takes for the phaser to charge/uncharge
    private static float MAX_POWER_CONSUMPTION = 0.3f; //equates to 3 circles

    private List<string> CONTROL_NAMES = new List<string>() { "LONG-RANGE PHASER", "SHORT-RANGE LEFT PHASER", "SHORT-RANGE RIGHT PHASER" };
    private static string INFO_MESSAGE = "Enables/disables corresponding phasers. Phasers increase in temperature and can overheat if left enabled for too long.";
    private List<string> CONTROL_DESCS = new List<string> { "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    public List<GameObject> phaser_switches = null;
    public List<GameObject> phaser_coverups = null;
    public GameObject phaser_activator_display;

    private PhaserIntensities phaser_intensities;
    private ShortRangePhasers short_range_phasers;
    private LongRangePhaser long_range_phaser;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine;
    private Coroutine[] phaser_switch_coroutines = { null, null, null };
    private bool[] phaser_is_enabled = { false, false, false };
    private float[] switch_rotations = new float[] { 210.0f, 210.0f, 210.0f };

    private List<string> ray_targets = new List<string> { "long_range_activator", "short_range_left_activator", "short_range_right_activator" };

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    public float lerp_speed = 5f;

    public int finger_position = 0;

    private void Start()
    {
        phaser_intensities = GetComponent<PhaserIntensities>();
        short_range_phasers = GetComponent<ShortRangePhasers>();
        long_range_phaser = GetComponent<LongRangePhaser>();

        hud_info = new HUDInfo(CONTROL_NAMES[0], true);

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[2].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        hud_info.setButtons(BUTTON_LISTS[0], 6);
        hud_info.setInfo(INFO_MESSAGE);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 6);
        float power_consumption = 0.0f;
        if (phaser_is_enabled[index] == true)
        {
            power_consumption = MAX_POWER_CONSUMPTION / 3.0f;
        }
        hud_info.setPowerConsumption(power_consumption);
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

    public bool[] getActivePhasers()
    {
        return phaser_is_enabled;
    }

    private void handlePowerConsumptionChange()
    {
        float consumed_power = 0.0f;
        for (int i = 0; i < 3; i++)
        {
            if (phaser_is_enabled[i] == true)
            {
                consumed_power += (MAX_POWER_CONSUMPTION / 3);
            }
        }
        ReferenceAssistor.Instance.power_manager.controlPowerChange(1, this.GetType().Name, consumed_power);
    }

    IEnumerator switchPhaser(int index)
    {
        bool increasing = true;

        //disable phasers
        if (phaser_is_enabled[index] == true)
        {
            phaser_coverups[index].SetActive(true);
            phaser_is_enabled[index] = false;
            handlePowerConsumptionChange();
            increasing = false;

            // Notify visuals immediately on disable - beams should stop right away
            if (index == 0)
            {
                if (long_range_phaser != null) long_range_phaser.setActive(false);
            }
            else
            {
                if (short_range_phasers != null) short_range_phasers.setBeamActive(index - 1, false);
            }

            if (index == 0)
            {
                phaser_intensities.changeInPower(0, false);
            }
            else
            {
                phaser_intensities.changeInPower(index, phaser_is_enabled[1] == true || phaser_is_enabled[2] == true);
            }
        }

        float switch_time = SWITCH_TIME;
        float charge_time = ENABLE_TIME;

        //flip switch, fill meter
        while (charge_time > 0)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            charge_time = Mathf.Max(0.0f, charge_time - dt);
            switch_time = Mathf.Max(0.0f, switch_time - dt);

            switch_rotations[index] = Mathf.Lerp(210.0f, 285.0f, switch_time / SWITCH_TIME);
            float charge_fill = charge_time / ENABLE_TIME;
            if (increasing == true)
            {
                switch_rotations[index] = Mathf.Lerp(210.0f, 285.0f, 1.0f - (switch_time / SWITCH_TIME));
                charge_fill = 1.0f - (charge_time / ENABLE_TIME);
            }

            phaser_activator_display.transform.GetChild(1 + (index)).GetChild(0).gameObject.GetComponent<UnityEngine.UI.Image>().fillAmount = charge_fill;
            phaser_switches[index].transform.localRotation = Quaternion.Euler(switch_rotations[index], 0.0f, 0.0f);

            if (switch_time <= 0.0f)
            {
                BUTTON_LISTS[index][0].untoggle();
            }

            yield return null;
        }

        //enable phasers
        if (increasing == true)
        {
            phaser_coverups[index].SetActive(false);
            phaser_is_enabled[index] = true;
            handlePowerConsumptionChange();
            if (index == 0)
            {
                phaser_intensities.changeInPower(0, true);
            }
            else
            {
                phaser_intensities.changeInPower(1, true);
            }
            BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[1]);

            // Notify visuals - beam comes on after the charge animation completes
            if (index == 0)
            {
                if (long_range_phaser != null) long_range_phaser.setActive(true);
            }
            else
            {
                if (short_range_phasers != null) short_range_phasers.setBeamActive(index - 1, true);
            }
        }
        else
        {
            BUTTON_LISTS[index][0].updateDesc(CONTROL_DESCS[0]);
        }
        BUTTON_LISTS[index][0].updateInteractable(true);

        phaser_switch_coroutines[index] = null;
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float[] starting_rotations = new float[3] { 0.0f, 0.0f, 0.0f };
        for (int i = 0; i < 3; i++)
        {
            if (phaser_switch_coroutines[i] != null)
            {
                StopCoroutine(phaser_switch_coroutines[i]);
                phaser_switch_coroutines[i] = null;
            }
            phaser_activator_display.transform.GetChild(1 + i).GetChild(0).gameObject.GetComponent<UnityEngine.UI.Image>().fillAmount = 0.0f;
            phaser_is_enabled[i] = false;

            BUTTON_LISTS[i][0].updateDesc(CONTROL_DESCS[0]);
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][0].untoggle();

            starting_rotations[i] = switch_rotations[i];

        if (i == 0)
        {
            if (long_range_phaser != null) long_range_phaser.setActive(false);
        }
        else
        {
            if (short_range_phasers != null) short_range_phasers.setBeamActive(i - 1, false);
        }
        }

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            //turn switches
            for (int i = 0; i < 3; i++)
            {
                switch_rotations[i] = Mathf.Lerp(starting_rotations[i], 210.0f, 1.0f - (anim_time / power_off_time));
                phaser_switches[i].transform.localRotation =
                    Quaternion.Euler(switch_rotations[i], 0.0f, 0.0f);
            }

            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        phaser_activator_display.SetActive(true);
        BUTTON_LISTS[0][0].updateInteractable(true);
        BUTTON_LISTS[1][0].updateInteractable(true);
        BUTTON_LISTS[2][0].updateInteractable(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        phaser_activator_display.SetActive(false);
        hud_info.setPowerConsumption(0.0f);

        //return phasers to 0
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int index = ray_targets.IndexOf(current_target.name);

        if (phaser_switch_coroutines[index] == null)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTON_LISTS[index][0].toggle();
                transmitPhaserPowerRPC(index, phaser_is_enabled[index]);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitPhaserPowerRPC(int index, bool is_enabled)
    {
        phaser_is_enabled[index] = is_enabled;
        if (phaser_switch_coroutines[index] != null)
        {
            StopCoroutine(phaser_switch_coroutines[index]);
        }
        phaser_switch_coroutines[index] = StartCoroutine(switchPhaser(index));

    }
}