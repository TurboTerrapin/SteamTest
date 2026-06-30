/*
    Mine.cs
    Contributor(s): Henryk Musial
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class Mine : NetworkBehaviour, IDamageable, ITorpedoTargetable
{
    protected static float DISABLED_BLINK_INTERVAL = 0.2f;

    public AudioSource shield_sound;
    public GameObject mine_shield;
    public GameObject mine_light;
    protected Material mine_light_material;
    protected Material mine_shield_material;

    protected Transform target_ship;
    protected Minefield mine_field;

    protected bool disabled = false;
    protected bool permanently_disabled = false;
    protected Color explosion_color;
    protected float move_speed;
    protected float rotation_speed;
    protected float health = 5.0f;
    protected Coroutine disable_flash_coroutine = null;
    protected Coroutine shield_change_coroutine = null;

    protected IEnumerator ShieldFlash(Color c)
    {
        Color shield_color = ReferenceAssistor.COLOR_OPTIONS[0];
        mine_shield.SetActive(true);
        shield_sound.Play();

        float anim_time = 1.0f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            float animation_progress = Mathf.PingPong(anim_time, 0.5f) / 0.5f;
            mine_shield_material.SetColor("_EmissionColor", Color.Lerp(Color.black, c, animation_progress));
            shield_color.a = Mathf.Lerp(0.0f, 0.5f, animation_progress);
            mine_shield_material.color = shield_color;

            yield return null;
        }

        shield_change_coroutine = null;
    }

    protected IEnumerator DisabledFlash()
    {
        Material active_material = mine_light.GetComponent<Renderer>().material;
        while (true)
        {
            mine_light.transform.GetChild(0).gameObject.SetActive(false);
            mine_light.GetComponent<Renderer>().material = ReferenceAssistor.Instance.pure_black;
            yield return new WaitForSeconds(DISABLED_BLINK_INTERVAL);
            mine_light.transform.GetChild(0).gameObject.SetActive(true);
            mine_light.GetComponent<Renderer>().material = active_material;
            yield return new WaitForSeconds(DISABLED_BLINK_INTERVAL);
        }
    }

    protected void LookAtShip()
    {
        // Determine direction to ship
        Vector3 target_direction = (target_ship.position - transform.position).normalized;

        // Face the ship over time
        Quaternion target_rotation = Quaternion.LookRotation(target_direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, target_rotation, Time.deltaTime * rotation_speed);
    }

    protected void MoveTowardShip()
    {
        // Move forward based on the current rotation
        transform.position += transform.forward * move_speed * Time.deltaTime;
    }

    public void damage(float damage, IDamageable.DamageType damage_type)
    {
        // Check if need to flash shields and abort damage
        if (permanently_disabled == false && mine_field.damageTypeBypassesMineShields(damage_type) == false)
        {
            ShieldFlashRPC();
            return;
        }

        // If hit by ion torpedo, permanently disable the mine 
        if (damage_type == IDamageable.DamageType.IonTorpedo)
        {
            if (permanently_disabled == false)
            {
                PermanentlyDisableRPC();
            }
            return;
        }

        // Else, damage the mine
        if (health > 0.0f)
        {
            health -= damage;
            if (health <= 0.0f)
            {
                Explode();
            }
        }
    }

    public bool getTorpedoTargetable(IDamageable.DamageType torpedo_type)
    {
        return (mine_field.torpedoTracksMine(torpedo_type) && !(torpedo_type != IDamageable.DamageType.IonTorpedo && permanently_disabled == true));
    }

    public void UpdateEnabledStatus(bool enabled)
    {
        // Disable or not
        if (permanently_disabled == false)
        {
            disabled = !enabled;
        }
        else
        {
            disabled = true;
        }

        // Stop flashing, turn back to default
        if (enabled == true && permanently_disabled == false)
        {
            if (disable_flash_coroutine != null)
            {
                StopCoroutine(disable_flash_coroutine);
                disable_flash_coroutine = null;
            }
            mine_light.GetComponent<Renderer>().material = mine_light_material;
            mine_light.transform.GetChild(0).gameObject.SetActive(true);
            return;
        }

        // Turn green and reset if permanently disabled
        if (permanently_disabled == true)
        {
            if (disable_flash_coroutine != null)
            {
                StopCoroutine(disable_flash_coroutine);
                disable_flash_coroutine = null;
            }
            mine_light.GetComponent<Renderer>().material = mine_field.mineLitGreen;
            mine_light.transform.GetChild(0).GetComponent<Light>().color = ReferenceAssistor.COLOR_OPTIONS[3];
        }

        // Start flash animation
        if (disable_flash_coroutine == null)
        {
            disable_flash_coroutine = StartCoroutine(DisabledFlash());
        }
    }

    protected void Explode()
    {
        if (GetComponent<NetworkObject>() != null && GetComponent<NetworkObject>().IsSpawned == true)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
        ReferenceAssistor.Instance.effects_handler.createExplosion(transform.position, 75.0f, false, explosion_color);
        Destroy(this);
    }

    [Rpc(SendTo.Everyone)]
    protected void ShieldFlashRPC()
    {
        if (shield_change_coroutine == null)
        {
            shield_change_coroutine = StartCoroutine(ShieldFlash(new Color(0.0f, 0.1f, 0.2f)));
        }
    }

    [Rpc(SendTo.Everyone)]
    protected void PermanentlyDisableRPC()
    {
        permanently_disabled = true;
        UpdateEnabledStatus(false);
        if (shield_change_coroutine != null)
        {
            StopCoroutine(shield_change_coroutine);
        }
        shield_change_coroutine = StartCoroutine(ShieldFlash(new Color(0.0f, 0.15f, 0.0f)));
    }
}