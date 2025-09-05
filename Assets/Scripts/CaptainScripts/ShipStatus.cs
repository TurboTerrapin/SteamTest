/*
    ShipStatus.cs
    - Handles slider
    - Changes lights at highest status
    Contributor(s): Jake Schott
    Last Updated: 9/2/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ShipStatus: NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    Color[] COLOR_OPTIONS = new Color[3] { new Color(0f, 0.84f, 1f), new Color(0.89f, 1f, 0.0f), new Color(1f, 0.01f, 0.0f)};
    private static float MOVE_TIME = 0.5f;
    private static float MAX_POWER_CONSUMPTION = 0.1f; //equates to 1 circle

    private string CONTROL_NAME = "SHIP ALERT STATUS";
    private List<string> CONTROL_DESCS = new List<string> { "LOWER", "ELEVATE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    private bool is_powered = false;
    public List<GameObject> position_warnings = null;
    public List<GameObject> indicators = null;
    public GameObject selector_lever;
    public LightsManager lights_manager;
    private Vector3 initial_pos;
    private Vector3 final_pos = new Vector3(0.3821f, -0.5888f, 13.6847f);
    private int curr_status = 0;

    private Coroutine status_shift_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        hud_info.setButtons(BUTTONS);

        initial_pos = selector_lever.transform.localPosition;
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }
    public int getCurrColor()
    {
        return curr_status;
    }

    private void displayAdjustment()
    {
        //hide all indicators
        for (int i = 0; i < 3; i++)
        {
            indicators[i].SetActive(false);
        }

        //set colors, make visible up to highest status
        for (int i = 0; i <= curr_status; i++)
        {
            indicators[i].GetComponent<UnityEngine.UI.RawImage>().color = COLOR_OPTIONS[curr_status];
            indicators[i].SetActive(true);
        }

        //change lights
        if (curr_status == 2)
        {
            lights_manager.enableRedAlert();
        }
        else
        {
            lights_manager.disableRedAlert();
        }
    }

    IEnumerator statusShift()
    {
        float animation_time = MOVE_TIME;

        Vector3 starting_pos = selector_lever.transform.localPosition;
        Vector3 dest_pos =
            new Vector3(Mathf.Lerp(initial_pos.x, final_pos.x, curr_status / 2.0f),
                        Mathf.Lerp(initial_pos.y, final_pos.y, curr_status / 2.0f),
                        Mathf.Lerp(initial_pos.z, final_pos.z, curr_status / 2.0f));

        //move slider
        while (animation_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            animation_time = Mathf.Max(0.0f, animation_time - dt);
            selector_lever.transform.localPosition =
                new Vector3(Mathf.Lerp(starting_pos.x, dest_pos.x, 1.0f - (animation_time / MOVE_TIME)),
                            Mathf.Lerp(starting_pos.y, dest_pos.y, 1.0f - (animation_time / MOVE_TIME)),
                            Mathf.Lerp(starting_pos.z, dest_pos.z, 1.0f - (animation_time / MOVE_TIME)));

            yield return null;
        }

        if (curr_status > 0)
        {
            transform.GetComponent<PowerControl>().power_manager.controlPowerChange(3, this.GetType().Name, MAX_POWER_CONSUMPTION);
        }
        else
        {
            transform.GetComponent<PowerControl>().power_manager.controlPowerChange(3, this.GetType().Name, 0.0f);
        }

        displayAdjustment();

        BUTTONS[0].updateInteractable(curr_status > 0);
        BUTTONS[1].updateInteractable(curr_status < 2);
        BUTTONS[0].untoggle();
        BUTTONS[1].untoggle();

        status_shift_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        keys_down = inputs;
        if (status_shift_coroutine == null)
        {
            bool shifted = false;
            if (curr_status < 2)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], keys_down)) //shift up
                {
                    shifted = true;
                    BUTTONS[1].toggle();
                    BUTTONS[0].updateInteractable(false);
                    curr_status++;
                    transmitColorSelectionAdjustmentRPC(curr_status);
                }
            }
            if (shifted == false)
            {
                if (curr_status > 0)
                {
                    if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], keys_down)) //shift down
                    {
                        BUTTONS[0].toggle();
                        BUTTONS[1].updateInteractable(false);
                        curr_status--;
                        transmitColorSelectionAdjustmentRPC(curr_status);
                    }
                }
            }
        }
    }

    public void powerOn(int position)
    {
        if (position < 2)
        {
            position_warnings[position].SetActive(true);
        }
        else if (position == 3)
        {
            is_powered = true;
            for (int i = 0; i <= curr_status; i++)
            {
                indicators[i].SetActive(true);
            }
            BUTTONS[0].updateInteractable(curr_status > 0);
            BUTTONS[1].updateInteractable(curr_status < 2);
            BUTTONS[0].untoggle();
            BUTTONS[1].untoggle();
        }
    }

    public void powerOff(int position, float time)
    {
        if (position <= 1)
        {
            position_warnings[position].SetActive(false);
        }
        if (position == 3)
        {
            is_powered = false;
            for (int i = 0; i < 3; i++)
            {
                indicators[i].SetActive(false);
            }
            BUTTONS[0].updateInteractable(false);
            BUTTONS[1].updateInteractable(false);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitColorSelectionAdjustmentRPC(int cs)
    {
        curr_status = cs;
        if (status_shift_coroutine != null)
        {
            StopCoroutine(status_shift_coroutine);
        }
        status_shift_coroutine = StartCoroutine(statusShift());
    }
}
