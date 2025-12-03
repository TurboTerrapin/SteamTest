/*
    Headlights.cs
    - Handles inputs for headlights
    - Moves physical slider
    - Updates corresponding screen
    Contributor(s): Jake Schott
    Last Updated: 11/12/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Headlights : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float MOVE_TIME = 0.25f;
    private static float DELAY_TIME = 0.1f;
    private static float MAX_POWER_CONSUMPTION = 0.1f; //equates to 1 circle

    private string CONTROL_NAME = "HEADLIGHTS";
    private static string INFO_MESSAGE = "Increases visibility and illuminates the surrounding area outside of the window.";
    private List<string> CONTROL_DESCS = new List<string> {"DIM", "BRIGHTEN"};
    private List<int> CONTROL_INDEXES = new List<int>() {2, 0};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject slider;
    public GameObject headlights_display;
    public GameObject ship_headlights;

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private int headlight_configuration = 0;
    private Vector3 initial_pos;
    private Vector3 final_pos;
    private Coroutine headlight_shift_coroutine = null;
    private Coroutine headlight_adjustment_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME, true);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);

        initial_pos = slider.transform.localPosition;
        final_pos = new Vector3(0.2817f, -1.2825f, 19.2646f);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    private void setHeadlights(float a, float range, float scale)
    {
        foreach (Transform light in ship_headlights.transform)
        {
            light.GetComponent<Light>().range = range;
            light.GetComponent<Light>().intensity = range;
            light.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(0.0f, 0.84f, 1.0f, a);
            light.GetChild(0).localScale = new Vector3(scale, scale, 1.0f);
        }
    }

    IEnumerator headlightShift()
    {
        float animation_time = MOVE_TIME;

        Vector3 starting_pos = slider.transform.localPosition;
        Vector3 dest_pos = Vector3.Lerp(initial_pos, final_pos, headlight_configuration / 7.0f);

        float starting_a = ship_headlights.transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().color.a;
        float dest_a = Mathf.Lerp(0.0f, 0.5f, headlight_configuration / 7.0f);

        float starting_range = ship_headlights.transform.GetChild(0).GetComponent<Light>().range;
        float dest_range = Mathf.Lerp(0.0f, 2000.0f, headlight_configuration / 7.0f);

        float starting_scale = ship_headlights.transform.GetChild(0).GetChild(0).localScale.x;
        float dest_scale = Mathf.Lerp(0.5f, 1.5f, headlight_configuration / 7.0f);

        float starting_fill = headlights_display.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount;
        float dest_fill = headlight_configuration / 7.0f;

        //move slider
        while (animation_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            animation_time = Mathf.Max(0.0f, animation_time - dt);

            float slide_percentage = 1.0f - (animation_time / MOVE_TIME);

            slider.transform.localPosition = Vector3.Lerp(starting_pos, dest_pos, slide_percentage);

            setHeadlights(Mathf.Lerp(starting_a, dest_a, slide_percentage), Mathf.Lerp(starting_range, dest_range, slide_percentage), Mathf.Lerp(starting_scale, dest_scale, slide_percentage));
            
            headlights_display.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = Mathf.Lerp(starting_fill, dest_fill, slide_percentage);
            yield return null;
        }

        //cooldown
        yield return new WaitForSeconds(DELAY_TIME);

        headlight_shift_coroutine = null;
    }

    private bool checkIfChangeNecessary()
    {
        if (is_powered == false)
        {
            return false;
        }
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && headlight_configuration > 0){
            return true;
        }
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], keys_down) && headlight_configuration < 7)
        {
            return true;
        }
        return false;
    }

    IEnumerator headlightAdjustment()
    {
        while (checkIfChangeNecessary())
        {
            bool shifted = false;
            if (headlight_configuration < 7)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], keys_down) && is_powered == true) //brighten
                {
                    shifted = true;
                    BUTTONS[1].toggle();
                    BUTTONS[0].updateInteractable(false);
                    headlight_configuration++;
                    transmitTractorHeadlightAdjustmentRPC(headlight_configuration);
                }
            }
            if (shifted == false)
            {
                if (headlight_configuration > 0)
                {
                    if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && is_powered == true) //dim
                    {
                        BUTTONS[0].toggle();
                        BUTTONS[1].updateInteractable(false);
                        headlight_configuration--;
                        transmitTractorHeadlightAdjustmentRPC(headlight_configuration);
                    }
                }
            }

            //wait for coroutine to start
            while (headlight_shift_coroutine == null)
            {
                yield return null;
            }
            //wait for coroutine to end
            while (headlight_shift_coroutine != null)
            {
                yield return null;
            }

            keys_down.Clear();

            int iterator = 0; //counts frames
            while (keys_down.Count == 0 && iterator < 2)
            {
                yield return null;
                iterator++;
            }

            BUTTONS[0].updateInteractable(headlight_configuration > 0 && is_powered == true);
            BUTTONS[1].updateInteractable(headlight_configuration < 7 && is_powered == true);
            BUTTONS[0].untoggle();
            BUTTONS[1].untoggle();
        }

        headlight_adjustment_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        keys_down = inputs;
        if (headlight_adjustment_coroutine == null && is_powered == true)
        {
            if (checkIfChangeNecessary())
            {
                headlight_adjustment_coroutine = StartCoroutine(headlightAdjustment());
            }
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        Vector3 start_pos = slider.transform.localPosition;
        float starting_a = ship_headlights.transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().color.a;
        float starting_range = ship_headlights.transform.GetChild(0).GetComponent<Light>().range;
        float starting_scale = ship_headlights.transform.GetChild(0).GetChild(0).localScale.x;

        float anim_time = power_off_time;
        headlight_configuration = 0;
        headlights_display.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = 0.0f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            float off_percentage = 1.0f - (anim_time / power_off_time);
            slider.transform.localPosition = Vector3.Lerp(start_pos, initial_pos, off_percentage);
            setHeadlights(Mathf.Lerp(starting_a, 0.0f, off_percentage), Mathf.Lerp(starting_range, 0.0f, off_percentage), Mathf.Lerp(starting_scale, 0.0f, off_percentage));
            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        BUTTONS[0].updateInteractable(headlight_configuration > 0);
        BUTTONS[1].updateInteractable(headlight_configuration < 7);
        headlights_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
        headlights_display.SetActive(false);
        hud_info.setPowerConsumption(0.0f);

        //return headlight slider to 0
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTractorHeadlightAdjustmentRPC(int headlight_config)
    {
        headlight_configuration = headlight_config;
        transform.GetComponent<PowerControl>().power_manager.controlPowerChange(0, this.GetType().Name, (headlight_configuration / 7.0f) * MAX_POWER_CONSUMPTION);
        hud_info.setPowerConsumption((headlight_configuration / 7.0f) * MAX_POWER_CONSUMPTION);
        if (headlight_shift_coroutine != null)
        {
            StopCoroutine(headlight_shift_coroutine);
        }
        headlight_shift_coroutine = StartCoroutine(headlightShift());
    }
}
