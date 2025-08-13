/*
    ImpulseThrottle.cs
    - Handles inputs for impulse throttle
    - Moves throttle lever accordingly
    Contributor(s): Jake Schott
    Last Updated: 8/11/2025
*/

using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ImpulseThrottle : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 25.0f;

    private string CONTROL_NAME = "IMPULSE THROTTLE";
    private List<string> CONTROL_DESCS = new List<string> {"DECREASE", "INCREASE"};
    private List<int> CONTROL_INDEXES = new List<int>() {4, 5};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject handle;
    public GameObject blue_bars; //used to display the bars beneath the handle
    public GameObject speed_text; //used to update the speedometer

    private float impulse = 0.0f;
    private float inertial_dampener_modifier = 1.0f;
    private Vector3 initial_pos; //handle starting position (0% impulse)
    private Vector3 final_pos = new Vector3(0.2816f, -1.2306f, 19.3834f);

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], true, false)); //increase button
        hud_info.setButtons(BUTTONS);

        initial_pos = handle.transform.localPosition; //sets the initial position
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    public void adjustInertialDampenerModifier(float new_modifier)
    {
        inertial_dampener_modifier = new_modifier;
    }

    public float getCurrentImpulse()
    {
        return impulse;
    }
    private void displayAdjustment()
    {
        //update bars on screen
        float tmp_imp = impulse;
        for (int i = 0; i <= 19; i++)
        {
            tmp_imp = impulse - (0.05f * i);
            float a = tmp_imp / 0.05f;
            blue_bars.transform.GetChild(1 + i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
        }

        //update lever position
        handle.transform.localPosition =
            new Vector3(Mathf.Lerp(initial_pos.x, final_pos.x, impulse),
                        Mathf.Lerp(initial_pos.y, final_pos.y, impulse),
                        Mathf.Lerp(initial_pos.z, final_pos.z, impulse));

        //update speedometer text in engineer position
        string rounded_speed = (Mathf.Round(impulse * 1000.0f) / 10.0f).ToString();
        if (rounded_speed.Contains(".") == false)
        {
            rounded_speed += ".0";
        }
        speed_text.GetComponent<TMP_Text>().SetText("IMPULSE SPEED: " + rounded_speed + "%");
    }
    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int impulse_direction = 0;
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //E to increment
        {
            impulse_direction += 1;
        }
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))  //Q to decrement
        {
            impulse_direction -= 1;
        }
        if (impulse_direction != 0)
        {
            if (impulse_direction > 0)
            {
                impulse = Mathf.Min(1.0f, impulse + (0.002f * (impulse / 0.5f) + 0.001f) * dt * MOVE_SPEED * inertial_dampener_modifier);
            }
            else
            {
                impulse = Mathf.Max(0.0f, impulse - (0.002f * (impulse / 0.5f) + 0.001f) * dt * MOVE_SPEED * inertial_dampener_modifier);
            }
            hud_info.getButtons()[0].updateInteractable(impulse > 0.0f);
            hud_info.getButtons()[1].updateInteractable(impulse < 1.0f);
            transmitImpulseAdjustmentRPC(impulse);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitImpulseAdjustmentRPC(float imp)
    {
        impulse = imp;
        displayAdjustment();
    }
}
