/*
    TractorBeam.cs
    - Implements the tractor beam cone visualization and object attraction logic
    - Communicates with TractorBeamOptions once item collected
    - Driven by TractorBeamPower.cs by power
    Contributor(s): Henryk Musial
    Last Updated: 5/15/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TractorBeam : NetworkBehaviour
{
    // REFERENCES
    public Transform beamOriginPoint;
    public Material tractorBeamMaterial;
    public AudioClip tractor_beam_capture_notification;
    private EffectsHandler effectsHandler;
    private TractorBeamPower tractorBeamPower;
    private TractorBeamOptions tractorBeamOptions;

    private float baseAttractionSpeed = 15f;
    private float captureDistance = 7f; // Distance before object is considered captured
    private float captureTransformAdjustmentTime = 1f; // How long it takes for the item to be brought in once capured
    public AnimationCurve attractionCurve = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f); // Attraction acceleration curve
    private float beamRangeOffset = 5f; // Distance that the tractor beam origin point is set back inside the ship

    private List<Transform> activeTargetXforms = new List<Transform>();
    private Coroutine tractorBeamLoopCoroutine = null;
    private Coroutine itemCaptureAdjustmentCoroutine = null;

    private bool itemCurrentlyCaptured = false;
    private GameObject capturedItem = null;
    private string capturedItemSerialNumber = "";

    private float currentRange = 0f;
    private bool currentlyAttracting = false;
    private float coneHalfAngle = 12f; // Halfangle (cone base diameter)

    private void Start()
    {
        tractorBeamPower = GetComponent<TractorBeamPower>();
        tractorBeamOptions = GetComponent<TractorBeamOptions>();
        effectsHandler = ReferenceAssistor.Instance.effects_handler;

        effectsHandler.initializeConeGameObject(beamOriginPoint, tractorBeamMaterial);
        beamOriginPoint.GetComponent<Renderer>().renderingLayerMask = 2;
    }

    public void UpdateBeam(float power)
    {
        currentRange = (power * TractorBeamPower.TRACTOR_BEAM_RANGE) + beamRangeOffset;
        effectsHandler.drawConeMesh(beamOriginPoint.GetComponent<MeshFilter>().mesh, currentRange, coneHalfAngle);
        if (tractorBeamLoopCoroutine == null && NetworkManager.Singleton.IsHost == true)
        {
            tractorBeamLoopCoroutine = StartCoroutine(TractorBeamLoop());
        }
    }

    // Runs every frame while tractor beam power is greater than 0
    private IEnumerator TractorBeamLoop()
    {
        float elapsed_time = 0.0f;
        while (tractorBeamPower.getTractorBeamPower() > 0.0f)
        {
            elapsed_time += Time.fixedDeltaTime;
            tractorBeamMaterial.SetColor("_EmissionColor", new Color(0.0f, 0.09f, Mathf.Lerp(0.3f, 0.75f, Mathf.PingPong(elapsed_time, 0.4f) / 0.4f)));

            FindTargets();
            if (!itemCurrentlyCaptured)
            {
                AttractTargets(Mathf.Min(Time.deltaTime, 1.0f / 30.0f));
            }
            if (currentlyAttracting != activeTargetXforms.Count > 0)
            {
                currentlyAttracting = !currentlyAttracting;
                OnLitIndicatorChangeRPC(currentlyAttracting);
            }

            yield return new WaitForFixedUpdate();
        }

        activeTargetXforms.Clear();
        beamOriginPoint.GetComponent<MeshFilter>().mesh.Clear();
        tractorBeamLoopCoroutine = null;
    }

    private void FindTargets()
    {
        Collider[] potentialTargets = Physics.OverlapSphere(beamOriginPoint.transform.position, currentRange, LayerMask.GetMask("CollisionObjects")); // Query all colliders within sphere surrounding cone 
        HashSet<Transform> foundXforms = new HashSet<Transform>(); // O(1) access

        foreach (Collider collider in potentialTargets)
        {
            if (collider.GetComponent<ITractorBeamable>() != null)
            {
                Vector3 toTarget = collider.transform.position - beamOriginPoint.transform.position;
                float distance = toTarget.magnitude;
                float angle = Vector3.Angle(beamOriginPoint.transform.forward, toTarget);
                if (distance <= (currentRange + 2f) && angle <= (coneHalfAngle + 2f))
                {
                    Transform targetXform = collider.transform;
                    foundXforms.Add(targetXform);

                    if (!activeTargetXforms.Contains(targetXform))
                    {
                        // Add the new target to the list to track
                        activeTargetXforms.Add(targetXform);
                    }
                }
            }
        }

        RemoveInactiveTargets(foundXforms); // Target fell out of cone bounds or destroyed
    }

    // Iterates through active targets and removes non-valid members
    private void RemoveInactiveTargets(HashSet<Transform> validXforms)
    {
        for (int i = activeTargetXforms.Count - 1; i >= 0; i--)
        {
            Transform targetXform = activeTargetXforms[i];

            if (!validXforms.Contains(targetXform))
            {
                activeTargetXforms.RemoveAt(i);
            }
        }
    }

    private void AttractTargets(float dt)
    {
        for (int i = activeTargetXforms.Count - 1; i >= 0; i--)
        {
            Transform targetXform = activeTargetXforms[i];

            if (targetXform == null)
            {
                activeTargetXforms.RemoveAt(i);
            }
            else
            {
                float distance = Vector3.Distance(beamOriginPoint.transform.position, targetXform.position) - beamRangeOffset;

                if (distance <= captureDistance)
                {
                    string serialNumber = targetXform.GetComponent<CollectibleItem>().getSerialNumber();
                    OnTargetCapturedRPC(targetXform.GetComponent<NetworkObject>().NetworkObjectId, serialNumber);
                    return; // Stop attracting, target found
                }
                else
                {
                    float distanceNormalized = Mathf.Clamp01(distance / currentRange);
                    float curveMultiplier = attractionCurve.Evaluate(distanceNormalized);
                    float attractionStrength = baseAttractionSpeed * tractorBeamPower.getTractorBeamPower() * curveMultiplier;

                    Vector3 direction = (beamOriginPoint.transform.position - targetXform.position).normalized; // Normalized direction vector
                    Vector3 movement = direction * attractionStrength * dt; 

                    targetXform.position += movement;
                }
            }
        }
    }

    public GameObject GetCapturedItem()
    {
        return capturedItem;
    }

    public string GetCapturedItemSerialNumber()
    {
        return capturedItemSerialNumber;
    }

    public void ClearCapturedItem()
    {
        itemCurrentlyCaptured = false;
        capturedItem = null;
        capturedItemSerialNumber = "";
        itemCaptureAdjustmentCoroutine = null;
        tractorBeamPower.onItemCapturedChange();
    }

    private IEnumerator TargetCaptureAdjustment()
    {
        float transformAdjustmentTime = captureTransformAdjustmentTime;
        Vector3 startingPosition = capturedItem.transform.position;
        while (capturedItem != null && transformAdjustmentTime > 0.0f)
        {
            transformAdjustmentTime = Mathf.Max(0.0f, transformAdjustmentTime - Time.deltaTime);

            capturedItem.transform.position = Vector3.Lerp(beamOriginPoint.transform.position + new Vector3(0.0f, -7.0f, -15.0f), startingPosition,  transformAdjustmentTime / captureTransformAdjustmentTime);

            yield return null;
        }

        if (capturedItem == null) // Item was destroyed
        {
            itemCurrentlyCaptured = false;
        }

        itemCaptureAdjustmentCoroutine = null;
    }

    [Rpc(SendTo.Everyone)]
    private void OnLitIndicatorChangeRPC(bool active)
    {
        tractorBeamPower.setTractorBeamStatusIndicators(active);
    }

    [Rpc(SendTo.Everyone)]
    private void OnTargetCapturedRPC(ulong itemID, string serialNumber)
    {
        itemCurrentlyCaptured = true;
        capturedItemSerialNumber = serialNumber;
        
        NetworkObject itemNetworkObject = GetNetworkObject(itemID);
        if (itemNetworkObject == null)
        {
            capturedItem = null;
            return;
        }
        capturedItem = itemNetworkObject.gameObject;
        capturedItem.tag = "Untagged";
        
        tractorBeamOptions.activate(capturedItem, capturedItemSerialNumber);
        tractorBeamPower.onItemCapturedChange();

        ReferenceAssistor.Instance.audio_manager.AddLowPriorityNotification(tractor_beam_capture_notification);

        if (NetworkManager.Singleton.IsHost == true)
        {
            if (capturedItem.GetComponent<Probe>() != null)
            {
                ReferenceAssistor.Instance.module_handlers[1].GetComponent<ProbeController>().probeCollected();
            }
            capturedItem.GetComponent<NetworkObject>().TrySetParent(GameObject.FindGameObjectWithTag("Spaceship"), true);
            capturedItem.GetComponent<Collider>().excludeLayers = LayerMask.NameToLayer("Everything");
            capturedItem.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            capturedItem.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            itemCaptureAdjustmentCoroutine = StartCoroutine(TargetCaptureAdjustment());
        }
    }
}