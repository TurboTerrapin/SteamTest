using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    private GameObject worldRoot;
    private RootTracker rootChildren;
    public float collisionDamageMultiplier = 1.0f;
    public float collisionThreshold = 0.0f; // Force threshold for damage to be applied
    public float maxDamage = 30.0f;

    // Collision type damage
    const float ASTEROID_DMG = 0.5f;

    private ShipHealth shipHealth;

    private void Start()
    {
        shipHealth = GetComponent<ShipHealth>();
        worldRoot = GameObject.FindWithTag("WorldRoot");
        rootChildren = worldRoot.GetComponent<RootTracker>();
        if (shipHealth == null)
        {
            Debug.LogError("ShipHealth component not found on this GameObject!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Detected");

        if (shipHealth == null)
        {
            return;
        }

        if (other.CompareTag("Asteroid"))
        {
            Debug.Log("Asteroid Hit");
            CalculateDamage(ASTEROID_DMG, other);
        }
    }

    private void CalculateDamage(float baseDamage, Collider other)
    {
        Rigidbody otherRb = other.attachedRigidbody;
        float collisionForce = (rootChildren.GetRelativeVelocity(otherRb)).magnitude;

        if (collisionForce < collisionThreshold)
        {
            Debug.Log("Collision force under threshold.");
            return;
        }

        float totalDamage = Mathf.Min(baseDamage + collisionForce * collisionDamageMultiplier, maxDamage);

        Vector3 impactPoint = other.ClosestPoint(transform.position);
        Vector3 localHitPoint = transform.InverseTransformPoint(impactPoint);
        Vector3 normalizedLocalHit = localHitPoint.normalized;

        float forwardDot = Vector3.Dot(normalizedLocalHit, Vector3.forward);
        float rightDot = Vector3.Dot(normalizedLocalHit, Vector3.right);

        float[] weights = new float[4];
        weights[0] = Mathf.Max(0f, forwardDot);     // Forward
        weights[1] = Mathf.Max(0f, -rightDot);      // Port
        weights[2] = Mathf.Max(0f, rightDot);       // Starboard
        weights[3] = Mathf.Max(0f, -forwardDot);    // Aft

        float totalWeight = weights[0] + weights[1] + weights[2] + weights[3];
        if (totalWeight > 0.001f)
        {
            for (int i = 0; i < weights.Length; i++)
                weights[i] /= totalWeight;
        }
        else
        {
            for (int i = 0; i < weights.Length; i++)
                weights[i] = 0.25f;
        }

        float[] sectionDamage = new float[4];
        for (int i = 0; i < sectionDamage.Length; i++)
        {
            sectionDamage[i] = totalDamage * weights[i];
        }

        shipHealth.damageMultipleSections(sectionDamage);
        Debug.Log($"[Collision Detected] Impact at world coord: {impactPoint}");
        Debug.Log($"Damage calculation:\n" +
            $"  Forward:   {sectionDamage[0]:F2}\n" +
            $"  Port:      {sectionDamage[1]:F2}\n" +
            $"  Starboard: {sectionDamage[2]:F2}\n" +
            $"  Aft:       {sectionDamage[3]:F2}");
        Debug.Log($"[Collision] Force: {collisionForce:F2}, Total Damage: {totalDamage:F2}");
    }
}