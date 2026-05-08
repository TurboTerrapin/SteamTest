/*
    Mine.cs
    Contributor(s): Henryk Musial

    NETWORKING NOTES:
    - This prefab is expected to have a NetworkTransform (and optionally a
      NetworkRigidbody) component. Authority is the server.
    - Only the host registers the mine with WorldRoot and runs simulation.
      Clients receive position/rotation updates via NetworkTransform with
      smooth interpolation.
    - On the client, the Rigidbody is set kinematic so the local physics
      engine doesn't fight the NetworkTransform's authoritative writes.
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

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // Kinematic state is set in OnNetworkSpawn based on whether we're host or client.
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Host: this is the authoritative simulation. Non-kinematic so
            // collisions produce real impulse responses.
            body.isKinematic = false;

            if (WorldRoot.Instance != null)
            {
                WorldRoot.Instance.RegisterRigidbody(body);
                registeredWithWorldRoot = true;
            }
            else
            {
                Debug.LogWarning("Mine: WorldRoot.Instance not found at spawn time.");
            }
        }
        else
        {
            // Client: NetworkTransform writes the position every tick. Make the
            // Rigidbody kinematic so local physics doesn't fight those writes
            // (gravity, residual velocity, etc. would all conflict otherwise).
            body.isKinematic = true;
        }
    }

    public override void OnNetworkDespawn()
    {
        DetachFromWorldRoot();
    }

    void Start()
    {
        GameObject spaceship_obj = GameObject.FindGameObjectWithTag("Spaceship");
        if (spaceship_obj != null)
        {
            target_ship = spaceship_obj.transform;
        }

        if (line_renderer != null)
        {
            line_renderer.positionCount = 2;
            line_renderer.enabled = false;
        }
    }

    public void damage(float dam)
    {
        if (!IsServer) return;

        health -= dam;
        if (health <= 0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        DetachFromWorldRoot();
        Destroy(gameObject); // NetworkObject teardown will despawn on clients too.
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
        // Host-only: detach from WorldRoot so the impact impulse isn't immediately
        // overwritten by the next FixedUpdate's velocity assignment. The mine
        // tumbles freely until destroyed. NetworkTransform replicates the tumble
        // to clients automatically.
        if (!IsServer) return;
        DetachFromWorldRoot();
    }

    void FixedUpdate()
    {
        if (target_ship == null)
        {
            return;
        }

        float distance_to_ship = Vector3.Distance(transform.position, target_ship.position);

        // Laser visuals can run on host and clients (each peer reads the local
        // transform position which is kept in sync by NetworkTransform).
        if (distance_to_ship <= LASER_RANGE)
        {
            //FireLaser();
        }
        else
        {
            //StopLaser();
        }

        if (!IsServer)
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
        Vector3 target_direction = (target_ship.position - transform.position).normalized;

        Quaternion target_rotation = Quaternion.LookRotation(target_direction);
        Quaternion newRotation = Quaternion.Slerp(transform.rotation, target_rotation,
                                                  Time.fixedDeltaTime * ROTATION_SPEED);
        body.MoveRotation(newRotation);
    }

    private void FireLaser()
    {
        if (line_renderer == null || laser_aperture == null) return;

        if (!line_renderer.enabled) line_renderer.enabled = true;

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