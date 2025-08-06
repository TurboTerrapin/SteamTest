using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    public float ASTEROID_DMG = 10;
    private ShipHealth shipHealth;

    private void Start()
    {
        shipHealth = GetComponentInParent<ShipHealth>();

        if (shipHealth == null)
        {
            Debug.LogError("ShipHealth component not found on this GameObject!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (shipHealth == null || !other.CompareTag("Asteroid")) 
            return;
        // Delete asteroid / Trigger VFX her
        
        int sectionIndex = GetHitSectionIndex();
        shipHealth.damageSection(ASTEROID_DMG, sectionIndex);
        
        Debug.Log($"Collision @ Section: {sectionIndex}, Damage: {ASTEROID_DMG}");
    }

    private int GetHitSectionIndex()
    {
        string colliderName = gameObject.name;

        if (colliderName.Contains("ForwardCollider")) return 0; 
        if (colliderName.Contains("PortCollider")) return 1;   
        if (colliderName.Contains("StarboardCollider")) return 2; 
        if (colliderName.Contains("AftCollider")) return 3;     

        Debug.LogWarning("Error - Looking for " + colliderName );
        return 0; 
    }
}