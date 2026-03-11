/*
    DirectionalShifter.cs
    - Handles shifting between forward and reverse
    - Moves shift lever accordingly
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DirectionalShifter : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float MOVE_SPEED = 0.6f;
    private static float DELAY_TIME = 1.0f;
    private static Vector3 REVERSE_POS = new Vector3(0.2816f, -1.3416f, 19.1194f);

    private string CONTROL_NAME = "DIRECTIONAL SHIFTER";
    private static string INFO_MESSAGE = "Handles the direction the ship travels at impulse speed. Can only be used when stationary.";
    private List<string> CONTROL_DESCS = new List<string> { "SHIFT" };
    private List<int> CONTROL_INDEXES = new List<int>() {6};
    private List<Button> BUTTONS = new List<Button>();

    public GameObject lever;
    public GameObject forward_indicator;
    public GameObject reverse_indicator;
    private GameObject spaceship;
    public GameObject IK_target;

    private bool is_powered = false;
    private bool in_reverse = false; //true means in reverse, false means forward
    private float shift_percentage = 1.0f; //1 is forward, 0 is reverse
    private Vector3 forward_pos;
    private List<KeyCode> keys_down = new List<KeyCode>();
    private Coroutine shift_adjuster_coroutine;

    private static HUDInfo hud_info = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Grasp;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);

        spaceship = GameObject.FindGameObjectWithTag("Spaceship");

        forward_pos = lever.transform.localPosition;
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        if (shift_adjuster_coroutine == null)
        {
            BUTTONS[0].updateInteractable(GetComponent<ImpulseThrottle>().getCurrentImpulse() == 0.0f && is_powered == true);
        }
        return hud_info;
    }
    public Transform getIKTarget(GameObject current_target)
    {
        return IK_target.transform;
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
    private void displayAdjustment()
    {
        float percent_to_top = Mathf.Min(1.0f, Mathf.Max(0.0f, (shift_percentage - 0.4f) / 0.2f));
        float percent_to_center = (shift_percentage / 0.4f) * 0.5f;
        if (shift_percentage > 0.6f)
        {
            percent_to_center = (((shift_percentage - 0.6f) / 0.4f) * 0.5f) + 0.5f;
        }
        else if(shift_percentage > 0.4)
        {
            percent_to_center = 0.5f;
        }

        lever.transform.localPosition =
            new Vector3(Mathf.Lerp(REVERSE_POS.x, forward_pos.x, percent_to_top),
                        Mathf.Lerp(REVERSE_POS.y, forward_pos.y, percent_to_top),
                        Mathf.Lerp(REVERSE_POS.z, forward_pos.z, percent_to_center));

        forward_indicator.SetActive(!in_reverse && is_powered);
        reverse_indicator.SetActive(in_reverse && is_powered);
    }

    private bool checkNeutralState()
    {
        return (shift_percentage == 0.0f || shift_percentage == 1.0f);
    }

    IEnumerator shiftAdjuster()
    {
        while (keys_down.Count > 0 || !checkNeutralState())
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            bool shifting = PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && is_powered == true;
            float temp_shift_percentage = shift_percentage;

            if (shifting == true)
            {
                if (in_reverse == false) //moving towards reverse
                {
                    shift_percentage = Mathf.Max(0.0f, shift_percentage - dt * MOVE_SPEED);
                }
                else //moving towards forward
                {
                    shift_percentage = Mathf.Min(1.0f, shift_percentage + dt * MOVE_SPEED);
                }

            }
            else
            {
                if (shift_percentage != 0.0f && shift_percentage != 1.0f)
                {
                    if (shift_percentage > 0.5f)
                    {
                        shift_percentage = Mathf.Min(1.0f, shift_percentage + dt * MOVE_SPEED);
                    }
                    else
                    {
                        shift_percentage = Mathf.Max(0.0f, shift_percentage - dt * MOVE_SPEED);
                    }
                }
            }

            bool cooling_down = false;

            //handle shift percentage adjustment
            if (temp_shift_percentage != shift_percentage) //shift percentage has changed
            {
                if (shift_percentage == 1.0f || shift_percentage == 0.0f)
                {
                    //potential reverse change
                    if ((shift_percentage == 1.0f && in_reverse == true) || (shift_percentage == 0.0f && in_reverse == false))
                    {
                        BUTTONS[0].updateInteractable(false);
                        in_reverse = !in_reverse;
                        cooling_down = true;
                    }
                }
                transmitDirectionalShifterRPC(shift_percentage, in_reverse);
            }

            keys_down.Clear();
            if (!cooling_down) 
            { 
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(DELAY_TIME);
                BUTTONS[0].updateInteractable(is_powered == true);
            }
        }

        shift_adjuster_coroutine = null;
    }

    public void resetToDefault()
    {
        in_reverse = false;
        shift_percentage = 1.0f;
        displayAdjustment();
    }

    public void powerOn(int position)
    {
        is_powered = true;
        forward_indicator.SetActive(!in_reverse);
        reverse_indicator.SetActive(in_reverse);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        forward_indicator.SetActive(false);
        reverse_indicator.SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDirectionalShifterRPC(float sp, bool reverse)
    {
        shift_percentage = sp;
        in_reverse = reverse;
        spaceship.GetComponent<PilotingSystem>().ShiftDirection(in_reverse);
        displayAdjustment();
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        keys_down = inputs;
        if (shift_adjuster_coroutine == null && GetComponent<ImpulseThrottle>().getCurrentImpulse() == 0.0f)
        {
            for (int i = 0; i < CONTROL_INDEXES.Count; i++)
            {
                if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[i], inputs))
                {
                    shift_adjuster_coroutine = StartCoroutine(shiftAdjuster());
                    return;
                }
            }
        }
    }
}