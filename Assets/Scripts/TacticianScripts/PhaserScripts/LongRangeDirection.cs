/*
    LongRangeDirection.cs
    - Handles inputs for long-range phaser direction
    Contributor(s): Jake Schott
    Last Updated: 5/14/2025
*/

using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LongRangeDirection : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 10.0f;

    private string CONTROL_NAME = "LONG-RANGE PHASER DIRECTION";
    private List<string> CONTROL_DESCS = new List<string> {"ROTATE LEFT", "ROTATE RIGHT"};
    private List<int> CONTROL_INDEXES = new List<int>() {4, 5};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject long_range_lever = null;
    public GameObject long_range_indicator = null;

    private float long_range_angle = 0.0f;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], true, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], true, false));
        hud_info.setButtons(BUTTONS);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }
    public float getPhaserDirectionAngle()
    {
        return long_range_angle;
    }

    private void displayAdjustment()
    {
        long_range_indicator.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, long_range_angle);
        long_range_lever.transform.localRotation = Quaternion.Euler(-69.4f, 180f, 270f + long_range_angle);
    }
    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int angle_direction = 0;
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //E to increment
        {
            angle_direction += 1;
        }
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))  //Q to decrement
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

    [Rpc(SendTo.Everyone)]
    private void transmitLongRangePhaserAngleAdjustmentRPC(float ang)
    {
        long_range_angle = ang;
        displayAdjustment();
    }
}