/*
    TractorBeam.cs
    - Implements the tractor beam cone visualization and object attraction logic
    - Driven by TractorBeamPower.cs by power
    
    Contributor(s): Henryk Musial
    Last Updated: 01/12/2026
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TractorBeam : NetworkBehaviour
{
    // REFERENCES
    public Transform beamOriginPoint;

    // BEAM CONFIGURATION
    // ****** Make these private once you pick the right colors****
    public Color beamColorLow = new Color(0f, 0.84f, 1f, 0.3f);
    public Color beamColorHigh = new Color(0f, 0.95f, 1f, 0.5f);

    private Transform cachedXform;
    private Vector3 beamOrigin;
    private Vector3 beamDirection;

    public LayerMask attractableLayers = ~0; // Layer mask for objects that can be effected by tractor beam (~0 means all of them jake)
    public float baseAttractionSpeed = 10f;
    public float captureDistance = 1f; // Distance before object is considered captured
    public AnimationCurve attractionCurve = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f); // attraction acceleration curve

    private GameObject beamObject;
    private Transform beamXform;
    private Material beamMaterial;

    private float previousPower = -1f;
    private float currentPower = 0f;
    private float currentRange = 0f;
    public float maxRange = 100f;

    private List<Transform> activeTargetXforms = new List<Transform>();

    // CONE MESH DATA
    private Mesh coneMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private float coneHalfAngle = 7.5f; // Halfangle (cone base diameter)
    private int coneSegments = 32; // Cone res (# cone base vertices)

    // precomputed angles for mesh generation
    private float[] sinAngles;
    private float[] cosAngles;
    private float halfAngleRad;

    // preallocated arrays for vertex & triangle data
    private Vector3[] vertices;
    private Vector2[] uvs;
    private Color[] colors;
    private int[] triangles;

    private void Awake()
    {
        cachedXform = transform;
        InitializeBeamObject();
        InitializeBeamMaterial();
        InitializeConeMesh();
    }

    private void InitializeBeamObject()
    {
        // empty Gameobject for the beam mesh and set as a child of the tractorbeamorigin
        beamObject = new GameObject("TractorBeam");
        Transform parentXform = beamOriginPoint; // Parent to beam origin point

        beamObject.transform.SetParent(parentXform);
        beamObject.transform.localPosition = Vector3.zero;
        beamObject.transform.localRotation = Quaternion.identity;
        beamObject.transform.localScale = Vector3.one;

        beamXform = beamObject.transform;

        // Add mesh components to the empty beam object
        meshFilter = beamObject.AddComponent<MeshFilter>();
        meshRenderer = beamObject.AddComponent<MeshRenderer>();

        coneMesh = new Mesh();
        coneMesh.name = "TractorBeamCone";
        meshFilter.mesh = coneMesh;
    }

    private void InitializeBeamMaterial()
    {
        // Setup tractor beam material
        beamMaterial = new Material( Shader.Find("Particles/Standard Unlit")); // New standard unlit
        beamMaterial.SetFloat("_Mode", 3); // Transparent render mode
        beamMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha); // Use pixels alpha channel
        beamMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha) ; // darken pixel based on alpha, then add beam pixel
        beamMaterial.SetInt("_ZWrite", 0); // disable depth buffer
        beamMaterial.DisableKeyword("_ALPHATEST_ON"); // Turn off alpha clipping (for partial trans)
        beamMaterial.EnableKeyword("_ALPHABLEND_ON");
        beamMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        beamMaterial.renderQueue = 3000; // Start of transparent draw in render order
        beamMaterial.SetColor("_Color", beamColorLow);

        // Set the beam material 
        meshRenderer.material = beamMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // disable shadows
        meshRenderer.receiveShadows = false;
    }

    private void InitializeConeMesh()
    {
        halfAngleRad = coneHalfAngle * Mathf.Deg2Rad;
        // apex + base circle vertices + base center
        int totalVertices = 1 + coneSegments + 1;

        // Vertex data
        vertices = new Vector3[totalVertices]; // 3D positions
        uvs = new Vector2[totalVertices]; // tex uvs
        colors = new Color[totalVertices]; // vert color data
        triangles = new int[coneSegments * 2 * 3];

        // sin/cos angles
        sinAngles = new float[coneSegments];
        cosAngles = new float[coneSegments];

        uvs[0] = new Vector2(0.5f, 0f); // Apex at origin
        uvs[totalVertices - 1] = new Vector2(0.5f, 1f); // base center vertex

        // Base circle UVs
        for (int i = 0; i < coneSegments; i++)
        {
            float angle = (float)i / coneSegments * Mathf.PI * 2f;

            // cache trig values
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

        //base circle tris
        int baseTriStart = coneSegments * 3; // 3 vertices per triangle, 1 triangle per cone seg
        for (int i = 0; i < coneSegments; i++)
        {
            int baseIndex = baseTriStart + i * 3;
            triangles[baseIndex] = totalVertices - 1; //v1
            triangles[baseIndex + 1] = 1 + (i + 1) % coneSegments; // v2
            triangles[baseIndex + 2] = 1 + i; // v3
        }
    }

    private void Update()
    {
        if (currentPower > 0f) { // Run physics while beam is powered on

            beamOrigin = beamOriginPoint.position; // Update cached beam position
            beamDirection = beamOriginPoint.forward;

            FindTargets();
            AttractTargets();
        }
    }
    public void DrawBeam(float newPower)
    {
        bool wasActive = currentPower > 0f; // Tracks if the beam was just turned off

        currentPower = newPower;
        currentRange = currentPower * maxRange;

        if (currentPower > 0f)
        {
            DrawConeMesh(currentRange);
            previousPower = currentPower;
        }
        else if (wasActive)
        {
            coneMesh.Clear(); // Clear mesh data
            activeTargetXforms.Clear(); // clear targets
            previousPower = 0f;
        }
    }

    private void DrawConeMesh(float range)
    {
        coneMesh.Clear();

        if (range >= 0.01f) // Skip rebuilding 
        {
            float radius = range * Mathf.Tan(halfAngleRad);
            int totalVertices = 1 + coneSegments + 1;

            // build gradient 
            Color baseColor = Color.Lerp(beamColorLow, beamColorHigh, currentPower);
            Color edgeColor = baseColor;
            edgeColor.a *= 0.3f;
            Color centerColor = baseColor;
            centerColor.a *= 0.5f;

            vertices[0] = Vector3.zero;
            colors[0] = baseColor;

            // Build base
            for (int i = 0; i < coneSegments; i++)
            {
                vertices[1 + i] = new Vector3(cosAngles[i] * radius, sinAngles[i] * radius, range);
                colors[1 + i] = edgeColor;
            }

            vertices[totalVertices - 1] = new Vector3(0, 0, range);
            colors[totalVertices - 1] = centerColor;

            coneMesh.vertices = vertices;
            coneMesh.uv = uvs;
            coneMesh.colors = colors;
            coneMesh.triangles = triangles;
            coneMesh.RecalculateNormals();
            coneMesh.RecalculateBounds();
        }
    }

    private void FindTargets()
    {
        Collider[] potentialTargets = Physics.OverlapSphere(beamOrigin, currentRange, attractableLayers); // query all colliders within sphere surrounding cone 
        HashSet<Transform> foundXforms = new HashSet<Transform>(); // O(1) access

        foreach (Collider collider in potentialTargets)
        {
            if (IsValidTarget(collider)) // ** we can get rid of this once we define a specific layer and just use the distance check
            { 
                Transform targetXform = collider.transform;
                foundXforms.Add(targetXform);

                if (!activeTargetXforms.Contains(targetXform)) 
                {
                    // Add the new target to the list to track
                    AddTarget(targetXform);
                }
            }
        }

        RemoveTargets(foundXforms); // Target fell out of cone bounds or destroyed
    }

    private bool IsValidTarget(Collider collider) // We can get rid of this entire method once we define what is a attractable object
    {
        if (collider == null)
        {
            return false;
        }
        else
        {
            Transform targetXform = collider.transform;

            // so we dont attract the beam itself or the parentXform
            bool isSelf = targetXform.IsChildOf(cachedXform) || cachedXform.IsChildOf(targetXform);
            bool isBeam = (targetXform.IsChildOf(beamXform) || beamXform.IsChildOf(targetXform));

            if (isSelf || isBeam)
            {
                return false;
            }
            else
            {
                Vector3 toTarget = targetXform.position - beamOrigin;
                float distance = toTarget.magnitude;
                float angle = Vector3.Angle(beamDirection, toTarget);

                // If not aleady captured, within beam reach & spread
                if (distance >= captureDistance && distance <= currentRange && angle <= coneHalfAngle)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }

    private void AddTarget( Transform targetXform)
    {
        activeTargetXforms.Add(targetXform);

        if (IsServer)
        {
            NetworkObject netObj = targetXform.GetComponent<NetworkObject>();
            if (netObj == null) // check for networkobject
            {
               netObj = targetXform.GetComponentInParent<NetworkObject>();
            }

            ulong id = 0;
            if (netObj != null)
            {
                id = netObj.NetworkObjectId;
            }

            //NotifyTargetCapturedRPC(id);
        }
    }

    private void RemoveTargets(HashSet<Transform> validXforms )
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


    private void AttractTargets()
    {
        // Assumes that the attractable objects have a networkObject and networktransform component
        float dt = Time.deltaTime;

        for (int i = activeTargetXforms.Count - 1; i >= 0; i--)
        {
            Transform targetXform = activeTargetXforms[i];

            if (targetXform == null)
            {
                activeTargetXforms.RemoveAt(i);
            }
            else
            {
                Vector3 toOrigin = beamOrigin - targetXform.position; // The vector pointing to cone apex
                float distance = toOrigin.magnitude; // distance to cone apex

                if (distance < captureDistance)
                {
                    targetCaptured(targetXform); // Run the logic for when we fully capture an object
                }
                else
                {
                    float distanceNormalized = Mathf.Clamp01(distance / currentRange);
                    float curveMultiplier = attractionCurve.Evaluate(distanceNormalized); // if you dont like the animation curve just use lerp or something and ditch acceleration surve
                    float attractionStrength = baseAttractionSpeed * currentPower * curveMultiplier;

                    Vector3 direction = toOrigin.normalized; // normalized direction vector
                    Vector3 movement = direction * attractionStrength * dt; 

                    if (movement.magnitude > distance - captureDistance) //prevent overshooting origin
                    {
                        movement = direction * (distance - captureDistance);
                    }

                    targetXform.position += movement;
                }
            }
        }
    }

    private void targetCaptured(Transform target)
    {
        // ***Add collection  logic here in future ****
        
        if (IsServer)
        {
            NetworkObject netObj = target.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                netObj = target.GetComponentInParent<NetworkObject>();
            }

            ulong id = 0; // default to 0
            if (netObj != null)
            {
                id = netObj.NetworkObjectId;
            }

            //NotifyTargetFullyCapturedRPC(id);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyTargetCapturedRPC(ulong networkObjectId)
    {
        // Called when an object initially enters the tractor beam
        // placeholder used to trigger effects or notify other stuff
    }

    [Rpc(SendTo.Everyone)]
    private void NotifyTargetFullyCapturedRPC(ulong networkObjectId)
    {
        // Called when an object reaches the beam origin
        // placeholdr used to trigger collection logic
    }

    new private void OnDestroy() // Called when ship is destroyed
    {
        activeTargetXforms.Clear();

        // Clean up run-time resources
        if (coneMesh != null)
        {
            Destroy(coneMesh);
        }
        if (beamMaterial != null)
        {
            Destroy(beamMaterial);
        }
        if (beamObject != null)
        {
            Destroy(beamObject);
        }
    }

    private void OnDisable()
    {
        activeTargetXforms.Clear();
        coneMesh.Clear();
    }

}
