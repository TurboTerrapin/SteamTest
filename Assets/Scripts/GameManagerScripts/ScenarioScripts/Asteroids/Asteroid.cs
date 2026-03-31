/*
    Asteroid.cs
    - Handles asteroid health
    - Handles adjusting map icon size
    Contributor(s): Jake Schott
    Last Updated: 3/24/2026
*/

using Unity.Netcode;
using UnityEngine;

public class Asteroid : MonoBehaviour, IDamageable
{
    //CLASS CONSTANTS
    private Color EXPLOSION_COLOR = new Color(0.4f, 0.4f, 0.3f);
    private float EXPLOSION_SIZE_FACTOR = 0.85f;
    private float ITEM_HEALTH_SIZE_FACTOR = 0.75f;

    private float item_health = 1.0f;

    private void Start()
    {
        //if not host, destroy collider/rigidbody and rely on Network Object to send transform updates through host
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(transform.GetComponent<Collider>());
        }

        GetComponent<MapItem>().setSize(Mathf.FloorToInt(transform.localScale.x * 2.5f));
        item_health = transform.localScale.x * ITEM_HEALTH_SIZE_FACTOR;
    }

    public void damage(float dam)
    {
        if (NetworkManager.Singleton.IsHost == false || item_health <= 0.0f)
        {
            return;
        }

        item_health = Mathf.Max(0.0f, item_health - dam);

        //handle destruction
        if (item_health <= 0.0f)
        {
            GameObject.Find("EffectsHandler").GetComponent<EffectsHandler>().createExplosion(transform.position, transform.localScale.x * EXPLOSION_SIZE_FACTOR, EXPLOSION_COLOR);
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}
