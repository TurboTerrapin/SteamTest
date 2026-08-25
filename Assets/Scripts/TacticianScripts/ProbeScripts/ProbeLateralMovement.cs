/*
    ProbeLateralMovement.cs
    - Moves probe lateral movement stick
    - Adjusts probe controller screen (the four directional arcs)
    - Affects probe position if host
    Contributor(s): Jake Schott
    Last Updated: 8/25/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ProbeLateralMovement : NetworkBehaviour, IControllable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float BUTTON_SPEED = 10.0f;
    private static float PROBE_SPEED = 25.0f;
    private static float LATERAL_MOVEMENT_STICK_RADIUS = 0.008f;

    private string CONTROL_NAME = "PROBE LATERAL MOVEMENT";
    private static string INFO_MESSAGE = "Handles the forward, reverse, left, and right movements of an active probe.";
    private List<string> CONTROL_DESCS = new List<string> { "FORWARD", "REVERSE", "LEFT", "RIGHT"};
    private List<int> CONTROL_INDEXES = new List<int>() {0, 2, 1, 3};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject probe_lateral_movement_stick;
    public GameObject probe_monitoring_display;

    private bool is_active = false;
    private GameObject probe;
    private float[] lateral_movement_factors = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f }; //forward, reverse, left, right
    private Coroutine lateral_adjustment_coroutine = null;

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
        hud_info = new HUDInfo(CONTROL_NAME);

        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[3], false, false));

        hud_info.setButtons(BUTTONS, 8);
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
        //update stick position
        float horizontal_pos = (lateral_movement_factors[3] - lateral_movement_factors[2]);
        float vertical_pos = (lateral_movement_factors[1] - lateral_movement_factors[0]);
        Vector2 stick_position = new Vector2(horizontal_pos, vertical_pos);
        if (stick_position.magnitude > 1.0f)
        {
            stick_position.Normalize();
        }
        probe_lateral_movement_stick.transform.localPosition = new Vector3(LATERAL_MOVEMENT_STICK_RADIUS * stick_position.x, LATERAL_MOVEMENT_STICK_RADIUS * stick_position.y, 0.0f);

        //don't update if already disconnected
        if (GetComponent<ProbeController>().getProbeIsConnected() == false)
        {
            return;
        }

        //update directional arcs
        for (int i = 0; i < 4; i++)
        {
            probe_monitoring_display.transform.GetChild(i + 1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, Mathf.Max(0.2f, lateral_movement_factors[i]));
        }

        //notify probe controller
        GetComponent<ProbeController>().onProbeDistanceChange();
    }

    public void linkProbe(GameObject new_probe)
    {
        probe = new_probe;
        for (int i = 0; i <= 3; i++)
        {
            BUTTONS[i].updateInteractable(true);
        }
        activate();
    }

    public void unlinkProbe()
    {
        probe = null;
        for (int i = 0; i <= 3; i++)
        {
            BUTTONS[i].updateInteractable(false);
        }
        deactivate();
    }

    private bool isNeutralState()
    {
        for (int i = 0; i <= 3; i++)
        {
            if (lateral_movement_factors[i] != 0.0f)
            {
                return false;
            }
        }
        return true;
    }

    IEnumerator lateralAdjustment()
    {
        while (keys_down.Count > 0 || !isNeutralState())
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            Vector3 positional_adjustment = Vector3.zero;

            //check inputs/return buttons to default
            for (int i = 0; i < 4; i++)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[i], keys_down) && probe != null && is_active == true)
                {
                    lateral_movement_factors[i] = Mathf.Min(1.0f, lateral_movement_factors[i] + dt * BUTTON_SPEED);
                }
                else
                {
                    lateral_movement_factors[i] = Mathf.Max(0.0f, lateral_movement_factors[i] - dt * BUTTON_SPEED);
                }
            }

            //if probe is active, update its position
            if (probe != null)
            {
                if (Mathf.Abs(lateral_movement_factors[0] - lateral_movement_factors[1]) > 0.0f)
                {
                    positional_adjustment += probe.transform.forward * (lateral_movement_factors[0] - lateral_movement_factors[1]) * dt * PROBE_SPEED;
                }
                if (Mathf.Abs(lateral_movement_factors[3] - lateral_movement_factors[2]) > 0.0f)
                {
                    positional_adjustment += probe.transform.right * (lateral_movement_factors[3] - lateral_movement_factors[2]) * dt * PROBE_SPEED;
                }
            }

            for (int i = 0; i < 4; i++)
            {
                if (lateral_movement_factors[i] != 1.0f)
                {
                    transmitProbeLateralAdjustmentRPC(positional_adjustment, lateral_movement_factors[0], lateral_movement_factors[1], lateral_movement_factors[2], lateral_movement_factors[3]);
                    break;
                }
            }

            keys_down.Clear();
            yield return null;
        }

        lateral_adjustment_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_active == false)
        {
            return;
        }

        keys_down = inputs;
        if (lateral_adjustment_coroutine == null)
        {
            for (int i = 0; i < CONTROL_INDEXES.Count; i++)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    lateral_adjustment_coroutine = StartCoroutine(lateralAdjustment());
                    return;
                }
            }
        }
    }

    public void activate()
    {
        is_active = true;
        for (int i = 0; i < 4; i++)
        {
            BUTTONS[i].updateInteractable(true);
        }
    }

    public void deactivate()
    {
        is_active = false;
        for (int i = 0; i < 4; i++)
        {
            BUTTONS[i].updateInteractable(false);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitProbeLateralAdjustmentRPC(Vector3 positional_adjustment, float fwd, float rev, float left, float right)
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (probe != null)
            {
                probe.transform.localPosition += positional_adjustment;
            }
        }
        lateral_movement_factors[0] = fwd;
        lateral_movement_factors[1] = rev;
        lateral_movement_factors[2] = left;
        lateral_movement_factors[3] = right;
        displayAdjustment();
    }
}