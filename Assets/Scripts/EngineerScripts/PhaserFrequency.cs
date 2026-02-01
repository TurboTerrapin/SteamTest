/*
    PhaserFrequency.cs
    - Handles inputs for engineer phaser frequency adjustment
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PhaserFrequency : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float DIAL_SPEED = 100.0f;
    private static float FREQUENCY_SWITCH_SPEED = 10.0f;
    private Vector3 PHASER_FREQ_SLIDER_FINAL_POS = new Vector3(7.7154f, -0.1597f, -8.3744f);
    private static float SWITCH_TIME = 0.5f;
    private static int[] MIN_FREQUENCIES = { 40, 20 }; //long-range, short-range
    private static int[] MAX_FREQUENCIES = { 70, 90 }; //long-range, short-range

    private string CONTROL_NAME = "PHASER FREQUENCY";
    private static string INFO_MESSAGE = "Adjusts phaser frequency for either long-range or short-range phasers to improve efficiency.";
    private List<string> CONTROL_DESCS = new List<string> { "SWITCH", "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6, 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject phaser_frequency_display;
    public GameObject phaser_frequency_slider;
    public GameObject phaser_frequency_dial;

    private Vector3 phaser_freq_slider_initial_pos; //slider starting position (long-range phaser)


    private bool is_powered = false;
    private int phaser_to_adjust = 0; //0 is long-range, 1 is short-range
    private float dial_rotation = 0.0f; //actual rotation of the dial
    private float[] frequency_update = { 0.5f, 0.5f }; //increases at 1.0, decreases at 0.0
    private int[] phaser_frequencies = { 40, 20 };
    private Coroutine phaser_switch_coroutine = null;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        phaser_freq_slider_initial_pos = phaser_frequency_slider.transform.localPosition;

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, false));
        hud_info.setButtons(BUTTONS, 5);
        hud_info.setInfo(INFO_MESSAGE);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    private void displayFrequencyAdjustment()
    {
        //update frequency
        phaser_frequency_display.transform.GetChild(0).GetComponent<TMP_Text>().SetText(phaser_frequencies[phaser_to_adjust].ToString() + ".0GH");

        //rotate dial
        phaser_frequency_dial.transform.localRotation = Quaternion.Euler(-54.0f, -45.0f, dial_rotation);
    }

    private void displaySwitchAdjustment()
    {
        //switch phaser icons
        phaser_frequency_display.transform.GetChild(1).gameObject.SetActive(phaser_to_adjust == 0);
        phaser_frequency_display.transform.GetChild(2).gameObject.SetActive(phaser_to_adjust == 1);

        //update text
        phaser_frequency_display.transform.GetChild(0).GetComponent<TMP_Text>().color = phaser_frequency_display.transform.GetChild(1 + phaser_to_adjust).GetComponent<UnityEngine.UI.RawImage>().color;
        displayFrequencyAdjustment();
    }

    IEnumerator phaserToAdjustSwitch()
    {
        Vector3 start_pos = phaser_frequency_slider.transform.localPosition;
        Vector3 dest_pos = Vector3.Lerp(phaser_freq_slider_initial_pos, PHASER_FREQ_SLIDER_FINAL_POS, phaser_to_adjust);
        float anim_time = SWITCH_TIME;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            phaser_frequency_slider.transform.localPosition = Vector3.Lerp(start_pos, dest_pos, 1.0f - (anim_time / SWITCH_TIME));

            yield return null;
        }

        displaySwitchAdjustment();

        BUTTONS[0].untoggle();
        BUTTONS[0].updateInteractable(is_powered);
        BUTTONS[1].updateInteractable(is_powered);
        BUTTONS[2].updateInteractable(is_powered);

        phaser_switch_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        if (phaser_switch_coroutine == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //phaser switch
            {
                BUTTONS[0].toggle(0.2f);
                BUTTONS[1].updateInteractable(false);
                BUTTONS[2].updateInteractable(false);
                transmitPhaserToAdjustChangeRPC((phaser_to_adjust + 1) % 2);
            }
            else
            {
                int dial_direction = 0;
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[2], inputs)) //E to increment
                {
                    dial_direction += 1;
                }
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //Q to decrement
                {
                    dial_direction -= 1;
                }
                if (dial_direction != 0)
                {
                    int freq = phaser_frequencies[phaser_to_adjust];
                    if (dial_direction > 0)
                    {
                        dial_rotation += dt * DIAL_SPEED;
                        frequency_update[phaser_to_adjust] += dt * FREQUENCY_SWITCH_SPEED;
                    }
                    else
                    {
                        dial_rotation -= dt * DIAL_SPEED;
                        frequency_update[phaser_to_adjust] -= dt * FREQUENCY_SWITCH_SPEED;
                    }
                    if (frequency_update[phaser_to_adjust] >= 1.0f)
                    {
                        frequency_update[phaser_to_adjust] -= 1.0f;
                        freq++;
                        if (freq > MAX_FREQUENCIES[phaser_to_adjust])
                        {
                            freq = MIN_FREQUENCIES[phaser_to_adjust];
                        }
                    }
                    else if (frequency_update[phaser_to_adjust] <= 0.0f)
                    {
                        frequency_update[phaser_to_adjust] += 1.0f;
                        freq--;
                        if (freq < MIN_FREQUENCIES[phaser_to_adjust])
                        {
                            freq = MAX_FREQUENCIES[phaser_to_adjust];
                        }
                    }
                    if (dial_rotation > 360.0f)
                    {
                        dial_rotation -= 360.0f;
                    }
                    else if (dial_rotation > 0.0f)
                    {
                        dial_rotation += 360.0f;
                    }
                    transmitPhaserFrequencyAdjustmentRPC(phaser_to_adjust, dial_rotation, frequency_update[phaser_to_adjust], freq);
                }
            }
        }
    }

    public void resetToDefault()
    {
        phaser_frequency_slider.transform.localPosition = phaser_freq_slider_initial_pos;
        phaser_to_adjust = 0;
        dial_rotation = 0.0f;
        frequency_update[0] = 0.5f;
        frequency_update[1] = 0.5f;
        phaser_frequencies[0] = MIN_FREQUENCIES[0];
        phaser_frequencies[1] = MIN_FREQUENCIES[1];
        displayFrequencyAdjustment();
        displaySwitchAdjustment();
    }

    public void powerOn(int position)
    {
        is_powered = true;
        phaser_frequency_display.SetActive(true);
        BUTTONS[0].updateInteractable(true);
        BUTTONS[1].updateInteractable(true);
        BUTTONS[2].updateInteractable(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        phaser_frequency_display.SetActive(false);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
        BUTTONS[2].updateInteractable(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitPhaserFrequencyAdjustmentRPC(int pta, float dr, float fu, int freq)
    {
        phaser_to_adjust = pta;
        dial_rotation = dr;
        frequency_update[phaser_to_adjust] = fu;
        phaser_frequencies[pta] = freq;
        displayFrequencyAdjustment();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitPhaserToAdjustChangeRPC(int pta)
    {
        phaser_to_adjust = pta;
        if (phaser_switch_coroutine != null)
        {
            StopCoroutine(phaser_switch_coroutine);
        }
        phaser_switch_coroutine = StartCoroutine(phaserToAdjustSwitch());
    }
}