/*
    ProbeOrientation.cs
    - Turns lever
    - Adjusts probe heading
    - Affects probe
    Contributor(s): Jake Schott
    Last Updated: 8/22/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ProbeOrientation : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float LEVER_SPEED = 50.0f;
    private static float TURN_SPEED = 25.0f;

    private string CONTROL_NAME = "PROBE ORIENTATION";
    private List<string> CONTROL_DESCS = new List<string> {"TURN LEFT", "TURN RIGHT"};
    private List<int> CONTROL_INDEXES = new List<int>() {4, 5};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject orientation_lever;
    public GameObject orientation_display;
    public GameObject orientation_icon_display;

    private bool is_powered = false;
    private GameObject probe;
    private float orientation_lever_angle = 0.0f;
    private float orientation_angle = 0.0f;

    private Coroutine orientation_adjustment_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
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
        orientation_display.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().SetText(display_orientation + "°");

        //update lever positions
        orientation_lever.transform.localRotation = Quaternion.Euler(270f + orientation_lever_angle, 0f, 90f);

        //update probe
        if (probe != null)
        {
            probe.transform.localRotation = Quaternion.Euler(0f, orientation_angle, 0f);
        }
    }

    public void linkProbe(GameObject new_probe)
    {
        probe = new_probe;
        for (int i = 0; i <= 1; i++)
        {
            BUTTONS[i].updateInteractable(true);
        }
        orientation_angle = (Mathf.Round(probe.transform.localRotation.eulerAngles.y * 10) / 10.0f);
        //show heading
        orientation_display.transform.GetChild(0).gameObject.SetActive(true);
        //lighten probe icon
        orientation_icon_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        displayAdjustment();
    }

    public void unlinkProbe()
    {
        probe = null;
        for (int i = 0; i <= 1; i++)
        {
            BUTTONS[i].updateInteractable(false);
        }
        orientation_angle = 0.0f;
        //hide heading
        orientation_display.transform.GetChild(0).gameObject.SetActive(false);
        //darken probe icon
        orientation_icon_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.2f);
        displayAdjustment();
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

            if (is_powered == true)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], keys_down) && probe != null)
                {
                    orientation_direction += 1;
                }
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && probe != null)
                {
                    orientation_direction -= 1;
                }
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
        if (is_powered == false)
        {
            return;
        }

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

    public void powerOn(int position)
    {
        is_powered = true;
        orientation_display.SetActive(true);
        orientation_icon_display.SetActive(true);
        BUTTONS[0].updateInteractable(probe != null);
        BUTTONS[1].updateInteractable(probe != null);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        orientation_display.SetActive(false);
        orientation_icon_display.SetActive(false);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitProbeOrientationAdjustmentRPC(float or_angle, float lev_angle)
    { 
        orientation_angle = or_angle;
        orientation_lever_angle = lev_angle;
        displayAdjustment();
    }
}