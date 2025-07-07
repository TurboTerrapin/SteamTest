/*
    ProbeOrientation.cs
    - Turns lever
    - Adjusts probe heading
    - Affects probe
    Contributor(s): Jake Schott
    Last Updated: 5/15/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ProbeOrientation : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float LEVER_SPEED = 100.0f;
    private static float TURN_SPEED = 50.0f;

    private string CONTROL_NAME = "PROBE ORIENTATION";
    private List<string> CONTROL_DESCS = new List<string> {"TURN LEFT", "TURN RIGHT"};
    private List<int> CONTROL_INDEXES = new List<int>() {4, 5};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject orientation_lever;
    public GameObject orientation_canvas;
    public GameObject probe;

    private float orientation_lever_angle = 0.0f;
    private float orientation_angle = 0.0f;

    private Coroutine orientation_adjustment_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], true, false));
        hud_info.setButtons(BUTTONS);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }
    private void displayAdjustment()
    {
        //set orientation text
        string display_orientation = orientation_angle.ToString();
        if (!display_orientation.Contains("."))
        {
            display_orientation += ".0";
        }
        orientation_canvas.transform.GetChild(1).gameObject.GetComponent<TMP_Text>().SetText(display_orientation + "°");

        //update lever positions
        orientation_lever.transform.localRotation = Quaternion.Euler(270f + orientation_lever_angle, 0f, 90f);

        //update probe
        probe.transform.localRotation = Quaternion.Euler(0f, orientation_angle, 0f);
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

            if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], keys_down))
            {
                orientation_direction += 1;
            }
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], keys_down))
            {
                orientation_direction -= 1;
            }

            if (orientation_direction != 0)
            {
                if (orientation_direction > 0)
                {
                    orientation_lever_angle = Mathf.Max(-35.0f, orientation_lever_angle - dt * LEVER_SPEED);
                }
                else
                {
                    orientation_lever_angle = Mathf.Min(35.0f, orientation_lever_angle + dt * LEVER_SPEED);
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
                if (orientation_lever_angle > 0.0f)
                {
                    orientation_angle -= (orientation_lever_angle / 35.0f) * TURN_SPEED * dt;
                }
                else
                {
                    orientation_angle += (orientation_lever_angle / -35.0f) * TURN_SPEED * dt;
                }
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

            if (orientation_lever_angle != 0.0f)
            {
                transmitProbeOrientationAdjustmentRPC(orientation_angle, orientation_lever_angle);
            }

            keys_down.Clear();
            yield return null;
        }

        orientation_adjustment_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        keys_down = inputs;
        if (orientation_adjustment_coroutine == null)
        {
            for (int i = 0; i < CONTROL_INDEXES.Count; i++)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    orientation_adjustment_coroutine = StartCoroutine(verticalAdjustment());
                    return;
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitProbeOrientationAdjustmentRPC(float or_angle, float lev_angle)
    { 
        orientation_angle = or_angle;
        orientation_lever_angle = lev_angle;
        displayAdjustment();
    }
}