/*
    ProbeLateralMovement.cs
    - Pushes in lateral movement buttons
    - Adjusts screen
    - Affects probe
    Contributor(s): Jake Schott
    Last Updated: 8/22/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ProbeLateralMovement : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float BUTTON_SPEED = 10.0f;
    private static float PROBE_SPEED = 10.0f;

    private string CONTROL_NAME = "PROBE LATERAL MOVEMENT";
    private List<string> CONTROL_DESCS = new List<string> {"FORWARD", "LEFT", "REVERSE", "RIGHT"};
    private List<int> CONTROL_INDEXES = new List<int>() {0, 1, 2, 3};
    private List<Button> BUTTONS = new List<Button>();

    public List<GameObject> lateral_buttons = null; //forward, left, reverse, right
    public GameObject lateral_display;

    private bool is_powered = false;
    private GameObject probe;
    private Vector3[] initial_positions = new Vector3[4];
    private Vector3[] final_positions = new Vector3[4];
    private float[] lateral_movement_factors = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f }; //forward, left, reverse, right
    private Vector3 lateral_button_move_direction = new Vector3(0, -0.006f, 0.0024f);
    private Vector3 probe_position = new Vector3(0.0f, 0.0f, 0.0f);
    private Coroutine lateral_adjustment_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[3], CONTROL_INDEXES[3], false, false));
        hud_info.setButtons(BUTTONS);

        for (int i = 0; i <= 3; i++)
        {
            initial_positions[i] = lateral_buttons[i].transform.localPosition;
            final_positions[i] = lateral_buttons[i].transform.localPosition + lateral_button_move_direction;
        }
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }
    private void displayAdjustment()
    {
        //push lateral buttons, update circle
        for (int i = 0; i <= 3; i++)
        {
            lateral_buttons[i].transform.localPosition =
                new Vector3(Mathf.Lerp(initial_positions[i].x, final_positions[i].x, lateral_movement_factors[i]),
                            Mathf.Lerp(initial_positions[i].y, final_positions[i].y, lateral_movement_factors[i]),
                            Mathf.Lerp(initial_positions[i].z, final_positions[i].z, lateral_movement_factors[i]));

            lateral_display.transform.GetChild(i).gameObject.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, lateral_movement_factors[i]);
        }

        //update probe
        if (probe != null)
        {
            probe.transform.localPosition = probe_position;
            probe.GetComponent<Probe>().updateDistance();
        }
    }

    public void linkProbe(GameObject new_probe)
    {
        probe = new_probe;
        for (int i = 0; i <= 3; i++)
        {
            BUTTONS[i].updateInteractable(true);
        }
        lateral_display.transform.GetChild(4).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0, 0.84f, 1.0f, 1.0f);
    }

    public void unlinkProbe()
    {
        probe = null;
        for (int i = 0; i <= 3; i++)
        {
            BUTTONS[i].updateInteractable(false);
        }
        lateral_display.transform.GetChild(4).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0, 0.84f, 1.0f, 0.196f);
    }

    private bool isNeutralState()
    {
        for (int i = 0; i <= 3; i++)
        {
            if (lateral_movement_factors[i] != 0.0f)
            {
                return false;
            }
        }
        return true;
    }

    IEnumerator lateralAdjustment()
    {
        while (keys_down.Count > 0 || !isNeutralState())
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            if (probe != null)
            {
                probe_position = probe.transform.localPosition;
            }

            if (is_powered == true)
            {
                //check inputs/return buttons to default
                for (int i = 0; i <= 3; i++)
                {
                    if (ControlScript.checkInputIndex(CONTROL_INDEXES[i], keys_down) && probe != null)
                    {
                        lateral_movement_factors[i] = Mathf.Min(1.0f, lateral_movement_factors[i] + dt * BUTTON_SPEED);
                    }
                    else
                    {
                        lateral_movement_factors[i] = Mathf.Max(0.0f, lateral_movement_factors[i] - dt * BUTTON_SPEED);
                    }
                }
            }

            //if probe is active, update its position
            if (probe != null)
            {
                if (Mathf.Abs(lateral_movement_factors[0] - lateral_movement_factors[2]) > 0.0f)
                {
                    probe_position += probe.transform.forward * (lateral_movement_factors[0] - lateral_movement_factors[2]) * dt * PROBE_SPEED;
                }
                if (Mathf.Abs(lateral_movement_factors[3] - lateral_movement_factors[1]) > 0.0f)
                {
                    probe_position += probe.transform.right * (lateral_movement_factors[3] - lateral_movement_factors[1]) * dt * PROBE_SPEED;
                }
            }

            for (int i = 0; i <= 3; i++)
            {
                if (lateral_movement_factors[i] != 1.0f)
                {
                    transmitProbeLateralAdjustmentRPC(probe_position, lateral_movement_factors[0], lateral_movement_factors[1], lateral_movement_factors[2], lateral_movement_factors[3]);
                    break;
                }
            }

            keys_down.Clear();
            yield return null;
        }

        lateral_adjustment_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        keys_down = inputs;
        if (lateral_adjustment_coroutine == null)
        {
            for (int i = 0; i < CONTROL_INDEXES.Count; i++)
            {
                if (ControlScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    lateral_adjustment_coroutine = StartCoroutine(lateralAdjustment());
                    return;
                }
            }
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        lateral_display.SetActive(true);
        BUTTONS[0].updateInteractable(probe != null);
        BUTTONS[1].updateInteractable(probe != null);
        BUTTONS[2].updateInteractable(probe != null);
        BUTTONS[3].updateInteractable(probe != null);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        lateral_display.SetActive(false);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
        BUTTONS[2].updateInteractable(false);
        BUTTONS[3].updateInteractable(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitProbeLateralAdjustmentRPC(Vector3 new_pos, float fwd, float left, float rev, float right)
    {
        lateral_movement_factors[0] = fwd;
        lateral_movement_factors[1] = left;
        lateral_movement_factors[2] = rev;
        lateral_movement_factors[3] = right;
        probe_position = new_pos;
        displayAdjustment();
    }
}
