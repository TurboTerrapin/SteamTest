/*
    ShipMovement.cs
    - Handles moving WorldRoot to traverse through space
    - Handles rotating Spaceship
    - Handles boundary checking/handling
    - Handles getting "stunned" when running into immovable objects
    - Tells ScenarioManager when ship reaches endpoint or leaves boundary for too long
    Contributor(s): Henryk Musial
    Last Updated: 7/20/2026
*/

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ShipMovement : NetworkBehaviour
{
    [Header("Speed Settings")]
    private float maxThrusterSpeed = 15f;
    private float maxImpulseForwardSpeed = 50f;
    private float maxImpulseReverseSpeed = 20f;

    [Header("Rotation Settings")]
    private float rotationPower = 15.0f;
    private float steeringResponsiveness = 10.0f;
    private float maxRotationSpeed = 25.0f;

    [Header("Miscellaneous Settings")]
    private int outOfBoundsCountdown = 15;

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
    private float impulseSpeedAdjustmentFactor = 1.0f;

    // Movement state
    private bool movementAllowed = false;
    private float smoothedSteeringInput = 0f;
    private bool inReverse;
    private float currentRotationSpeed;
    private float forwardSpeed;
    private Vector3 currentVelocity;

    private float currentImpulseSpeed = 0f;
    private float currentHorizontalSpeed = 0f;
    private float currentVerticalSpeed = 0f;

    // Stun values
    private float stunFactor = 0.0f; //ranges from 0 to 1
    private Coroutine stunPushbackCoroutine = null;

    // Boundary values
    private Vector2[] entrancePoints = new Vector2[2];
    private float entranceSlope = 0.0f;
    private float[] entranceIntercepts = new float[2];
    private Vector2 exitTarget;
    private Vector2[] exitPoints = new Vector2[2];
    private float exitSlope = 0.0f;
    private float[] exitIntercepts = new float[2];
    private bool insideBoundary = true;
    private bool insideAltitudeBoundary = true; //used for altitude boundary display in EngineerMap
    private Coroutine boundaryCountdownCoroutine = null;

    private void AssignControlReferences()
    {
        impulseThrottle = ReferenceAssistor.Instance.module_handlers[0].GetComponent<ImpulseThrottle>();
        shipSteering = ReferenceAssistor.Instance.module_handlers[0].GetComponent<ShipSteering>();
        horizontalThrusters = ReferenceAssistor.Instance.module_handlers[0].GetComponent<HorizontalThrusters>();
        verticalThrusters = ReferenceAssistor.Instance.module_handlers[0].GetComponent<VerticalThrusters>();
        probeController = ReferenceAssistor.Instance.module_handlers[1].GetComponent<ProbeController>();

        flyingInstruments = ReferenceAssistor.Instance.module_handlers[0].GetComponent<FlyingInstruments>();
        proximityMap = ReferenceAssistor.Instance.module_handlers[1].GetComponent<ProximityMap>();
        scenarioMap = ReferenceAssistor.Instance.module_handlers[2].GetComponent<ScenarioMap>();
    }

    private void Start()
    {
        AssignControlReferences();
    }

    private void FixedUpdate()
    {
        if (ReferenceAssistor.Instance.world_root != null && movementAllowed == true)
        {
            UpdateInput();
            UpdateMovement(ReferenceAssistor.Instance.world_root.transform, Time.fixedDeltaTime);
        }
    }

    //called by EngineCoolantSupply
    public void AdjustMaxImpulseSpeed(float factor)
    {
        impulseSpeedAdjustmentFactor = factor;
    }

    //called by ScenarioManager
    public void UnlockMovement()
    {
        movementAllowed = true;
    }

    //called by ScenarioManager
    public void LockMovement()
    {
        currentRotationSpeed = 0.0f;
        currentVelocity = Vector3.zero;
        currentImpulseSpeed = 0.0f;
        currentVerticalSpeed = 0.0f;
        currentHorizontalSpeed = 0.0f;
        smoothedSteeringInput = 0.0f;

        movementAllowed = false;
    }

    //called by DirectionalShifter
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

    //called by ScenarioManager.setNewPathsRPC()
    public void PlaceShip(Vector2 position, float rotation)
    {
        GameObject worldRoot = GameObject.FindGameObjectWithTag("WorldRoot");
        //place at entrance path point
        worldRoot.transform.position = new Vector3(-position.y, worldRoot.transform.position.y, -position.x - (ScenarioManager.BOUNDARY_SIZE * 0.5f));
        //rotate to match entrance path channel
        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        //set back a little bit in the entrance path channel
        worldRoot.transform.position += transform.forward * ScenarioManager.START_DIST_OFFSET;
        //randomize height
        worldRoot.transform.position = new Vector3(worldRoot.transform.position.x, Random.Range(-10.0f, 10.0f), worldRoot.transform.position.z);
        LateralMovementRPC();
        AltitudeChangeRPC();
        RotationChangeRPC();
        //start inside the boundary
        insideBoundary = true;
        insideAltitudeBoundary = true;
    }

    //called by PlayerManager and this
    public string GetTargetHeading()
    {
        GameObject worldRoot = GameObject.FindGameObjectWithTag("WorldRoot");
        Vector2 shipPos = new Vector2(-worldRoot.transform.position.z - (ScenarioManager.BOUNDARY_SIZE * 0.5f), -worldRoot.transform.position.x);
        if (shipPos.x > exitTarget.x)
        {
            return "---.-°";
        }
        float slope = (shipPos.y - exitTarget.y) / (shipPos.x - exitTarget.x);
        float angle = Mathf.Rad2Deg * Mathf.Atan(slope);
        angle += 90.0f;
        return FlyingInstruments.getRoundedDegreeReading(angle);
    }

    //returns true if within boundary (including entrance/exit channels)
    private bool ShipIsWithinBoundary()
    {
        GameObject worldRoot = GameObject.FindGameObjectWithTag("WorldRoot");
        Vector2 shipPosition = new Vector2(-worldRoot.transform.position.z - (ScenarioManager.BOUNDARY_SIZE * 0.5f), -worldRoot.transform.position.x);

        Vector2[] checkPoints = new Vector2[2]; //0 is lower, 1 is upper
        Vector2[] pathPoints = entrancePoints; //where the boundary interesects the 
        float[] pathIntercepts = entranceIntercepts;
        float currentSlope = entranceSlope;

        if (shipPosition.x > 0.0f) //if true, means we are checking the exit path
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

        //assign lower (0) and upper (1) points
        for (int i = 0; i < 2; i++)
        {
            checkPoints[i].x = shipPosition.x;
            checkPoints[i].y = -9999.9f; //lower minimum
            if (i == 1)
            {
                checkPoints[i].y = 9999.9f; //upper maximum
            }
            if (shipPosition.x < 0.0f)
            {
                if (shipPosition.x <= pathPoints[i].x) //ship position is to the right of the entrance point
                {
                    checkPoints[i].y = (currentSlope * shipPosition.x) + pathIntercepts[i];
                }
            }
            else
            {
                if (shipPosition.x >= pathPoints[i].x) //ship position is to the left of the exit point
                {
                    checkPoints[i].y = (currentSlope * shipPosition.x) + pathIntercepts[i];
                }
            }
        }

        //check if ship is between the two points
        return (shipPosition.y > checkPoints[0].y && shipPosition.y < checkPoints[1].y);
    }

    //takes a point and an angle and returns a new point on the circle shifted angleDifference degrees
    private Vector2 CalculatePoint(Vector2 pathPoint, float angleDifference)
    {
        float pathPointAngle = (Mathf.Rad2Deg * Mathf.Atan(pathPoint.y / pathPoint.x));
        Vector2 returnPoint = ScenarioManager.getBoundaryPointFromAngle(pathPointAngle + angleDifference);
        return returnPoint;
    }

    //called by ScenarioManager.setNewPathsRPC() at the start of every scenario
    public void SetPaths(Vector2 entrancePath, float entranceRotation, Vector2 exitPath, float exitRotation)
    {
        //plot entrance points
        entrancePoints[0] = CalculatePoint(entrancePath, ScenarioManager.PATH_SIZE * 0.5f);
        entrancePoints[0] *= -1.0f;
        entrancePoints[1] = CalculatePoint(entrancePath, -ScenarioManager.PATH_SIZE * 0.5f);
        entrancePoints[1] *= -1.0f;
        entranceSlope = Mathf.Tan(Mathf.Deg2Rad * entranceRotation);
        entranceIntercepts[0] = entrancePoints[0].y - (entranceSlope * entrancePoints[0].x);
        entranceIntercepts[1] = entrancePoints[1].y - (entranceSlope * entrancePoints[1].x);

        //plot exit points
        exitTarget = exitPath;
        exitPoints[0] = CalculatePoint(exitPath, -ScenarioManager.PATH_SIZE * 0.5f);
        exitPoints[1] = CalculatePoint(exitPath, ScenarioManager.PATH_SIZE * 0.5f);
        exitSlope = Mathf.Tan(Mathf.Deg2Rad * exitRotation);
        exitIntercepts[0] = exitPoints[0].y - (exitSlope * exitPoints[0].x);
        exitIntercepts[1] = exitPoints[1].y - (exitSlope * exitPoints[1].x);
    }

    public void UpdateMovement(Transform worldRoot, float dt)
    {
        Vector3 forward = transform.forward;
        Vector3 horizontal = -transform.right;
        Vector3 vertical = transform.up;

        if (inReverse == false)
        {
            currentImpulseSpeed = currentImpulse * maxImpulseForwardSpeed;
        }
        else
        {
            currentImpulseSpeed = currentImpulse * -maxImpulseReverseSpeed;
        }

        currentImpulseSpeed *= impulseSpeedAdjustmentFactor;

        currentHorizontalSpeed = maxThrusterSpeed * horizontalThrust;
        currentVerticalSpeed = maxThrusterSpeed * verticalThrust;

        if (stunPushbackCoroutine != null)
        {
            currentImpulseSpeed = stunFactor * (-maxImpulseForwardSpeed * 2.0f);
        }

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

        HandleRotation(dt);

        //vertical movement
        if (currentVerticalSpeed != 0.0f)
        {
            //update pilot altimeter
            AltitudeChangeRPC();
        }

        //lateral movement
        if (currentImpulseSpeed != 0.0f || currentHorizontalSpeed != 0.0f)
        {
            LateralMovementRPC();
        }

        //any movement at all
        if (currentImpulseSpeed != 0.0f || currentVerticalSpeed != 0.0f || currentHorizontalSpeed != 0.0f)
        {
            //update probe (if it exists)
            GameObject probe = GameObject.FindGameObjectWithTag("Probe");
            if (probe != null)
            {
                ProbeDistanceChangeRPC();
            }
        }
    }

    private void HandleRotation(float dt)
    {
        forwardSpeed = currentVelocity.magnitude;
        float speedFactor = Mathf.Clamp01(forwardSpeed / maxImpulseForwardSpeed);

        // Steering input smoothing
        float steeringT = 1f - Mathf.Exp(-steeringResponsiveness * dt);
        smoothedSteeringInput = Mathf.Lerp(smoothedSteeringInput, steeringInput, steeringT);

        float targetRotationSpeed = smoothedSteeringInput * maxRotationSpeed * speedFactor;

        if (Mathf.Abs(forwardSpeed) < 0.01f)
        {
            return;
        }

        // Rotation speed smoothing 
        float rotationT = 1f - Mathf.Exp(-rotationPower * dt);
        currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, targetRotationSpeed, rotationT);

        if (Mathf.Abs(steeringInput) < 0.1f && Mathf.Abs(smoothedSteeringInput) < 0.1f)
        {
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, 0f, rotationT);
        }

        if (inReverse == false)
        {
            transform.Rotate(0f, currentRotationSpeed * dt, 0f);
        }
        else
        {
            transform.Rotate(0f, -1.0f * currentRotationSpeed * dt, 0f);
        }

        // Update maps/rotation slider
        RotationChangeRPC();
    }


    [Rpc(SendTo.Everyone)]
    private void ProbeDistanceChangeRPC()
    {
        //update probe (if it exists)
        probeController.onProbeDistanceChange();
    }

    private void BoundaryCheck(GameObject worldRoot)
    {
        //check for boundary
        Vector2 shipPosition = new Vector2(-worldRoot.transform.position.z - (ScenarioManager.BOUNDARY_SIZE * 0.5f), -worldRoot.transform.position.x);
        Vector2 circleCenter = new Vector2(0.0f, 0.0f);

        if (Mathf.Abs(worldRoot.transform.position.y) > ScenarioManager.BOUNDARY_ALTITUDE)
        {
            ShipBoundaryChangeRPC(false, false); //ship is outside of altitude boundary
            return;
        }

        float distanceFromCenter = Vector2.Distance(shipPosition, circleCenter);
        if (distanceFromCenter > (ScenarioManager.BOUNDARY_SIZE * 0.5f)) //check if outside circle
        {
            if (ShipIsWithinBoundary() == true)
            {
                if (shipPosition.x < 0.0f) //check if too far back in entrance path
                {
                    if (distanceFromCenter < ((ScenarioManager.BOUNDARY_SIZE * 0.5f) + ScenarioManager.START_DIST_OFFSET + 50.0f))
                    {
                        if (insideBoundary == false)
                        {
                            ShipBoundaryChangeRPC(true, true); //ship is inside entrance path and not far back enough to say out of bounds
                        }
                        return;
                    }
                }
                else //check if reached exit in exit path
                {
                    if (distanceFromCenter > ((ScenarioManager.BOUNDARY_SIZE * 0.5f) + ScenarioManager.DIST_TO_ENDPOINT))
                    {
                        if (transform.GetComponent<ShipHealth>().getHullIntegrity() > 0.0f)
                        {
                            ReferenceAssistor.Instance.scenario_manager.endScenario(ScenarioManager.EndCondition.ReachedEndpoint);
                        }
                    }
                    return;
                }
            }
            if (insideBoundary == true)
            {
                ShipBoundaryChangeRPC(false, true); //ship is outside boundary but inside altitude boundary
            }
        }
        else //is inside the circle and altitude boundary
        {
            if (insideBoundary == false)
            {
                ShipBoundaryChangeRPC(true, true);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void LateralMovementRPC()
    {
        //update map
        GameObject worldRoot = GameObject.FindGameObjectWithTag("WorldRoot");

        scenarioMap.updateShipLocation();

        //if host, check boundary
        if (NetworkManager.Singleton.IsHost == true)
        {
            BoundaryCheck(worldRoot);
        }
    }

    IEnumerator BoundaryCountdown()
    {
        int countdown = outOfBoundsCountdown;
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
            ReferenceAssistor.Instance.scenario_manager.endScenario(ScenarioManager.EndCondition.LeftBoundary);
        }

        boundaryCountdownCoroutine = null;
    }

    IEnumerator StunPushback()
    {
        stunFactor = 1.0f;
        yield return new WaitForSeconds(3.0f);
        float animTime = 3.0f;
        while (animTime > 0.0f)
        {
            animTime = Mathf.Max(0.0f, animTime - Time.deltaTime);
            stunFactor = animTime / 3.0f;

            yield return null;
        }
        stunPushbackCoroutine = null;
    }

    //pushes the ship back and disables power
    public void StunShip()
    {
        if (stunPushbackCoroutine == null)
        {
            ReferenceAssistor.Instance.power_manager.totalShutdown(true);
            stunPushbackCoroutine = StartCoroutine(StunPushback());
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ShipBoundaryChangeRPC(bool withinBoundary, bool withinAltitudeBoundary)
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (withinBoundary == false && withinBoundary != insideBoundary)
            {
                if (boundaryCountdownCoroutine != null)
                {
                    StopCoroutine(boundaryCountdownCoroutine);
                }
                boundaryCountdownCoroutine = StartCoroutine(BoundaryCountdown());
            }
            else if (withinBoundary == true && boundaryCountdownCoroutine != null)
            {
                StopCoroutine(boundaryCountdownCoroutine);
                boundaryCountdownCoroutine = null;
            }
        }
        if (insideBoundary != withinBoundary || insideAltitudeBoundary != withinAltitudeBoundary)
        {
            insideBoundary = withinBoundary;
            insideAltitudeBoundary = withinAltitudeBoundary;
            scenarioMap.updateShipBoundaryStatus(insideBoundary, insideAltitudeBoundary);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ShipBoundaryCountdownChangeRPC(int countdownValue)
    {
        scenarioMap.updateShipBoundaryCountdownStatus(countdownValue);
    }

    [Rpc(SendTo.Everyone)]
    private void RotationChangeRPC()
    {
        proximityMap.rotateMap();
        float ship_rotation = transform.rotation.eulerAngles.y;
        string current_heading = FlyingInstruments.getRoundedDegreeReading(ship_rotation + 90.0f);
        flyingInstruments.updateCourseHeadingScreen(ship_rotation, current_heading);
        scenarioMap.updateShipOrientation(ship_rotation, current_heading, GetTargetHeading());
    }

    [Rpc(SendTo.Everyone)]
    private void AltitudeChangeRPC()
    {
        GameObject worldRoot = GameObject.FindGameObjectWithTag("WorldRoot");
        flyingInstruments.updateAltimeterScreen();
        scenarioMap.updateAltitude();

        //if host, check boundary
        if (NetworkManager.Singleton.IsHost == true)
        {
            BoundaryCheck(worldRoot);
        }
    }
}