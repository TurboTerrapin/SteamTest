/*
    ThrusterControl.cs
    - Defines binary left/right/up/down structure for thrusters
    - Handles physical buttons
    - Meant to be extended
    Contributor(s): Jake Schott
    Last Updated: 9/1/2025
*/

using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ThrusterControl : NetworkBehaviour
{
    //CLASS CONSTANTS
    protected static float PUSH_SPEED = 4.0f; //how fast the physical button takes to be pushed relative to the bars
    protected static float MOVE_SPEED = 0.5f;
    protected static float MAX_POWER_CONSUMPTION = 0.1f; //equates to 1 circle

    public List<Transform> thruster_buttons;
    public GameObject thruster_display;

    protected float[] thruster_percentage = new float[2]{0.0f, 0.0f};
    protected float[] button_push_percentage = new float[2]{0.0f, 0.0f};
    protected Vector3 button_initial_pos;
    protected Vector3 button_final_pos;
    protected float inertial_dampener_modifier = 0.0f;
    protected float thrust_direction = 0;
    protected Coroutine thruster_coroutine;

    public void adjustInertialDampenerModifier(float new_modifier)
    {
        inertial_dampener_modifier = new_modifier;
    }

    protected void updateThrust()
    {
        thrust_direction = thruster_percentage[1] - thruster_percentage[0];
        float greatest_thruster = Mathf.Max(thruster_percentage[0], thruster_percentage[1]);
        transform.GetComponent<PowerControl>().power_manager.controlPowerChange(0, this.GetType().Name, greatest_thruster * MAX_POWER_CONSUMPTION);
    }

    protected bool checkNeutralState()
    {
        bool neutral_state = true;
        for (int i = 0; i < 2; i++)
        {
            if (thruster_percentage[i] > 0.0f)
            {
                neutral_state = false;
            }
        }
        for (int i = 0; i < 2; i++)
        {
            if (button_push_percentage[i] > 0.0f)
            {
                neutral_state = false;
            }
        }
        return neutral_state;
    }
    protected void adjustButton(Transform thruster_button, int button_index)
    {
        //push the physical button in
        thruster_button.transform.localPosition =
            new Vector3(Mathf.Lerp(button_initial_pos.x, button_final_pos.x, button_push_percentage[button_index]),
                        Mathf.Lerp(button_initial_pos.y, button_final_pos.y, button_push_percentage[button_index]),
                        Mathf.Lerp(button_initial_pos.z, button_final_pos.z, button_push_percentage[button_index]));

        //handle thruster bars
        int starting_bar = 11 - (button_index * 10);
        //hide all to start
        for (int i = starting_bar; i < starting_bar + 10; i++)
        {
            thruster_display.transform.GetChild(i).gameObject.SetActive(false);
        }
        int thruster_as_int = (int)(thruster_percentage[button_index] * 100.0f);
        for (int i = starting_bar; i < starting_bar + 10; i++)
        {
            if (thruster_as_int >= (i - starting_bar + 1) * 10)
            {
                thruster_display.transform.GetChild(i).gameObject.SetActive(true);
            }
        }
    }
}
