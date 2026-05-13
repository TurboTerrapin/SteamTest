/*
    UniversalCommunicator.cs
    - Handles inputs for communicator keyboard
    - Displays to code screen
    Contributor(s): Jake Schott
    Last Updated: 5/12/2026
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

public class UniversalCommunicator : NetworkBehaviour, IPowerable
{
    //CLASS CONSTANTS
    private static float POINTER_SPEED = 0.2f;

    public GameObject character_displays;
    public GameObject message_preview_display;
    public GameObject code_display;
    public GameObject symbol_toggle_display;
    public GameObject color_selector_display;
    public GameObject input_output_toggle_display;
    public GameObject character_delete_display;
    public AudioSource universal_communicator_character_boop_sound;

    private GameObject input_view;
    private GameObject output_view;
    private GameObject pointer;

    private TransmissionHandler transmission_handler;
    private SymbolToggle symbol_toggle;
    private ColorSelector color_selector;
    private InputOutputToggle input_output_toggle;
    private CharacterDelete character_delete;
    private CharacterInput character_input;

    private bool is_powered = false;
    private bool input_mode = true; //true means keyboard, false means read-only
    private List<int> code_index = new List<int>(); //0-11, corresponds to A0-A5, B0-B5 where B5 is 11 and A0 is 0
    private List<int> code_is_symbol = new List<int>(); //0 is symbol (ex. square), 1 means number (ex. 5) 
    private int code_color = 0; //0 is blue, 1 is green, 2 is purple, 3 is orange
    private Coroutine pointer_shift_coroutine = null;

    private void Start()
    {
        input_view = code_display.transform.GetChild(0).gameObject;
        output_view = code_display.transform.GetChild(1).gameObject;
        pointer = input_view.transform.GetChild(24).gameObject;

        transmission_handler = transform.GetComponent<TransmissionHandler>();
        symbol_toggle = transform.GetComponent<SymbolToggle>();
        input_output_toggle = transform.GetComponent<InputOutputToggle>();
        color_selector = transform.GetComponent<ColorSelector>();
        character_delete = transform.GetComponent<CharacterDelete>();
        character_input = transform.GetComponent<CharacterInput>();
    }

    IEnumerator shiftPointer()
    {
        float animation_time = POINTER_SPEED;

        float starting_x = input_view.transform.GetChild(24).transform.localPosition.x;
        float dest_x = Mathf.Lerp(-0.14f, 0.14f, (1.0f - code_index.Count / 7.0f));

        //move pointer
        while (animation_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            animation_time = Mathf.Max(0.0f, animation_time - dt);
            pointer.transform.localPosition = new Vector3(Mathf.Lerp(starting_x, dest_x, 1.0f - (animation_time / POINTER_SPEED)), 0.038f, 0.0f);

            yield return null;
        }

        pointer_shift_coroutine = null;
    }

    public void deleteLastCharacter()
    {
        transmitCharacterDeleteRPC();
    }

    public bool getIsPowered()
    {
        return is_powered;
    }

    public void inputCharacter(int index)
    {
        if (code_index.Count < 8)
        {
            code_index.Add(index);
            code_is_symbol.Add(transform.gameObject.GetComponent<SymbolToggle>().getSymbolMode());
            string index_as_code = DataConverter.listToString(code_index);
            string code_is_numeric_as_code = DataConverter.listToString(code_is_symbol);
            transmitCharacterUpdateRPC(index, index_as_code, code_is_numeric_as_code, code_color);
        }
    }

    public GameObject getCharacterDisplay(int index)
    {
        return character_displays.transform.GetChild(index).gameObject;
    }

    //only updates the characters in the input mode
    private void displayInputAdjustment()
    {
        //hide everything
        for (int i = 0; i <= 7; i++)
        {
            input_view.transform.GetChild(i).gameObject.SetActive(false);
            input_view.transform.GetChild(i + 8).gameObject.SetActive(false);
        }

        //show current numbers/shapes
        for (int i = 0; i < code_index.Count; i++)
        {
            if (code_is_symbol[i] == 0) //symbol
            {
                input_view.transform.GetChild(i + 8).gameObject.GetComponent<UnityEngine.UI.RawImage>().texture = getCharacterDisplay(code_index[i]).transform.GetChild(1).gameObject.GetComponent<RawImage>().texture;
                input_view.transform.GetChild(i + 8).gameObject.GetComponent<UnityEngine.UI.RawImage>().color = ReferenceAssistor.COLOR_OPTIONS[code_color];
                input_view.transform.GetChild(i + 8).gameObject.SetActive(true);
            }
            else //numeric
            {
                input_view.transform.GetChild(i).gameObject.GetComponent<TMP_Text>().SetText(getCharacterDisplay(code_index[i]).transform.GetChild(0).gameObject.GetComponent<TMP_Text>().text);
                input_view.transform.GetChild(i).gameObject.GetComponent<TMP_Text>().color = ReferenceAssistor.COLOR_OPTIONS[code_color];
                input_view.transform.GetChild(i).gameObject.SetActive(true);
            }
        }
    }

    //only updates the chracter in the output (read-only) mode
    private void displayOutputAdjustment()
    {
        //TODO
    }

    private void rebuildMessagePreview()
    {
        //hide transmission preview message icons
        clearMessagePreview();

        //update message preview
        for (int i = 0; i < code_index.Count; i++)
        {
            message_preview_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = ReferenceAssistor.COLOR_OPTIONS[code_color];
            message_preview_display.transform.GetChild(i).gameObject.SetActive(true);
        }
    }

    //called by CharacterInput.cs when a character button has been pushed in or 
    public void onInputChange()
    {
        //handle input characters
        displayInputAdjustment();

        //clears and rebuilds
        rebuildMessagePreview();

        //handle pointer
        if (code_index.Count < 8)
        {
            transform.gameObject.GetComponent<CharacterInput>().activate();
            pointer.SetActive(true);

            if (pointer_shift_coroutine != null)
            {
                StopCoroutine(pointer_shift_coroutine);
            }

            pointer_shift_coroutine = StartCoroutine(shiftPointer());
        }
        else
        {
            transform.gameObject.GetComponent<CharacterInput>().deactivate();
            pointer.SetActive(false);
        }
    }

    //called by ColorSelector.cs
    public void changeColor(int new_color)
    {
        code_color = new_color;
        displayInputAdjustment();
        rebuildMessagePreview();
    }

    public bool getInputMode()
    {
        return input_mode;
    }

    public List<int> getCodeIndexes()
    {
        return new List<int>(code_index);
    }

    public int getCodeColor()
    {
        return code_color;
    }

    public List<int> getCodeIsSymbol()
    {
        return new List<int>(code_is_symbol);
    }

    public void enableKeyboard()
    {
        symbol_toggle.activate();
        color_selector.activate();
        character_delete.activate();
        character_input.activate();
    }

    public void disableKeyboard()
    {
        symbol_toggle.deactivate();
        color_selector.deactivate();
        character_delete.deactivate();
        character_input.deactivate();
    }

    public void setInputMode(bool new_mode)
    {
        input_mode = new_mode;

        inputModeDisplayHelper(input_mode && is_powered);

        input_view.SetActive(input_mode && is_powered);
        output_view.SetActive(!input_mode && is_powered);

        if (input_mode == true && is_powered)
        {
            enableKeyboard();
        }
        else
        {
            disableKeyboard();
        }
    }

    //erases whatever message is currently stored regardless of input or output
    public void clearUniversalCommunicator()
    {
        //wipe code data
        code_index.Clear();
        code_is_symbol.Clear();

        //show changes
        if (input_mode == true)
        {
            displayInputAdjustment();
        }
        else
        {
            displayOutputAdjustment();
        }

        //move pointer to default position
        pointer.transform.localPosition = new Vector3(0.14f, 0.038f, 0.0f);
        pointer.SetActive(true);
    }

    public void clearMessagePreview()
    {
        //fade icons
        for (int i = 0; i <= 7; i++)
        {
            message_preview_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.2f);
        }
    }

    //helper method used to show/hide the fo
    private void inputModeDisplayHelper(bool to_show)
    {
        symbol_toggle_display.SetActive(to_show);
        color_selector_display.SetActive(to_show);
        character_delete_display.SetActive(to_show);

        for (int i = 0; i < 12; i++)
        {
            getCharacterDisplay(i).SetActive(to_show);
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        code_display.SetActive(true);
        message_preview_display.SetActive(true);
        input_output_toggle_display.SetActive(true);
        transmission_handler.activate();
        input_output_toggle.activate();
        setInputMode(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        code_display.SetActive(false);
        message_preview_display.SetActive(false);
        input_output_toggle_display.SetActive(false);
        inputModeDisplayHelper(false);
        clearUniversalCommunicator();
        clearMessagePreview();
        disableKeyboard();
        transmission_handler.deactivate();
        if (input_mode == false)
        {
            input_output_toggle.forceSwitch(true);
        }
        input_output_toggle.deactivate();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitCharacterDeleteRPC()
    {
        if (code_index.Count > 0)
        {
            code_index.RemoveAt(code_index.Count - 1);
            code_is_symbol.RemoveAt(code_is_symbol.Count - 1);
        }

        transform.gameObject.GetComponent<CharacterDelete>().pushDeleteButton();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitCharacterUpdateRPC(int button_index, string indexes, string is_numeric, int color)
    {
        int[] temp_code_index = DataConverter.stringToArray(indexes);
        int[] temp_is_numeric = DataConverter.stringToArray(is_numeric);

        code_index.Clear();
        code_is_symbol.Clear();

        for (int i = 0; i < indexes.Length; i++)
        {
            code_index.Add(temp_code_index[i]);
            code_is_symbol.Add(temp_is_numeric[i]);
        }
        code_color = color;

        character_input.pushButton(button_index);
    }
}