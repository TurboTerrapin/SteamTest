/*
    WorldRoot.cs

    Host-authoritative "Futurama Engine" using stateless relative integration.

    The host drives all registered Rigidbodies by computing per-frame deltas
    from CumulativeOffset and VirtualHeading and applying them as velocities
    relative to each body's current position. Because we read rb.position to
    compute the next velocity, collisions are respected organically ? if a body
    is shoved off-course by a collision, the next frame integrates from its new
    position.

    NETWORKING:
    - CumulativeOffset and VirtualHeading are NetworkVariables (server-write)
      so clients can read them for HUD/minimap/scenario boundary purposes.
    - Physics is NOT simulated on clients. Mines, asteroids, etc. should use
      NetworkTransform/NetworkRigidbody to replicate their positions from the
      host. Clients should NOT register their local copies with WorldRoot.
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WorldRoot : NetworkBehaviour
{
    public static WorldRoot Instance { get; private set; }

    [Header("World Map State (networked)")]
    private NetworkVariable<Vector3> netCumulativeOffset = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<float> netVirtualHeading = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Vector3 CumulativeOffset => netCumulativeOffset.Value;
    public float VirtualHeading => netVirtualHeading.Value;

    // Track previous state so we can compute per-frame deltas. Host-only.
    private float lastVirtualHeading;
    private Vector3 lastCumulativeOffset;
    private bool lastStateInitialized = false;

    private HashSet<Rigidbody> registeredBodies = new HashSet<Rigidbody>();
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
        lastCumulativeOffset = netCumulativeOffset.Value;
        lastVirtualHeading = netVirtualHeading.Value;
        lastStateInitialized = true;
    }

    /// <summary>
    /// Register a Rigidbody to be driven by WorldRoot. Should only be called on
    /// the host ? clients should rely on NetworkTransform to receive positions.
    /// </summary>
    public void RegisterRigidbody(Rigidbody rb)
    {
        if (rb != null) pendingAdds.Add(rb);
    }

    public void UnregisterRigidbody(Rigidbody rb)
    {
        if (rb != null) pendingRemoves.Add(rb);
    }

    public void ApplyOffsetDelta(Vector3 delta)
    {
        if (!IsServer) return;
        netCumulativeOffset.Value = netCumulativeOffset.Value + delta;
    }

    public void SetVirtualHeading(float heading)
    {
        if (!IsServer) return;
        netVirtualHeading.Value = heading;
    }

    /// <summary>
    /// Host-only teleport. Snaps every registered body by the teleport delta,
    /// zeroing their velocities so the integration restarts cleanly.
    /// </summary>
    public void TeleportWorld(Vector3 newOffset, float newHeading)
    {
        if (!IsServer) return;

        Vector3 deltaO = newOffset - lastCumulativeOffset;
        Quaternion rCurrent = Quaternion.Euler(0f, -newHeading, 0f);
        Quaternion rOld = Quaternion.Euler(0f, -lastVirtualHeading, 0f);
        Quaternion deltaR = rCurrent * Quaternion.Inverse(rOld);

        netCumulativeOffset.Value = newOffset;
        netVirtualHeading.Value = newHeading;

        foreach (var rb in registeredBodies)
        {
            if (rb != null)
            {
                rb.position = (deltaR * rb.position) + (rCurrent * deltaO);
                rb.linearVelocity = Vector3.zero;
            }
        }

        lastVirtualHeading = newOffset.magnitude > 0f ? newHeading : newHeading; // keep
        lastCumulativeOffset = newOffset;
        lastVirtualHeading = newHeading;
    }

    private void FixedUpdate()
    {
        // Only the host drives physics. Clients receive object positions via
        // NetworkTransform on each replicated object.
        if (!IsServer) return;

        // 1. Process queues
        foreach (var rb in pendingAdds) registeredBodies.Add(rb);
        pendingAdds.Clear();

        foreach (var rb in pendingRemoves) registeredBodies.Remove(rb);
        pendingRemoves.Clear();

        registeredBodies.RemoveWhere(rb => rb == null);

        if (!lastStateInitialized)
        {
            lastCumulativeOffset = netCumulativeOffset.Value;
            lastVirtualHeading = netVirtualHeading.Value;
            lastStateInitialized = true;
        }

        // 2. Compute deltas since last FixedUpdate
        float fdt = Time.fixedDeltaTime;
        if (fdt <= 0f) return;

        float currentHeading = netVirtualHeading.Value;
        Vector3 currentOffset = netCumulativeOffset.Value;

        Quaternion rCur = Quaternion.Euler(0f, -currentHeading, 0f);
        Quaternion rOld = Quaternion.Euler(0f, -lastVirtualHeading, 0f);
        Quaternion deltaR = rCur * Quaternion.Inverse(rOld);
        Vector3 deltaO = currentOffset - lastCumulativeOffset;

        // 3. Drive velocities
        foreach (Rigidbody rb in registeredBodies)
        {
            if (rb.isKinematic) continue;

            Vector3 targetPosition = (deltaR * rb.position) + (rCur * deltaO);
            Vector3 requiredVelocity = (targetPosition - rb.position) / fdt;

            if (float.IsNaN(requiredVelocity.x) || float.IsNaN(requiredVelocity.y) || float.IsNaN(requiredVelocity.z))
            {
                requiredVelocity = Vector3.zero;
            }

            rb.linearVelocity = requiredVelocity;
        }

        // 4. Save for next frame
        lastVirtualHeading = currentHeading;
        lastCumulativeOffset = currentOffset;
    }
}