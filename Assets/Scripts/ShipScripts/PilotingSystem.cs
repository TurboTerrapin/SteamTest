/*
    PilotingSystem.cs
    - Handles moving WorldRoot to traverse through space
    - Handles rotating Spaceship
    - Handles boundary checking/handling
    - Tells ScenarioManager when ship reaches endpoint or leaves boundary for too long
    Contributor(s): Henryk Musial
    Last Updated: 8/13/2025
*/

using System.Collections;
using System.Net.Sockets;
using Unity.Netcode;
using UnityEngine;

public class PilotingSystem : NetworkBehaviour
{
    [Header("Control References")]
    private GameObject controlHandler;

    [Header("Speed Settings")]
    private float maxThrusterSpeed = 6f;
    private float maxImpulseForwardSpeed = 50f;
    private float maxImpulseReverseSpeed = 20f;

    [Header("Rotation Settings")]
     private float rotationPower = 3f;
     private float steeringResponsiveness = 2.5f;
     private float maxRotationSpeed = 5f;

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
    private bool inReverse;
    public float currentRotationSpeed;
    public float forwardSpeed;
    public Vector3 currentVelocity;

    public float currentImpulseSpeed = 0f;
    public float currentHorizontalSpeed = 0f;
    public float currentVerticalSpeed = 0f;

    // Boundary values
    private Vector2[] entrancePoints = new Vector2[2];
    private float entranceSlope = 0.0f;
    private float[] entranceIntercepts = new float[2];
    private Vector2[] exitPoints = new Vector2[2];
    private float exitSlope = 0.0f;
    private float[] exitIntercepts = new float[2];
    private bool insideBoundary = true;
    private bool insideAltitudeBoundary = true; //used for altitude boundary display in EngineerMap
    private Coroutine boundaryCountdownCoroutine = null;
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

    public void ShiftDirection(bool newDirection)
    {
        inReverse = newDirection;
    }

    public void UpdateInput()
    {
        currentImpulse = impulseThrottle.getCurrentImpulse();
        steeringInput = courseHeading.getSteeringValue();
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
        exitPoints[0] = CalculatePoint(exitPath, -ScenarioManager.PATH_SIZE * 0.5f);
        exitPoints[1] = CalculatePoint(exitPath, ScenarioManager.PATH_SIZE * 0.5f);
        exitSlope = Mathf.Tan(Mathf.Deg2Rad * exitRotation);
        exitIntercepts[0] = exitPoints[0].y - (exitSlope * exitPoints[0].x);
        exitIntercepts[1] = exitPoints[1].y - (exitSlope * exitPoints[1].x);
    }

    public void UpdateMovement(Transform worldRoot)
    {
        float dt = Time.deltaTime;

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

        currentHorizontalSpeed = maxThrusterSpeed * horizontalThrust;
        currentVerticalSpeed = maxThrusterSpeed * verticalThrust;

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

        if (inReverse == false)
        {
            transform.Rotate(0f, currentRotationSpeed * dt, 0f);
        }
        else
        {
            transform.Rotate(0f, -1.0f * currentRotationSpeed * dt, 0f);
        }

        //update maps/rotation slider
        RotationChangeRPC();
    }

    [Rpc(SendTo.Everyone)]
    private void ProbeDistanceChangeRPC()
    {
        //update probe (if it exists)
        GameObject probe = GameObject.FindGameObjectWithTag("Probe");
        if (probe != null)
        {
            probe.GetComponent<Probe>().updateDistance();
        }
    }

    private void BoundaryCheck(GameObject worldRoot)
    {
        //check for boundary
        Vector2 shipPosition = new Vector2(-worldRoot.transform.position.z - (ScenarioManager.BOUNDARY_SIZE * 0.5f), -worldRoot.transform.position.x);
        Vector2 circleCenter = new Vector2(0.0f, 0.0f);

        if (Mathf.Abs(worldRoot.transform.position.y) > ScenarioManager.BOUNDARY_ALTITUDE)
        {
            if (insideAltitudeBoundary == true)
            {
                insideAltitudeBoundary = false;
                string msg = "INCREASE";
                if (worldRoot.transform.position.y < 0)
                {
                    msg = "DECREASE";
                }
                ShipBoundaryAltitudeWarningChangeRPC(true, msg);
            }
            if (insideBoundary == true)
            {
                ShipBoundaryChangeRPC(false);
            }
            return;
        }

        if (insideAltitudeBoundary == false)
        {
            insideAltitudeBoundary = true;
            ShipBoundaryAltitudeWarningChangeRPC(false, "");
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
                            ShipBoundaryChangeRPC(true); //ship is inside entrance path but too far back
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
                            GameObject.Find("ScenarioManager").GetComponent<ScenarioManager>().endScenario(ScenarioManager.EndCondition.ReachedEndpoint);
                        }
                    }
                    return;
                }
            }
            if (insideBoundary == true)
            {
                ShipBoundaryChangeRPC(false); //ship is outside boundary
            }
        }
        else //is inside the circle
        {
            if (insideBoundary == false)
            {
                ShipBoundaryChangeRPC(true);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void LateralMovementRPC()
    {
        //update map
        GameObject worldRoot = GameObject.FindGameObjectWithTag("WorldRoot");

        engineerMap.updateShipLocation();

        //if host, check boundary
        if (NetworkManager.Singleton.IsHost == true)
        {
            BoundaryCheck(worldRoot);
        }
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
        insideBoundary = withinBoundary;
        engineerMap.updateShipBoundaryStatus(withinBoundary);
    }

    [Rpc(SendTo.Everyone)]
    private void ShipBoundaryCountdownChangeRPC(int countdownValue)
    {
        engineerMap.updateShipBoundaryCountdownStatus(countdownValue);
    }

    [Rpc(SendTo.Everyone)]
    private void ShipBoundaryAltitudeWarningChangeRPC(bool active, string msg)
    {
        engineerMap.updateAltitudeWarning(active, msg);
    }

    [Rpc(SendTo.Everyone)]
    private void RotationChangeRPC()
    {
        pilotNavigation.updateCourseHeadingScreen();
        tacticianMap.rotateMap();
        engineerMap.updateShipOrientation();
    }

    [Rpc(SendTo.Everyone)]
    private void AltitudeChangeRPC()
    {
        GameObject worldRoot = GameObject.FindGameObjectWithTag("WorldRoot");
        pilotNavigation.updateAltimeterScreen();
        engineerMap.updateAltitude();

        //if host, check boundary
        if (NetworkManager.Singleton.IsHost == true)
        {
            BoundaryCheck(worldRoot);   
        }
    }
}