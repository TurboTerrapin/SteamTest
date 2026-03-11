/*
    ShipSteering.cs
    - Handles inputs for steering wheel
    - Moves wheel accordingly
    Contributor(s): Jake Schott, Henryk Musial
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static AnimatorHandler;

public class ShipSteering : NetworkBehaviour, IControllable, IPowerable, IIKTargetable
{
    //CLASS CONSTANTS
    private const float maxAngularVelocity = 1.2f;
    private const float accelerationRate = 1.5f;
    private const float decelerationRate = 4.0f;
    private const float returnSpringForce = 6.0f;
    private const float wheelFriction = 0.95f;

    private string CONTROL_NAME = "SHIP STEERING";
    private static string INFO_MESSAGE = "Controls ship steering when impulse throttle is active.";
    private List<string> CONTROL_DESCS = new List<string> { "TURN LEFT", "TURN RIGHT" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject wheel;
    public GameObject wheel_light;
    public GameObject fill_circle;
    public GameObject IK_target;

    // State variables
    private float angularVelocity = 0f;
    public float wheel_angle = 0.0f; // Normalized wheel angle (-1, 1), visual wheel position 
    public float steering_input; // True steering input (Does not register spring oscillations beyond neutral)

    private bool is_powered = false;
    private Coroutine wheel_spin_coroutine = null;
    private List<KeyCode> keys_down = new List<KeyCode>();

    private HUDInfo hud_info = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Grasp;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    private void Start()
    {
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
    public float getSteeringValue()
    {
        return steering_input;
    }

    private void displayAdjustment()
    {
        //adjust blue fill circle beneath steering wheel
        fill_circle.transform.localRotation = Quaternion.Euler(0f, wheel_angle >= 0f ? 180f : 0f, 0f);
        fill_circle.GetComponent<UnityEngine.UI.Image>().fillAmount = Mathf.Abs(wheel_angle / 2.0f);

        //point physical wheel in right direction
        wheel.transform.localRotation = Quaternion.Euler(-113.0f, 0.0f, 450.0f * wheel_angle);
    }

    IEnumerator wheelSpinning()
    {
        steering_input = 0f;
        int lastInputDirection = 0;
        bool hasCrossedZeroSinceLastInput = false;

        while (keys_down.Count > 0 || Mathf.Abs(wheel_angle) > 0f || Mathf.Abs(angularVelocity) > 0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            int inputDirection = 0;
            bool isPlayerInputActive = false;

            if (!(PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], keys_down)))
            {
                if (is_powered == true)
                {
                    if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[1], keys_down)) //E
                    {
                        inputDirection = 1;
                        isPlayerInputActive = true;
                    }
                    else if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], keys_down)) //Q
                    {
                        inputDirection = -1;
                        isPlayerInputActive = true;
                    }
                }

                if (isPlayerInputActive)
                {
                    lastInputDirection = inputDirection;
                    hasCrossedZeroSinceLastInput = false;
                }

                if (inputDirection != 0)
                {
                    if (Mathf.Sign(angularVelocity) != inputDirection && Mathf.Abs(angularVelocity) > 0.1f)
                    {
                        angularVelocity = Mathf.MoveTowards(angularVelocity, 0f, decelerationRate * dt);
                    }
                    else
                    {
                        angularVelocity += inputDirection * accelerationRate * dt;
                        angularVelocity = Mathf.Clamp(angularVelocity, -maxAngularVelocity, maxAngularVelocity);
                    }
                }
                else
                {
                    float springAccel = -wheel_angle * returnSpringForce;
                    angularVelocity += springAccel * dt;
                }
            }
            else
            {
                angularVelocity *= wheelFriction;
            }

            float previousAngle = wheel_angle;
            angularVelocity *= Mathf.Pow(wheelFriction, dt * 60f);
            wheel_angle += angularVelocity * dt;
            wheel_angle = Mathf.Clamp(wheel_angle, -1f, 1f);

            // Detect zero crossing
            if (Mathf.Sign(previousAngle) != Mathf.Sign(wheel_angle) && !isPlayerInputActive)
            {
                hasCrossedZeroSinceLastInput = true;
            }

            if (isPlayerInputActive)
            {
                steering_input = wheel_angle;
            }
            else
            {
                if (hasCrossedZeroSinceLastInput)
                {
                    // Crossed 0 - Ignore all values on the opposite side
                    steering_input = 0f;
                }
                else
                {
                    // Clamp the steering input to avoid registering oscillations past neutral
                    if (lastInputDirection == 1) // last input was right
                    {
                        steering_input = Mathf.Clamp(wheel_angle, 0f, 1f); // Clamp [0, 1]
                    }
                    else if (lastInputDirection == -1) // last input was left
                    {
                        steering_input = Mathf.Clamp(wheel_angle, -1f, 0f); // Clamp [-1, 0]
                    }
                    else
                    {
                        steering_input = 0f;
                    }
                }
            }

            // Reset the wheel to the neutral position
            if (Mathf.Abs(wheel_angle) < 0.001f && Mathf.Abs(angularVelocity) < 0.01f)
            {
                wheel_angle = Mathf.MoveTowards(wheel_angle, 0.0f, Time.deltaTime * 0.001f);
                angularVelocity = 0f;
                steering_input = 0f;
                hasCrossedZeroSinceLastInput = false;
            }

            transmitWheelAngleRPC(wheel_angle, steering_input);
            keys_down.Clear();
            yield return null;
        }

        wheel_spin_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        keys_down = inputs;
        if (wheel_spin_coroutine == null && inputs.Count > 0)
        {
            wheel_spin_coroutine = StartCoroutine(wheelSpinning());
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        BUTTONS[0].updateInteractable(true);
        BUTTONS[1].updateInteractable(true);
        fill_circle.SetActive(true);
        wheel_light.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_neon;
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
        fill_circle.SetActive(false);
        wheel_light.GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_neon;
    }

    [Rpc(SendTo.Everyone)]
    private void transmitWheelAngleRPC(float wheel_ang, float steering_in)
    {
        wheel_angle = wheel_ang;
        steering_input = steering_in;
        displayAdjustment();
    }
}