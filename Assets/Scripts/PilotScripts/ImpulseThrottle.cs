/*
    ImpulseThrottle.cs
    - Handles inputs for impulse throttle
    - Moves throttle lever accordingly
    Contributor(s): Jake Schott
    Last Updated: 8/20/2025
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ImpulseThrottle : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 35.0f;

    private string CONTROL_NAME = "IMPULSE THROTTLE";
    private List<string> CONTROL_DESCS = new List<string> {"DECREASE", "INCREASE"};
    private List<int> CONTROL_INDEXES = new List<int>() {4, 5};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject handle;
    public GameObject impulse_bars_display; //used to display the bars beneath the handle
    public GameObject speed_text; //used to update the speedometer

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;

    private float impulse = 0.0f;
    private float inertial_dampener_modifier = 0.0f;
    private Vector3 initial_pos; //handle starting position (0% impulse)
    private Vector3 final_pos = new Vector3(0.2816f, -1.2306f, 19.3834f);

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false)); //decrease button
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false)); //increase button
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
            impulse_bars_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
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
        if (is_powered == false)
        {
            return;
        }
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
                impulse = Mathf.Min(1.0f, impulse + (0.002f * (impulse / 0.5f) + 0.001f) * dt * MOVE_SPEED * (1.0f + (3.5f * inertial_dampener_modifier)));
            }
            else
            {
                impulse = Mathf.Max(0.0f, impulse - (0.002f * (impulse / 0.5f) + 0.001f) * dt * MOVE_SPEED * (1.0f + (3.5f * inertial_dampener_modifier)));
            }
            BUTTONS[0].updateInteractable(impulse > 0.0f);
            BUTTONS[1].updateInteractable(impulse < 1.0f);
            transmitImpulseAdjustmentRPC(impulse);
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float start_imp = impulse;
        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            impulse = Mathf.Lerp(start_imp, 0.0f, 1.0f - (anim_time / power_off_time));
            displayAdjustment();
            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        BUTTONS[0].updateInteractable(impulse > 0.0f);
        BUTTONS[1].updateInteractable(impulse < 1.0f);
        impulse_bars_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
        impulse_bars_display.SetActive(false);

        //return impulse throttle/impulse to 0
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
    }

    [Rpc(SendTo.Everyone)]
    private void transmitImpulseAdjustmentRPC(float imp)
    {
        impulse = imp;
        displayAdjustment();
    }
}
