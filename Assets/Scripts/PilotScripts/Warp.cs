/*
    Warp.cs
    - Handles warp throttle
    - Does nothing
    Contributor(s): Jake Schott
    Last Updated: 5/2/2026
*/

using System.Collections.Generic;
using UnityEngine;

public class Warp : MonoBehaviour, IControllable, IPowerable, IIKTargetable
{
    private string CONTROL_NAME = "WARP THROTTLE";
    private static string INFO_MESSAGE = "Enables ship capability to reach superluminal speed at variable warp factors.";
    private List<string> CONTROL_DESCS = new List<string>() { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5, 6 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject warp_display;

    //private bool is_powered = false;
    
    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public GameObject IK_target;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Grasp;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);

        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);
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

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        //does nothing
    }

    public void powerOn(int position)
    {
        warp_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        warp_display.SetActive(false);
    }
}