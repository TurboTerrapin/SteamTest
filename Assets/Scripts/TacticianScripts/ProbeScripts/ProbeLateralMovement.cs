/*
    ProbeLateralMovement.cs
    - Pushes in lateral movement buttons
    - Adjusts probe controller screen (the four directional arcs)
    - Affects probe position if host
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ProbeLateralMovement : NetworkBehaviour, IControllable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float BUTTON_SPEED = 10.0f;
    private static float PROBE_SPEED = 10.0f;
    private static Vector3 LATERAL_BUTTON_MOVE_DIRECTION = new Vector3(0.0016f, -0.006f, 0.0016f);

    private string CONTROL_NAME = "PROBE LATERAL MOVEMENT";
    private static string INFO_MESSAGE = "Handles the forward, reverse, left, and right movements of an active probe.";
    private List<string> CONTROL_DESCS = new List<string> { "FORWARD", "REVERSE", "LEFT", "RIGHT"};
    private List<int> CONTROL_INDEXES = new List<int>() {0, 2, 1, 3};
    private List<Button> BUTTONS = new List<Button>();

    public List<GameObject> lateral_buttons = null; //forward, reverse, left, right
    public GameObject probe_monitoring_display;

    private bool is_active = false;
    private GameObject probe;
    private Vector3[] initial_positions = new Vector3[4];
    private Vector3[] final_positions = new Vector3[4];
    private float[] lateral_movement_factors = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f }; //forward, reverse, left, right
    private Coroutine lateral_adjustment_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public int finger_position = 0;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);

        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[3], false, false));

        hud_info.setButtons(BUTTONS, 8);
        hud_info.setInfo(INFO_MESSAGE);

        for (int i = 0; i <= 3; i++)
        {
            initial_positions[i] = lateral_buttons[i].transform.localPosition;
            final_positions[i] = lateral_buttons[i].transform.localPosition + LATERAL_BUTTON_MOVE_DIRECTION;
        }
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }
    public Transform getIKTarget(GameObject current_target)
    {
        return IK_targets[0].transform;
        /*
        finger_position = 0;
        for (int i = 0; i < button_push_percentage.Length; i++)
        {
            if (button_push_percentage[i] > 0) finger_position = i + 1;
        }
        return IK_targets[finger_position].transform;
        
        int index = ray_targets.IndexOf(current_target.name);
        return IK_targets[index].transform;
        */
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

    private void displayAdjustment()
    {
        //push lateral buttons, update circle
        for (int i = 0; i <= 3; i++)
        {
            lateral_buttons[i].transform.localPosition = Vector3.Lerp(initial_positions[i], final_positions[i], lateral_movement_factors[i]);
            probe_monitoring_display.transform.GetChild(i + 1).gameObject.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, Mathf.Max(0.2f, lateral_movement_factors[i]));
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
            for (int i = 0; i <= 3; i++)
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

            for (int i = 0; i <= 3; i++)
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
        for (int i = 0; i <= 3; i++)
        {
            BUTTONS[i].updateInteractable(true);
        }
    }

    public void deactivate()
    {
        is_active = false;
        for (int i = 0; i <= 3; i++)
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