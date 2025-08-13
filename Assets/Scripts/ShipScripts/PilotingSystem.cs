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
    private EngineerMap engineerMap;

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

    //boundary values
    private Vector2[] entrance_points = new Vector2[2];
    private float entrance_slope = 0.0f;
    private float[] entrance_intercepts = new float[2];
    private Vector2[] exit_points = new Vector2[2];
    private float exit_slope = 0.0f;
    private float[] exit_intercepts = new float[2];

    public bool AssignControlReferences(GameObject controlHandler)
    {
        impulseThrottle = controlHandler.GetComponent<ImpulseThrottle>();
        courseHeading = controlHandler.GetComponent<CourseHeading>();
        horizontalThrusters = controlHandler.GetComponent<HorizontalThrusters>();
        verticalThrusters = controlHandler.GetComponent<VerticalThrusters>();

        pilotNavigation = GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<PilotNavigation>();
        tacticianMap = GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<TacticianMap>();
        engineerMap = GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerMap>();

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

    public void PlaceShip(Vector2 position, float rotation)
    {
        GameObject worldRoot = GameObject.FindGameObjectWithTag("WorldRoot");
        worldRoot.transform.position = new Vector3(-position.y, worldRoot.transform.position.y, -position.x - (ScenarioManager.BOUNDARY_SIZE * 0.5f));
        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        lateralMovementRPC();
        rotationChangeRPC();
    }

    private bool ShipIsWithinBoundary()
    {
        GameObject worldRoot = GameObject.FindGameObjectWithTag("WorldRoot");
        Vector2 ship_position = new Vector2(-worldRoot.transform.position.z - (ScenarioManager.BOUNDARY_SIZE * 0.5f), -worldRoot.transform.position.x);

        Vector2[] check_points = new Vector2[2]; //0 is upper, 1 is lower
        Vector2[] path_points = entrance_points;
        float[] path_intercepts = entrance_intercepts;
        float current_slope = entrance_slope;

        if (ship_position.x > 0.0f)
        {
            path_points = exit_points;
            path_intercepts = exit_intercepts;
            current_slope = exit_slope;
        }

        for (int i = 0; i < 2; i++)
        {
            check_points[i].x = ship_position.x;
            check_points[i].y = 9999.9f;
            if (ship_position.x < 0.0f)
            {
                if (ship_position.x <= path_points[i].x) //ship position is to the right of the entrance point
                {
                    check_points[i].y = (current_slope * ship_position.x) + path_intercepts[i];
                }
            }
            else
            {
                if (ship_position.x >= path_points[i].x) //ship position is to the left of the exit point
                {
                    check_points[i].y = (current_slope * ship_position.x) + path_intercepts[i];
                }
            }
        }

        return (ship_position.y > check_points[0].y && ship_position.y < check_points[1].y);
    }

    private Vector2 CalculatePoint(Vector2 path_point, float angle_difference)
    {
        float path_point_angle = (Mathf.Rad2Deg * Mathf.Atan(path_point.y / path_point.x));
        Vector2 return_point = ScenarioManager.getBoundaryPointFromAngle(path_point_angle + angle_difference);
        return return_point;
    }

    public void SetPaths(Vector2 entrance_path, float entrance_rotation, Vector2 exit_path, float exit_rotation)
    {
        //plot entrance points
        entrance_points[0] = CalculatePoint(entrance_path, ScenarioManager.PATH_SIZE * 0.5f);
        entrance_points[0] *= -1.0f;
        entrance_points[1] = CalculatePoint(entrance_path, -ScenarioManager.PATH_SIZE * 0.5f);
        entrance_points[1] *= -1.0f;
        entrance_slope = Mathf.Tan(Mathf.Deg2Rad * entrance_rotation);
        entrance_intercepts[0] = entrance_points[0].y - (entrance_slope * entrance_points[0].x);
        entrance_intercepts[1] = entrance_points[1].y - (entrance_slope * entrance_points[1].x);

        //plot exit points
        exit_points[0] = CalculatePoint(exit_path, -ScenarioManager.PATH_SIZE * 0.5f);
        exit_points[1] = CalculatePoint(exit_path, ScenarioManager.PATH_SIZE * 0.5f);
        exit_slope = Mathf.Tan(Mathf.Deg2Rad * exit_rotation);
        exit_intercepts[0] = exit_points[0].y - (exit_slope * exit_points[0].x);
        exit_intercepts[1] = exit_points[1].y - (exit_slope * exit_points[1].x);
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

        //lateral movement
        if (currentImpulseSpeed != 0.0f || currentHorizontalSpeed != 0.0f)
        {
            lateralMovementRPC();
        }

        //any movement at all
        if (currentImpulseSpeed != 0.0f || currentVerticalSpeed != 0.0f || currentHorizontalSpeed != 0.0f)
        {
            //update probe (if it exists)
            GameObject probe = GameObject.FindGameObjectWithTag("Probe");
            if (probe != null)
            {
                probeDistanceChangeRPC();
            }
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

        //update maps/rotation slider
        rotationChangeRPC();
    }

    [Rpc(SendTo.Everyone)]
    private void probeDistanceChangeRPC()
    {
        //update probe (if it exists)
        GameObject probe = GameObject.FindGameObjectWithTag("Probe");
        if (probe != null)
        {
            probe.GetComponent<Probe>().updateDistance();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void lateralMovementRPC()
    {
        GameObject worldRoot = GameObject.FindGameObjectWithTag("WorldRoot");

        //update map
        Vector2 ship_position = new Vector2(-worldRoot.transform.position.z - (ScenarioManager.BOUNDARY_SIZE * 0.5f), -worldRoot.transform.position.x);
        engineerMap.updateShipLocation(ship_position);

        if (NetworkManager.Singleton.IsHost == true)
        {
            //check for boundary
            Vector2 shipPosition = new Vector2(worldRoot.transform.position.x, worldRoot.transform.position.z);
            Vector2 circleCenter = new Vector2(0.0f, ScenarioManager.BOUNDARY_SIZE * -0.5f);
            if (Vector2.Distance(shipPosition, circleCenter) > (ScenarioManager.BOUNDARY_SIZE * 0.5f))
            {
                if (ShipIsWithinBoundary() == false)
                {
                    Debug.Log("OUTSIDE BOUNDARY!");
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void rotationChangeRPC()
    {
        pilotNavigation.updateCourseHeadingScreen();
        tacticianMap.rotateMap();
        engineerMap.updateShipOrientation(transform.rotation.eulerAngles.y);
    }

    [Rpc(SendTo.Everyone)]
    private void altitudeChangeRPC()
    {
        GameObject worldRoot = GameObject.FindGameObjectWithTag("WorldRoot");
        pilotNavigation.updateAltimeterScreen();
        engineerMap.updateAltitude(-worldRoot.transform.position.y);
    }
}