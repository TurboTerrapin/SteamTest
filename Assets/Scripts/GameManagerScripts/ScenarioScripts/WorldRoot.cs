/*
    WorldRoot.cs
    - Manages the absolute offset map coordinates for the ScenarioManager.
    - Acts as the physics driver for the "Futurama Engine".
    - Moves all registered child/world Rigidbodies around the origin (0,0,0)
      to simulate the player ship moving and rotating through space.

    - Uses "Stateless Relative Integration"
    - Zero Compounding Drift, Zero Rubber-Banding, Organic Collision Acceptance.

    NETWORKING:
    - CumulativeOffset and VirtualHeading are NetworkVariables, written by the host
      and replicated to all clients. This lets every peer run the same physics
      integration locally so registered Rigidbodies (mines, asteroids, etc.) move
      identically on host and clients without needing a NetworkTransform per-object.
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WorldRoot : NetworkBehaviour
{
    public static WorldRoot Instance { get; private set; }

    [Header("World Map State (networked)")]
    // Host writes, everyone reads. Replicated every time the host changes the value.
    private NetworkVariable<Vector3> netCumulativeOffset = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<float> netVirtualHeading = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Public accessors that read from the networked state
    public Vector3 CumulativeOffset => netCumulativeOffset.Value;
    public float VirtualHeading => netVirtualHeading.Value;

    // Track previous states to calculate exact frame-by-frame deltas (LOCAL ONLY)
    private float lastVirtualHeading;
    private Vector3 lastCumulativeOffset;
    private bool lastStateInitialized = false;

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

    public override void OnNetworkSpawn()
    {
        // Initialise the "last" trackers to the current networked state on spawn so
        // the first FixedUpdate doesn't compute a giant delta from (0,0,0).
        lastCumulativeOffset = netCumulativeOffset.Value;
        lastVirtualHeading = netVirtualHeading.Value;
        lastStateInitialized = true;
    }

    public void RegisterRigidbody(Rigidbody rb) { if (rb != null) pendingAdds.Add(rb); }
    public void UnregisterRigidbody(Rigidbody rb) { if (rb != null) pendingRemoves.Add(rb); }

    /// <summary>
    /// Host-only: shift the world offset. On clients this is a no-op because
    /// NetworkVariable writes are server-authoritative.
    /// </summary>
    public void ApplyOffsetDelta(Vector3 delta)
    {
        if (!IsServer) return;
        netCumulativeOffset.Value = netCumulativeOffset.Value + delta;
    }

    /// <summary>
    /// Host-only: set the virtual heading.
    /// </summary>
    public void SetVirtualHeading(float heading)
    {
        if (!IsServer) return;
        netVirtualHeading.Value = heading;
    }

    /// <summary>
    /// Safely teleports the world without breaking physics limits. Host-only;
    /// clients receive the new offset/heading via NetworkVariable replication
    /// and their own FixedUpdate will warp registered bodies to match.
    /// </summary>
    public void TeleportWorld(Vector3 newOffset, float newHeading)
    {
        if (!IsServer) return;

        netCumulativeOffset.Value = newOffset;
        netVirtualHeading.Value = newHeading;

        // Calculate the massive delta of the teleport
        Vector3 deltaO = newOffset - lastCumulativeOffset;
        Quaternion rCurrent = Quaternion.Euler(0f, -newHeading, 0f);
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
        lastVirtualHeading = newHeading;
        lastCumulativeOffset = newOffset;
    }

    private void FixedUpdate()
    {
        // Don't drive physics until network is ready — otherwise on a freshly-joined
        // client the "last" trackers may be stale and cause a one-frame snap.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !lastStateInitialized)
        {
            lastCumulativeOffset = netCumulativeOffset.Value;
            lastVirtualHeading = netVirtualHeading.Value;
            lastStateInitialized = true;
        }

        // 1. Safely process queues
        foreach (var rb in pendingAdds) registeredBodies.Add(rb);
        pendingAdds.Clear();

        foreach (var rb in pendingRemoves) registeredBodies.Remove(rb);
        pendingRemoves.Clear();

        // 2. Calculate the exact Deltas since the last physics step
        float fdt = Time.fixedDeltaTime;

        float currentHeading = netVirtualHeading.Value;
        Vector3 currentOffset = netCumulativeOffset.Value;

        Quaternion rCurrent = Quaternion.Euler(0f, -currentHeading, 0f);
        Quaternion rOld = Quaternion.Euler(0f, -lastVirtualHeading, 0f);

        // Delta_R = Current Rotation * Inverse(Old Rotation)
        Quaternion deltaR = rCurrent * Quaternion.Inverse(rOld);
        Vector3 deltaO = currentOffset - lastCumulativeOffset;

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
        lastVirtualHeading = currentHeading;
        lastCumulativeOffset = currentOffset;
    }
}