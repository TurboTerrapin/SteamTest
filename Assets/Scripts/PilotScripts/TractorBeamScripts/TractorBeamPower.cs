/*
    TractorBeamPower.cs
    - Handles inputs for tractor beam power
    - Moves tractor beam lever accordingly
    Contributor(s): Jake Schott, Henryk Musial
    Last Updated: 1/25/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TractorBeamPower : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 75.0f;
    public static float TRACTOR_BEAM_RANGE = 50.0f;
    private static float MAX_POWER_CONSUMPTION = 0.5f; //equates to 5 circles

    private string CONTROL_NAME = "TRACTOR BEAM";
    private static string INFO_MESSAGE = "Controls the strength and radius of tractor beam for item collection and analysis.";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject tractor_beam_handle;
    public GameObject tractor_beam_active_indicator;
    public GameObject tractor_beam_inactive_indicator;
    public GameObject bars_display; //used to display the bars beneath the handle
    public GameObject info_display;
    private GameObject range_display; 
    private GameObject item_captured_display;
    private Material lit_green;
    private Material lit_red;
    private Material unlit_green;
    private Material unlit_red;
    private TractorBeamOptions tractor_beam_options;
    private TractorBeam tractor_beam;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private float power = 0.0f;

    private static HUDInfo hud_info = null;

    private void Start()
    {
        tractor_beam_options = GetComponent<TractorBeamOptions>();
        tractor_beam = GetComponent<TractorBeam>();
        lit_green = GetComponent<TorpedoTrigger>().lit_green;
        unlit_green = GetComponent<TorpedoTrigger>().unlit_green;
        lit_red = GetComponent<TorpedoTrigger>().lit_red;
        unlit_red = GetComponent<TorpedoTrigger>().unlit_red;
        range_display = info_display.transform.GetChild(0).gameObject;
        item_captured_display = info_display.transform.GetChild(1).gameObject;

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

    private void displayAdjustment()
    {
        //update bars on screen
        float tmp_pwr = power;
        for (int i = 0; i <= 9; i++)
        {
            tmp_pwr = power - (0.1f * i);
            float a = Mathf.Lerp(0.05f, 1.0f, tmp_pwr / 0.1f);
            bars_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
        }

        //update handle rotation
        tractor_beam_handle.transform.localRotation = Quaternion.Euler(-150.0f + (80.0f * power), 0.0f, 0.0f);

        //update either range or item captured screen
        range_display.SetActive(tractor_beam.GetCapturedItem() == null);
        item_captured_display.SetActive(tractor_beam.GetCapturedItem() != null);
        if (tractor_beam.GetCapturedItem() == null)
        {
            //update range
            string range_text = (Mathf.Round(power * TRACTOR_BEAM_RANGE * 10.0f) / 10.0f).ToString();
            if (range_text.Contains(".") == false)
            {
                range_text += ".0";
            }
            range_display.transform.GetChild(0).GetChild(0).gameObject.SetActive(power == 0.0f);
            range_display.transform.GetChild(0).GetChild(1).gameObject.SetActive(power > 0.0f);
            range_display.transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().SetText(range_text + "M");

            //update the waves thing
            range_display.transform.GetChild(1).GetChild(0).GetChild(0).gameObject.SetActive(power > 0.0f);
            float tmp_power = power;
            for (int i = 0; i <= 3; i++)
            {
                tmp_power = power - (0.25f * i);
                float a = 0.05f;
                if (tmp_power > 0.0f)
                {
                    a = Mathf.Lerp(0.05f, 1.0f, tmp_power / 0.25f);
                }
                range_display.transform.GetChild(1).GetChild(1 + i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
            }
        }

        //redraw tractor beam cone
        tractor_beam.UpdateBeam(power);
    }

    //called by TractorBeam
    public void onItemCapturedChange()
    {
        displayAdjustment();
        if (tractor_beam.GetCapturedItem() != null)
        {
            //update item captured
            Color c = tractor_beam_options.getCapturedItemColor();
            item_captured_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().texture = tractor_beam_options.getCapturedItemTexture();
            c.a = 1.0f;
            item_captured_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = c;
            setTractorBeamStatusIndicators(false);
        }
        else
        {
            tractor_beam.UpdateBeam(power);
        }
    }

    //called by TractorBeam
    public void setTractorBeamStatusIndicators(bool active)
    {
        if (is_powered == false || tractor_beam.GetCapturedItem() != null)
        {
            tractor_beam_active_indicator.GetComponent<Renderer>().material = unlit_green;
            tractor_beam_inactive_indicator.GetComponent<Renderer>().material = unlit_red;
            return;
        }

        if (active == true)
        {
            tractor_beam_active_indicator.GetComponent<Renderer>().material = lit_green;
            tractor_beam_inactive_indicator.GetComponent<Renderer>().material = unlit_red;
        }
        else
        {
            tractor_beam_active_indicator.GetComponent<Renderer>().material = unlit_green;
            tractor_beam_inactive_indicator.GetComponent<Renderer>().material = lit_red;
        }
    }

    public float getTractorBeamPower()
    {
        return power;
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
            BUTTONS[0].updateInteractable(power > 0.0f);
            BUTTONS[1].updateInteractable(power < 1.0f);
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
        setTractorBeamStatusIndicators(false);
        bars_display.SetActive(true);
        info_display.SetActive(true);
        BUTTONS[0].updateInteractable(power > 0.0f);
        BUTTONS[1].updateInteractable(power < 1.0f);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        setTractorBeamStatusIndicators(false);
        bars_display.SetActive(false);
        info_display.SetActive(false);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
        hud_info.setPowerConsumption(0.0f);

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
        hud_info.setPowerConsumption(pwr * MAX_POWER_CONSUMPTION);
        displayAdjustment();
    }
}