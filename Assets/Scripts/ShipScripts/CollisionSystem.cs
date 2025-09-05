using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(ShipHealth))]
public class CollisionSystem : NetworkBehaviour
{
    public float ASTEROID_DMG = 15; 
    private ShipHealth shipHealth;

    // Asteroid impact
    public GameObject spriteRendererPrefab = null;
    public List<Sprite> explosionSprites;
    public float frameRate = 60f;

    
    private List<BoxCollider> shipColliders = new List<BoxCollider>();

    public override void OnNetworkSpawn()
    {
        shipHealth = GetComponent<ShipHealth>();

        if (shipHealth == null)
        {
            Debug.LogError($"ShipHealth missing on {gameObject.name}");
        }
        InitializeColliders();
    }

    private void InitializeColliders()
    {
        BoxCollider[] colliders = GetComponentsInChildren<BoxCollider>();
        
        foreach (BoxCollider collider in colliders)
        {
            if (collider.gameObject.name.Contains("Collider") &&  (collider.gameObject.name.Contains("Forward") || 
                    collider.gameObject.name.Contains("Port") || collider.gameObject.name.Contains("Starboard") || 
                        collider.gameObject.name.Contains("Aft"))) {

                shipColliders.Add(collider);
                collider.isTrigger = true;
                CollisionForwarder forwarder = collider.gameObject.AddComponent<CollisionForwarder>();
                forwarder.parentDetection = this;
                
                //Debug.Log($"Registered collider: {collider.gameObject.name}");
            }
        }

        if (shipColliders.Count == 0)
        {
            Debug.LogWarning("No ship colliders found!");
        }
        else
        {
            //Debug.Log($"Found {shipColliders.Count} ship colliders");
        }
    }

    public void HandleCollision(Collider other, GameObject colliderObject)
    {
  
        if (!IsServer) return;

        if (shipHealth == null || !other.CompareTag("Asteroid"))
            return;

        int sectionIndex = GetHitSectionIndex(colliderObject.name);
        if (sectionIndex != -1)
        {
            Vector3 impactCoord = other.ClosestPoint(colliderObject.transform.position);

            
            if (sectionIndex == 0) // If collision with forward collider
            {
                Vector3 directionToImpact = (impactCoord - transform.position).normalized;
                float forwardAlignment = Vector3.Dot(directionToImpact, transform.forward);

                if (forwardAlignment > 0.75) // If front face of forard collider
                {
                    SpawnImpactVfxClientRpc(impactCoord); // Spawn & broadcast VFX to all clients
                }
            }

            // asteroid is despawned across the network
            other.GetComponent<NetworkObject>().Despawn();

            // damage is applied authoritatively by serve
            shipHealth.damageSection(ASTEROID_DMG, sectionIndex);

            Debug.Log($"[SERVER] Collision @ Section: {colliderObject.name}, Damage: {ASTEROID_DMG}");
        }
    }

    [ClientRpc]
    private void SpawnImpactVfxClientRpc(Vector3 coord)
    {

        if (spriteRendererPrefab != null && explosionSprites != null && explosionSprites.Count > 0)
        {
            StartCoroutine(AsteroidImpactAnimation(coord));
        }
    }

    private IEnumerator AsteroidImpactAnimation(Vector3 coord)
    {
        GameObject spriteObject = Instantiate(spriteRendererPrefab, coord, Quaternion.identity);
        SpriteRenderer renderer = spriteObject.GetComponent<SpriteRenderer>();

        spriteObject.transform.LookAt(Camera.main.transform);

        float spf = 1f / frameRate; // seconds per frame
        foreach (Sprite sprite in explosionSprites)
        {
            renderer.sprite = sprite;
            yield return new WaitForSeconds(spf);
        }

        Destroy(spriteObject);
    }

    private int GetHitSectionIndex(string colliderName)
    {
        if (colliderName.Contains("ForwardCollider")) return 0;
        if (colliderName.Contains("PortCollider")) return 1;
        if (colliderName.Contains("StarboardCollider")) return 2;
        if (colliderName.Contains("AftCollider")) return 3;

        Debug.LogWarning("Error - Looking for " + colliderName);
        return -1;
    }
}

// forward collision events
public class CollisionForwarder : MonoBehaviour
{
    public CollisionSystem parentDetection;

    private void OnTriggerEnter(Collider other)
    {
        if (parentDetection != null)
        {
            parentDetection.HandleCollision(other, gameObject);
        }
    }
}