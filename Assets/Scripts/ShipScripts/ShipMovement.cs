/*
    ShipMovement.cs
    - Handles moving the world (via WorldRoot) to traverse through space
    - Handles rotating Spaceship
    - Handles boundary checking/handling
    - Tells ScenarioManager when ship reaches endpoint or leaves boundary for too long

    Contributor(s): Henryk Musial
    Last Updated: 2/1/2026
*/

/*
    ShipMovement.cs
    - Handles moving the world (via WorldRoot) to traverse through space
    - Handles rotating Spaceship
    - Handles boundary checking/handling
    - Tells ScenarioManager when ship reaches endpoint or leaves boundary for too long

    Contributor(s): Henryk Musial
    Last Updated: 2/1/2026
*/

using System.Collections;
using System.Net.Sockets;
using Unity.Netcode;
using UnityEngine;

public class ShipMovement : NetworkBehaviour
{
    [Header("Speed Settings")]
    private float maxThrusterSpeed = 25f;
    private float thrusterResponsiveness = 20.0f;
    private float maxImpulseForwardSpeed = 50f;
    private float maxImpulseReverseSpeed = 20f;

    [Header("Rotation Settings")]
    private float rotationPower = 15.0f;
    private float steeringResponsiveness = 10.0f;
    private float maxRotationSpeed = 25.0f;

    // Component references
    private ImpulseThrottle impulseThrottle;
    private ShipSteering shipSteering;
    private HorizontalThrusters horizontalThrusters;
    private VerticalThrusters verticalThrusters;
    private FlyingInstruments flyingInstruments;
    private ProximityMap proximityMap;
    private ScenarioMap scenarioMap;
    private ProbeController probeController;

    // Input values
    private float currentImpulse;
    private float steeringInput;
    private float horizontalThrust;
    private float verticalThrust;

    // Movement state
    private float smoothedSteeringInput = 0f;
    private float smoothedHorizontalThrust = 0f;
    private float smoothedVerticalThrust = 0f;
    private bool inReverse;

    public float currentRotationSpeed;
    public float forwardSpeed;
    public Vector3 currentVelocity;

    public float impulseSpeedAdjustmentFactor = 1.0f;
    public float currentImpulseSpeed = 0f;
    public float currentHorizontalSpeed = 0f;
    public float currentVerticalSpeed = 0f;

    // ----- VIRTUAL HEADING -----
    public float virtualHeading = 0f;

    // Timer to prevent RPC flooding (fixes physics interpolation stutter)
    private float mapRpcTimer = 0f;
    private const float MAP_RPC_INTERVAL = 0.1f;

    // Boundary values
    private Vector2[] entrancePoints = new Vector2[2];
    private float entranceSlope = 0.0f;
    private float[] entranceIntercepts = new float[2];
    private Vector2 exitTarget;
    private Vector2[] exitPoints = new Vector2[2];
    private float exitSlope = 0.0f;
    private float[] exitIntercepts = new float[2];
    private bool insideBoundary = true;
    private bool insideAltitudeBoundary = true;
    private Coroutine boundaryCountdownCoroutine = null;

    private Vector3 GetWorldRootOffset()
    {
        return WorldRoot.Instance != null ? WorldRoot.Instance.CumulativeOffset : Vector3.zero;
    }

    private void ApplyWorldRootDelta(Vector3 delta)
    {
        if (WorldRoot.Instance != null) WorldRoot.Instance.ApplyOffsetDelta(delta);
    }

    public bool AssignControlReferences()
    {
        impulseThrottle = ReferenceAssistor.Instance.module_handlers[0].GetComponent<ImpulseThrottle>();
        shipSteering = ReferenceAssistor.Instance.module_handlers[0].GetComponent<ShipSteering>();
        horizontalThrusters = ReferenceAssistor.Instance.module_handlers[0].GetComponent<HorizontalThrusters>();
        verticalThrusters = ReferenceAssistor.Instance.module_handlers[0].GetComponent<VerticalThrusters>();
        probeController = ReferenceAssistor.Instance.module_handlers[1].GetComponent<ProbeController>();

        flyingInstruments = ReferenceAssistor.Instance.module_handlers[0].GetComponent<FlyingInstruments>();
        proximityMap = ReferenceAssistor.Instance.module_handlers[1].GetComponent<ProximityMap>();
        scenarioMap = ReferenceAssistor.Instance.module_handlers[2].GetComponent<ScenarioMap>();

        return impulseThrottle && shipSteering &&
               horizontalThrusters && verticalThrusters;
    }

    public void AdjustMaxImpulseSpeed(float factor)
    {
        impulseSpeedAdjustmentFactor = factor;
    }

    public void ShiftDirection(bool newDirection)
    {
        inReverse = newDirection;
    }

    public void UpdateInput()
    {
        currentImpulse = impulseThrottle.getCurrentImpulse();
        steeringInput = shipSteering.getSteeringValue();
        horizontalThrust = horizontalThrusters.getHorizontalThrusterState();
        verticalThrust = verticalThrusters.getVerticalThrusterState();
    }

    public void PlaceShip(Vector2 position, float rotation)
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        virtualHeading = rotation;

        Vector3 newOffset = new Vector3(
            -position.y,
            GetWorldRootOffset().y,
            -position.x - (ScenarioManager.BOUNDARY_SIZE * 0.5f));

        Quaternion virtualRot = Quaternion.Euler(0.0f, virtualHeading, 0.0f);
        newOffset += (virtualRot * Vector3.forward) * ScenarioManager.START_DIST_OFFSET;
        newOffset.y = Random.Range(-10.0f, 10.0f);

        // SAFELY TELEPORT THE WORLD TO PREVENT PHYSICS CRASH
        if (WorldRoot.Instance != null)
        {
            WorldRoot.Instance.TeleportWorld(newOffset, virtualHeading);
        }

        LateralMovementRPC();
        AltitudeChangeRPC();
        RotationChangeRPC();

        insideBoundary = true;
        insideAltitudeBoundary = true;
    }

    public string GetTargetHeading()
    {
        Vector3 offset = GetWorldRootOffset();
        Vector2 shipPos = new Vector2(-offset.z - (ScenarioManager.BOUNDARY_SIZE * 0.5f), -offset.x);
        if (shipPos.x > exitTarget.x) return "---.-°";

        float slope = (shipPos.y - exitTarget.y) / (shipPos.x - exitTarget.x);
        float angle = Mathf.Rad2Deg * Mathf.Atan(slope);
        angle += 90.0f;
        return FlyingInstruments.getRoundedDegreeReading(angle);
    }

    private bool ShipIsWithinBoundary()
    {
        Vector3 offset = GetWorldRootOffset();
        Vector2 shipPosition = new Vector2(-offset.z - (ScenarioManager.BOUNDARY_SIZE * 0.5f), -offset.x);

        Vector2[] checkPoints = new Vector2[2];
        Vector2[] pathPoints = entrancePoints;
        float[] pathIntercepts = entranceIntercepts;
        float currentSlope = entranceSlope;

        if (shipPosition.x > 0.0f)
        {
            pathPoints = exitPoints;
            pathIntercepts = exitIntercepts;
            currentSlope = exitSlope;
            if (shipPosition.x < pathPoints[0].x && shipPosition.x < pathPoints[1].x) return false;
        }
        else
        {
            if (shipPosition.x > pathPoints[0].x && shipPosition.x > pathPoints[1].x) return false;
        }

        for (int i = 0; i < 2; i++)
        {
            checkPoints[i].x = shipPosition.x;
            checkPoints[i].y = -9999.9f;
            if (i == 1) checkPoints[i].y = 9999.9f;

            if (shipPosition.x < 0.0f)
            {
                if (shipPosition.x <= pathPoints[i].x)
                    checkPoints[i].y = (currentSlope * shipPosition.x) + pathIntercepts[i];
            }
            else
            {
                if (shipPosition.x >= pathPoints[i].x)
                    checkPoints[i].y = (currentSlope * shipPosition.x) + pathIntercepts[i];
            }
        }
        return (shipPosition.y > checkPoints[0].y && shipPosition.y < checkPoints[1].y);
    }

    private Vector2 CalculatePoint(Vector2 pathPoint, float angleDifference)
    {
        float pathPointAngle = (Mathf.Rad2Deg * Mathf.Atan(pathPoint.y / pathPoint.x));
        return ScenarioManager.getBoundaryPointFromAngle(pathPointAngle + angleDifference);
    }

    public void SetPaths(Vector2 entrancePath, float entranceRotation, Vector2 exitPath, float exitRotation)
    {
        entrancePoints[0] = CalculatePoint(entrancePath, ScenarioManager.PATH_SIZE * 0.5f) * -1.0f;
        entrancePoints[1] = CalculatePoint(entrancePath, -ScenarioManager.PATH_SIZE * 0.5f) * -1.0f;
        entranceSlope = Mathf.Tan(Mathf.Deg2Rad * entranceRotation);
        entranceIntercepts[0] = entrancePoints[0].y - (entranceSlope * entrancePoints[0].x);
        entranceIntercepts[1] = entrancePoints[1].y - (entranceSlope * entrancePoints[1].x);

        exitTarget = exitPath;
        exitPoints[0] = CalculatePoint(exitPath, -ScenarioManager.PATH_SIZE * 0.5f);
        exitPoints[1] = CalculatePoint(exitPath, ScenarioManager.PATH_SIZE * 0.5f);
        exitSlope = Mathf.Tan(Mathf.Deg2Rad * exitRotation);
        exitIntercepts[0] = exitPoints[0].y - (exitSlope * exitPoints[0].x);
        exitIntercepts[1] = exitPoints[1].y - (exitSlope * exitPoints[1].x);
    }

    public void UpdateMovement(Transform worldRoot)
    {
        float fdt = Time.fixedDeltaTime;

        // 1. Update the virtual rotation
        HandleRotation(fdt);

        // 2. Physics Movement is now purely LOCAL. 
        float thrustT = 1f - Mathf.Exp(-thrusterResponsiveness * fdt);
        smoothedHorizontalThrust = Mathf.Lerp(smoothedHorizontalThrust, horizontalThrust, thrustT);
        smoothedVerticalThrust = Mathf.Lerp(smoothedVerticalThrust, verticalThrust, thrustT);

        Vector3 forward = Vector3.forward;
        Vector3 horizontal = Vector3.left;
        Vector3 vertical = Vector3.up;

        currentImpulseSpeed = (inReverse ? currentImpulse * -maxImpulseReverseSpeed : currentImpulse * maxImpulseForwardSpeed) * impulseSpeedAdjustmentFactor;

        currentHorizontalSpeed = maxThrusterSpeed * smoothedHorizontalThrust;
        currentVerticalSpeed = maxThrusterSpeed * smoothedVerticalThrust;

        Vector3 impulseVelocity = forward * currentImpulseSpeed;
        Vector3 horizontalVelocity = horizontal * currentHorizontalSpeed;
        Vector3 verticalVelocity = vertical * currentVerticalSpeed;

        Vector3 localVelocity = impulseVelocity + horizontalVelocity + verticalVelocity;
        if (localVelocity.magnitude > maxImpulseForwardSpeed)
        {
            localVelocity = localVelocity.normalized * maxImpulseForwardSpeed;
        }

        forwardSpeed = localVelocity.magnitude;

        // 3. Update the Absolute Map Offset
        // WorldRoot now derives actual physics velocity purely from deltas of this offset!
        Quaternion virtualRotation = Quaternion.Euler(0f, virtualHeading, 0f);
        Vector3 mapVelocity = virtualRotation * localVelocity;
        ApplyWorldRootDelta(-mapVelocity * fdt);

        // 4. Rate-limit ALL the RPCs (including Rotation)
        mapRpcTimer += fdt;
        if (mapRpcTimer >= MAP_RPC_INTERVAL)
        {
            mapRpcTimer = 0f;

            RotationChangeRPC(); // Rate-limited to prevent network choke
            if (currentVerticalSpeed != 0.0f) AltitudeChangeRPC();
            if (currentImpulseSpeed != 0.0f || currentHorizontalSpeed != 0.0f) LateralMovementRPC();

            if (currentImpulseSpeed != 0.0f || currentVerticalSpeed != 0.0f || currentHorizontalSpeed != 0.0f)
            {
                GameObject probe = GameObject.FindGameObjectWithTag("Probe");
                if (probe != null) ProbeDistanceChangeRPC();
            }
        }
    }

    private void HandleRotation(float fdt)
    {
        float maxSpeed = inReverse ? maxImpulseReverseSpeed : maxImpulseForwardSpeed;
        float inputForwardSpeed = Mathf.Abs(currentImpulse * maxSpeed * impulseSpeedAdjustmentFactor);
        float speedFactor = Mathf.Clamp01(inputForwardSpeed / maxImpulseForwardSpeed);

        float steeringT = 1f - Mathf.Exp(-steeringResponsiveness * fdt);
        smoothedSteeringInput = Mathf.Lerp(smoothedSteeringInput, steeringInput, steeringT);

        float targetRotationSpeed = smoothedSteeringInput * maxRotationSpeed * speedFactor;

        if (inputForwardSpeed < 0.01f) targetRotationSpeed = 0f;

        float rotationT = 1f - Mathf.Exp(-rotationPower * fdt);
        currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, targetRotationSpeed, rotationT);

        if (Mathf.Abs(steeringInput) < 0.1f && Mathf.Abs(smoothedSteeringInput) < 0.1f)
        {
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, 0f, rotationT);
        }

        float direction = inReverse ? -1.0f : 1.0f;
        float rotationThisFrame = currentRotationSpeed * direction * fdt;

        // 1. Update our Virtual Heading
        virtualHeading += rotationThisFrame;
        if (virtualHeading >= 360f) virtualHeading -= 360f;
        if (virtualHeading < 0f) virtualHeading += 360f;

        // 2. Pass the virtual heading to WorldRoot so it can compute deltas
        if (WorldRoot.Instance != null)
        {
            WorldRoot.Instance.SetVirtualHeading(virtualHeading);
        }

        // 3. Rotate the Skybox
        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Rotation"))
        {
            RenderSettings.skybox.SetFloat("_Rotation", virtualHeading);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ProbeDistanceChangeRPC()
    {
        probeController.onProbeDistanceChange();
    }

    private void BoundaryCheck()
    {
        Vector3 offset = GetWorldRootOffset();
        Vector2 shipPosition = new Vector2(-offset.z - (ScenarioManager.BOUNDARY_SIZE * 0.5f), -offset.x);
        Vector2 circleCenter = new Vector2(0.0f, 0.0f);

        if (Mathf.Abs(offset.y) > ScenarioManager.BOUNDARY_ALTITUDE)
        {
            if (insideAltitudeBoundary == true)
            {
                insideAltitudeBoundary = false;
                ShipBoundaryAltitudeWarningChangeRPC(true, offset.y < 0 ? "DECREASE" : "INCREASE");
            }
            if (insideBoundary == true) ShipBoundaryChangeRPC(false);
            return;
        }

        if (insideAltitudeBoundary == false)
        {
            insideAltitudeBoundary = true;
            ShipBoundaryAltitudeWarningChangeRPC(false, "");
        }

        float distanceFromCenter = Vector2.Distance(shipPosition, circleCenter);
        if (distanceFromCenter > (ScenarioManager.BOUNDARY_SIZE * 0.5f))
        {
            if (ShipIsWithinBoundary() == true)
            {
                if (shipPosition.x < 0.0f)
                {
                    if (distanceFromCenter < ((ScenarioManager.BOUNDARY_SIZE * 0.5f) + ScenarioManager.START_DIST_OFFSET + 50.0f))
                    {
                        if (insideBoundary == false) ShipBoundaryChangeRPC(true);
                        return;
                    }
                }
                else
                {
                    if (distanceFromCenter > ((ScenarioManager.BOUNDARY_SIZE * 0.5f) + ScenarioManager.DIST_TO_ENDPOINT))
                    {
                        if (transform.GetComponent<ShipHealth>().getHullIntegrity() > 0.0f)
                        {
                            GameObject.Find("ScenarioManager").GetComponent<ScenarioManager>().endScenario(ScenarioManager.EndCondition.ReachedEndpoint);
                        }
                    }
                    return;
                }
            }
            if (insideBoundary == true) ShipBoundaryChangeRPC(false);
        }
        else
        {
            if (insideBoundary == false) ShipBoundaryChangeRPC(true);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void LateralMovementRPC()
    {
        scenarioMap.updateShipLocation();
        if (NetworkManager.Singleton.IsHost == true) BoundaryCheck();
    }

    IEnumerator BoundaryCountdown()
    {
        int countdown = 10;
        ShipBoundaryCountdownChangeRPC(countdown);
        while (countdown > 0)
        {
            yield return new WaitForSeconds(1.0f);
            countdown--;
            ShipBoundaryCountdownChangeRPC(countdown);
        }
        yield return new WaitForSeconds(2.0f);
        if (countdown <= 0)
        {
            GameObject.Find("ScenarioManager").GetComponent<ScenarioManager>().endScenario(ScenarioManager.EndCondition.LeftBoundary);
        }
        boundaryCountdownCoroutine = null;
    }

    [Rpc(SendTo.Everyone)]
    private void ShipBoundaryChangeRPC(bool withinBoundary)
    {
        if (withinBoundary == false && withinBoundary != insideBoundary)
        {
            if (boundaryCountdownCoroutine != null) StopCoroutine(boundaryCountdownCoroutine);
            boundaryCountdownCoroutine = StartCoroutine(BoundaryCountdown());
        }
        else if (withinBoundary == true && boundaryCountdownCoroutine != null)
        {
            StopCoroutine(boundaryCountdownCoroutine);
            boundaryCountdownCoroutine = null;
        }
        insideBoundary = withinBoundary;
        scenarioMap.updateShipBoundaryStatus(withinBoundary);
    }

    [Rpc(SendTo.Everyone)]
    private void ShipBoundaryCountdownChangeRPC(int countdownValue)
    {
        scenarioMap.updateShipBoundaryCountdownStatus(countdownValue);
    }

    [Rpc(SendTo.Everyone)]
    private void ShipBoundaryAltitudeWarningChangeRPC(bool active, string msg)
    {
        scenarioMap.updateAltitudeWarning(active, msg);
    }

    [Rpc(SendTo.Everyone)]
    private void RotationChangeRPC()
    {
        proximityMap.rotateMap();

        string current_heading = FlyingInstruments.getRoundedDegreeReading(virtualHeading + 90.0f);
        flyingInstruments.updateCourseHeadingScreen(virtualHeading, current_heading);
        scenarioMap.updateShipOrientation(virtualHeading, current_heading, GetTargetHeading());
    }

    [Rpc(SendTo.Everyone)]
    private void AltitudeChangeRPC()
    {
        flyingInstruments.updateAltimeterScreen();
        scenarioMap.updateAltitude();
        if (NetworkManager.Singleton.IsHost == true) BoundaryCheck();
    }
}

/*
using System.Collections;
using System.Net.Sockets;
using Unity.Netcode;
using UnityEngine;

public class ShipMovement : NetworkBehaviour
{
    [Header("Speed Settings")]
    private float maxThrusterSpeed = 25f; // Increased from 6f for much more power
    private float thrusterResponsiveness = 20.0f; // Makes thrusters punchy and snappy
    private float maxImpulseForwardSpeed = 50f;
    private float maxImpulseReverseSpeed = 20f;

    [Header("Rotation Settings")]
    private float rotationPower = 15.0f;
    private float steeringResponsiveness = 10.0f; // Steering stays heavy and smooth
    private float maxRotationSpeed = 25.0f;

    // Component references
    private ImpulseThrottle impulseThrottle;
    private ShipSteering shipSteering;
    private HorizontalThrusters horizontalThrusters;
    private VerticalThrusters verticalThrusters;
    private FlyingInstruments flyingInstruments;
    private ProximityMap proximityMap;
    private ScenarioMap scenarioMap;
    private ProbeController probeController;

    // Input values
    private float currentImpulse;
    private float steeringInput;
    private float horizontalThrust;
    private float verticalThrust;

    // Movement state
    private float smoothedSteeringInput = 0f;
    private float smoothedHorizontalThrust = 0f;
    private float smoothedVerticalThrust = 0f;
    private bool inReverse;

    public float currentRotationSpeed;
    public float forwardSpeed;
    public Vector3 currentVelocity;

    public float impulseSpeedAdjustmentFactor = 1.0f;
    public float currentImpulseSpeed = 0f;
    public float currentHorizontalSpeed = 0f;
    public float currentVerticalSpeed = 0f;

    // ----- VIRTUAL HEADING -----
    // The physical ship is now locked at (0,0,0) and never rotates.
    // We use virtualHeading to track which way the ship is "pointing" for 
    // the UI maps, boundary checks, and offset calculations.
    public float virtualHeading = 0f;

    // Timer to prevent RPC flooding (fixes physics interpolation stutter)
    private float mapRpcTimer = 0f;
    private const float MAP_RPC_INTERVAL = 0.1f; // Sync maps 10 times a second instead of 50

    // Boundary values
    private Vector2[] entrancePoints = new Vector2[2];
    private float entranceSlope = 0.0f;
    private float[] entranceIntercepts = new float[2];
    private Vector2 exitTarget;
    private Vector2[] exitPoints = new Vector2[2];
    private float exitSlope = 0.0f;
    private float[] exitIntercepts = new float[2];
    private bool insideBoundary = true;
    private bool insideAltitudeBoundary = true;
    private Coroutine boundaryCountdownCoroutine = null;

    private Vector3 GetWorldRootOffset()
    {
        return WorldRoot.Instance != null ? WorldRoot.Instance.CumulativeOffset : Vector3.zero;
    }

    private void ApplyWorldRootDelta(Vector3 delta)
    {
        if (WorldRoot.Instance != null) WorldRoot.Instance.ApplyOffsetDelta(delta);
    }

    private void SetWorldRootOffset(Vector3 offset)
    {
        if (WorldRoot.Instance != null) WorldRoot.Instance.SetOffset(offset);
    }

    public bool AssignControlReferences()
    {
        impulseThrottle = ReferenceAssistor.Instance.module_handlers[0].GetComponent<ImpulseThrottle>();
        shipSteering = ReferenceAssistor.Instance.module_handlers[0].GetComponent<ShipSteering>();
        horizontalThrusters = ReferenceAssistor.Instance.module_handlers[0].GetComponent<HorizontalThrusters>();
        verticalThrusters = ReferenceAssistor.Instance.module_handlers[0].GetComponent<VerticalThrusters>();
        probeController = ReferenceAssistor.Instance.module_handlers[1].GetComponent<ProbeController>();

        flyingInstruments = ReferenceAssistor.Instance.module_handlers[0].GetComponent<FlyingInstruments>();
        proximityMap = ReferenceAssistor.Instance.module_handlers[1].GetComponent<ProximityMap>();
        scenarioMap = ReferenceAssistor.Instance.module_handlers[2].GetComponent<ScenarioMap>();

        return impulseThrottle && shipSteering &&
               horizontalThrusters && verticalThrusters;
    }

    public void AdjustMaxImpulseSpeed(float factor)
    {
        impulseSpeedAdjustmentFactor = factor;
    }

    public void ShiftDirection(bool newDirection)
    {
        inReverse = newDirection;
    }

    public void UpdateInput()
    {
        currentImpulse = impulseThrottle.getCurrentImpulse();
        steeringInput = shipSteering.getSteeringValue();
        horizontalThrust = horizontalThrusters.getHorizontalThrusterState();
        verticalThrust = verticalThrusters.getVerticalThrusterState();
    }

    public void PlaceShip(Vector2 position, float rotation)
    {
        // 1. Lock the physical ship and assign the virtual heading
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        virtualHeading = rotation;

        Vector3 newOffset = new Vector3(
            -position.y,
            GetWorldRootOffset().y,
            -position.x - (ScenarioManager.BOUNDARY_SIZE * 0.5f));

        // Use virtual heading to calculate the backward start distance
        Quaternion virtualRot = Quaternion.Euler(0.0f, virtualHeading, 0.0f);
        newOffset += (virtualRot * Vector3.forward) * ScenarioManager.START_DIST_OFFSET;
        newOffset.y = Random.Range(-10.0f, 10.0f);

        SetWorldRootOffset(newOffset);

        LateralMovementRPC();
        AltitudeChangeRPC();
        RotationChangeRPC();

        insideBoundary = true;
        insideAltitudeBoundary = true;
    }

    public string GetTargetHeading()
    {
        Vector3 offset = GetWorldRootOffset();
        Vector2 shipPos = new Vector2(-offset.z - (ScenarioManager.BOUNDARY_SIZE * 0.5f), -offset.x);
        if (shipPos.x > exitTarget.x)
        {
            return "---.-°";
        }
        float slope = (shipPos.y - exitTarget.y) / (shipPos.x - exitTarget.x);
        float angle = Mathf.Rad2Deg * Mathf.Atan(slope);
        angle += 90.0f;
        return FlyingInstruments.getRoundedDegreeReading(angle);
    }

    private bool ShipIsWithinBoundary()
    {
        Vector3 offset = GetWorldRootOffset();
        Vector2 shipPosition = new Vector2(-offset.z - (ScenarioManager.BOUNDARY_SIZE * 0.5f), -offset.x);

        Vector2[] checkPoints = new Vector2[2];
        Vector2[] pathPoints = entrancePoints;
        float[] pathIntercepts = entranceIntercepts;
        float currentSlope = entranceSlope;

        if (shipPosition.x > 0.0f)
        {
            pathPoints = exitPoints;
            pathIntercepts = exitIntercepts;
            currentSlope = exitSlope;
            if (shipPosition.x < pathPoints[0].x && shipPosition.x < pathPoints[1].x)
            {
                return false;
            }
        }
        else
        {
            if (shipPosition.x > pathPoints[0].x && shipPosition.x > pathPoints[1].x)
            {
                return false;
            }
        }

        for (int i = 0; i < 2; i++)
        {
            checkPoints[i].x = shipPosition.x;
            checkPoints[i].y = -9999.9f;
            if (i == 1) checkPoints[i].y = 9999.9f;

            if (shipPosition.x < 0.0f)
            {
                if (shipPosition.x <= pathPoints[i].x)
                {
                    checkPoints[i].y = (currentSlope * shipPosition.x) + pathIntercepts[i];
                }
            }
            else
            {
                if (shipPosition.x >= pathPoints[i].x)
                {
                    checkPoints[i].y = (currentSlope * shipPosition.x) + pathIntercepts[i];
                }
            }
        }
        return (shipPosition.y > checkPoints[0].y && shipPosition.y < checkPoints[1].y);
    }

    private Vector2 CalculatePoint(Vector2 pathPoint, float angleDifference)
    {
        float pathPointAngle = (Mathf.Rad2Deg * Mathf.Atan(pathPoint.y / pathPoint.x));
        Vector2 returnPoint = ScenarioManager.getBoundaryPointFromAngle(pathPointAngle + angleDifference);
        return returnPoint;
    }

    public void SetPaths(Vector2 entrancePath, float entranceRotation, Vector2 exitPath, float exitRotation)
    {
        entrancePoints[0] = CalculatePoint(entrancePath, ScenarioManager.PATH_SIZE * 0.5f) * -1.0f;
        entrancePoints[1] = CalculatePoint(entrancePath, -ScenarioManager.PATH_SIZE * 0.5f) * -1.0f;
        entranceSlope = Mathf.Tan(Mathf.Deg2Rad * entranceRotation);
        entranceIntercepts[0] = entrancePoints[0].y - (entranceSlope * entrancePoints[0].x);
        entranceIntercepts[1] = entrancePoints[1].y - (entranceSlope * entrancePoints[1].x);

        exitTarget = exitPath;
        exitPoints[0] = CalculatePoint(exitPath, -ScenarioManager.PATH_SIZE * 0.5f);
        exitPoints[1] = CalculatePoint(exitPath, ScenarioManager.PATH_SIZE * 0.5f);
        exitSlope = Mathf.Tan(Mathf.Deg2Rad * exitRotation);
        exitIntercepts[0] = exitPoints[0].y - (exitSlope * exitPoints[0].x);
        exitIntercepts[1] = exitPoints[1].y - (exitSlope * exitPoints[1].x);
    }

    public void UpdateMovement(Transform worldRoot)
    {
        float fdt = Time.fixedDeltaTime;

        // 1. Update the virtual rotation
        HandleRotation(fdt);

        // 2. Physics Movement is now purely LOCAL. 
        // Smooth the lateral/vertical inputs using the snappy thrusterResponsiveness
        float thrustT = 1f - Mathf.Exp(-thrusterResponsiveness * fdt);
        smoothedHorizontalThrust = Mathf.Lerp(smoothedHorizontalThrust, horizontalThrust, thrustT);
        smoothedVerticalThrust = Mathf.Lerp(smoothedVerticalThrust, verticalThrust, thrustT);

        Vector3 forward = Vector3.forward;
        Vector3 horizontal = Vector3.left; // left is -right
        Vector3 vertical = Vector3.up;

        if (inReverse == false)
            currentImpulseSpeed = currentImpulse * maxImpulseForwardSpeed;
        else
            currentImpulseSpeed = currentImpulse * -maxImpulseReverseSpeed;

        currentImpulseSpeed *= impulseSpeedAdjustmentFactor;

        // Use the smoothed inputs for the speed calculations
        currentHorizontalSpeed = maxThrusterSpeed * smoothedHorizontalThrust;
        currentVerticalSpeed = maxThrusterSpeed * smoothedVerticalThrust;

        Vector3 impulseVelocity = forward * currentImpulseSpeed;
        Vector3 horizontalVelocity = horizontal * currentHorizontalSpeed;
        Vector3 verticalVelocity = vertical * currentVerticalSpeed;

        // This velocity represents how the ship is moving within its own LOCAL space
        Vector3 localVelocity = impulseVelocity + horizontalVelocity + verticalVelocity;

        if (localVelocity.magnitude > maxImpulseForwardSpeed)
        {
            localVelocity = localVelocity.normalized * maxImpulseForwardSpeed;
        }

        forwardSpeed = localVelocity.magnitude;

        // 3. Update the Abstract Map Offset
        // The Scenario Map needs to know where the ship is relative to the absolute grid.
        // We calculate this by multiplying the local velocity by the virtual rotation.
        Quaternion virtualRotation = Quaternion.Euler(0f, virtualHeading, 0f);
        Vector3 mapVelocity = virtualRotation * localVelocity;
        ApplyWorldRootDelta(-mapVelocity * fdt);

        // 4. Update the Physics World
        // Pass the purely local velocity to WorldRoot. Asteroids will fly straight 
        // backward, while the HandleRotation method handles the orbital turning.
        if (WorldRoot.Instance != null)
        {
            WorldRoot.Instance.SetWorldVelocity(-localVelocity);
        }

        // 5. Rate-limit the RPCs so you don't choke the physics engine
        mapRpcTimer += fdt;
        if (mapRpcTimer >= MAP_RPC_INTERVAL)
        {
            mapRpcTimer = 0f;

            if (currentVerticalSpeed != 0.0f) AltitudeChangeRPC();
            if (currentImpulseSpeed != 0.0f || currentHorizontalSpeed != 0.0f) LateralMovementRPC();

            if (currentImpulseSpeed != 0.0f || currentVerticalSpeed != 0.0f || currentHorizontalSpeed != 0.0f)
            {
                GameObject probe = GameObject.FindGameObjectWithTag("Probe");
                if (probe != null) ProbeDistanceChangeRPC();
            }
        }
    }

    private void HandleRotation(float fdt)
    {
        float maxSpeed = inReverse ? maxImpulseReverseSpeed : maxImpulseForwardSpeed;
        float inputForwardSpeed = Mathf.Abs(currentImpulse * maxSpeed * impulseSpeedAdjustmentFactor);
        float speedFactor = Mathf.Clamp01(inputForwardSpeed / maxImpulseForwardSpeed);

        float steeringT = 1f - Mathf.Exp(-steeringResponsiveness * fdt);
        smoothedSteeringInput = Mathf.Lerp(smoothedSteeringInput, steeringInput, steeringT);

        float targetRotationSpeed = smoothedSteeringInput * maxRotationSpeed * speedFactor;

        if (inputForwardSpeed < 0.01f)
        {
            // Tell WorldRoot to stop orbiting
            if (WorldRoot.Instance != null) WorldRoot.Instance.SetWorldRotationSpeed(0f);
            return;
        }

        float rotationT = 1f - Mathf.Exp(-rotationPower * fdt);
        currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, targetRotationSpeed, rotationT);

        if (Mathf.Abs(steeringInput) < 0.1f && Mathf.Abs(smoothedSteeringInput) < 0.1f)
        {
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, 0f, rotationT);
        }

        float direction = inReverse ? -1.0f : 1.0f;
        float rotationThisFrame = currentRotationSpeed * direction * fdt;

        // 1. Update our Virtual Heading for the Maps
        virtualHeading += rotationThisFrame;
        if (virtualHeading >= 360f) virtualHeading -= 360f;
        if (virtualHeading < 0f) virtualHeading += 360f;

        // 2. Pass the rotation speed to WorldRoot so it can orbit the asteroids
        if (WorldRoot.Instance != null)
        {
            WorldRoot.Instance.SetWorldRotationSpeed(currentRotationSpeed * direction);
        }

        // 3. Rotate the Skybox so the background stars turn (SYNCED WITH PHYSICS)
        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Rotation"))
        {
            // Updated to positive virtualHeading as requested!
            RenderSettings.skybox.SetFloat("_Rotation", virtualHeading);
        }

        RotationChangeRPC();
    }

    [Rpc(SendTo.Everyone)]
    private void ProbeDistanceChangeRPC()
    {
        probeController.onProbeDistanceChange();
    }

    private void BoundaryCheck()
    {
        Vector3 offset = GetWorldRootOffset();
        Vector2 shipPosition = new Vector2(-offset.z - (ScenarioManager.BOUNDARY_SIZE * 0.5f), -offset.x);
        Vector2 circleCenter = new Vector2(0.0f, 0.0f);

        if (Mathf.Abs(offset.y) > ScenarioManager.BOUNDARY_ALTITUDE)
        {
            if (insideAltitudeBoundary == true)
            {
                insideAltitudeBoundary = false;
                string msg = "INCREASE";
                if (offset.y < 0) msg = "DECREASE";
                ShipBoundaryAltitudeWarningChangeRPC(true, msg);
            }
            if (insideBoundary == true) ShipBoundaryChangeRPC(false);
            return;
        }

        if (insideAltitudeBoundary == false)
        {
            insideAltitudeBoundary = true;
            ShipBoundaryAltitudeWarningChangeRPC(false, "");
        }

        float distanceFromCenter = Vector2.Distance(shipPosition, circleCenter);
        if (distanceFromCenter > (ScenarioManager.BOUNDARY_SIZE * 0.5f))
        {
            if (ShipIsWithinBoundary() == true)
            {
                if (shipPosition.x < 0.0f)
                {
                    if (distanceFromCenter < ((ScenarioManager.BOUNDARY_SIZE * 0.5f) + ScenarioManager.START_DIST_OFFSET + 50.0f))
                    {
                        if (insideBoundary == false) ShipBoundaryChangeRPC(true);
                        return;
                    }
                }
                else
                {
                    if (distanceFromCenter > ((ScenarioManager.BOUNDARY_SIZE * 0.5f) + ScenarioManager.DIST_TO_ENDPOINT))
                    {
                        if (transform.GetComponent<ShipHealth>().getHullIntegrity() > 0.0f)
                        {
                            GameObject.Find("ScenarioManager").GetComponent<ScenarioManager>().endScenario(ScenarioManager.EndCondition.ReachedEndpoint);
                        }
                    }
                    return;
                }
            }
            if (insideBoundary == true) ShipBoundaryChangeRPC(false);
        }
        else
        {
            if (insideBoundary == false) ShipBoundaryChangeRPC(true);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void LateralMovementRPC()
    {
        scenarioMap.updateShipLocation();
        if (NetworkManager.Singleton.IsHost == true) BoundaryCheck();
    }

    IEnumerator BoundaryCountdown()
    {
        int countdown = 10;
        ShipBoundaryCountdownChangeRPC(countdown);
        while (countdown > 0)
        {
            yield return new WaitForSeconds(1.0f);
            countdown--;
            ShipBoundaryCountdownChangeRPC(countdown);
        }
        yield return new WaitForSeconds(2.0f);
        if (countdown <= 0)
        {
            GameObject.Find("ScenarioManager").GetComponent<ScenarioManager>().endScenario(ScenarioManager.EndCondition.LeftBoundary);
        }
        boundaryCountdownCoroutine = null;
    }

    [Rpc(SendTo.Everyone)]
    private void ShipBoundaryChangeRPC(bool withinBoundary)
    {
        if (withinBoundary == false && withinBoundary != insideBoundary)
        {
            if (boundaryCountdownCoroutine != null) StopCoroutine(boundaryCountdownCoroutine);
            boundaryCountdownCoroutine = StartCoroutine(BoundaryCountdown());
        }
        else if (withinBoundary == true && boundaryCountdownCoroutine != null)
        {
            StopCoroutine(boundaryCountdownCoroutine);
            boundaryCountdownCoroutine = null;
        }
        insideBoundary = withinBoundary;
        scenarioMap.updateShipBoundaryStatus(withinBoundary);
    }

    [Rpc(SendTo.Everyone)]
    private void ShipBoundaryCountdownChangeRPC(int countdownValue)
    {
        scenarioMap.updateShipBoundaryCountdownStatus(countdownValue);
    }

    [Rpc(SendTo.Everyone)]
    private void ShipBoundaryAltitudeWarningChangeRPC(bool active, string msg)
    {
        scenarioMap.updateAltitudeWarning(active, msg);
    }

    [Rpc(SendTo.Everyone)]
    private void RotationChangeRPC()
    {
        proximityMap.rotateMap();

        // --- USE VIRTUAL HEADING INSTEAD OF TRANSFORM ---
        string current_heading = FlyingInstruments.getRoundedDegreeReading(virtualHeading + 90.0f);
        flyingInstruments.updateCourseHeadingScreen(virtualHeading, current_heading);
        scenarioMap.updateShipOrientation(virtualHeading, current_heading, GetTargetHeading());
    }

    [Rpc(SendTo.Everyone)]
    private void AltitudeChangeRPC()
    {
        flyingInstruments.updateAltimeterScreen();
        scenarioMap.updateAltitude();
        if (NetworkManager.Singleton.IsHost == true) BoundaryCheck();
    }
}
*/