/*
    TractorBeamPower.cs
    - Handles inputs for tractor beam power
    - Moves tractor beam lever accordingly
    Contributor(s): Jake Schott
    Last Updated: 9/1/2025
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public class TractorBeamPower : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 50.0f;
    private static float TRACTOR_BEAM_RANGE = 100.0f;
    private static float MAX_POWER_CONSUMPTION = 0.5f; //equates to 5 circles

    private string CONTROL_NAME = "TRACTOR BEAM";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject lever;
    public GameObject bars_display; //used to display the bars beneath the handle
    public GameObject info_display; //used to display range in meters, visual indicator

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private float power = 0.0f;

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
        //update bars on screen
        float tmp_pwr = power;
        for (int i = 0; i <= 19; i++)
        {
            tmp_pwr = power - (0.05f * i);
            float a = tmp_pwr / 0.05f;
            bars_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.85f, 0.62f, 0.0f, a);
        }

        //update lever position
        lever.transform.localRotation = Quaternion.Euler(-150 + (80 * power), 0f, 0f);

        //update range
        string range_text = (Mathf.Round(power * TRACTOR_BEAM_RANGE * 10.0f) / 10.0f).ToString();
        if (range_text.Contains(".") == false)
        {
            range_text += ".0";
        }
        info_display.transform.GetChild(0).gameObject.SetActive(power > 0.0f);
        info_display.transform.GetChild(0).GetComponent<TMP_Text>().SetText(range_text + "M");

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
            info_display.transform.GetChild(1 + i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
        }
    }
    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }
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
            BUTTONS[0].updateInteractable(power > 0);
            BUTTONS[1].updateInteractable(power < 1);
            transmitTractorBeamPowerAdjustmentRPC(power);
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float start_pow = power;
        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            power = Mathf.Lerp(start_pow, 0.0f, 1.0f - (anim_time / power_off_time));
            displayAdjustment();
            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        bars_display.SetActive(true);
        info_display.SetActive(true);
        BUTTONS[0].updateInteractable(power > 0);
        BUTTONS[1].updateInteractable(power < 1);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        bars_display.SetActive(false);
        info_display.SetActive(false);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);

        //return tractor beam handle/power to 0
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTractorBeamPowerAdjustmentRPC(float pwr)
    {
        power = pwr;
        transform.GetComponent<PowerControl>().power_manager.controlPowerChange(0, this.GetType().Name, pwr * MAX_POWER_CONSUMPTION);
        displayAdjustment();
    }
}