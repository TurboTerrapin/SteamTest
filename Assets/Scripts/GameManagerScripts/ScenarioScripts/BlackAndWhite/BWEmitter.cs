/*
    BWEmitter.cs
    - Used to control one of the six radiation emitters behind the wall
    Contributor(s): Jake Schott
    Last Updated: 7/6/2026
*/

using Unity.Netcode;
using UnityEngine;

public class BWEmitter : MonoBehaviour, IDamageable, ITorpedoTargetable, IPhaserTargetable
{
    private static float EMITTER_ROTATION_SPEED = 25.0f;
    private static float RADIATION_ROTATION_SPEED = 150.0f;

    public BlackAndWhite black_and_white;

    private float emitter_health = 1.0f;

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }
        transform.Rotate(0.0f, 0.0f, Random.Range(0.0f, 180.0f));
        transform.GetChild(0).Rotate(0.0f, 0.0f, Random.Range(0.0f, 180.0f));
    }

    private void Update()
    {
        transform.Rotate(0.0f, 0.0f, Time.deltaTime * EMITTER_ROTATION_SPEED);
        transform.GetChild(0).Rotate(0.0f, 0.0f, Time.deltaTime * RADIATION_ROTATION_SPEED);
    }

    public void damage(float damage, IDamageable.DamageType damage_type)
    {
        if (NetworkManager.Singleton.IsHost == false || emitter_health <= 0.0f)
        {
            return;
        }

        emitter_health = Mathf.Max(0.0f, emitter_health - damage);

        //handle destruction
        if (emitter_health <= 0.0f)
        {
            ReferenceAssistor.Instance.effects_handler.createExplosion(transform.position, 15.0f, true, Color.gray);
            black_and_white.onEmitterDestroyed(gameObject);
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
}
