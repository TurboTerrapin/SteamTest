/*
    SignalOptions.cs
    - Handles the controls that send/receive transmissions
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SignalOptions : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float TURN_TIME = 0.5f;

    private string[] CONTROL_NAMES = new string[2] {"RECEIVE TRANSMISSION", "BROADCAST TRANSMISSION"};
    private List<string> INFO_MESSAGES = new List<string>() { "Receives any messages on given frequency and displays on universal communicator.", "Broadcasts universal communicator message on given frequency (green light indicates success)." };
    private List<string> CONTROL_DESCS = new List<string>{"TRANSMIT"};
    private List<int> CONTROL_INDEXES = new List<int>(){6};
    private List<Button>[] BUTTON_LISTS = new List<Button>[2]{new List<Button>(), new List<Button>()};

    public List<GameObject> dials = null;

    private UniversalCommunicator universal_communicator;
    private TransmissionHandler transmission_handler;

    private bool is_active = false;
    private Coroutine dial_turn_coroutine = null;
    private float[] dial_turn_percentages = { 0.0f, 0.0f };

    private List<string> ray_targets = new List<string> { "transmission_receive", "transmission_broadcast" };
    private int ray_target_index = -1;

    private static HUDInfo hud_info = null;

    private void Start()
    {
        universal_communicator = GetComponent<UniversalCommunicator>();
        transmission_handler = GetComponent<TransmissionHandler>();

        hud_info = new HUDInfo(CONTROL_NAMES[0], true);

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));

        hud_info.setButtons(BUTTON_LISTS[0], 6);
    }

    public HUDInfo getHUDinfo()
    {
        return hud_info;
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 6);
        hud_info.setInfo(INFO_MESSAGES[index]);

        return hud_info;
    }

    private void displayDialTurn(int index)
    {
        dials[index].transform.localRotation =
            Quaternion.Euler(dials[index].transform.localEulerAngles.x,
                             dials[index].transform.localEulerAngles.y,
                             Mathf.Lerp(180.0f, 90.0f, dial_turn_percentages[index]));
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

    IEnumerator dialTurn(int index)
    {
        float anim_time = TURN_TIME;

        while (anim_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            dial_turn_percentages[index] = 1.0f - (anim_time / TURN_TIME);
            displayDialTurn(index);

            yield return null;
        }

        if (NetworkManager.Singleton.IsHost == true)
        {
            if (transmission_handler.isTransmitting() == false)
            {
                transmission_handler.transmitSignal(index); //handles the transmission stuff
            }
        }

        dial_turn_coroutine = null;
    }

    public void activate()
    {
        is_active = true;
        BUTTON_LISTS[0][0].updateInteractable(true);
        BUTTON_LISTS[1][0].updateInteractable(true);
    }

    public void deactivate()
    {
        is_active = false;
        BUTTON_LISTS[0][0].updateInteractable(false);
        BUTTON_LISTS[1][0].updateInteractable(false);
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_active == false || transmission_handler.isTransmitting() == true)
        {
            return;
        }

        ray_target_index = ray_targets.IndexOf(current_target.name);

        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
        {
            if (dial_turn_coroutine == null)
            {
                BUTTON_LISTS[ray_target_index][0].toggle(0.2f);
                transmitDialArmRPC(ray_target_index);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDialArmRPC(int index)
    {
        universal_communicator.disableKeyboard();

        if (dial_turn_coroutine != null)
        {
            StopCoroutine(dial_turn_coroutine);
        }

        dial_turn_coroutine = StartCoroutine(dialTurn(index));
    }
}