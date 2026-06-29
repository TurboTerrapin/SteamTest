/*
    SeekerMine.cs
    Contributor(s): Henryk Musial
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class SeekerMine : NetworkBehaviour, IDamageable
{
    private static float MOVE_SPEED = 35.0f;
    private static float ROTATION_SPEED = 2.0f;

    public AudioSource shield_sound;
    public GameObject mine_shield;
    private Material mine_shield_material;

    private Transform target_ship;
    private Minefield mine_field;

    private Coroutine shield_change_coroutine = null;
    private float health = 5.0f;

    private void Start()
    {
        target_ship = ReferenceAssistor.Instance.spaceship.transform;
        mine_shield_material = new Material(mine_shield.GetComponent<Renderer>().material);
        mine_shield.GetComponent<Renderer>().material = mine_shield_material;
        mine_field = GameObject.FindGameObjectWithTag("ScenarioHandler").GetComponent<Minefield>();

        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }

        StartCoroutine(MineController());
    }

    IEnumerator ShieldFlash()
    {
        Color shield_color = ReferenceAssistor.COLOR_OPTIONS[0];
        mine_shield.SetActive(true);
        shield_sound.Play();

        float anim_time = 1.0f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            float animation_progress = Mathf.PingPong(anim_time, 0.5f) / 0.5f;
            mine_shield_material.SetColor("_EmissionColor", Color.Lerp(Color.black, new Color(0.0f, 0.1f, 0.2f), animation_progress));
            shield_color.a = Mathf.Lerp(0.0f, 0.5f, animation_progress);
            mine_shield_material.color = shield_color;

            yield return null;
        }

        shield_change_coroutine = null;
    }

    IEnumerator MineController()
    {
        while (target_ship != null)
        {
            float distance_to_ship = Vector3.Distance(transform.position, target_ship.position);
            if (distance_to_ship <= mine_field.getMineDetectionRange() && ReferenceAssistor.Instance.scenario_manager.getGameOver() == false)
            {
                LookAtShip();
                MoveTowardShip();
            }

            yield return null;
        }
    }

    private void LookAtShip()
    {
        // Determine direction to ship
        Vector3 target_direction = (target_ship.position - transform.position).normalized;

        // Face the ship over time
        Quaternion target_rotation = Quaternion.LookRotation(target_direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, target_rotation, Time.deltaTime * ROTATION_SPEED);
    }

    private void MoveTowardShip()
    {
        // Move forward based on the current rotation
        transform.position += transform.forward * MOVE_SPEED * Time.deltaTime;
    }

    public void damage(float damage, IDamageable.DamageType damage_type)
    {
        if (mine_field.damageTypeBypassesMineShields(damage_type) == false || health < 0.0f)
        {
            ShieldFlashRPC();
            return;
        }
        health -= damage;
        if (health <= 0.0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (GetComponent<NetworkObject>() != null && GetComponent<NetworkObject>().IsSpawned == true)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
        ReferenceAssistor.Instance.effects_handler.createExplosion(transform.position, 75.0f, Color.red);
        Destroy(this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If touching the ship, detonate
        if (health > 0.0f && collision.gameObject.transform.parent.name.CompareTo("ShipColliders") == 0 && ReferenceAssistor.Instance.scenario_manager.getGameOver() == false)
        {
            Explode();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ShieldFlashRPC()
    {
        if (shield_change_coroutine == null)
        {
            shield_change_coroutine = StartCoroutine(ShieldFlash());
        }
    }

}