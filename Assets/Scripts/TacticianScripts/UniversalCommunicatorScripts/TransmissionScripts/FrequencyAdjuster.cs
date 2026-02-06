/*
    FrequencyAdjuster.cs
    - Switches frequencies
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class FrequencyAdjuster : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float TURN_TIME = 70.0f; //for dial
    private static float FREQUENCY_SWITCH_SPEED = 2.5f; //for frequency

    private string CONTROL_NAME = "FREQUENCY ADJUSTER";
    private static string INFO_MESSAGE = "Adjusts the frequency for universal communicator transmissions.";
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject frequency_dial;

    private TransmissionHandler transmission_handler;

    private bool is_active = false;
    private float dial_rotation = 0.0f;
    private float frequency_update = 0.5f; //increases at 1.0, decreases at 0.0

    private static HUDInfo hud_info = null;

    private void Start()
    {
        transmission_handler = GetComponent<TransmissionHandler>();  

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

    private void displayAdjustment()
    {
        //rotate dial
        frequency_dial.transform.localRotation = Quaternion.Euler(248.0f, 0.0f, dial_rotation);
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_active == false || transmission_handler.isTransmitting() == true)
        {
            return;
        }

        int dial_direction = 0;
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //E to increment
        {
            dial_direction += 1;
        }
        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))  //Q to decrement
        {
            dial_direction -= 1;
        }
        if (dial_direction != 0)
        {
            int freq = transform.GetComponent<TransmissionHandler>().getCurrentFrequencyIndex();
            if (dial_direction > 0)
            {
                dial_rotation += dt * TURN_TIME;
                frequency_update += dt * FREQUENCY_SWITCH_SPEED;
            }
            else
            {
                dial_rotation -= dt * TURN_TIME;
                frequency_update -= dt * FREQUENCY_SWITCH_SPEED;
            }
            if (frequency_update >= 1.0f)
            {
                frequency_update -= 1.0f;
                freq++;
            }
            else if (frequency_update <= 0.0f)
            {
                frequency_update += 1.0f;
                freq--;
            }
            if (dial_rotation > 360.0f)
            {
                dial_rotation -= 360.0f;
            }
            else if (dial_rotation > 0.0f)
            {
                dial_rotation += 360.0f;
            }
            transmitFrequencyAdjustmentRPC(dial_rotation, frequency_update, freq);
        }
    }

    public void activate()
    {
        is_active = true;
        BUTTONS[0].updateInteractable(true);
        BUTTONS[1].updateInteractable(true);
    }

    public void deactivate()
    {
        is_active = false;
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitFrequencyAdjustmentRPC(float dr, float fu, int freq)
    {
        dial_rotation = dr;
        frequency_update = fu;
        transmission_handler.updateFrequency(freq);
        displayAdjustment();
    }
}