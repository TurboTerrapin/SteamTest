using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    public float ASTEROID_DMG = 10;
    private ShipHealth shipHealth;

    // Asteroid impact
    public GameObject spriteRendererPrefab = null;
    public List<Sprite> explosionSprites;
    public float frameRate = 120f;

    private void Start()
    {
        shipHealth = GetComponentInParent<ShipHealth>();

        if (shipHealth == null)
        {
            Debug.LogError("ShipHealth missing");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (shipHealth == null || !other.CompareTag("Asteroid")) 
            return;
        
        int sectionIndex = GetHitSectionIndex();
        if (sectionIndex != -1){

            Vector3 impactCoord = other.ClosestPoint(transform.position);
            if (sectionIndex == 0)
            {

                Vector3 directionToImpact = (impactCoord - transform.position).normalized;
                float forwardAlignment = Vector3.Dot(directionToImpact, transform.forward);

                if (forwardAlignment > 0.75f)
                {
                    StartCoroutine(AsteroidImpactAnimation(impactCoord));
                }
            }

            Destroy(other.gameObject);
            shipHealth.damageSection(ASTEROID_DMG, sectionIndex);
        }
        Debug.Log($"Collision @ Section: {sectionIndex}, Damage: {ASTEROID_DMG}");
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
    private int GetHitSectionIndex()
    {
        string colliderName = gameObject.name;

        if (colliderName.Contains("ForwardCollider")) return 0; 
        if (colliderName.Contains("PortCollider")) return 1;   
        if (colliderName.Contains("StarboardCollider")) return 2; 
        if (colliderName.Contains("AftCollider")) return 3;     

        Debug.LogWarning("Error - Looking for " + colliderName );
        return -1; 
    }
}
