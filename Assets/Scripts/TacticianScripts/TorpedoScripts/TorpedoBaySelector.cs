/*
    TorpedoBaySelector.cs
    - Handles torpedo bay slider
    - Updates arrow screen
    Contributor(s): Jake Schott
    Last Updated: 3/6/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering;

public class TorpedoBaySelector : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float MOVE_TIME = 0.5f;
    private static Vector3 FINAL_POS = new Vector3(0.091f, 0.0f, 0.0f);

    private string CONTROL_NAME = "TORPEDO BAY SELECTOR";
    private static string INFO_MESSAGE = "Handles selecting which torpedo bay/direction to use for the torpedo trigger.";
    private List<string> CONTROL_DESCS = new List<string> { "SHIFT LEFT", "SHIFT RIGHT" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject selector_lever;
    public GameObject selector_display;

    private bool is_powered = false;
    private Vector3 initial_pos;
    private int current_bay = 0;
    private Coroutine torpedo_shift_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;
    
    [Header("IK Targetable Details")]
    public GameObject IK_target;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, true));
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);

        initial_pos = selector_lever.transform.localPosition;
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

    public int getDirectionIndex()
    {
        return current_bay;
    }

    IEnumerator selectorShift()
    {
        for (int i = 0; i < 4; i++)
        {
            selector_display.transform.GetChild(0).GetChild(i * 2).gameObject.SetActive(false);
            for (int x = 0; x < 2; x++)
            {
                Color c = selector_display.transform.GetChild(1).GetChild(x + (i * 2)).GetComponent<UnityEngine.UI.RawImage>().color;
                c.a = 0.2f;
                selector_display.transform.GetChild(1).GetChild(x + (i * 2)).GetComponent<UnityEngine.UI.RawImage>().color = c;
            }
        }

        float animation_time = MOVE_TIME;

        Vector3 starting_pos = selector_lever.transform.localPosition;
        Vector3 dest_pos = Vector3.Lerp(initial_pos, FINAL_POS, current_bay / 3.0f);

        //move slider
        while (animation_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            animation_time = Mathf.Max(0.0f, animation_time - dt);
            selector_lever.transform.localPosition = Vector3.Lerp(starting_pos, dest_pos, 1.0f - (animation_time / MOVE_TIME));

            yield return null;
        }

        selector_display.transform.GetChild(0).GetChild(current_bay * 2).gameObject.SetActive(true);
        for (int x = 0; x < 2; x++)
        {
            Color c = selector_display.transform.GetChild(1).GetChild(x + (current_bay * 2)).GetComponent<UnityEngine.UI.RawImage>().color;
            c.a = 1.0f;
            selector_display.transform.GetChild(1).GetChild(x + (current_bay * 2)).GetComponent<UnityEngine.UI.RawImage>().color = c;
        }

        BUTTONS[0].updateInteractable(current_bay > 0 && is_powered == true);
        BUTTONS[1].updateInteractable(current_bay < 3 && is_powered == true);
        BUTTONS[0].untoggle();
        BUTTONS[1].untoggle();

        torpedo_shift_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        keys_down = inputs;
        if (torpedo_shift_coroutine == null)
        {
            bool shifted = false;
            if (current_bay < 3)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], keys_down)) //shift right
                {
                    shifted = true;
                    BUTTONS[1].toggle();
                    BUTTONS[0].updateInteractable(false);
                    current_bay++;
                    transmitTorpedoSelectionAdjustmentRPC(current_bay);
                }
            }
            if (shifted == false)
            {
                if (current_bay > 0)
                {
                    if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], keys_down)) //shift left
                    {
                        BUTTONS[0].toggle();
                        BUTTONS[1].updateInteractable(false);
                        current_bay--;
                        transmitTorpedoSelectionAdjustmentRPC(current_bay);
                    }
                }
            }
        }
    }

    public void resetToDefault()
    {
        current_bay = 0;
        selector_lever.transform.localPosition = initial_pos;
        selector_display.transform.GetChild(0).GetChild(0).gameObject.SetActive(true);
        for (int i = 1; i < 4; i++)
        {
            selector_display.transform.GetChild(0).GetChild(i * 2).gameObject.SetActive(false);
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        selector_display.SetActive(true);
        BUTTONS[0].updateInteractable(current_bay > 0);
        BUTTONS[1].updateInteractable(current_bay < 3);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        selector_display.SetActive(false);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTorpedoSelectionAdjustmentRPC(int to)
    {
        current_bay = to;
        if (torpedo_shift_coroutine != null)
        {
            StopCoroutine(torpedo_shift_coroutine);
        }
        torpedo_shift_coroutine = StartCoroutine(selectorShift());
    }
}