/*
    SymbolToggle.cs
    - Slider that switches UniversalCommunicator input mode between symbols and numbers
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SymbolToggle : NetworkBehaviour, IControllable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.4f;
    private static Vector3 FINAL_POS = new Vector3(0.0375f, 0.0f, 0.0f);

    private string CONTROL_NAME = "SYMBOL MODE";
    private static string INFO_MESSAGE = "Switches between symbol and character mode for input mode.";
    private List<string> CONTROL_DESCS = new List<string> { "SWITCH" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject symbol_toggle_switch;

    private GameObject symbol_toggle_display;

    private UniversalCommunicator universal_communicator;

    private bool is_active = false;
    private bool symbol_mode = true;
    private Vector3 initial_pos;
    private Coroutine symbol_switch_coroutine = null;

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public GameObject IK_target = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public int finger_position = 0;

    private void Start()
    {
        universal_communicator = GetComponent<UniversalCommunicator>();
        symbol_toggle_display = universal_communicator.symbol_toggle_display;

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);

        initial_pos = symbol_toggle_switch.transform.localPosition;
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }
    public Transform getIKTarget(GameObject current_target)
    {
        return IK_target.transform;
    }
    public AnimatorHandler.HandInteractionType getHandInteractionType()
    {
        return hand_interaction_type;
    }
    public float getHandPose()
    {
        return hand_pose;
    }
    public bool getRightHandFlip()
    {
        return does_right_hand_flip;
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

    private void displayAdjustment()
    {
        //update character keyboard
        for (int i = 0; i < 12; i++)
        {
            GameObject cd = universal_communicator.getCharacterDisplay(i);
            cd.transform.GetChild(0).gameObject.SetActive(symbol_mode);
            cd.transform.GetChild(1).gameObject.SetActive(!symbol_mode);
        }

        //update symbol toggle glass
        for (int i = 0; i < 2; i++)
        {
            float a = 0.2f;
            if (symbol_mode == true && i == 1)
            {
                a = 1.0f;
            }
            else if (symbol_mode == false && i == 0)
            {
                a = 1.0f;
            }
            symbol_toggle_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
        }
    }

    public int getSymbolMode()
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
            if (symbol_mode == true)
            {
                slider_percentage = 1.0f - (switch_time / SWITCH_TIME);
            }

            symbol_toggle_switch.transform.localPosition = Vector3.Lerp(initial_pos, FINAL_POS, slider_percentage);

            yield return null;
        }

        displayAdjustment();

        symbol_mode = !symbol_mode;

        BUTTONS[0].untoggle();
        BUTTONS[0].updateInteractable(is_active);

        symbol_switch_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (symbol_switch_coroutine == null && is_active == true && universal_communicator.getIsPowered() == true)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
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
        if (symbol_switch_coroutine != null)
        {
            StopCoroutine(symbol_switch_coroutine);
        }
        symbol_switch_coroutine = StartCoroutine(symbolSwitch());
    }
}