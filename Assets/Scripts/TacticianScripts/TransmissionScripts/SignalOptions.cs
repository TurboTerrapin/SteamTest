/*
    SignalOptions.cs
    - Handles the controls that send/receive transmissions
    Contributor(s): Jake Schott
    Last Updated: 8/23/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SignalOptions : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float TURN_TIME = 0.75f;

    private string CONTROL_NAME = "SIGNAL OPTIONS";
    private List<string> CONTROL_DESCS = new List<string>{"RECEIVE", "BROADCAST"};
    private List<int> CONTROL_INDEXES = new List<int>(){6};
    private List<Button>[] BUTTON_LISTS = new List<Button>[2]{new List<Button>(), new List<Button>()};

    public List<GameObject> dials = null;

    private TransmissionHandler transmission_handler;
    private Coroutine dial_turn_coroutine = null;
    private float[] dial_turn_percentages = { 0.0f, 0.0f };

    private List<KeyCode> keys_down = new List<KeyCode>();
    private List<string> ray_targets = new List<string> { "transmission_receive", "transmission_broadcast" };
    private int ray_target_index = -1;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        transmission_handler = transform.GetComponent<TransmissionHandler>();

        hud_info = new HUDInfo(CONTROL_NAME);

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[0], false, false));

        hud_info.setButtons(BUTTON_LISTS[0]);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setButtons(BUTTON_LISTS[index]);
        return hud_info;
    }

    private void displayDialTurn(int index)
    {
        dials[index].transform.localRotation =
            Quaternion.Euler(dials[index].transform.localEulerAngles.x,
                             dials[index].transform.localEulerAngles.y,
                             Mathf.Lerp(180.0f, 90.0f, dial_turn_percentages[index]));
    }

    private bool checkNeutralState()
    {
        for (int i = 0; i < 2; i++)
        {
            if (dial_turn_percentages[i] > 0.0f)
            {
                return false;
            }
        }
        return true;
    }

    IEnumerator dialReturn()
    {
        while (dial_turn_percentages[0] > 0.0f || dial_turn_percentages[1] > 0.0f)
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < 2; i++)
            {
                dial_turn_percentages[i] = Mathf.Max(0.0f, dial_turn_percentages[i] - (dt / TURN_TIME));
                displayDialTurn(i);
            }
            yield return null;
        }

        dial_turn_coroutine = null;
    }

    public void returnDials()
    {
        if (dial_turn_coroutine != null)
        {
            StopCoroutine(dial_turn_coroutine);
        }
        dial_turn_coroutine = StartCoroutine(dialReturn());
    }

    IEnumerator dialTurn()
    {
        while (keys_down.Count > 0 || checkNeutralState() == false)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            if (ray_target_index >= 0)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && transmission_handler.getIsPowered() == true)
                {
                    dial_turn_percentages[ray_target_index] = Mathf.Min(1.0f, dial_turn_percentages[ray_target_index] + (dt / TURN_TIME));
                    if (dial_turn_percentages[ray_target_index] >= 1.0f)
                    {
                        if (transmission_handler.isTransmitting() == false)
                        {
                            transmission_handler.transmitSignal(ray_target_index); //handles the transmission stuff
                        }
                    }
                }
                else
                {
                    dial_turn_percentages[ray_target_index] = Mathf.Max(0.0f, dial_turn_percentages[ray_target_index] - (dt / TURN_TIME));
                }
            }

            for (int i = 0; i < 2; i++)
            {
                if (i != ray_target_index)
                {
                    dial_turn_percentages[i] = Mathf.Max(0.0f, dial_turn_percentages[i] - (dt / TURN_TIME));
                }
            }

            transmitDialArmRPC(dial_turn_percentages[0], dial_turn_percentages[1]);

            keys_down.Clear();
            ray_target_index = -1;
            yield return null;
        }

        dial_turn_coroutine = null;
    }

    public void activate()
    {
        BUTTON_LISTS[0][0].updateInteractable(true);
        BUTTON_LISTS[1][0].updateInteractable(true);
    }

    public void deactivate()
    {
        BUTTON_LISTS[0][0].updateInteractable(false);
        BUTTON_LISTS[1][0].updateInteractable(false);
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (transmission_handler.getIsPowered() == false || transmission_handler.isTransmitting() == true)
        {
            return;
        }

        keys_down = inputs;
        ray_target_index = ray_targets.IndexOf(current_target.name);

        if (dial_turn_percentages[ray_target_index] == 0.0f )
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                if (dial_turn_coroutine == null)
                {
                    dial_turn_coroutine = StartCoroutine(dialTurn());
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDialArmRPC(float dp_receive, float dp_broadcast)
    {
        dial_turn_percentages[0] = dp_receive;
        dial_turn_percentages[1] = dp_broadcast;

        for (int i = 0; i < 2; i++)
        {
            displayDialTurn(i);
        }
    }
}