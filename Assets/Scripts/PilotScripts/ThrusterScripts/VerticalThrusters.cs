/*
    VerticalThrusters.cs
    - Handles inputs for vertical thrusters
    - Extends ThrusterControl.cs
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class VerticalThrusters : ThrusterControl, IControllable, IPowerable, IIKTargetable
{
    private string CONTROL_NAME = "VERTICAL THRUSTERS";
    private static string INFO_MESSAGE = "Controls the altitude of the ship through upward and downward movement.";
    private List<string> CONTROL_DESCS = new List<string>{"DESCEND", "ASCEND"};
    private List<int> CONTROL_INDEXES = new List<int>(){2, 0};
    private List<Button> BUTTONS = new List<Button>();

    private List<KeyCode> keys_down = new List<KeyCode>();

    private bool is_powered = false;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public int finger_position = 0;

    private void Start()
    {
        button_initial_pos = thruster_buttons[0].transform.localPosition;

        hud_info = new HUDInfo(CONTROL_NAME, true);
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
        finger_position = 0;
        for(int i = 0; i < button_push_percentage.Length; i++)
        {
            if (button_push_percentage[i] > 0) finger_position = i + 1;
        }
        


        return IK_targets[finger_position].transform;
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
    public float getVerticalThrusterState()
    {
        return (thruster_percentage[1] - thruster_percentage[0]);
    }

    IEnumerator adjustingThrust()
    {
        while (keys_down.Count > 0 || !checkNeutralState())
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            //check inputs and adjust thruster/button percentages
            for (int i = 0; i < 2; i++)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[i], keys_down) && is_powered == true)
                {
                    thruster_percentage[i] = Mathf.Min(1.0f, thruster_percentage[i] + (dt * MOVE_SPEED * (1.0f + (1.5f * inertial_dampener_modifier))));
                    button_push_percentage[i] = Mathf.Min(1.0f, button_push_percentage[i] + (dt * MOVE_SPEED * PUSH_SPEED));
                }
                else
                {
                    thruster_percentage[i] = Mathf.Max(0.0f, thruster_percentage[i] - (dt * MOVE_SPEED));
                    button_push_percentage[i] = Mathf.Max(0.0f, button_push_percentage[i] - (dt * MOVE_SPEED * PUSH_SPEED));
                }
            }

            transmitVerticalThrusterRPC(thruster_percentage[0], thruster_percentage[1], button_push_percentage[0], button_push_percentage[1]);
            keys_down.Clear();
            yield return null;
        }
        thruster_coroutine = null;
    }
    private void displayAdjustment()
    {
        //adjust physical buttons
        adjustButton(thruster_buttons[0], 0);
        adjustButton(thruster_buttons[1], 1);

        //update diamond
        GameObject diamond = thruster_display.transform.GetChild(0).gameObject;
        float diamond_location = (thrust_direction + 1.0f) / 2.0f;

        diamond.transform.localPosition =
            new Vector3(Mathf.Lerp(0.055f, -0.055f, diamond_location),
                        diamond.transform.localPosition.y,
                        diamond.transform.localPosition.z);
    }

    public void powerOn(int position)
    {
        is_powered = true;
        BUTTONS[0].updateInteractable(true);
        BUTTONS[1].updateInteractable(true);
        thruster_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
        thruster_display.SetActive(false);
        hud_info.setPowerConsumption(0.0f);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitVerticalThrusterRPC(float down_thrust, float up_thrust, float down_button, float up_button)
    {
        thruster_percentage[0] = down_thrust;
        thruster_percentage[1] = up_thrust;
        button_push_percentage[0] = down_button;
        button_push_percentage[1] = up_button;
        updateThrust();
        displayAdjustment();
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        keys_down = inputs;
        if (thruster_coroutine == null && is_powered == true)
        {
            for (int i = 0; i < CONTROL_INDEXES.Count; i++)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    thruster_coroutine = StartCoroutine(adjustingThrust());
                    return;
                }
            }
        }
    }
}