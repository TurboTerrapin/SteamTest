/*
    TractorBeamPower.cs
    - Handles inputs for tractor beam power
    - Moves tractor beam lever accordingly
    Contributor(s): Jake Schott
    Last Updated: 7/3/2025
*/

using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class TractorBeamPower : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 50.0f;
    private static float TRACTOR_BEAM_RANGE = 100.0f;

    private string CONTROL_NAME = "TRACTOR BEAM";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public Material lit_green;
    public Material lit_red;
    public Material unlit_green;
    public Material unlit_red;

    public GameObject lever;
    public GameObject bars_canvas; //used to display the bars beneath the handle
    public GameObject info_canvas; //used to display range in meters, visual indicator
    public GameObject active_indicator; //green light
    public GameObject inactive_indicator; //red light

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
        float tmp_pwr = power;
        for (int i = 0; i <= 19; i++)
        {
            tmp_pwr = power - (0.05f * i);
            float a = tmp_pwr / 0.05f;
            bars_canvas.transform.GetChild(1 + i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.85f, 0.62f, 0.0f, a);
        }

        //update lever position
        lever.transform.localRotation = Quaternion.Euler(-150 + (80 * power), 0f, 0f);

        //update range
        string range_text = (Mathf.Round(power * TRACTOR_BEAM_RANGE * 10.0f) / 10.0f).ToString();
        if (range_text.Contains(".") == false)
        {
            range_text += ".0";
        }
        info_canvas.transform.GetChild(1).gameObject.SetActive(power > 0.0f);
        info_canvas.transform.GetChild(1).GetComponent<TMP_Text>().SetText(range_text + "M");

        float tmp_power = power;
        //update the waves thing
        for (int i = 0; i <= 4; i++)
        {
            tmp_power = power - (0.2f * i);
            float a = 0.0f;
            if (tmp_power > 0.0f)
            {
                a = tmp_power / 0.2f;
            }
            info_canvas.transform.GetChild(2 + i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
        }
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