/*
    ThrusterControl.cs
    - Defines binary left/right/up/down structure for thrusters
    - Handles physical buttons
    - Meant to be extended
    Contributor(s): Jake Schott
    Last Updated: 5/12/2025
*/

using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ThrusterControl : NetworkBehaviour
{
    //CLASS CONSTANTS
    protected static float PUSH_SPEED = 4.0f; //how fast the physical button takes to be pushed relative to the bars
    protected static float MOVE_SPEED = 0.5f;

    public List<Transform> thruster_buttons;
    public GameObject display_canvas;

    protected float[] thruster_percentage = new float[2]{0.0f, 0.0f};
    protected float[] button_push_percentage = new float[2]{0.0f, 0.0f};
    protected Vector3 button_initial_pos;
    protected Vector3 button_final_pos;
    protected float inertial_dampener_modifier = 1.0f;
    protected float thrust_direction = 0;
    protected Coroutine thruster_coroutine;

    private void Start()
    {
        button_initial_pos = thruster_buttons[0].transform.localPosition;
        button_final_pos = new Vector3(0.2816f, -1.3473f, 19.1217f);
    }

    public void adjustInertialDampenerModifier(float new_modifier)
    {
        inertial_dampener_modifier = new_modifier;
    }

    protected void updateThrust()
    {
        thrust_direction = thruster_percentage[1] - thruster_percentage[0];
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
        int starting_bar = 10 - (button_index * 10);
        //hide all to start
        for (int i = starting_bar + 1; i < starting_bar + 12; i++)
        {
            display_canvas.transform.GetChild(i).gameObject.SetActive(false);
        }
        int thruster_as_int = (int)(thruster_percentage[button_index] * 100.0f);
        for (int i = starting_bar + 1; i < starting_bar + 12; i++)
        {
            if (thruster_as_int >= (i - starting_bar - 1) * 10)
            {
                display_canvas.transform.GetChild(i).gameObject.SetActive(true);
            }
        }
    }
}
