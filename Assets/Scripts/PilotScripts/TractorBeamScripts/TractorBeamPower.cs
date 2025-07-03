/*
    TractorBeamPower.cs
    - Handles inputs for tractor beam power
    - Moves tractor beam lever accordingly
    Contributor(s): Jake Schott
    Last Updated: 5/12/2025
*/

using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TractorBeamPower : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 50.0f;

    private string CONTROL_NAME = "TRACTOR BEAM";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject lever;
    public GameObject display_canvas; //used to display the bars beneath the handle

    private float power = 0.0f;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], true, false));
        hud_info.setButtons(BUTTONS);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    private void displayAdjustment()
    {
        //update bars on screen
        int power_as_int = (int)(power * 100.0f);
        if (power_as_int < 100)
        {
            int position = (power_as_int / 5);
            if (display_canvas.transform.childCount > position + 1)
            {
                display_canvas.transform.GetChild(position + 1).gameObject.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.5f + (0.5f * (position / 20.0f)), 0.5f, 0f, (0.2f * (power_as_int % 5)));
            }
        }

        //update lever position
        lever.transform.localRotation = Quaternion.Euler(-150 + (80 * power), 0f, 0f);
    }
    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int power_direction = 0;
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //E to increment
        {
            power_direction += 1;
        }
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))  //Q to decrement
        {
            power_direction -= 1;
        }
        if (power_direction != 0)
        {
            if (power_direction > 0)
            {
                power = Mathf.Min(1.0f, power + (0.002f * (power / 0.5f) + 0.001f) * dt * MOVE_SPEED);
            }
            else
            {
                power = Mathf.Max(0.0f, power - (0.002f * (power / 0.5f) + 0.001f) * dt * MOVE_SPEED);
            }
            if (power <= 0)
            {
                hud_info.getButtons()[0].updateInteractable(false);
            }
            else
            {
                hud_info.getButtons()[0].updateInteractable(true);
            }
            if (power >= 1f)
            {
                hud_info.getButtons()[1].updateInteractable(false);
            }
            else
            {
                hud_info.getButtons()[1].updateInteractable(true);
            }
            transmitTractorBeamPowerAdjustmentRPC(power);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTractorBeamPowerAdjustmentRPC(float pwr)
    {
        power = pwr;
        displayAdjustment();
    }
}