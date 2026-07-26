/*
    Asteroid.cs
    - Handles asteroid health
    - Handles adjusting map icon size
    Contributor(s): Jake Schott
    Last Updated: 7/6/2026
*/

using Unity.Netcode;
using UnityEngine;

public class Asteroid : MonoBehaviour, IDamageable, ITractorBeamable, ITorpedoTargetable, IPhaserTargetable
{
    //CLASS CONSTANTS
    private Color EXPLOSION_COLOR = new Color(0.4f, 0.4f, 0.3f);
    private float EXPLOSION_SIZE_FACTOR = 0.85f;
    private float ITEM_HEALTH_SIZE_FACTOR = 0.75f;

    public Texture tractor_beam_asteroid_texture;
    public Color tractor_beam_asteroid_color;

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

    public void damage(float damage, IDamageable.DamageType damage_type)
    {
        if (NetworkManager.Singleton.IsHost == false || item_health <= 0.0f)
        {
            return;
        }

        item_health = Mathf.Max(0.0f, item_health - damage);

        //handle destruction
        if (item_health <= 0.0f)
        {
            ReferenceAssistor.Instance.effects_handler.createExplosion(transform.position, transform.localScale.x * EXPLOSION_SIZE_FACTOR, false, EXPLOSION_COLOR);
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    public bool getTorpedoTargetable(IDamageable.DamageType torpedo_type)
    {
        return true;
    }

    public bool getPhaserTargetable(IDamageable.DamageType phaser_type) 
    { 
        return true; 
    }

    public bool getTractorBeamable()
    {
        return (transform.localScale.x < 4.5f);
    }

    public Texture getItemTexture()
    {
        return tractor_beam_asteroid_texture;
    }

    public Color getItemColor()
    {
        return tractor_beam_asteroid_color;
    }
}
