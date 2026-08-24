/*
    BWShieldGenerator.cs
    - Used to control one of the four shield generators
    Contributor(s): Jake Schott
    Last Updated: 8/23/2026
*/

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BWShieldGenerator : NetworkBehaviour, IDamageable, IPhaserTargetable, ITorpedoTargetable
{
    private static float GENERATOR_ROTATION_SPEED = 75.0f;
    private static float STARTING_HEALTH = 125.0f;

    public BlackAndWhite black_and_white;

    [SerializeField]
    private float rotation_direction = 1.0f;
    private float generator_health;
    private Coroutine spin_coroutine = null;
    private Coroutine disabled_coroutine = null;

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }

        generator_health = STARTING_HEALTH;
        spin_coroutine = StartCoroutine(generatorSpin());
    }

    IEnumerator generatorSpin()
    {
        while (true)
        {
            transform.Rotate(0.0f, 0.0f, Time.deltaTime * GENERATOR_ROTATION_SPEED * rotation_direction);

            yield return null;
        }
    }

    IEnumerator deactivatedAnimation()
    {
        //play sound
        GetComponent<AudioSource>().Play();

        //flicker lights
        for (int i = 0; i < 16; i++)
        {
            foreach (Transform t in transform)
            {
                t.GetComponent<MeshRenderer>().material = ReferenceAssistor.Instance.lit_neon;
            }

            yield return new WaitForSeconds(0.05f);

            foreach (Transform t in transform)
            {
                t.GetComponent<MeshRenderer>().material = ReferenceAssistor.Instance.pure_black;
            }

            yield return new WaitForSeconds(0.05f);
        }

        //stop spinning
        if (spin_coroutine != null)
        {
            StopCoroutine(spin_coroutine);
            spin_coroutine = null;
        }

        disabled_coroutine = null;
    }

    public void damage(float damage, IDamageable.DamageType damage_type)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        if (generator_health <= 0.0f && disabled_coroutine == null)
        {
            if (GetComponent<NetworkObject>() != null && GetComponent<NetworkObject>().IsSpawned == true)
            {
                GetComponent<NetworkObject>().Despawn(true);
            }
            ReferenceAssistor.Instance.effects_handler.createExplosion(transform.position, 5.0f, true, ReferenceAssistor.COLOR_OPTIONS[0]);
            Destroy(this);
            return;
        }
        else if (generator_health <= 0.0f && disabled_coroutine != null)
        {
            return;
        }

        generator_health = Mathf.Max(0.0f, generator_health - damage);

        //handle disabling
        if (generator_health <= 0.0f)
        {
            black_and_white.onShieldGeneratorDisabled(gameObject);
        }
        generatorHealthUpdateRPC(generator_health);
    }

    public bool getPhaserTargetable(IDamageable.DamageType damage_type)
    {
        return true;
    }

    public bool getTorpedoTargetable(IDamageable.DamageType damage_type)
    {
        return true;
    }

    [Rpc(SendTo.Everyone)]
    private void generatorHealthUpdateRPC(float new_health)
    {
        generator_health = new_health;

        //update health circles
        for (int i = 0; i < 3; i++)
        {
            if (generator_health <= (STARTING_HEALTH * (i / 3.0f)))
            {
                transform.GetChild(2 - i).GetComponent<MeshRenderer>().material = ReferenceAssistor.Instance.pure_black;
            }
        }

        //play deactivation animation
        if (generator_health <= 0.0f && spin_coroutine != null)
        {
            disabled_coroutine = StartCoroutine(deactivatedAnimation());
        }
    }
}