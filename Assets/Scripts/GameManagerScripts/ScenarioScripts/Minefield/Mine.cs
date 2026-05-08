/*
    Mine.cs
    Contributor(s): Henryk Musial

    NETWORKING NOTES:
    - This object intentionally has no NetworkTransform / NetworkRigidbody.
    - Motion is driven by WorldRoot, which is now networked: every peer reads the
      same CumulativeOffset / VirtualHeading and integrates the same physics.
    - The ONLY thing that needs to be replicated explicitly is the initial spawn
      position (where the host placed the mine in world space). Without that,
      a freshly-spawned mine on the client appears at the prefab's default
      position ? typically the origin / right next to the ship ? and from then on
      the WorldRoot integration moves a "ghost" mine that doesn't match the host.
    - We sync the spawn position through a NetworkVariable. On the host it's
      written once during spawn; on clients the OnValueChanged callback warps
      the mine to the correct spot the first time it arrives.
*/

using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Mine : NetworkBehaviour, IDamageable
{
    private float ROTATION_SPEED = 1.5f;
    private float DETONATION_RANGE = 100.0f;
    private float LASER_RANGE = 500.0f; // Distance threshold to fire the laser
    private float DETECTION_RANGE = 1000.0f;

    public Transform laser_aperture;
    public LineRenderer line_renderer;

    private Transform target_ship;
    private Rigidbody body;
    private bool registeredWithWorldRoot = false;

    [SerializeField] private float health = 50f;

    // Replicated spawn position. Host writes this in OnNetworkSpawn; clients read
    // it (either immediately if the value arrived before OnNetworkSpawn, or via
    // the OnValueChanged callback if it arrives later).
    private NetworkVariable<Vector3> netSpawnPosition = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<Quaternion> netSpawnRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool initialPositionApplied = false;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        // Non-kinematic so the physics engine produces real collision response when
        // the ship rams us. Driven by linearVelocity from WorldRoot.
        body.isKinematic = false;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Host: capture wherever the spawning code placed us so clients can mirror it.
            netSpawnPosition.Value = transform.position;
            netSpawnRotation.Value = transform.rotation;
            initialPositionApplied = true;
        }
        else
        {
            // Client: if the value already arrived in the spawn payload, apply it now.
            // Otherwise wait for the OnValueChanged callback below.
            if (netSpawnPosition.Value != Vector3.zero || netSpawnRotation.Value != Quaternion.identity)
            {
                ApplyInitialPose(netSpawnPosition.Value, netSpawnRotation.Value);
            }
            netSpawnPosition.OnValueChanged += OnSpawnPositionReceived;
            netSpawnRotation.OnValueChanged += OnSpawnRotationReceived;
        }

        // Register with the world root so it drives our velocity every FixedUpdate.
        // Both host and clients register their local copy so each peer simulates the
        // same motion locally (physics integrates velocity, the networked world state
        // keeps the inputs in sync).
        if (WorldRoot.Instance != null)
        {
            WorldRoot.Instance.RegisterRigidbody(body);
            registeredWithWorldRoot = true;
        }
        else
        {
            Debug.LogWarning("Mine: WorldRoot.Instance not found at spawn time; mine will not move with the world.");
        }
    }

    public override void OnNetworkDespawn()
    {
        netSpawnPosition.OnValueChanged -= OnSpawnPositionReceived;
        netSpawnRotation.OnValueChanged -= OnSpawnRotationReceived;
        DetachFromWorldRoot();
    }

    private void OnSpawnPositionReceived(Vector3 oldVal, Vector3 newVal)
    {
        if (initialPositionApplied) return;
        ApplyInitialPose(newVal, netSpawnRotation.Value);
    }

    private void OnSpawnRotationReceived(Quaternion oldVal, Quaternion newVal)
    {
        if (initialPositionApplied) return;
        ApplyInitialPose(netSpawnPosition.Value, newVal);
    }

    private void ApplyInitialPose(Vector3 pos, Quaternion rot)
    {
        // Warp the rigidbody (not just the transform) so physics state agrees.
        body.position = pos;
        body.rotation = rot;
        transform.position = pos;
        transform.rotation = rot;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        initialPositionApplied = true;
    }

    void Start()
    {
        // Find & ref the spaceship in scene
        GameObject spaceship_obj = GameObject.FindGameObjectWithTag("Spaceship");
        if (spaceship_obj != null)
        {
            target_ship = spaceship_obj.transform;
        }

        // Initialize the laser to off
        if (line_renderer != null)
        {
            line_renderer.positionCount = 2;
            line_renderer.enabled = false;
        }
    }

    public void damage(float dam)
    {
        if (!IsServer) return; // Only host mutates health; result propagates via destroy/RPC

        health -= dam;
        if (health <= 0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        DetachFromWorldRoot();
        // NetworkObject.Despawn (called implicitly by Destroy on a NetworkBehaviour
        // that's the root of a NetworkObject) will tear it down on clients too.
        Destroy(gameObject);
    }

    private void DetachFromWorldRoot()
    {
        if (registeredWithWorldRoot && WorldRoot.Instance != null)
        {
            WorldRoot.Instance.UnregisterRigidbody(body);
            registeredWithWorldRoot = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // On collision, detach from the world root so the impact response isn't
        // immediately overwritten by the next FixedUpdate's velocity assignment.
        // Host-authoritative: only the host detaches its mine. Without a
        // NetworkTransform the client copies will keep tracking world motion,
        // which is acceptable for the brief window before the host destroys
        // the mine via damage(). If you want clients to see the physical
        // tumble too, add a NetworkRigidbody / NetworkTransform.
        if (!IsHost) return;
        DetachFromWorldRoot();
    }

    void FixedUpdate()
    {
        if (target_ship == null)
        {
            return;
        }

        float distance_to_ship = Vector3.Distance(transform.position, target_ship.position);

        // Visual logic (Laser) runs on Host AND Clients so everyone sees it
        if (distance_to_ship <= LASER_RANGE)
        {
            //FireLaser();
        }
        else
        {
            //StopLaser();
        }

        if (!IsHost)
        {
            return;
        }

        if (distance_to_ship <= DETECTION_RANGE)
        {
            float dynamic_detection_range = CalculateDetectionRange();

            if (distance_to_ship < dynamic_detection_range)
            {
                LookAtShip();
            }
        }
    }

    private float CalculateDetectionRange()
    {
        EmissionReducers reducers = ReferenceAssistor.Instance.module_handlers[0].GetComponent<EmissionReducers>();
        if (reducers == null)
        {
            return DETECTION_RANGE;
        }
        int active_count = 0;

        // Check port and starboard reducers
        if (reducers.enabled_reducers[0]) active_count++;
        if (reducers.enabled_reducers[1]) active_count++;

        if (active_count == 0)
        {
            return DETECTION_RANGE;
        }
        else
        {
            return DETECTION_RANGE - (active_count * 200.0f);
        }
    }

    private void LookAtShip()
    {
        // Determine direction to ship
        Vector3 target_direction = (target_ship.position - transform.position).normalized;

        Quaternion target_rotation = Quaternion.LookRotation(target_direction);
        // MoveRotation works on non-kinematic bodies ? it routes the rotation through
        // the physics engine for proper interpolation, just like MovePosition would
        // for translation. Using this instead of transform.rotation = ... keeps motion
        // physics-driven so visuals interpolate cleanly between FixedUpdates.
        Quaternion newRotation = Quaternion.Slerp(transform.rotation, target_rotation,
                                                  Time.fixedDeltaTime * ROTATION_SPEED);
        body.MoveRotation(newRotation);
    }

    private void FireLaser()
    {
        if (line_renderer == null || laser_aperture == null) return;

        if (!line_renderer.enabled) line_renderer.enabled = true;

        // Update beam positions
        line_renderer.SetPosition(0, laser_aperture.position);
        line_renderer.SetPosition(1, target_ship.position);
    }

    private void StopLaser()
    {
        if (line_renderer != null && line_renderer.enabled)
        {
            line_renderer.enabled = false;
        }
    }

    private void Detonate()
    {

    }
}