/*
    CollisionHandler.cs
    - Deals with collision impacts
    - Communicates to ShipHealth 
    Contributor(s): Henryk Musial
    Last Updated: 1/23/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class CollisionHandler : MonoBehaviour
{
    //private static float ASTEROID_DMG = 15.0f;
    // Add other collision object damage values here

    public LightsManager lightsManager;
    public List<Collider> shipColliders = new List<Collider>();

    private ShipHealth shipHealth;

    /*
    // Asteroid impact
    public GameObject spriteRendererPrefab = null;
    public List<Sprite> explosionSprites;
    public float frameRate = 60f;
    */

    private void Start()
    {
        shipHealth = GetComponent<ShipHealth>();

        // If not host, destroy this and colliders
        if (NetworkManager.Singleton.IsHost == false)
        {
            foreach (Collider collider in shipColliders)
            {
                GameObject.Destroy(collider.gameObject);
            }
            Destroy(this);
        }
    }

    public void HandleCollision(Collider shipSectionCollider, Collider impactObjectCollider)
    {
        int sectionIndex = shipColliders.IndexOf(shipSectionCollider);
        Debug.Log(shipColliders[sectionIndex].gameObject.name + " impacted a " + impactObjectCollider.gameObject.name);
        /*
        if (sectionIndex != -1)
        {
            Vector3 impactCoord = other.ClosestPoint(colliderObject.transform.position);
            TriggerFlickerClientRpc();

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
        }*/
    }

    /*
    [ClientRpc]
    private void SpawnImpactVfxClientRpc(Vector3 coord)
    {

        if (spriteRendererPrefab != null && explosionSprites != null && explosionSprites.Count > 0)
        {
            StartCoroutine(AsteroidImpactAnimation(coord));
        }
    }

    [ClientRpc]
    private void TriggerFlickerClientRpc()
    {

        // debug line to test flicker duration / itensity
        float damage = (float)Random.Range(5.0f, 50.0f);
        if (lightsManager != null)
        {
            //lightsManager.TriggerCollisionFlicker(ASTEROID_DMG); // original 
            lightsManager.TriggerCollisionFlicker(damage); // debug

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
    */
}