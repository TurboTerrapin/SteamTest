/*
    HorizontalThrusters.cs
    - Handles inputs for horizontal thrusters
    - Extends ThrusterControl.cs
    Contributor(s): Jake Schott
    Last Updated: 5/12/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class HorizontalThrusters : ThrusterControl, IControllable
{
    private string CONTROL_NAME = "HORIZONTAL THRUSTERS";
    private List<string> CONTROL_DESCS = new List<string> { "MOVE LEFT", "MOVE RIGHT" };
    private List<int> CONTROL_INDEXES = new List<int>() {1, 3};
    private List<Button> BUTTONS = new List<Button>();

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        if (hud_info == null)
        {
            hud_info = new HUDInfo(CONTROL_NAME);
            BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, false));
            BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], true, false));
            hud_info.setButtons(BUTTONS);
        }
        return hud_info;
    }

    public float getHorizontalThrusterState()
    {
        return (thruster_percentage[0] - thruster_percentage[1]);
    }

    IEnumerator adjustingThrust()
    {
        while (keys_down.Count > 0 || !checkNeutralState())
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            //check inputs and adjust thruster/button percentages
            for (int i = 0; i < 2; i++)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[i], keys_down))
                {
                    thruster_percentage[i] = Mathf.Min(1.0f, thruster_percentage[i] + (dt * MOVE_SPEED * inertial_dampener_modifier));
                    button_push_percentage[i] = Mathf.Min(1.0f, button_push_percentage[i] + (dt * MOVE_SPEED * PUSH_SPEED));
                }
                else
                {
                    thruster_percentage[i] = Mathf.Max(0.0f, thruster_percentage[i] - (dt * MOVE_SPEED));
                    button_push_percentage[i] = Mathf.Max(0.0f, button_push_percentage[i] - (dt * MOVE_SPEED * PUSH_SPEED));
                }
            }

            transmitHorizontalThrusterRPC(thruster_percentage[0], thruster_percentage[1], button_push_percentage[0], button_push_percentage[1]);
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
        GameObject diamond = display_canvas.transform.GetChild(1).gameObject;
        float diamond_location = (thrust_direction + 1.0f) / 2.0f;

        diamond.transform.localPosition =
            new Vector3(Mathf.Lerp(0.055f, -0.055f, diamond_location),
                        diamond.transform.localPosition.y,
                        diamond.transform.localPosition.z);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitHorizontalThrusterRPC(float left_thrust, float right_thrust, float left_button, float right_button)
    {
        thruster_percentage[0] = left_thrust;
        thruster_percentage[1] = right_thrust;
        button_push_percentage[0] = left_button;
        button_push_percentage[1] = right_button;
        updateThrust();
        displayAdjustment();
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        keys_down = inputs;
        if (thruster_coroutine == null)
        {
            for (int i = 0; i < CONTROL_INDEXES.Count; i++)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    thruster_coroutine = StartCoroutine(adjustingThrust());
                    return;
                }
            }
        }
    }
}