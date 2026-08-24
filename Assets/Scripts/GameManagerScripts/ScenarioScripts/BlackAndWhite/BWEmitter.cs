/*
    BWEmitter.cs
    - Used to control one of the six radiation emitters behind the wall
    Contributor(s): Jake Schott
    Last Updated: 8/23/2026
*/

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BWEmitter : MonoBehaviour, IDamageable, ITorpedoTargetable, IPhaserTargetable
{
    private static float EMITTER_ROTATION_SPEED = 25.0f;
    private static float RADIATION_ROTATION_SPEED = 150.0f;

    public BlackAndWhite black_and_white;

    private float emitter_health = 1.0f;
    private bool protected_by_shields = true;
    private Coroutine spin_coroutine = null;
    private Coroutine flash_coroutine = null;

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }
        transform.Rotate(0.0f, 0.0f, Random.Range(0.0f, 180.0f));
        transform.GetChild(0).Rotate(0.0f, 0.0f, Random.Range(0.0f, 180.0f));
        spin_coroutine = StartCoroutine(emitterSpin());
    }

    IEnumerator emitterSpin()
    {
        while (true)
        {
            transform.Rotate(0.0f, 0.0f, Time.deltaTime * EMITTER_ROTATION_SPEED);
            transform.GetChild(0).Rotate(0.0f, 0.0f, Time.deltaTime * RADIATION_ROTATION_SPEED);

            yield return null;
        }
    }

    IEnumerator emitterFlash()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Material[] flash_materials = renderer.materials;
        while (true)
        {
            flash_materials[0] = ReferenceAssistor.Instance.lit_white;
            renderer.materials = flash_materials;
            yield return new WaitForSeconds(0.25f);
            flash_materials[0] = ReferenceAssistor.Instance.pure_black;
            renderer.materials = flash_materials;
            yield return new WaitForSeconds(0.25f);
        }
    }

    //only flashes if ship collects token then broadcasts token's serial number
    public void enableFlash()
    {
        //stop spinning
        if (spin_coroutine != null)
        {
            StopCoroutine(spin_coroutine);
            spin_coroutine = null;
        }

        //start flashing
        if (flash_coroutine == null)
        {
            flash_coroutine = StartCoroutine(emitterFlash());
        }
    }

    public void damage(float damage, IDamageable.DamageType damage_type)
    {
        if (NetworkManager.Singleton.IsHost == false || emitter_health <= 0.0f)
        {
            return;
        }

        if (protected_by_shields == true && damage_type != IDamageable.DamageType.Explosive)
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

    public void onProtectiveShieldsDiabled()
    {
        protected_by_shields = false;
    }

    public bool getTorpedoTargetable(IDamageable.DamageType torpedo_type)
    {
        return protected_by_shields == false;
    }

    public bool getPhaserTargetable(IDamageable.DamageType phaser_type)
    {
        return protected_by_shields == false;
    }
}
