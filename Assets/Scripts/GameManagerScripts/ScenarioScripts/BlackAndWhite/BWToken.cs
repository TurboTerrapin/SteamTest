/*
    BWShieldGenerator.cs
    - Used to control one of the four shield generators
    Contributor(s): Jake Schott
    Last Updated: 8/23/2026
*/

using Unity.Netcode;
using UnityEngine;

public class BWToken : NetworkBehaviour, IDamageable, ITractorBeamable
{
    private static float STARTING_HEALTH = 250.0f;

    public BlackAndWhite black_and_white;
    public Texture token_texture;

    private float token_health;

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }

        token_health = STARTING_HEALTH;
    }

    public bool getTractorBeamable()
    {
        return true;
    }

    public Texture getItemTexture()
    {
        return token_texture;
    }

    public Color getItemColor()
    {
        return new Color(0.2f, 0.2f, 0.2f);
    }

    public string getSerialNumber()
    {
        return black_and_white.getTokenSerialNumber();
    }

    public void damage(float damage, IDamageable.DamageType damage_type)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        if (token_health <= 0.0f)
        {
            return;
        }

        token_health = Mathf.Max(0.0f, token_health - damage);

        if (token_health <= 0.0f)
        {
            if (GetComponent<NetworkObject>() != null && GetComponent<NetworkObject>().IsSpawned == true)
            {
                GetComponent<NetworkObject>().Despawn(true);
            }
            ReferenceAssistor.Instance.effects_handler.createExplosion(transform.position, 10.0f, true, Color.gray);
            Destroy(this);
        }
    }
}
