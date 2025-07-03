/*
    InertialDampeners.cs
    - Handles inertial dampeners
    - When enabled, increase acceleration rates for thrusters and impulse throttle
    - Each one has an equal, 33% effect on both thrusters and impulse throttle (all three enabled means 100% effect)
    Contributor(s): Jake Schott
    Last Updated: 7/2/2025
*/

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InertialDampeners : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.5f;

    private string CONTROL_NAME = "INERTIAL DAMPENERS";
    private List<string> CONTROL_DESCS = new List<string>() { "ENABLE PRIMARY", "ENABLE SECONDARY", "ENABLE TERTIARY" };
    private List<string> ALT_CONTROL_DESCS = new List<string>() { "DISABLE PRIMARY", "DISABLE SECONDARY", "DISABLE TERTIARY" };
    private List<int> CONTROL_INDEXES = new List<int>() {7, 8, 9};
    private List<Button> BUTTONS = new List<Button>();

    private bool[] dampener_is_enabled = new bool[3]{false, false, false};
    private Coroutine dampener_switch_coroutine = null;

    public List<GameObject> dampener_sticks = null;
    public List<GameObject> dampener_displays = null;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], true, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], true, true));
        hud_info.setButtons(BUTTONS);
        hud_info.adjustButtonFontSizes(34.0f);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    private void adjustInertialDampenerModifiers()
    {
        float modifier = 1.0f;
        for (int i = 0; i <= 2; i++)
        {
            if (dampener_is_enabled[i] == true)
            {
                modifier += 1.5f;
            }
        }
        transform.GetComponent<ImpulseThrottle>().adjustInertialDampenerModifier(modifier);
        transform.GetComponent<HorizontalThrusters>().adjustInertialDampenerModifier(modifier);
        transform.GetComponent<VerticalThrusters>().adjustInertialDampenerModifier(modifier);
    }

    IEnumerator switchDampener(int index, bool to_switch_to)
    {
        GameObject current_stick = dampener_sticks[index];
        GameObject current_display = dampener_displays[index];
        UnityEngine.UI.Image current_diamond = current_display.transform.GetChild(5).gameObject.GetComponent<UnityEngine.UI.Image>();

        float starting_stick_rotation = -80.0f;
        float starting_canvas_rotation = 45.0f;
        float starting_diamond_fill_amount = 1.0f;

        float desired_stick_rotation = -60.0f;
        float desired_canvas_rotation = 0.0f;
        float desired_diamond_fill_amount = 0.0f;

        if (to_switch_to == true)
        {
            starting_stick_rotation = -140.0f;
            starting_canvas_rotation = 0.0f;
            starting_diamond_fill_amount = 0.0f;
            desired_stick_rotation = 60.0f;
            desired_canvas_rotation = 45.0f;
            desired_diamond_fill_amount = 1.0f;
        }

        float anim_time = SWITCH_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time -= Time.deltaTime);

            //set stick
            dampener_sticks[index].transform.localRotation =
                Quaternion.Euler(starting_stick_rotation + Mathf.Lerp(desired_stick_rotation, 0.0f, anim_time / SWITCH_TIME),
                                 0.0f,
                                 0.0f);
            //set center diamond
            dampener_displays[index].transform.localRotation =
                Quaternion.Euler(-23.0f,
                                 -180.0f,
                                 Mathf.Lerp(desired_canvas_rotation, starting_canvas_rotation, anim_time / SWITCH_TIME));
            dampener_displays[index].transform.GetChild(5).GetComponent<UnityEngine.UI.Image>().fillAmount = 
                Mathf.Lerp(desired_diamond_fill_amount, starting_diamond_fill_amount, anim_time / SWITCH_TIME); 

            yield return null;
        }

        if (to_switch_to == true)
        {
            BUTTONS[index].updateDesc(ALT_CONTROL_DESCS[index]);
        }
        else
        {
            BUTTONS[index].updateDesc(CONTROL_DESCS[index]);
        }

        adjustInertialDampenerModifiers();

        for (int i = 0; i <= 2; i++)
        {
            BUTTONS[i].updateInteractable(true);
        }
        dampener_switch_coroutine = null;
    }

    //used to ensure all are in the correct place
    private void setAllDampeners()
    {
        for (int i = 0; i <= 2; i++)
        {
            float stick_rotation = -140.0f;
            float canvas_rotation = 0.0f;
            float diamond_fill_amount = 0.0f;
            if (dampener_is_enabled[i] == true)
            {
                stick_rotation = -80.0f;
                canvas_rotation = 45.0f;
                diamond_fill_amount = 1.0f;
            }
            //reset stick
            dampener_sticks[i].transform.localRotation =
                Quaternion.Euler(stick_rotation,
                                 0.0f,
                                 0.0f);
            //reset center diamond
            dampener_displays[i].transform.localRotation =
                Quaternion.Euler(-23.0f,
                                 -180.0f,
                                 canvas_rotation);
            dampener_displays[i].transform.GetChild(5).GetComponent<UnityEngine.UI.Image>().fillAmount = diamond_fill_amount;
        }
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (dampener_switch_coroutine == null)
        {
            for (int i = 0; i <= 2; i++)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    BUTTONS[i].toggle();
                    for (int x = 0; x <= 2; x++)
                    {
                        BUTTONS[x].updateInteractable(false);
                    }
                    transmitInertialDampenerRPC(i, !dampener_is_enabled[i]);
                }
            }
        }

    }

    [Rpc(SendTo.Everyone)]
    private void transmitInertialDampenerRPC(int index, bool is_enabled)
    {
        dampener_is_enabled[index] = is_enabled;
        if (dampener_switch_coroutine != null)
        {
            StopCoroutine(dampener_switch_coroutine);
            setAllDampeners();
        }
        dampener_switch_coroutine = StartCoroutine(switchDampener(index, is_enabled));
    }
}