/*
    InputOutputToggle.cs
    - Switch that switches between input/output mode
    Contributor(s): Jake Schott
    Last Updated: 7/29/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class InputOutputToggle : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.5f;

    private string CONTROL_NAME = "INPUT/OUTPUT TOGGLE";
    private List<string> CONTROL_DESCS = new List<string>{"SWITCH"};
    private List<int> CONTROL_INDEXES = new List<int>(){6};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject input_glasses;
    public GameObject input_display;
    public GameObject output_display;
    public GameObject colors_display;
    public GameObject numeric_display;
    public GameObject input_output_switch;

    private bool input_mode = true; //true means keyboard, false means read
    private bool is_active = true;
    private Coroutine input_output_switch_coroutine = null;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        hud_info.setButtons(BUTTONS);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    private GameObject getCharacterDisplay(int index)
    {
        return input_glasses.transform.GetChild(index).GetChild(0).GetChild(1).gameObject;
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
        BUTTONS[0].untoggle();
    }

    public void deactivateUC()
    {
        transform.GetComponent<CharacterInput>().deactivate();
        transform.GetComponent<ColorSelector>().deactivate();
        transform.GetComponent<SymbolToggle>().deactivate();
        transform.GetComponent<ResetDisplay>().deactivate();
    }

    public void activateUC()
    {
        transform.GetComponent<CharacterInput>().activate();
        transform.GetComponent<ColorSelector>().activate();
        transform.GetComponent<SymbolToggle>().activate();
        transform.GetComponent<ResetDisplay>().activate();
    }

    public void displayAdjustment()
    {
        bool is_powered = transform.GetComponent<UniversalCommunicator>().getIsPowered();
        colors_display.SetActive(input_mode && is_powered);
        numeric_display.SetActive(input_mode && is_powered);
        input_display.SetActive(input_mode);
        output_display.SetActive(!input_mode);

        for (int i = 0; i < 12; i++)
        {
            GameObject cd = getCharacterDisplay(i);
            cd.SetActive(input_mode && is_powered);
        }
    }

    public bool getIsInputMode()
    {
        return input_mode;
    }

    IEnumerator inputOutputSwitch()
    {
        float switch_time = SWITCH_TIME;

        if (input_mode == true)
        {
            deactivateUC();
        }

        //slide slider
        while (switch_time > 0)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            switch_time = Mathf.Max(0.0f, switch_time - dt);

            float switch_percentage = switch_time / SWITCH_TIME;
            if (input_mode == true)
            {
                switch_percentage = 1.0f - (switch_time / SWITCH_TIME);
            }

            input_output_switch.transform.localRotation =
                Quaternion.Euler(Mathf.Lerp(-68.0f, -120.0f, switch_percentage),
                            0.0f,
                            90.0f);
                
            yield return null;
        }

        input_mode = !input_mode;

        displayAdjustment();

        bool is_powered = transform.GetComponent<UniversalCommunicator>().getIsPowered();
        if (input_mode == true && is_powered == true)
        {
            activateUC();
        }
        else
        {
            transform.GetComponent<UniversalCommunicator>().clearUC();
        }

        BUTTONS[0].updateInteractable(is_active && is_powered);

        input_output_switch_coroutine = null;
    }

    public void forceSwitch(bool switch_to)
    {
        input_mode = !switch_to;
        if (input_output_switch_coroutine != null)
        {
            StopCoroutine(input_output_switch_coroutine);
        }
        input_output_switch_coroutine = StartCoroutine(inputOutputSwitch());
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (input_output_switch_coroutine == null && is_active == true && transform.GetComponent<UniversalCommunicator>().getIsPowered() == true)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTONS[0].toggle(0.2f);
                transmitInputOutputSwitchRPC(input_mode);
            }
        }
    }
    
    [Rpc(SendTo.Everyone)]
    private void transmitInputOutputSwitchRPC(bool om)
    {
        transform.GetComponent<UniversalCommunicator>().clearMsgPreview();
        input_mode = om;
        if (input_output_switch_coroutine != null && is_active == true)
        {
            StopCoroutine(input_output_switch_coroutine);
        }
        input_output_switch_coroutine = StartCoroutine(inputOutputSwitch());
    }
}