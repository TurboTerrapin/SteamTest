/*
    ProbeVerticalMovement.cs
    - Turns lever
    - Affects probe if host
    - Tells ProbeInfo to update altimeter
    Contributor(s): Jake Schott
    Last Updated: 7/4/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static AnimatorHandler;

public class ProbeVerticalMovement : NetworkBehaviour, IControllable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float LEVER_SPEED = 200.0f;
    private static float PROBE_SPEED = 0.5f;

    private string CONTROL_NAME = "PROBE VERTICAL MOVEMENT";
    private static string INFO_MESSAGE = "Handles the up and down movement of an active probe.";
    private List<string> CONTROL_DESCS = new List<string> {"DESCEND", "ASCEND"};
    private List<int> CONTROL_INDEXES = new List<int>(){ 2,0 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject vertical_lever;
    private ProbeInfo probe_info;

    private bool is_active = false;
    private GameObject probe;
    private float vertical_lever_angle = 0.0f;
    private float altimeter_update_time_remaining = 0.0f; //needed to account for the delay in positional updates driven by host
    private Coroutine altimeter_update_coroutine = null;
    private Coroutine vertical_adjustment_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public GameObject IK_target = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public int finger_position = 0;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private void Start()
    {
        probe_info = GetComponent<ProbeInfo>();

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        hud_info.setButtons(BUTTONS, 7);
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

    private void displayAdjustment()
    {
        //update lever position
        vertical_lever.transform.localRotation = Quaternion.Euler(-114.0f + vertical_lever_angle, 45.0f, 90.0f);

        //notify probe controller
        GetComponent<ProbeController>().onProbeDistanceChange();

        //update altimeter
        if (probe != null)
        {
            altimeter_update_time_remaining = 1.0f;
            if (altimeter_update_coroutine == null)
            {
                altimeter_update_coroutine = StartCoroutine(altimeterUpdater());
            }
        }
    }

    public void linkProbe(GameObject new_probe)
    {
        probe = new_probe;
        for (int i = 0; i <= 1; i++)
        {
            BUTTONS[i].updateInteractable(true);
        }
        activate();
        probe_info.displayProbeAltitude(probe.transform.position.y);
    }

    public void unlinkProbe()
    {
        probe = null;
        for (int i = 0; i <= 1; i++)
        {
            BUTTONS[i].updateInteractable(false);
        }
        deactivate();
    }

    private bool isNeutralState()
    {
        return (vertical_lever_angle == 0.0f);
    }

    IEnumerator verticalAdjustment()
    {
        while (keys_down.Count > 0 || !isNeutralState())
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            Vector3 positional_adjustment = Vector3.zero;

            int vertical_direction = 0;

            if (is_active == true)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], keys_down) && probe != null)
                {
                    vertical_direction += 1;
                }
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && probe != null)
                {
                    vertical_direction -= 1;
                }
            }

            if (vertical_direction != 0)
            {
                if (vertical_direction > 0)
                {
                    vertical_lever_angle = Mathf.Min(35.0f, vertical_lever_angle + dt * LEVER_SPEED);
                }
                else
                {
                    vertical_lever_angle = Mathf.Max(-35.0f, vertical_lever_angle - dt * LEVER_SPEED);
                }
            }
            else
            {
                if (vertical_lever_angle > 0.0f)
                {
                    vertical_lever_angle = Mathf.Max(0.0f, vertical_lever_angle - dt * LEVER_SPEED);
                }
                else
                {
                    vertical_lever_angle = Mathf.Min(0.0f, vertical_lever_angle + dt * LEVER_SPEED);
                }
            }

            if (Mathf.Abs(vertical_lever_angle) > 0.0f && probe != null)
            {
                positional_adjustment = probe.transform.up * vertical_lever_angle * dt * PROBE_SPEED;
            }

            if (vertical_lever_angle != 0.0f)
            {
                transmitProbeVerticalAdjustmentRPC(positional_adjustment, vertical_lever_angle);
            }

            keys_down.Clear();
            yield return null;
        }

        vertical_adjustment_coroutine = null;
    }

    IEnumerator altimeterUpdater()
    {
        while (probe != null && altimeter_update_time_remaining > 0.0f)
        {
            altimeter_update_time_remaining = Mathf.Max(0.0f, altimeter_update_time_remaining - Time.deltaTime);

            probe_info.displayProbeAltitude(probe.transform.position.y);

            yield return null;
        }

        altimeter_update_coroutine = null;
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
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_active == false)
        {
            return;
        }

        keys_down = inputs;
        if (vertical_adjustment_coroutine == null)
        {
            for (int i = 0; i < CONTROL_INDEXES.Count; i++)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    vertical_adjustment_coroutine = StartCoroutine(verticalAdjustment());
                    return;
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitProbeVerticalAdjustmentRPC(Vector3 positional_adjustment, float ang)
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (probe != null)
            {
                probe.transform.localPosition += positional_adjustment;
            }
        }
        vertical_lever_angle = ang;
        displayAdjustment();
    }
}