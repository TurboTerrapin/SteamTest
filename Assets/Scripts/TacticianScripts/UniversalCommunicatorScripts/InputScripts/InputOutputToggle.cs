/*
    InputOutputToggle.cs
    - Switch that switches between input/output mode
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class InputOutputToggle : NetworkBehaviour, IControllable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 0.5f;
    private static Vector3 FINAL_POS = new Vector3(0.0f, -0.0084f, -0.0208f);

    private string CONTROL_NAME = "INPUT/OUTPUT TOGGLE";
    private static string INFO_MESSAGE = "Used to switch between input (character entry) or output (message reading) mode.";
    private List<string> CONTROL_DESCS = new List<string>{"SWITCH"};
    private List<int> CONTROL_INDEXES = new List<int>(){6};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject input_output_switch;

    private GameObject input_output_display;

    private UniversalCommunicator universal_communicator;

    private bool is_active = false;
    private Vector3 initial_pos;
    private Coroutine input_output_switch_coroutine = null;

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
        input_output_display = universal_communicator.input_output_toggle_display;

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        hud_info.setButtons(BUTTONS, 6);
        hud_info.setInfo(INFO_MESSAGE);

        initial_pos = input_output_switch.transform.localPosition;
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
        BUTTONS[0].untoggle();
    }

    private void displayAdjustment(int arrow_to_highlight)
    {
        for (int i = 0; i < 2; i++)
        {
            input_output_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.2f);
            input_output_display.transform.GetChild(i).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.2f);
        }

        input_output_display.transform.GetChild(arrow_to_highlight).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        input_output_display.transform.GetChild(arrow_to_highlight).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
    }

    IEnumerator inputOutputSwitch(bool to_switch_to)
    {
        float switch_time = SWITCH_TIME;

        //slide slider
        while (switch_time > 0)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            switch_time = Mathf.Max(0.0f, switch_time - dt);

            float switch_percentage = switch_time / SWITCH_TIME;
            if (universal_communicator.getInputMode() == true)
            {
                switch_percentage = 1.0f - (switch_time / SWITCH_TIME);
            }

            input_output_switch.transform.localPosition = Vector3.Lerp(initial_pos, FINAL_POS, switch_percentage);

            yield return null;
        }

        universal_communicator.clearUniversalCommunicator();
        universal_communicator.setInputMode(to_switch_to);
        int arrow_index = 0;
        if (to_switch_to == false)
        {
            arrow_index = 1;
        }
        displayAdjustment(arrow_index);
        BUTTONS[0].updateInteractable(is_active);

        input_output_switch_coroutine = null;
    }

    public void forceSwitch(bool to_switch_to)
    {
        if (input_output_switch_coroutine != null)
        {
            StopCoroutine(input_output_switch_coroutine);
        }
        input_output_switch_coroutine = StartCoroutine(inputOutputSwitch(to_switch_to));
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (input_output_switch_coroutine == null && is_active == true && universal_communicator.getIsPowered() == true)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTONS[0].toggle(0.2f);
                transmitInputOutputSwitchRPC(universal_communicator.getInputMode());
            }
        }
    }
    
    [Rpc(SendTo.Everyone)]
    private void transmitInputOutputSwitchRPC(bool current_mode)
    {
        universal_communicator.clearMessagePreview();
        universal_communicator.setInputMode(current_mode);
        if (input_output_switch_coroutine != null && is_active == true)
        {
            StopCoroutine(input_output_switch_coroutine);
        }
        input_output_switch_coroutine = StartCoroutine(inputOutputSwitch(!current_mode));
    }
}