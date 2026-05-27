/*
    Torpedo.cs
    - Handles torpedo movement and heatseeking
    - Manages stats based on type
    Contributor(s): Henryk Musial, Jake Schott
    Last Updated: 5/27/2026
*/

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum TorpedoType
{
    Photon,         // Default, damage
    Proton,         // Shield bonus
    Ion,            // Disable movement
    Quantum,        // Disable weapons
    Superluminal,   // Anti-cloak
    Chroniton       // Instakill
}

public class Torpedo : NetworkBehaviour
{
    // CLASS CONSTANTS
    private static float BASE_SPEED = 50.0f;
    private static float BASE_DAMAGE = 50.0f;
    private static float BASE_TURN_RATE = 60.0f; // Slower turn rate for realistic arcs
    private static float BASE_ANGLE_DELTA = 90.0f;
    private static float BASE_LIFETIME = 100.0f;
    private static float NAVIGATION_CONSTANT = 4.0f;
    private static float TRACKING_DELAY = 0.25f; // Time in seconds before tracking begins
    private static float MIN_HIT_DISTANCE = 5.0f; // Distance threshold to trigger damage

    [Header("TorpedoSettings")]
    [SerializeField]
    private TorpedoType torpedo_type;
    [SerializeField]
    private float speed = BASE_SPEED;
    [SerializeField]
    private float damage = BASE_DAMAGE;
    [SerializeField]
    private float turn_rate = BASE_TURN_RATE;
    [SerializeField]
    private float max_angle_delta = BASE_ANGLE_DELTA;
    [SerializeField]
    private float detection_radius = 5000.0f;
    [SerializeField]
    private LayerMask target_layer; // Bitwise collision layer

    private bool detonated = false;
    private Vector3 current_velocity; // Tracks actual momentum
    private float alive_time = 0.0f; // Tracks time since launch

    private Vector3 last_los; // Tracks the Line of Sight history
    private bool has_los = false; // Ensures we have a baseline to measure rotation

    private Transform current_target = null;

    public void Initialize(float power_percent)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        // Power from TorpedoPowers.cs affects damage capability
        float power_multiplier = 1.0f + power_percent;
        damage *= power_multiplier;

        // Give it initial forward momentum based on the launcher's orientation
        current_velocity = transform.forward * speed;

        // Call this HERE instead of Start() to ensure max_angle_delta is ready
        StartCoroutine(targetFinder());

        // Destroy self after lifetime if no hit
        Destroy(gameObject, BASE_LIFETIME);
    }

    IEnumerator targetFinder()
    {
        while (true)
        {
            current_target = null;
            findClosestTarget();
            yield return new WaitForSeconds(1.0f);
        }
    }

    private void findClosestTarget()
    {
        // Only the server needs to calculate targets for movement
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        // Bitwise layer check
        Collider[] hits = Physics.OverlapSphere(transform.position, detection_radius, target_layer);
        float closest_dist = Mathf.Infinity;
        Transform best_candidate = null;

        foreach (var hit in hits)
        {
            // Is a candidate if has an IDamageable script and is either not collectible or not in collectible item categories 0 (utility) or 1 (torpedo)
            if (hit.GetComponent<IDamageable>() != null && (hit.GetComponent<CollectibleItem>() == null || hit.GetComponent<CollectibleItem>().getItemCategory() > 1))
            {
                // NEW: Ensure the target is actually inside our forward seek cone
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closest_dist)
                {
                    closest_dist = dist;
                    best_candidate = hit.transform;
                }
            }
        }
        current_target = best_candidate;
    }

    private void Update()
    {
        moveTorpedo();
        animateTorpedo();
    }

    private void animateTorpedo()
    {
        // Rotate
        transform.GetChild(1).Rotate(0.0f, 0.0f, 200.0f * Time.deltaTime);
        transform.GetChild(2).Rotate(0.0f, 0.0f, 500.0f * Time.deltaTime);

        // Face towards player camera
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
        }
    }

    private void onTargetCollision()
    {
        // Ensure only detonates once
        if (detonated == true)
        {
            return;
        }
        detonated = false;

        // Hit detected - apply damage and destroy
        applyDamageEffect(current_target);

        // Create the explosion
        EffectsHandler effects_handler = ReferenceAssistor.Instance.effects_handler;
        effects_handler.createExplosion(transform.position, 4.0f, GetComponent<MapItem>().getColor());

        // Deactivate torpedo
        current_target = null;

        // Safely despawn and destroy across the network
        NetworkObject net_obj = GetComponent<NetworkObject>();
        if (net_obj != null && net_obj.IsSpawned)
        {
            net_obj.Despawn(true); // 'true' tells the server to also Destroy the GameObject
        }
        else
        {
            Destroy(gameObject); // Fallback just in case
        }
    }

    private void moveTorpedo()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        alive_time += Time.deltaTime;

        // Check if we've hit the target via distance check
        if (current_target != null)
        {
            float distance_to_target = Vector3.Distance(transform.position, current_target.position);
            if (distance_to_target < MIN_HIT_DISTANCE)
            {
                onTargetCollision();
                return;
            }
        }

        // Default desire is to keep drifting in our current direction
        Vector3 desired_velocity = current_velocity;

        // Only track if the clearance phase is done
        if (current_target != null && alive_time >= TRACKING_DELAY)
        {
            Vector3 current_los = (current_target.position - transform.position).normalized;

            // Initialize our Line of Sight string on the first tracking frame
            if (has_los == false)
            {
                last_los = current_los;
                has_los = true;
            }

            float angle_to_target = Vector3.Angle(current_velocity.normalized, current_los);

            // Heatseeking logic: Only track if target is within the seek cone
            if (angle_to_target <= max_angle_delta)
            {
                // 1. Calculate the rotation rate of our Line of Sight string
                Vector3 los_rate = Vector3.Cross(last_los, current_los) / Time.deltaTime;

                // 2. Proportional Navigation formula to calculate required turn force
                Vector3 acceleration_command = Vector3.Cross(los_rate, current_velocity.normalized) * NAVIGATION_CONSTANT * speed;

                // 3. Add the commanded force to our current trajectory
                desired_velocity = current_velocity + (acceleration_command * Time.deltaTime);
            }

            last_los = current_los; // Save for next frame's comparison
        }

        // Smoothly steer our momentum toward the desired ProNav trajectory, respecting our physical turn_rate limit
        current_velocity = Vector3.RotateTowards(current_velocity, desired_velocity, turn_rate * Mathf.Deg2Rad * Time.deltaTime, 0.0f);

        // Enforce constant speed so the torpedo doesn't artificially accelerate or brake
        current_velocity = current_velocity.normalized * speed;

        // Move the torpedo physically
        transform.position += current_velocity * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        // If spontaneously collides with something, detonate the torpedo if damageable and in target layer
        if (other.GetComponent<IDamageable>() != null && (target_layer.value & (1 << other.gameObject.layer)) != 0)
        {
            current_target = other.transform;
            onTargetCollision();
        }
    }

    private void applyDamageEffect(Transform hit_obj)
    {
        if (hit_obj == null)
        {
            return;
        }

        // Placeholder for damage application based on Type
        switch (torpedo_type)
        {
            case TorpedoType.Photon:
                // Standard Damage
                break;
            case TorpedoType.Proton:
                // Extra Shield Damage
                break;
            case TorpedoType.Ion:
                // Disable Movement
                break;
            case TorpedoType.Quantum:
                // Disable Weapons
                break;
            case TorpedoType.Superluminal:
                // Reveal Cloak
                break;
            case TorpedoType.Chroniton:
                // Instakill
                break;
        }

        // Apply damage to IDamageable
        IDamageable[] damage_targets = hit_obj.GetComponents<IDamageable>();
        foreach (IDamageable damage_target in damage_targets)
        {
            damage_target.damage(damage);
        }
    }
}