/*
    NormalMine.cs
    Contributor(s): Henryk Musial
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class NormalMine : NetworkBehaviour, IDamageable
{
    private static float ROTATION_SPEED = 0.5f;
    private static float FIRE_TIME = 1.0f;
    private static float PHASER_DAMAGE = 5.0f; // How much damage each phaser hit does to ship
    private static float PHASER_COOLDOWN_TIME = 3.0f; // Time in seconds between firing

    public AudioSource phaser_sound;
    public AudioSource shield_sound;
    public Transform phaser_origin;
    public GameObject mine_shield;
    public LineRenderer line_renderer;
    private Material mine_shield_material;

    private Transform target_ship;
    private Minefield mine_field;
    private GameObject current_target;

    private Coroutine shield_change_coroutine = null;
    private Coroutine phaser_fire_coroutine = null;
    private float health = 5.0f;

    private void Start()
    {
        target_ship = ReferenceAssistor.Instance.spaceship.transform;
        mine_shield_material = new Material(mine_shield.GetComponent<Renderer>().material);
        mine_shield.GetComponent<Renderer>().material = mine_shield_material;
        mine_field = GameObject.FindGameObjectWithTag("ScenarioHandler").GetComponent<Minefield>();

        // If not the host, strip the collider so clients don't calculate local physics
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }
        else
        {
            StartCoroutine(MineController());
        }
    }

    private void OnDisable()
    {
        Destroy(mine_shield_material);
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
            if (GetComponent<NetworkObject>() != null && GetComponent<NetworkObject>().IsSpawned == true)
            {
                GetComponent<NetworkObject>().Despawn(true);
            }
            ReferenceAssistor.Instance.effects_handler.createExplosion(transform.position, 75.0f, ReferenceAssistor.COLOR_OPTIONS[2]);
            Destroy(this);
        }
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

            if (phaser_fire_coroutine == null && distance_to_ship <= mine_field.getMineDetectionRange() && ReferenceAssistor.Instance.scenario_manager.getGameOver() == false)
            {
                // Check for collision point (and if hit something that isn't our target on the way there, then set the target to null to stop damage)
                if (Physics.Raycast(new Ray(phaser_origin.transform.position, phaser_origin.transform.forward), out RaycastHit hit, Minefield.DETECTION_RANGE, LayerMask.GetMask("ShipColliders")))
                {
                    Vector3 beam_end = hit.point;
                    current_target = hit.collider.gameObject;
                    // Check if hit ship
                    if (current_target.GetComponent<ShipCollider>() != null)
                    {
                        FirePhaserRPC(beam_end);
                    }
                }
            }

            if (distance_to_ship <= mine_field.getMineDetectionRange())
            {
                LookAtShip();
            }

            yield return null;
        }
    }

    IEnumerator PhaserFire(Vector3 beam_end)
    {
        line_renderer.enabled = true;

        // Give warning
        ReferenceAssistor.Instance.module_handlers[1].GetComponent<ThreatDetectors>().adjustPhaserWarningTime(FIRE_TIME + 1.2f);

        // Slight delay
        yield return new WaitForSeconds(0.5f);

        // Apply damage if host
        if (NetworkManager.Singleton.IsHost == true && current_target != null)
        {
            current_target.GetComponent<ShipCollider>().damage(PHASER_DAMAGE, IDamageable.DamageType.EnemyPhaser);
        }

        line_renderer.SetPosition(1, beam_end);
        phaser_sound.pitch = 1.25f;
        phaser_sound.Play();

        // Play animation
        float activeTime = FIRE_TIME;
        float activeHalftime = activeTime * 0.5f;
        float timeRemaining = activeTime;
        while (timeRemaining > 0.0f)
        {
            timeRemaining = Mathf.Max(0.0f, timeRemaining - Time.deltaTime);

            float beamWidth = Mathf.Lerp(0.0f, 22.5f, Mathf.Lerp(0.0f, 1.0f, Mathf.PingPong(timeRemaining, activeHalftime) / activeHalftime));
            line_renderer.startWidth = beamWidth;
            line_renderer.endWidth = beamWidth;
            line_renderer.SetPosition(0, phaser_origin.transform.position);

            yield return null;
        }

        // Disable phaser
        line_renderer.enabled = false;

        // Cooldown
        yield return new WaitForSeconds(PHASER_COOLDOWN_TIME);

        phaser_fire_coroutine = null;
    }

    private void LookAtShip()
    {
        // Determine direction to ship
        Vector3 target_direction = (target_ship.position - transform.position).normalized;

        Quaternion target_rotation = Quaternion.LookRotation(target_direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, target_rotation, Time.deltaTime * ROTATION_SPEED);
    }

    [Rpc(SendTo.Everyone)]
    private void FirePhaserRPC(Vector3 beam_end)
    {
        if (phaser_fire_coroutine != null)
        {
            StopCoroutine(phaser_fire_coroutine);
        }

        phaser_fire_coroutine = StartCoroutine(PhaserFire(beam_end));
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