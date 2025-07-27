/*
    EmergencyLights.cs
    - Handles inputs for emergency lights
    - Moves slider
    - Increases/decreases emergency lights
    Contributor(s): Jake Schott
    Last Updated: 5/21/2025
*/

using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class EmergencyLights : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 0.5f;
    private static float EMERGENCY_LIGHT_MAX_INTENSITY = 10.0f;

    private string CONTROL_NAME = "EMERGENCY LIGHTS";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject slider;
    public GameObject display_canvas; //used to display the bars beneath the handle
    public GameObject emergency_lights;

    private float light_level = 0.0f;
    private Vector3 initial_pos; //handle starting position (0% light_level)
    private Vector3 final_pos = new Vector3(0, -0.01307f, 10.8496f); //handle final position (100% light_level)

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], true, false)); //increase button
        hud_info.setButtons(BUTTONS);

        initial_pos = slider.transform.localPosition; //sets the initial position
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    public float getCurrentlight_level()
    {
        return light_level;
    }
    private void displayAdjustment()
    {
        //update bars on screen
        float tmp_lght = light_level;
        for (int i = 0; i <= 19; i++)
        {
            tmp_lght = light_level - (0.05f * i);
            float a = tmp_lght / 0.05f;
            display_canvas.transform.GetChild(1 + i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.93f, 1.0f, a);
        }

        //update lever position
        slider.transform.localPosition =
            new Vector3(Mathf.Lerp(initial_pos.x, final_pos.x, light_level),
                        Mathf.Lerp(initial_pos.y, final_pos.y, light_level),
                        Mathf.Lerp(initial_pos.z, final_pos.z, light_level));

        //update emergency lights
        for (int i = 0; i < emergency_lights.transform.childCount; i++)
        {
            emergency_lights.transform.GetChild(i).GetComponent<Light>().intensity = EMERGENCY_LIGHT_MAX_INTENSITY * light_level;
        }
    }
    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        int light_level_direction = 0;
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //E to increment
        {
            light_level_direction += 1;
        }
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))  //Q to decrement
        {
            light_level_direction -= 1;
        }
        if (light_level_direction != 0)
        {
            if (light_level_direction > 0)
            {
                light_level = Mathf.Min(1.0f, light_level + (dt * MOVE_SPEED));
            }
            else
            {
                light_level = Mathf.Max(0.0f, light_level - (dt * MOVE_SPEED));
            }
            if (light_level <= 0)
            {
                hud_info.getButtons()[0].updateInteractable(false);
            }
            else
            {
                hud_info.getButtons()[0].updateInteractable(true);
            }
            if (light_level >= 1f)
            {
                hud_info.getButtons()[1].updateInteractable(false);
            }
            else
            {
                hud_info.getButtons()[1].updateInteractable(true);
            }
            transmitEmergencyLightAdjustmentRPC(light_level);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEmergencyLightAdjustmentRPC(float el)
    {
        light_level = el;
        displayAdjustment();
    }
}
