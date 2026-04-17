/*
    ImpulseThrottle.cs
    - Handles inputs for impulse throttle
    - Moves throttle lever accordingly
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static AnimatorHandler;

public class ImpulseThrottle : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 35.0f;
    private static float MAX_POWER_CONSUMPTION = 0.5f; //equates to 5 circles

    private string CONTROL_NAME = "IMPULSE THROTTLE";
    private static string INFO_MESSAGE = "Controls the speed at which the ship moves in either the forward or reverse direction.";
    private List<string> CONTROL_DESCS = new List<string> {"DECREASE", "INCREASE"};
    private List<int> CONTROL_INDEXES = new List<int>() {4, 5};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject handle;
    public GameObject impulse_bars_display; //used to display the bars beneath the handle

    private EngineMonitoring engine_monitoring;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;

    private float impulse = 0.0f;
    private float inertial_dampener_modifier = 0.0f;
    private Vector3 initial_pos; //handle starting position (0% impulse)
    private Vector3 final_pos = new Vector3(0.0f, 0.111f, 0.264f);
    private Coroutine impulse_adjustment_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public GameObject IK_target;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Grasp;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private void Start()
    {
        engine_monitoring = GetComponent<EngineMonitoring>();

        hud_info = new HUDInfo(CONTROL_NAME, true);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false)); //increase button
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);

        initial_pos = handle.transform.localPosition; //sets the initial position
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    public Transform getIKTarget(GameObject current_target)
    {
        return IK_target.transform;
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
    public void adjustInertialDampenerModifier(float new_modifier)
    {
        inertial_dampener_modifier = new_modifier;
    }

    public float getCurrentImpulse()
    {
        return impulse;
    }

    private void displayAdjustment()
    {
        //update bars on screen
        float tmp_imp = impulse;
        for (int i = 0; i <= 19; i++)
        {
            tmp_imp = impulse - (0.05f * i);
            float a = tmp_imp / 0.05f;
            impulse_bars_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
        }

        //update lever position
        handle.transform.localPosition = Vector3.Lerp(initial_pos, final_pos, impulse);

        //update pilot position engine info
        engine_monitoring.impulseAdjustment();
    }

    private bool checkIfChangeNecessary()
    {
        if (is_powered == false)
        {
            return false;
        }
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], keys_down))
        {
            return false;
        }
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && impulse > 0.0f)
        {
            return true;
        }
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], keys_down) && impulse < 1.0f)
        {
            return true;
        }
        return false;
    }

    IEnumerator impulseAdjustment()
    {
        float momentum = 0.01f;
        while (checkIfChangeNecessary())
        {
            int impulse_direction = 0;
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], keys_down) && impulse < 1.0f) //E to increment
            {
                impulse_direction += 1;
            }
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && impulse > 0.0f)  //Q to decrement
            {
                impulse_direction -= 1;
            }
            if (impulse_direction != 0)
            {
                float dt = Mathf.Min(1.0f / 30.0f, Time.deltaTime);
                if (impulse_direction > 0)
                {
                    impulse = Mathf.Min(1.0f, impulse + (dt * MOVE_SPEED * 0.003f * momentum));
                }
                else
                {
                    impulse = Mathf.Max(0.0f, impulse - (dt * MOVE_SPEED * 0.003f * momentum));
                }

                momentum = Mathf.Min(1.1f + (inertial_dampener_modifier * 2.0f), momentum + (dt * (1.1f + (1.0f + (inertial_dampener_modifier * 0.05f)))));

                transmitImpulseAdjustmentRPC(impulse);
            }
            else
            {
                momentum = 0.01f;
            }

            BUTTONS[0].updateInteractable(impulse > 0.0f && is_powered == true);
            BUTTONS[1].updateInteractable(impulse < 1.0f && is_powered == true);

            keys_down.Clear();

            int iterator = 0; //counts frames
            while (keys_down.Count == 0 && iterator < 2)
            {
                yield return null;
                iterator++;
            }
        }

        impulse_adjustment_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        keys_down = inputs;
        if (impulse_adjustment_coroutine == null && is_powered == true)
        {
            if (checkIfChangeNecessary())
            {
                impulse_adjustment_coroutine = StartCoroutine(impulseAdjustment());
            }
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float start_imp = impulse;
        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            impulse = Mathf.Lerp(start_imp, 0.0f, 1.0f - (anim_time / power_off_time));
            displayAdjustment();
            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        BUTTONS[0].updateInteractable(impulse > 0.0f);
        BUTTONS[1].updateInteractable(impulse < 1.0f);
        impulse_bars_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
        impulse_bars_display.SetActive(false);
        hud_info.setPowerConsumption(0.0f);

        //return impulse throttle/impulse to 0
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitImpulseAdjustmentRPC(float imp)
    {
        impulse = imp;
        ReferenceAssistor.Instance.power_manager.controlPowerChange(0, this.GetType().Name, imp * MAX_POWER_CONSUMPTION);
        hud_info.setPowerConsumption(imp * MAX_POWER_CONSUMPTION);
        displayAdjustment();
    }
}