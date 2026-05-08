/*
    WorldRoot.cs
    - Manages the absolute offset map coordinates for the ScenarioManager.
    - Acts as the physics driver for the "Futurama Engine".
    - Moves all registered child/world Rigidbodies around the origin (0,0,0) 
      to simulate the player ship moving and rotating through space.
*/

/*
    WorldRoot.cs
    - Uses "Stateless Relative Integration"
    - Zero Compounding Drift, Zero Rubber-Banding, Organic Collision Acceptance.
*/

using System.Collections.Generic;
using UnityEngine;

public class WorldRoot : MonoBehaviour
{
    public static WorldRoot Instance { get; private set; }

    [Header("World Map State")]
    public Vector3 CumulativeOffset;
    public float VirtualHeading;

    // Track previous states to calculate exact frame-by-frame deltas
    private float lastVirtualHeading;
    private Vector3 lastCumulativeOffset;

    private HashSet<Rigidbody> registeredBodies = new HashSet<Rigidbody>();

    // Queues to prevent "Collection Modified" crashes when mines spawn mid-frame
    private HashSet<Rigidbody> pendingAdds = new HashSet<Rigidbody>();
    private HashSet<Rigidbody> pendingRemoves = new HashSet<Rigidbody>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterRigidbody(Rigidbody rb) { if (rb != null) pendingAdds.Add(rb); }
    public void UnregisterRigidbody(Rigidbody rb) { if (rb != null) pendingRemoves.Add(rb); }

    public void ApplyOffsetDelta(Vector3 delta) { CumulativeOffset += delta; }
    public void SetVirtualHeading(float heading) { VirtualHeading = heading; }

    // Safely Teleports the world without breaking physics limits
    public void TeleportWorld(Vector3 newOffset, float newHeading)
    {
        CumulativeOffset = newOffset;
        VirtualHeading = newHeading;

        // Calculate the massive delta of the teleport
        Vector3 deltaO = CumulativeOffset - lastCumulativeOffset;
        Quaternion rCurrent = Quaternion.Euler(0f, -VirtualHeading, 0f);
        Quaternion rOld = Quaternion.Euler(0f, -lastVirtualHeading, 0f);
        Quaternion deltaR = rCurrent * Quaternion.Inverse(rOld);

        foreach (var rb in registeredBodies)
        {
            if (rb != null)
            {
                // Instantly warp the object to match the teleport
                rb.position = (deltaR * rb.position) + (rCurrent * deltaO);
                rb.linearVelocity = Vector3.zero;
            }
        }

        // Reset the trackers
        lastVirtualHeading = VirtualHeading;
        lastCumulativeOffset = CumulativeOffset;
    }

    private void FixedUpdate()
    {
        // 1. Safely process queues
        foreach (var rb in pendingAdds) registeredBodies.Add(rb);
        pendingAdds.Clear();

        foreach (var rb in pendingRemoves) registeredBodies.Remove(rb);
        pendingRemoves.Clear();

        // 2. Calculate the exact Deltas since the last physics step
        float fdt = Time.fixedDeltaTime;

        Quaternion rCurrent = Quaternion.Euler(0f, -VirtualHeading, 0f);
        Quaternion rOld = Quaternion.Euler(0f, -lastVirtualHeading, 0f);

        // Delta_R = Current Rotation * Inverse(Old Rotation)
        Quaternion deltaR = rCurrent * Quaternion.Inverse(rOld);
        Vector3 deltaO = CumulativeOffset - lastCumulativeOffset;

        registeredBodies.RemoveWhere(rb => rb == null);

        // 3. Apply Frame-Relative Physics
        foreach (Rigidbody rb in registeredBodies)
        {
            if (rb.isKinematic) continue;

            // P_new = (Delta_R * P_old) + (R_current * Delta_O)
            // Because this is calculated from rb.position, it inherently respects physical collisions!
            Vector3 targetPosition = (deltaR * rb.position) + (rCurrent * deltaO);

            Vector3 requiredVelocity = (targetPosition - rb.position) / fdt;

            if (float.IsNaN(requiredVelocity.x) || float.IsNaN(requiredVelocity.y) || float.IsNaN(requiredVelocity.z))
            {
                requiredVelocity = Vector3.zero;
            }

            rb.linearVelocity = requiredVelocity;
        }

        // 4. Save state for next frame
        lastVirtualHeading = VirtualHeading;
        lastCumulativeOffset = CumulativeOffset;
    }
}

/*
using System.Collections.Generic;
using UnityEngine;

public class WorldRoot : MonoBehaviour
{
    public static WorldRoot Instance { get; private set; }

    [Header("World Map State")]
    public Vector3 CumulativeOffset;

    [Header("Current Physics State")]
    private Vector3 currentWorldVelocity;
    private float currentWorldRotationSpeed;

    // Use a HashSet for lightning-fast registration and removal of world objects
    private HashSet<Rigidbody> registeredRigidbodies = new HashSet<Rigidbody>();

    private void Awake()
    {
        // Standard Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // --- Object Registration ---
    // Asteroids, enemies, and debris should call WorldRoot.Instance.RegisterRigidbody(this) 
    // in their Start() methods, and Unregister in their OnDestroy() methods.

    public void RegisterRigidbody(Rigidbody rb)
    {
        if (rb != null)
        {
            registeredRigidbodies.Add(rb);
        }
    }

    public void UnregisterRigidbody(Rigidbody rb)
    {
        if (rb != null)
        {
            registeredRigidbodies.Remove(rb);
        }
    }

    // --- State Setters (Called by ShipMovement.cs) ---

    public void ApplyOffsetDelta(Vector3 delta)
    {
        CumulativeOffset += delta;
    }

    public void SetOffset(Vector3 offset)
    {
        CumulativeOffset = offset;
    }

    public void SetWorldVelocity(Vector3 velocity)
    {
        currentWorldVelocity = velocity;
    }

    public void SetWorldRotationSpeed(float speed)
    {
        currentWorldRotationSpeed = speed;
    }

    // --- The "Futurama Engine" Physics Loop ---

    private void FixedUpdate()
    {
        // Clean up any nulls just in case a registered object was destroyed improperly
        registeredRigidbodies.RemoveWhere(rb => rb == null);

        // 1. Calculate orbital parameters once per frame
        float rotationRadians = currentWorldRotationSpeed * Mathf.Deg2Rad;

        // The universe rotates in the opposite direction of the ship's virtual turn.
        // If the ship turns right (+Y), the universe orbits left (Down axis).
        Vector3 rotationAxis = Vector3.down;

        // Apply velocities to all registered world objects
        foreach (Rigidbody rb in registeredRigidbodies)
        {
            if (rb.isKinematic) continue; // Skip kinematic objects if any sneaked in

            // 2. Linear Movement (Simulates the ship moving forward/backward/strafe)
            Vector3 linearMove = currentWorldVelocity;

            // 3. Orbital Movement (Simulates the ship rotating)
            // Cross product of Down * Position gives the perfect tangent vector to orbit the origin
            Vector3 orbitalMove = Vector3.Cross(rotationAxis, rb.position) * rotationRadians;

            // 4. Combine and override the rigidbody's velocity.
            // Note: Using rb.linearVelocity (Unity 2023.3+). If on an older version, use rb.velocity.
            rb.linearVelocity = linearMove + orbitalMove;
        }
    }
}

*/