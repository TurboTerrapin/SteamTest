/*
    ProbeOrientation.cs
    - Turns lever
    - Affects probe if host
    Contributor(s): Jake Schott
    Last Updated: 8/25/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ProbeOrientation : NetworkBehaviour, IControllable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float LEVER_SPEED = 5.0f;
    private static float MAX_LEVER_ANGLE = 35.0f;
    private static float TURN_SPEED = 100.0f;
    private static float MAX_POWER_CONSUMPTION = 0.2f; //equates to 2 circles

    private string CONTROL_NAME = "PROBE ORIENTATION";
    private static string INFO_MESSAGE = "Handles the rotation and turning of an active probe.";
    private List<string> CONTROL_DESCS = new List<string> {"TURN LEFT", "TURN RIGHT"};
    private List<int> CONTROL_INDEXES = new List<int>() {4, 5};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject orientation_lever;

    private bool is_active = false;
    private GameObject probe;
    private float orientation_lever_angle = 0.0f;
    private float orientation_angle = 0.0f;
    private Coroutine orientation_adjustment_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public GameObject IK_target = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME, MAX_POWER_CONSUMPTION);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);
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

    private void updatePowerConsumption(float consumption)
    {
        hud_info.setPowerConsumption(consumption);
        ReferenceAssistor.Instance.power_manager.controlPowerChange(1, this.GetType().Name, consumption);
    }

    private void displayAdjustment()
    {
        //update lever positions
        orientation_lever.transform.localRotation = Quaternion.Euler(0.0f, MAX_LEVER_ANGLE * (-orientation_lever_angle), 0.0f);

        //update power
        if (is_active == true)
        {
            updatePowerConsumption(Mathf.Abs(orientation_lever_angle) * MAX_POWER_CONSUMPTION);
        }
    }

    public void linkProbe(GameObject new_probe)
    {
        probe = new_probe;
        orientation_angle = new_probe.transform.rotation.eulerAngles.y;
        for (int i = 0; i < 2; i++)
        {
            BUTTONS[i].updateInteractable(true);
        }
        displayAdjustment();
        activate();
    }

    public void unlinkProbe()
    {
        probe = null;
        for (int i = 0; i < 2; i++)
        {
            BUTTONS[i].updateInteractable(false);
        }
        orientation_angle = 0.0f;
        displayAdjustment();
        deactivate();
    }

    private bool isNeutralState()
    {
        return (orientation_lever_angle == 0.0f);
    }

    IEnumerator verticalAdjustment()
    {
        while (keys_down.Count > 0 || !isNeutralState())
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            int orientation_direction = 0;

            if (is_active == true)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], keys_down) && probe != null)
                {
                    orientation_direction += 1;
                }
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && probe != null)
                {
                    orientation_direction -= 1;
                }
            }

            if (orientation_direction != 0)
            {
                if (orientation_direction > 0)
                {
                    orientation_lever_angle = Mathf.Max(-1.0f, orientation_lever_angle - dt * LEVER_SPEED);
                }
                else
                {
                    orientation_lever_angle = Mathf.Min(1.0f, orientation_lever_angle + dt * LEVER_SPEED);
                }
            }
            else
            {
                if (orientation_lever_angle > 0.0f)
                {
                    orientation_lever_angle = Mathf.Max(0.0f, orientation_lever_angle - dt * LEVER_SPEED);
                }
                else
                {
                    orientation_lever_angle = Mathf.Min(0.0f, orientation_lever_angle + dt * LEVER_SPEED);
                }
            }

            if (Mathf.Abs(orientation_lever_angle) > 0.0f)
            {
                orientation_angle -= orientation_lever_angle * TURN_SPEED * dt;
                orientation_angle = (Mathf.Round(orientation_angle * 10) / 10.0f);
                if (orientation_angle > 359.9f)
                {
                    orientation_angle -= 360.0f;
                }
                else if (orientation_angle < 0.0f)
                {
                    orientation_angle += 360.0f;
                }
            }

            transmitProbeOrientationAdjustmentRPC(orientation_angle, orientation_lever_angle);

            keys_down.Clear();
            yield return null;
        }

        orientation_adjustment_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_active == false)
        {
            return;
        }

        keys_down = inputs;
        if (orientation_adjustment_coroutine == null)
        {
            for (int i = 0; i < CONTROL_INDEXES.Count; i++)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    orientation_adjustment_coroutine = StartCoroutine(verticalAdjustment());
                    return;
                }
            }
        }
    }

    public void activate()
    {
        is_active = true;
        BUTTONS[0].updateInteractable(probe != null);
        BUTTONS[1].updateInteractable(probe != null);
    }

    public void deactivate()
    {
        is_active = false;
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
        updatePowerConsumption(0.0f);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitProbeOrientationAdjustmentRPC(float or_angle, float lev_angle)
    {
        orientation_angle = or_angle;
        orientation_lever_angle = lev_angle;
        displayAdjustment();

        //update probe if host
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (probe != null)
            {
                probe.transform.rotation = Quaternion.Euler(0.0f, orientation_angle, 0.0f);
            }
        }
    }
}