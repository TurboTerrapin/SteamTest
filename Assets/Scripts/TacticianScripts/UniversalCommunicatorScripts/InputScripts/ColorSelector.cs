/*
    ColorSelector.cs
    - Handles color slider
    - Updates characters
    Contributor(s): Jake Schott
    Last Updated: 2/7/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ColorSelector : NetworkBehaviour, IControllable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float MOVE_TIME = 0.4f;
    private static Vector3 FINAL_POS = new Vector3(0.09f, 0.0f, 0.0f);

    private string CONTROL_NAME = "COLOR SELECTOR";
    private static string INFO_MESSAGE = "Change the color selector for character and symbol inputs in input mode.";
    private List<string> CONTROL_DESCS = new List<string> { "SHIFT LEFT", "SHIFT RIGHT" };
    private List<int> CONTROL_INDEXES = new List<int>() {4, 5};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject color_selector_lever;

    private GameObject color_selector_display;

    private UniversalCommunicator universal_communicator;

    private bool is_active = false;
    private int curr_color = 0;
    private Vector3 initial_pos;
    private Coroutine color_shift_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

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
        color_selector_display = universal_communicator.color_selector_display;

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);

        initial_pos = color_selector_lever.transform.localPosition;
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
        BUTTONS[0].updateInteractable(curr_color > 0);
        BUTTONS[1].updateInteractable(curr_color < 3);
    }

    public void deactivate()
    {
        is_active = false;
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
    }

    public int getCurrColor()
    {
        return curr_color;
    }

    private void displayAdjustment()
    {
        for (int i = 0; i < 12; i++)
        {
            GameObject cd = universal_communicator.getCharacterDisplay(i);
            cd.transform.GetChild(0).gameObject.GetComponent<TMP_Text>().color = ReferenceAssistor.COLOR_OPTIONS[curr_color];
            cd.transform.GetChild(1).gameObject.GetComponent<UnityEngine.UI.RawImage>().color = ReferenceAssistor.COLOR_OPTIONS[curr_color];
        }

        for (int i = 0; i < 4; i++)
        {
            Color diamond_color = color_selector_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color;
            diamond_color.a = 0.2f;
            if (i == curr_color)
            {
                diamond_color.a = 1.0f;
            }
            color_selector_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = diamond_color;
        }
    }

    IEnumerator selectorShift()
    {
        Vector3 starting_pos = color_selector_lever.transform.localPosition;
        Vector3 dest_pos = Vector3.Lerp(initial_pos, FINAL_POS, curr_color / 3.0f);

        float anim_time = MOVE_TIME;

        //move slider
        while (anim_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            color_selector_lever.transform.localPosition = Vector3.Lerp(starting_pos, dest_pos, 1.0f - (anim_time / MOVE_TIME));

            yield return null;
        }

        displayAdjustment();

        if (is_active == true)
        {
            BUTTONS[0].updateInteractable(curr_color > 0);
            BUTTONS[1].updateInteractable(curr_color < 3);
        }
        BUTTONS[0].untoggle();
        BUTTONS[1].untoggle();

        color_shift_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        keys_down = inputs;
        if (color_shift_coroutine == null && is_active == true && universal_communicator.getIsPowered() == true)
        {
            bool shifted = false;
            if (curr_color < 3)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], keys_down)) //shift right
                {
                    shifted = true;
                    BUTTONS[1].toggle();
                    BUTTONS[0].updateInteractable(false);
                    curr_color++;
                    transmitColorSelectionAdjustmentRPC(curr_color);
                }
            }
            if (shifted == false)
            {
                if (curr_color > 0)
                {
                    if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], keys_down)) //shift left
                    {
                        BUTTONS[0].toggle();
                        BUTTONS[1].updateInteractable(false);
                        curr_color--;
                        transmitColorSelectionAdjustmentRPC(curr_color);
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitColorSelectionAdjustmentRPC(int co)
    {
        curr_color = co;
        if (color_shift_coroutine != null)
        {
            StopCoroutine(color_shift_coroutine);
        }
        color_shift_coroutine = StartCoroutine(selectorShift());
    }
}