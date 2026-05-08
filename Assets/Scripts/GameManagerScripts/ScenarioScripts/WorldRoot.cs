/*
    WorldRoot.cs
    - Replaces the old "move the WorldRoot transform" approach.
    - Caches every child Rigidbody and drives them via linearVelocity each
      FixedUpdate so the physics engine fully simulates their motion (giving
      proper continuous collision detection AND real collision response when
      the ship rams them).
    - The WorldRoot transform itself stays at the origin and never moves.
    - ShipMovement mutates CumulativeOffset / CurrentWorldVelocity instead of
      worldRoot.transform.position.

    Why velocity instead of MovePosition:
      MovePosition on a kinematic body teleports through colliders and produces
      no collision response — fine for visuals but the ship would pass through
      mines on impact. Driving non-kinematic bodies with linearVelocity lets
      the physics engine sweep-test and produce real impact forces.

    Contributor(s): Henryk Musial
*/

/*
    WorldRoot.cs
    - Manages the absolute offset map coordinates for the ScenarioManager.
    - Acts as the physics driver for the "Futurama Engine".
    - Moves all registered child/world Rigidbodies around the origin (0,0,0) 
      to simulate the player ship moving and rotating through space.
*/

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

    public void RegisterBody(Rigidbody rb)
    {
        if (rb != null)
        {
            registeredRigidbodies.Add(rb);
        }
    }

    public void UnregisterBody(Rigidbody rb)
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
        // Clean up any nulls
        registeredRigidbodies.RemoveWhere(rb => rb == null);

        float fdt = Time.fixedDeltaTime;

        // 1. Calculate the curved orbit rotation for this exact frame.
        // The universe rotates opposite to the ship's virtual turn (-speed)
        Quaternion orbitRotation = Quaternion.Euler(0f, -currentWorldRotationSpeed * fdt, 0f);

        // Apply velocities to all registered world objects
        foreach (Rigidbody rb in registeredRigidbodies)
        {
            if (rb.isKinematic) continue;

            // 2. Where should this object be if it perfectly orbited the origin?
            Vector3 orbitedPosition = orbitRotation * rb.position;

            // 3. Add the linear thrust (moving forward/backward/strafe)
            Vector3 targetPosition = orbitedPosition + (currentWorldVelocity * fdt);

            // 4. Calculate the EXACT velocity required to travel that curve
            rb.linearVelocity = (targetPosition - rb.position) / fdt;
        }
    }
}

/*
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class WorldRoot : NetworkBehaviour
{
    public static WorldRoot Instance { get; private set; }

    // Synced from host -> clients. Represents what used to be worldRoot.transform.position.
    // Only used by gameplay code (boundary checks, map readouts, ship placement).
    // The actual physics motion is driven by CurrentWorldVelocity (below).
    private readonly NetworkVariable<Vector3> netCumulativeOffset = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Host-authored ship velocity, inverted. Synced so clients drive their local
    // mines with the same velocity the host does — keeps everyone visually in sync
    // without per-mine NetworkTransform churn.
    private readonly NetworkVariable<Vector3> netWorldVelocity = new NetworkVariable<Vector3>(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Local mirrors. Host writes these every FixedUpdate; clients read from the
    // network variables.
    private Vector3 cumulativeOffset = Vector3.zero;
    private Vector3 currentWorldVelocity = Vector3.zero;

    // Tracked bodies. No initial-position list needed anymore — motion is stateless
    // because the physics engine integrates velocity itself.
    private readonly List<Rigidbody> trackedBodies = new List<Rigidbody>();
    private readonly Dictionary<Rigidbody, int> indexByBody = new Dictionary<Rigidbody, int>();

    public Vector3 CumulativeOffset
    {
        get { return cumulativeOffset; }
    }

    public Vector3 CurrentWorldVelocity
    {
        get { return currentWorldVelocity; }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("WorldRoot: another instance already exists; destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsHost)
        {
            netCumulativeOffset.OnValueChanged += OnNetCumulativeOffsetChanged;
            netWorldVelocity.OnValueChanged += OnNetWorldVelocityChanged;
            // Apply current values immediately in case we joined late.
            cumulativeOffset = netCumulativeOffset.Value;
            currentWorldVelocity = netWorldVelocity.Value;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsHost)
        {
            netCumulativeOffset.OnValueChanged -= OnNetCumulativeOffsetChanged;
            netWorldVelocity.OnValueChanged -= OnNetWorldVelocityChanged;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnNetCumulativeOffsetChanged(Vector3 prev, Vector3 next)
    {
        cumulativeOffset = next;
    }

    private void OnNetWorldVelocityChanged(Vector3 prev, Vector3 next)
    {
        currentWorldVelocity = next;
        // Apply immediately to all tracked bodies so clients don't wait a frame
        // for the next FixedUpdate to react to a velocity change.
        ApplyVelocityToTrackedBodies();
    }

    /// <summary>
    /// Register a rigidbody to be driven by the world root. The rigidbody MUST be
    /// non-kinematic (we drive it via linearVelocity so the physics engine produces
    /// real collision response when the ship rams it). Gravity should be disabled.
    /// </summary>
    public void RegisterBody(Rigidbody rb)
    {
        if (rb == null) return;
        if (indexByBody.ContainsKey(rb)) return;

        if (rb.isKinematic)
        {
            Debug.LogWarning("WorldRoot: registering kinematic rigidbody '" + rb.name +
                             "'. Forcing isKinematic = false so collision response works.");
            rb.isKinematic = false;
        }
        if (rb.useGravity)
        {
            rb.useGravity = false;
        }
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        indexByBody[rb] = trackedBodies.Count;
        trackedBodies.Add(rb);

        // Set the initial velocity immediately so the body starts moving with the
        // world on its very first physics step (avoids a one-frame lag).
        rb.linearVelocity = currentWorldVelocity;
    }

    /// <summary>
    /// Unregister a rigidbody. Call this when a body should detach from the world's
    /// motion — e.g. after a collision so the impact response isn't immediately
    /// overwritten by the velocity loop. Also call from OnNetworkDespawn / OnDestroy.
    /// </summary>
    public void UnregisterBody(Rigidbody rb)
    {
        if (rb == null) return;
        int idx;
        if (!indexByBody.TryGetValue(rb, out idx)) return;

        // Swap-remove for O(1) deletion.
        int last = trackedBodies.Count - 1;
        if (idx != last)
        {
            Rigidbody lastBody = trackedBodies[last];
            trackedBodies[idx] = lastBody;
            indexByBody[lastBody] = idx;
        }
        trackedBodies.RemoveAt(last);
        indexByBody.Remove(rb);
    }

    /// <summary>
    /// Host-only: set the inverse-ship-velocity that all tracked bodies should move
    /// at. Called by ShipMovement each FixedUpdate.
    /// </summary>
    public void SetWorldVelocity(Vector3 velocity)
    {
        if (!IsHost) return;
        currentWorldVelocity = velocity;
    }

    /// <summary>
    /// Host-only: hard-set the offset (used by ShipMovement.PlaceShip when
    /// teleporting the ship into a new scenario). This does not teleport tracked
    /// bodies — PlaceShip happens before mines are spawned, so the offset only
    /// matters for boundary/readout math.
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        if (!IsHost) return;
        cumulativeOffset = newOffset;
        netCumulativeOffset.Value = newOffset;
    }

    /// <summary>
    /// Host-only: integrate the cumulative offset by `delta`. Called by ShipMovement
    /// each FixedUpdate alongside SetWorldVelocity. We keep the offset around because
    /// boundary/heading code reads it as the "ship world position" stand-in.
    /// </summary>
    public void ApplyOffsetDelta(Vector3 delta)
    {
        if (!IsHost) return;
        cumulativeOffset += delta;
    }

    void FixedUpdate()
    {
        // Host: publish offset + velocity to clients (Vector3 == uses approx equality
        // so stationary frames don't generate network traffic).
        if (IsHost)
        {
            if (netCumulativeOffset.Value != cumulativeOffset)
            {
                netCumulativeOffset.Value = cumulativeOffset;
            }
            if (netWorldVelocity.Value != currentWorldVelocity)
            {
                netWorldVelocity.Value = currentWorldVelocity;
            }
        }

        // Both host and clients: ensure every tracked body has the current world
        // velocity. We re-assign every step (not just on changes) because collisions
        // can perturb a mine's velocity — re-asserting it pulls the mine back onto
        // the world's "conveyor belt." Bodies that have just been hit and should
        // tumble freely have already been unregistered by their collision handler.
        ApplyVelocityToTrackedBodies();
    }

    private void ApplyVelocityToTrackedBodies()
    {
        int count = trackedBodies.Count;
        for (int i = 0; i < count; i++)
        {
            Rigidbody rb = trackedBodies[i];
            if (rb == null) continue;
            rb.linearVelocity = currentWorldVelocity;
        }
    }
}
*/