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

public class TorpedoBaySelector : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
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
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

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

    public Vector3 getRightHandOffset()
    {
        return right_hand_offset;
    }

    public float getLerpSpeed()
    {
        return lerp_speed;
    }

    public int getDirectionIndex()
    {
        return current_bay;
    }

    private void changeShiftMarkerColor(Color c)
    {
        selector_display.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = c;
        selector_display.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = c;
        selector_display.transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = c;
    }

    public void updateShiftMarker()
    {
        Color c = ReferenceAssistor.COLOR_OPTIONS[0];
        if (torpedo_shift_coroutine != null || ReferenceAssistor.Instance.module_handlers[2].GetComponent<TorpedoLoader>().getBayOccupant(current_bay) == -1)
        {
            c = new Color(0.2f, 0.2f, 0.2f, 1.0f);
        }
        changeShiftMarkerColor(c);
    }

    IEnumerator selectorShift()
    {
        //update markers and arrows
        changeShiftMarkerColor(new Color(0.2f, 0.2f, 0.2f, 1.0f));
        for (int i = 0; i < 4; i++)
        {
            selector_display.transform.GetChild(1).GetChild(i).GetChild(0).gameObject.SetActive(false);
            for (int t = 0; t < 5; t++)
            {
                Color c = selector_display.transform.GetChild(2).GetChild(i).GetChild(t).GetComponent<UnityEngine.UI.RawImage>().color;
                if (c.a > 0.05f)
                {
                    c.a = 0.1f;
                    selector_display.transform.GetChild(2).GetChild(i).GetChild(t).GetComponent<UnityEngine.UI.RawImage>().color = c;
                }
            }
        }

        //move slider and marker
        Vector3 starting_lever_pos = selector_lever.transform.localPosition;
        Vector3 dest_lever_pos = Vector3.Lerp(initial_pos, FINAL_POS, current_bay / 3.0f);

        Vector3 starting_marker_pos = selector_display.transform.GetChild(0).GetChild(0).transform.localPosition;
        Vector3 dest_marker_pos = Vector3.Lerp(new Vector3(0.0f, -0.04f, 0.0f), new Vector3(0.0f, 0.04f, 0.0f), current_bay / 3.0f);

        float animation_time = MOVE_TIME;
        while (animation_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            animation_time = Mathf.Max(0.0f, animation_time - dt);
            selector_lever.transform.localPosition = Vector3.Lerp(starting_lever_pos, dest_lever_pos, 1.0f - (animation_time / MOVE_TIME));
            selector_display.transform.GetChild(0).GetChild(0).localPosition = Vector3.Lerp(starting_marker_pos, dest_marker_pos, 1.0f - (animation_time / MOVE_TIME));

            yield return null;
        }


        selector_display.transform.GetChild(1).GetChild(current_bay).GetChild(0).gameObject.SetActive(true);

        for (int i = 0; i < 4; i++)
        {
            if (i == current_bay)
            {
                for (int t = 0; t < 5; t++)
                {
                    Color c = selector_display.transform.GetChild(2).GetChild(i).GetChild(t).GetComponent<UnityEngine.UI.RawImage>().color;
                    if (c.a > 0.05f)
                    {
                        c.a = 1.0f;
                        selector_display.transform.GetChild(2).GetChild(i).GetChild(t).GetComponent<UnityEngine.UI.RawImage>().color = c;
                    }
                }
            }
        }

        BUTTONS[0].updateInteractable(current_bay > 0 && is_powered == true);
        BUTTONS[1].updateInteractable(current_bay < 3 && is_powered == true);
        BUTTONS[0].untoggle();
        BUTTONS[1].untoggle();

        torpedo_shift_coroutine = null;
        updateShiftMarker();
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
        selector_display.transform.GetChild(0).GetChild(0).transform.localPosition = new Vector3(0.0f, -0.04f, 0.0f);
        for (int i = 0; i < 4; i++)
        {
            selector_display.transform.GetChild(1).GetChild(i).GetChild(0).gameObject.SetActive(i == 0);
        }
        updateShiftMarker();
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