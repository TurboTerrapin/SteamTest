/*
    LongRangeDirection.cs
    - Handles inputs for long-range phaser direction
    Contributor(s): Jake Schott
    Last Updated: 1/4/2026
*/

using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LongRangeDirection : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 15.0f;

    private string CONTROL_NAME = "LONG-RANGE PHASER DIRECTION";
    private static string INFO_MESSAGE = "Angles the long-range (blue) phaser in the corresponding direction of the onscreen indicator.";
    private List<string> CONTROL_DESCS = new List<string> {"ROTATE LEFT", "ROTATE RIGHT"};
    private List<int> CONTROL_INDEXES = new List<int>() {4, 5};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject long_range_dial;
    public GameObject ship_long_range_phaser;
    public GameObject long_range_direction_display;
    public GameObject long_range_degree_display;

    private bool is_powered = false;
    private float long_range_angle = 0.0f;

    private static HUDInfo hud_info = null;

    [Header("IK Targetable Details")]
    public List<GameObject> IK_targets = null;
    public List<GameObject> hand_placements = null;
    public AnimatorHandler myAnimatorHandler = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Pinch;
    public float hand_pose = 0;
    public bool has_right_arm_IK_targets = false;
    public bool right_arm_IK_targets_active = true;
    public bool does_right_hand_flip = false;
    public Vector3 right_hand_offset = Vector3.zero;
    [Tooltip("Set to -1 for no lerp")]
    public float lerp_speed = 5f;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        hud_info.setButtons(BUTTONS, 7);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }
    public void setRightHandSpecificTargets(bool value)
    {
        if (!has_right_arm_IK_targets) return;
        right_arm_IK_targets_active |= value;
    }
    public Transform getIKTarget(GameObject current_target)
    {
        //int index = ray_targets.IndexOf(current_target.name);

        float shortestDistance;
        int shortestIndex = 0;
        shortestDistance = Vector3.Distance(hand_placements[0].transform.position, IK_targets[shortestIndex].transform.position);

        for (int i = 1; i < IK_targets.Count; i++)
        {
            float distance = Vector3.Distance(hand_placements[0].transform.position, IK_targets[i].transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                shortestIndex = i;
            }
        }

        return IK_targets[shortestIndex].transform;
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

    public float getPhaserDirectionAngle()
    {
        return long_range_angle;
    }

    private void displayAdjustment()
    {
        //update physical turret
        ship_long_range_phaser.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, long_range_angle);

        //update the circular screen
        long_range_direction_display.transform.GetChild(0).localRotation = Quaternion.Euler(0.0f, 0.0f, long_range_angle);

        //update dial
        long_range_dial.transform.localRotation = Quaternion.Euler(248.0f, 0.0f, 180.0f + long_range_angle);

        //update degree text
        float rounded_angle = Mathf.Round(long_range_angle * 10.0f) / 10.0f;
        string display_angle = rounded_angle.ToString();
        if (display_angle.Contains(".") == false)
        {
            display_angle += ".0";
        }
        if (display_angle.CompareTo("360.0") == 0)
        {
            display_angle = "0.0";
        }
        long_range_degree_display.transform.GetChild(0).GetComponent<TMP_Text>().SetText(display_angle + "°");
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        int angle_direction = 0;
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //E to increment
        {
            angle_direction += 1;
        }
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))  //Q to decrement
        {
            angle_direction -= 1;
        }
        if (angle_direction != 0)
        {
            if (angle_direction > 0)
            {
                long_range_angle += dt * MOVE_SPEED;
            }
            else
            {
                long_range_angle -= dt * MOVE_SPEED;
            }
            if (long_range_angle > 360.0f)
            {
                long_range_angle -= 360.0f;
            }
            else if (long_range_angle < 0.0f)
            {
                long_range_angle += 360.0f;
            }
            transmitLongRangePhaserAngleAdjustmentRPC(long_range_angle);
        }
    }

    public void resetToDefault()
    {
        long_range_angle = 0.0f;
        displayAdjustment();
    }

    public void powerOn(int position)
    {
        is_powered = true;
        long_range_direction_display.SetActive(true);
        long_range_degree_display.SetActive(true);
        BUTTONS[0].updateInteractable(true);
        BUTTONS[1].updateInteractable(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        long_range_direction_display.SetActive(false);
        long_range_degree_display.SetActive(false);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitLongRangePhaserAngleAdjustmentRPC(float ang)
    {
        long_range_angle = ang;
        displayAdjustment();
    }
}