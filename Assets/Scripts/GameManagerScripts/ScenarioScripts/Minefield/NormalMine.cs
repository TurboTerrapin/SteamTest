/*
    NormalMine.cs
    Contributor(s): Henryk Musial
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class NormalMine : Mine, IDamageable, ITorpedoTargetable
{
    private static float ROTATION_SPEED = 0.5f;
    private static Color EXPLOSION_COLOR = ReferenceAssistor.COLOR_OPTIONS[2];
    private static float FIRE_TIME = 1.0f;
    private static float PHASER_DAMAGE = 5.0f; // How much damage each phaser hit does to ship
    private static float PHASER_COOLDOWN_TIME = 3.0f; // Time in seconds between firing

    public AudioSource phaser_sound;
    public Transform phaser_origin;
    public LineRenderer line_renderer;

    private GameObject current_target;
    private Coroutine phaser_fire_coroutine = null;

    private void Start()
    {
        rotation_speed = ROTATION_SPEED;
        explosion_color = EXPLOSION_COLOR;

        target_ship = ReferenceAssistor.Instance.spaceship.transform;
        mine_light_material = mine_light.GetComponent<Renderer>().material;
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

    IEnumerator MineController()
    {
        while (target_ship != null)
        {
            float distance_to_ship = Vector3.Distance(transform.position, target_ship.position);

            if (permanently_disabled == false && currently_resetting == false && phaser_fire_coroutine == null && distance_to_ship <= mine_field.getMineDetectionRange() && ReferenceAssistor.Instance.scenario_manager.getGameOver() == false)
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

            if (currently_disabled == false && currently_resetting == false && distance_to_ship <= mine_field.getMineDetectionRange())
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

    [Rpc(SendTo.Everyone)]
    private void FirePhaserRPC(Vector3 beam_end)
    {
        if (phaser_fire_coroutine != null)
        {
            StopCoroutine(phaser_fire_coroutine);
        }

        phaser_fire_coroutine = StartCoroutine(PhaserFire(beam_end));
    }
}