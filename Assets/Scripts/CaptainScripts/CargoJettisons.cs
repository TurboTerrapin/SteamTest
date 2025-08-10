/*
    CargoJettions.cs
    - Launches item loaded in cargo bay
    Contributor(s): Jake Schott
    Last Updated: 8/8/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CargoJettisons : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float ARM_TIME = 1.5f;
    private static float PUSH_TIME = 1.0f;
    private static float COOLDOWN_TIME = 3.0f;

    private string CONTROL_NAME = "CARGO JETTISON";
    private List<string> CONTROL_DESCS = new List<string>() { "EJECT", "ARM" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6, 11 };
    private List<Button> BUTTONS = new List<Button>(0);

    public GameObject dial;

    private Coroutine dial_turn_coroutine = null;
    private Coroutine cargo_eject_coroutine = null;
    private float dial_turn_percentage = 0.0f;
    private Vector3 initial_pos;
    private Vector3 push_direction = new Vector3(0.006f, -0.0151f, 0.0f);

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);

        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], true, false));
        initial_pos = dial.transform.localPosition;

        hud_info.setButtons(BUTTONS);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    private void displayDialTurn()
    {
        dial.transform.localRotation =
            Quaternion.Euler(dial.transform.localEulerAngles.x,
                             dial.transform.localEulerAngles.y,
                             Mathf.Lerp(-90.0f, -180.0f, dial_turn_percentage));
    }

    private bool checkNeutralState()
    {
        if (dial_turn_percentage > 0.0f && cargo_eject_coroutine == null)
        {
            return false;
        }
        return true;
    }

    IEnumerator dialTurn()
    {
        while (keys_down.Count > 0 || checkNeutralState() == false)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], keys_down))
            {
                dial_turn_percentage = Mathf.Min(1.0f, dial_turn_percentage + (dt / ARM_TIME));
            }
            else
            {
                dial_turn_percentage = Mathf.Max(0.0f, dial_turn_percentage - (dt / ARM_TIME));
            }
            BUTTONS[0].updateInteractable(dial_turn_percentage >= 1.0f);

            transmitDialArmRPC(dial_turn_percentage);

            keys_down.Clear();
            yield return null;
        }

        dial_turn_coroutine = null;
    }

    IEnumerator ejectCargo()
    {
        dial.transform.localPosition = initial_pos;
        dial.transform.localRotation =
            Quaternion.Euler(dial.transform.localEulerAngles.x,
                             dial.transform.localEulerAngles.y,
                             -180.0f);

        Vector3 final_pos = initial_pos + push_direction;

        //push the dial in
        float push_time = PUSH_TIME;
        while (push_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            push_time = Mathf.Max(0.0f, push_time - dt);

            dial.transform.localPosition =
                new Vector3(Mathf.Lerp(initial_pos.x, final_pos.x, 1.0f - (push_time / PUSH_TIME)),
                            Mathf.Lerp(initial_pos.y, final_pos.y, 1.0f - (push_time / PUSH_TIME)),
                            Mathf.Lerp(initial_pos.z, final_pos.z, 1.0f - (push_time / PUSH_TIME)));

            yield return null;
        }

        //bring the dial back and unrotate
        float cooldown_time = COOLDOWN_TIME;
        while (cooldown_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            cooldown_time = Mathf.Max(0.0f, cooldown_time - dt);

            dial.transform.localPosition =
                new Vector3(Mathf.Lerp(initial_pos.x, final_pos.x, cooldown_time / COOLDOWN_TIME),
                            Mathf.Lerp(initial_pos.y, final_pos.y, cooldown_time / COOLDOWN_TIME),
                            Mathf.Lerp(initial_pos.z, final_pos.z, cooldown_time / COOLDOWN_TIME));

            dial.transform.localRotation =
                Quaternion.Euler(dial.transform.localEulerAngles.x,
                                 dial.transform.localEulerAngles.y,
                                 Mathf.Lerp(-90.0f, -180.0f, cooldown_time / COOLDOWN_TIME));

            yield return null;
        }

        BUTTONS[1].updateInteractable(true);
        dial_turn_percentage = 0.0f;

        dial_turn_coroutine = null;
        cargo_eject_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        keys_down = inputs;

        //check for eject
        if (dial_turn_percentage >= 1.0f && cargo_eject_coroutine == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTONS[0].toggle(0.2f);
                BUTTONS[0].updateInteractable(false);
                BUTTONS[1].updateInteractable(false);
                transmitEjectRPC();
            }
        }

        //check for dial turn
        if (dial_turn_percentage == 0.0f && cargo_eject_coroutine == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs))
            {
                if (dial_turn_coroutine == null)
                {
                    dial_turn_coroutine = StartCoroutine(dialTurn());
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDialArmRPC(float dp)
    {
        dial_turn_percentage = dp;
        if (cargo_eject_coroutine == null)
        {
            displayDialTurn();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEjectRPC()
    {
        if (cargo_eject_coroutine != null)
        {
            StopCoroutine(cargo_eject_coroutine);
        }
        if (dial_turn_coroutine != null)
        {
            StopCoroutine(dial_turn_coroutine);
        }

        cargo_eject_coroutine = StartCoroutine(ejectCargo());
    }
}
