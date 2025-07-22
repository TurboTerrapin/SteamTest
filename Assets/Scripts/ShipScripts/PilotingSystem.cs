using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PilotingSystem : NetworkBehaviour
{
    [Header("Control References")]
    private GameObject controlHandler;

    [Header("Speed Settings")]
    private float maxThrusterSpeed = 6f;
    private float maxImpulseForwardSpeed = 70f;
    private float maxImpulseReverseSpeed = 20f;

    [Header("Rotation Settings")]
     private float rotationPower = 3f;
     private float steeringResponsiveness = 2.5f;
     private float maxRotationSpeed = 5f;

    [Header("Impulse Settings")]
    //private float impulseAccelerationRate = 0.8f;
    //private float impulseDecelerationRate = 1.75f;

    [Header("Thruster Settings")]
    //private float baseThrusterAccelerationRate = 0.5f;
    //private float maxThrusterAccelerationRate = 1.5f;
    //private float timeToMaxThrustAccel = 1.0f;
    //private float thrusterDecelerationRate = 5.0f;

    // Component references
    private ImpulseThrottle impulseThrottle;
    private CourseHeading courseHeading;
    private HorizontalThrusters horizontalThrusters;
    private VerticalThrusters verticalThrusters;
    private PilotNavigation pilotNavigation;
    private TacticianMap tacticianMap;

    // Input values
    private float currentImpulse;
    private float steeringInput;
    private float horizontalThrust;
    private float verticalThrust;

    // Movement state
    private float smoothedSteeringInput = 0f;
    //private float horizontalThrusterActiveTime;
    //private float verticalThrusterActiveTime;
    private bool in_reverse;
    public float currentRotationSpeed;
    public float forwardSpeed;
    public Vector3 currentVelocity;

    public float currentImpulseSpeed = 0f;
    public float currentHorizontalSpeed = 0f;
    public float currentVerticalSpeed = 0f;

    public bool AssignControlReferences(GameObject controlHandler)
    {
        impulseThrottle = controlHandler.GetComponent<ImpulseThrottle>();
        courseHeading = controlHandler.GetComponent<CourseHeading>();
        horizontalThrusters = controlHandler.GetComponent<HorizontalThrusters>();
        verticalThrusters = controlHandler.GetComponent<VerticalThrusters>();

        pilotNavigation = GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<PilotNavigation>();
        tacticianMap = GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<TacticianMap>();

        return impulseThrottle && courseHeading &&
               horizontalThrusters && verticalThrusters;
    }

    public void shiftDirection(bool new_direction)
    {
        in_reverse = new_direction;
    }

    public void UpdateInput()
    {
        currentImpulse = impulseThrottle.getCurrentImpulse();
        steeringInput = courseHeading.getSteeringValue();
        horizontalThrust = horizontalThrusters.getHorizontalThrusterState();
        verticalThrust = verticalThrusters.getVerticalThrusterState();
    }

    public void UpdateMovement(Transform worldRoot)
    {
        float dt = Time.deltaTime;

        Vector3 forward = transform.forward;
        Vector3 horizontal = -transform.right;
        Vector3 vertical = transform.up;

        if (in_reverse == false)
        {
            currentImpulseSpeed = currentImpulse * maxImpulseForwardSpeed;
        }
        else
        {
            currentImpulseSpeed = currentImpulse * -maxImpulseReverseSpeed;
        }

        currentHorizontalSpeed = maxThrusterSpeed * horizontalThrust;
        currentVerticalSpeed = maxThrusterSpeed * verticalThrust;

        /* OLD CODE 
        // Update impulse speed
        currentImpulseSpeed = Mathf.MoveTowards(
            currentImpulseSpeed,
            currentImpulse * maxImpulseSpeed,
            ((Mathf.Abs(currentImpulseSpeed) < Mathf.Abs(currentImpulse * maxImpulseSpeed)) ? impulseAccelerationRate : impulseDecelerationRate) * dt
        );
        

        // Update horizontal thruster speed
        float horizontalRate = GetThrusterAccelerationRate(horizontalThrusterActiveTime);
        currentHorizontalSpeed = Mathf.MoveTowards(
            currentHorizontalSpeed,
            horizontalThrust * maxThrusterSpeed,
            ((Mathf.Abs(currentHorizontalSpeed) < Mathf.Abs(horizontalThrust * maxThrusterSpeed)) ? horizontalRate : thrusterDecelerationRate) * dt
        );

        // Update vertical thruster speed
        float verticalRate = GetThrusterAccelerationRate(verticalThrusterActiveTime);
        currentVerticalSpeed = Mathf.MoveTowards(
            currentVerticalSpeed,
            verticalThrust * maxThrusterSpeed,
            ((Mathf.Abs(currentVerticalSpeed) < Mathf.Abs(verticalThrust * maxThrusterSpeed)) ? verticalRate : thrusterDecelerationRate) * dt
        );
        */

        Vector3 impulseVelocity = forward * currentImpulseSpeed;
        Vector3 horizontalVelocity = horizontal * currentHorizontalSpeed;
        Vector3 verticalVelocity = vertical * currentVerticalSpeed;

        currentVelocity = impulseVelocity + horizontalVelocity + verticalVelocity;

        if (currentVelocity.magnitude > maxImpulseForwardSpeed)
        {
            currentVelocity = currentVelocity.normalized * maxImpulseForwardSpeed;
        }
        
        if (worldRoot != null)
        {
            worldRoot.position -= currentVelocity * dt;
        }

        forwardSpeed = currentVelocity.magnitude;

        HandleRotation(dt);

        if (currentVerticalSpeed != 0.0f)
        {
            //update pilot altimeter
            altitudeChangeRPC();
        }
    }

    /* OLD CODE
    private float GetThrusterAccelerationRate(float activeTime)
    {
        return Mathf.Lerp(baseThrusterAccelerationRate, maxThrusterAccelerationRate,
            Mathf.Clamp01(activeTime / timeToMaxThrustAccel));
    }*/

    private void HandleRotation(float dt)
    {
        forwardSpeed = currentVelocity.magnitude;
        float speedFactor = Mathf.Clamp01(forwardSpeed / maxImpulseForwardSpeed);

        smoothedSteeringInput = Mathf.Lerp(
            smoothedSteeringInput,
            steeringInput,
            steeringResponsiveness * dt
        );

        float targetRotationSpeed = smoothedSteeringInput * maxRotationSpeed * speedFactor;

        if (Mathf.Abs(forwardSpeed) < 0.01f)
            return;

        currentRotationSpeed = Mathf.Lerp(
            currentRotationSpeed,
            targetRotationSpeed,
            rotationPower * dt
        );

        if (Mathf.Abs(steeringInput) < 0.1f && Mathf.Abs(smoothedSteeringInput) < 0.1f)
        {
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, 0f, rotationPower * dt);
        }

        if (in_reverse == false)
        {
            transform.Rotate(0f, currentRotationSpeed * dt, 0f);
        }
        else
        {
            transform.Rotate(0f, -1.0f * currentRotationSpeed * dt, 0f);
        }

        //update pilot course heading slider
        rotationChangeRPC();
    }

    [Rpc(SendTo.Everyone)]
    private void rotationChangeRPC()
    {
        pilotNavigation.updateCourseHeadingScreen();
        tacticianMap.rotateMap();
    }

    [Rpc(SendTo.Everyone)]
    private void altitudeChangeRPC()
    {
        pilotNavigation.updateAltimeterScreen();
    }
}