/*
    Headlights.cs
    - Handles inputs for headlights
    - Moves physical slider
    - Updates corresponding screen
    Contributor(s): Jake Schott
    Last Updated: 5/13/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Headlights : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float MOVE_TIME = 0.25f;
    private static float DELAY_TIME = 0.1f;

    private string CONTROL_NAME = "HEADLIGHTS";
    private List<string> CONTROL_DESCS = new List<string> {"DIM", "BRIGHTEN"};
    private List<int> CONTROL_INDEXES = new List<int>() {2, 0};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject slider;
    public GameObject screen;

    private int headlight_configuration = 0;
    private Vector3 initial_pos;
    private Vector3 final_pos;
    private Coroutine headlight_shift_coroutine = null;
    private Coroutine headlight_adjustment_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], true, false));
        hud_info.setButtons(BUTTONS);

        initial_pos = slider.transform.localPosition;
        final_pos = new Vector3(0.2817f, -1.2825f, 19.2646f);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    IEnumerator headlightShift()
    {
        float animation_time = MOVE_TIME;

        Vector3 starting_pos = slider.transform.localPosition;
        Vector3 dest_pos =
            new Vector3(Mathf.Lerp(initial_pos.x, final_pos.x, headlight_configuration / 7.0f),
                        Mathf.Lerp(initial_pos.y, final_pos.y, headlight_configuration / 7.0f),
                        Mathf.Lerp(initial_pos.z, final_pos.z, headlight_configuration / 7.0f));

        float starting_fill = screen.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount;
        float dest_fill = headlight_configuration / 7.0f;

        //move slider
        while (animation_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            animation_time = Mathf.Max(0.0f, animation_time - dt);
            slider.transform.localPosition =
                new Vector3(Mathf.Lerp(starting_pos.x, dest_pos.x, 1.0f - (animation_time / MOVE_TIME)),
                            Mathf.Lerp(starting_pos.y, dest_pos.y, 1.0f - (animation_time / MOVE_TIME)),
                            Mathf.Lerp(starting_pos.z, dest_pos.z, 1.0f - (animation_time / MOVE_TIME)));
            
            screen.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount = Mathf.Lerp(starting_fill, dest_fill, 1.0f - (animation_time / MOVE_TIME));
            yield return null;
        }

        //cooldown
        yield return new WaitForSeconds(DELAY_TIME);

        headlight_shift_coroutine = null;
    }

    private bool checkIfChangeNecessary()
    {
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
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], keys_down)) //brighten
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
                    if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], keys_down)) //dim
                    {
                        BUTTONS[0].toggle();
                        BUTTONS[1].updateInteractable(false);
                        headlight_configuration--;
                        transmitTractorHeadlightAdjustmentRPC(headlight_configuration);
                    }
                }
            }
            keys_down.Clear();
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

            BUTTONS[0].updateInteractable(headlight_configuration > 0);
            BUTTONS[1].updateInteractable(headlight_configuration < 7);
            BUTTONS[0].untoggle();
            BUTTONS[1].untoggle();
        }

        headlight_adjustment_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        keys_down = inputs;
        if (headlight_adjustment_coroutine == null)
        {
            for (int i = CONTROL_INDEXES.Count - 1; i >= 0; i--)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    headlight_adjustment_coroutine = StartCoroutine(headlightAdjustment());
                    return;
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTractorHeadlightAdjustmentRPC(int headlight_config)
    {
        headlight_configuration = headlight_config;
        if (headlight_shift_coroutine != null)
        {
            StopCoroutine(headlight_shift_coroutine);
        }
        headlight_shift_coroutine = StartCoroutine(headlightShift());
    }
}
