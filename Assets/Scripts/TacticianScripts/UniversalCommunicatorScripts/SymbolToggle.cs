/*
    SymbolToggle.cs
    - Slider that switches between symbols and numbers
    Contributor(s): Jake Schott
    Last Updated: 10/16/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SymbolToggle : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.2f;

    private string CONTROL_NAME = "SYMBOL MODE";
    private List<string> CONTROL_DESCS = new List<string> {"SWITCH"};
    private List<int> CONTROL_INDEXES = new List<int>() {6};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject input_glasses;
    public GameObject numeric_selector = null;
    public GameObject numeric_indicator_display = null;

    private bool symbol_mode = false;
    private bool is_active = true;
    private Vector3 initial_pos;
    private Vector3 final_pos = new Vector3(-3.1493f, 8.7417f, 3.7713f);
    private Coroutine numer_selector_coroutine = null;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        hud_info.setButtons(BUTTONS);

        initial_pos = numeric_selector.transform.localPosition;
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    public void activate()
    {
        is_active = true;
        BUTTONS[0].updateInteractable(true);
    }

    public void deactivate()
    {
        is_active = false;
        BUTTONS[0].updateInteractable(false);
    }

    private GameObject getCharacterDisplay(int index)
    {
        return input_glasses.transform.GetChild(index).GetChild(0).GetChild(1).gameObject;
    }

    private void displayAdjustment()
    {
        for (int i = 0; i < 12; i++)
        {
            GameObject cd = getCharacterDisplay(i);
            cd.transform.GetChild(0).gameObject.SetActive(symbol_mode);
            cd.transform.GetChild(1).gameObject.SetActive(!symbol_mode);
        }
        numeric_indicator_display.transform.GetChild(0).gameObject.SetActive(symbol_mode);
        numeric_indicator_display.transform.GetChild(1).gameObject.SetActive(!symbol_mode);
    }

    public int getIsNumeric()
    {
        if (symbol_mode == true)
        {
            return 0;
        }
        return 1;
    }

    IEnumerator symbolSwitch()
    {
        float switch_time = SWITCH_TIME;

        //slide slider
        while (switch_time > 0)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            switch_time = Mathf.Max(0.0f, switch_time - dt);

            float slider_percentage = switch_time / SWITCH_TIME;
            if (symbol_mode == false)
            {
                slider_percentage = 1.0f - (switch_time / SWITCH_TIME);
            }

            numeric_selector.transform.localPosition =
                new Vector3(Mathf.Lerp(initial_pos.x, final_pos.x, slider_percentage),
                            Mathf.Lerp(initial_pos.y, final_pos.y, slider_percentage),
                            Mathf.Lerp(initial_pos.z, final_pos.z, slider_percentage));

            yield return null;
        }

        displayAdjustment();

        symbol_mode = !symbol_mode;

        BUTTONS[0].untoggle();
        BUTTONS[0].updateInteractable(is_active);

        numer_selector_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (numer_selector_coroutine == null && is_active == true && transform.GetComponent<UniversalCommunicator>().getIsPowered() == true)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTONS[0].toggle();
                transmitSymbolSwitchRPC(symbol_mode);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitSymbolSwitchRPC(bool sb)
    {
        symbol_mode = sb;
        if (numer_selector_coroutine != null)
        {
            StopCoroutine(numer_selector_coroutine);
        }
        numer_selector_coroutine = StartCoroutine(symbolSwitch());
    }
}