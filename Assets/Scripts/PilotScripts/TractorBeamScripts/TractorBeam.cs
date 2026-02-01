/*
    TractorBeam.cs
    - Implements the tractor beam cone visualization and object attraction logic
    - Communicates with TractorBeamOptions once item collected
    - Driven by TractorBeamPower.cs by power
    Contributor(s): Henryk Musial
    Last Updated: 1/31/2026
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
    private TractorBeamPower tractorBeamPower;
    private TractorBeamOptions tractorBeamOptions;
    private GameObject beamObject;

    private float baseAttractionSpeed = 15f;
    private float captureDistance = 8f; // Distance before object is considered captured
    private float captureTransformAdjustmentTime = 1f; // How long it takes for the item to be brought in once capured
    public AnimationCurve attractionCurve = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f); // Attraction acceleration curve
    private float beamRangeOffset = 11f; // Distance that the tractor beam origin point is set back inside the ship

    private Vector3 beamOrigin;
    private Vector3 beamDirection;

    private List<Transform> activeTargetXforms = new List<Transform>();
    private Coroutine tractorBeamLoopCoroutine = null;
    private Coroutine itemCaptureAdjustmentCoroutine = null;

    // CONE MESH DATA
    private Mesh coneMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private bool itemCurrentlyCaptured = false;
    private GameObject capturedItem = null;
    private string capturedItemSerialNumber = "";

    private float currentRange = 0f;
    private bool currentlyAttracting = false;
    private float coneHalfAngle = 12f; // Halfangle (cone base diameter)
    private int coneSegments = 32; // Cone res (# cone base vertices)

    // Precomputed angles for mesh generation
    private float[] sinAngles;
    private float[] cosAngles;
    private float halfAngleRad;

    // Preallocated arrays for vertex & triangle data
    private Vector3[] vertices;
    private Vector2[] uvs;
    private int[] triangles;

    private void Start()
    {
        tractorBeamPower = GetComponent<TractorBeamPower>();
        tractorBeamOptions = GetComponent<TractorBeamOptions>();

        InitializeBeamObject();
        InitializeBeamMaterial();
        InitializeConeMesh();
    }

    private void InitializeBeamObject()
    {
        // Empty GameObject for the beam mesh and set as a child of the TractorBeamOrigin
        beamObject = new GameObject("TractorBeam");
        Transform parentXform = beamOriginPoint; // Parent to beam origin point

        beamObject.transform.SetParent(parentXform);
        beamObject.transform.localPosition = Vector3.zero;
        beamObject.transform.localRotation = Quaternion.identity;
        beamObject.transform.localScale = Vector3.one;

        // Add mesh components to the empty beam object
        meshFilter = beamObject.AddComponent<MeshFilter>();
        meshRenderer = beamObject.AddComponent<MeshRenderer>();

        coneMesh = new Mesh();
        coneMesh.name = "TractorBeamCone";
        meshFilter.mesh = coneMesh;
    }

    private void InitializeBeamMaterial()
    {
        // Set the beam material 
        meshRenderer.material = tractorBeamMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Disable shadows
        meshRenderer.receiveShadows = false;
    }

    private void InitializeConeMesh()
    {
        halfAngleRad = coneHalfAngle * Mathf.Deg2Rad;
        // Apex + base circle vertices + base center
        int totalVertices = 1 + coneSegments + 1;

        // Vertex data
        vertices = new Vector3[totalVertices]; // 3D positions
        uvs = new Vector2[totalVertices]; // tex uvs
        triangles = new int[coneSegments * 2 * 3];

        // Sin/cos angles
        sinAngles = new float[coneSegments];
        cosAngles = new float[coneSegments];

        uvs[0] = new Vector2(0.5f, 0f); // Apex at origin
        uvs[totalVertices - 1] = new Vector2(0.5f, 1f); // base center vertex

        // Base circle UVs
        for (int i = 0; i < coneSegments; i++)
        {
            float angle = (float)i / coneSegments * Mathf.PI * 2f;

            // Cache trig values
            sinAngles[i] = Mathf.Sin(angle);
            cosAngles[i] = Mathf.Cos(angle);
            uvs[1 + i] = new Vector2((cosAngles[i] + 1f) * 0.5f, 1f);
        }

        // Cone body tris
        for (int i = 0; i < coneSegments; i++) 
        {
            int baseIndex = i * 3; // 3 vertices per triangle, 1 triangle per cone seg
            triangles[baseIndex] = 0; // v1
            triangles[baseIndex + 1] = 1 + i; // v2
            triangles[baseIndex + 2] = 1 + (i + 1) % coneSegments; // v3
        }

        // Base circle tris
        int baseTriStart = coneSegments * 3; // 3 vertices per triangle, 1 triangle per cone seg
        for (int i = 0; i < coneSegments; i++)
        {
            int baseIndex = baseTriStart + i * 3;
            triangles[baseIndex] = totalVertices - 1; //v1
            triangles[baseIndex + 1] = 1 + (i + 1) % coneSegments; //v2
            triangles[baseIndex + 2] = 1 + i; //v3
        }

        // Flip normals
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int temp = triangles[i + 1];
            triangles[i + 1] = triangles[i + 2];
            triangles[i + 2] = temp;
        }
    }

    public void UpdateBeam(float power)
    {
        currentRange = (power * TractorBeamPower.TRACTOR_BEAM_RANGE) + beamRangeOffset;
        DrawConeMesh();
        if (tractorBeamLoopCoroutine == null && NetworkManager.Singleton.IsHost == true)
        {
            tractorBeamLoopCoroutine = StartCoroutine(TractorBeamLoop());
        }
    }

    // Runs every frame while tractor beam power is greater than 0
    private IEnumerator TractorBeamLoop()
    {
        while (tractorBeamPower.getTractorBeamPower() > 0.0f)
        {
            beamOrigin = beamOriginPoint.position; // Update cached beam position
            beamDirection = beamOriginPoint.forward;
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
        coneMesh.Clear();
        tractorBeamLoopCoroutine = null;
    }

    private void DrawConeMesh()
    {
        coneMesh.Clear();

        float radius = currentRange * Mathf.Tan(halfAngleRad);
        int totalVertices = 1 + coneSegments + 1;

        vertices[0] = Vector3.zero;

        // Build base
        for (int i = 0; i < coneSegments; i++)
        {
            vertices[1 + i] = new Vector3(cosAngles[i] * radius, sinAngles[i] * radius, currentRange);
        }

        vertices[totalVertices - 1] = new Vector3(0, 0, currentRange);

        coneMesh.vertices = vertices;
        coneMesh.uv = uvs;
        coneMesh.triangles = triangles;
        coneMesh.RecalculateNormals();
        coneMesh.RecalculateBounds();
    }

    private void FindTargets()
    {
        Collider[] potentialTargets = Physics.OverlapSphere(beamOrigin, currentRange, LayerMask.GetMask("CollisionObjects")); // Query all colliders within sphere surrounding cone 
        HashSet<Transform> foundXforms = new HashSet<Transform>(); // O(1) access

        foreach (Collider collider in potentialTargets)
        {
            if (collider.GetComponent<ITractorBeamable>() != null)
            {
                Vector3 toTarget = collider.transform.position - beamOrigin;
                float distance = toTarget.magnitude;
                float angle = Vector3.Angle(beamDirection, toTarget);
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
                float distance = Vector3.Distance(beamOrigin, targetXform.position) - beamRangeOffset;

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

                    Vector3 direction = (beamOrigin - targetXform.position).normalized; // Normalized direction vector
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

            capturedItem.transform.position = Vector3.Lerp(beamOrigin, startingPosition,  transformAdjustmentTime / captureTransformAdjustmentTime);

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