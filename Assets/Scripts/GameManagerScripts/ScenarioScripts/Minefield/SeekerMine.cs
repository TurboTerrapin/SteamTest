/*
    SeekerMine.cs
    Contributor(s): Henryk Musial
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class SeekerMine : Mine, IDamageable, ITorpedoTargetable
{
    private static float MOVE_SPEED = 35.0f;
    private static float ROTATION_SPEED = 2.0f;
    private static Color EXPLOSION_COLOR = new Color(1.0f, 0.0f, 0.0f);

    private void Start()
    {
        move_speed = MOVE_SPEED;
        rotation_speed = ROTATION_SPEED;
        explosion_color = EXPLOSION_COLOR;

        target_ship = ReferenceAssistor.Instance.spaceship.transform;
        mine_light_material = mine_light.GetComponent<Renderer>().material;
        mine_shield_material = new Material(mine_shield.GetComponent<Renderer>().material);
        mine_shield.GetComponent<Renderer>().material = mine_shield_material;
        mine_field = GameObject.FindGameObjectWithTag("ScenarioHandler").GetComponent<Minefield>();

        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }

        StartCoroutine(MineController());
    }

    IEnumerator MineController()
    {
        while (target_ship != null)
        {
            float distance_to_ship = Vector3.Distance(transform.position, target_ship.position);
            if (currently_disabled == false && currently_resetting == false && distance_to_ship <= mine_field.getMineDetectionRange() && ReferenceAssistor.Instance.scenario_manager.getGameOver() == false)
            {
                LookAtShip();
                MoveTowardShip();
            }

            yield return null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If touching the ship and not permanently disabled, detonate
        if (permanently_disabled == false && health > 0.0f && collision.gameObject.transform.parent.name.CompareTo("ShipColliders") == 0 && ReferenceAssistor.Instance.scenario_manager.getGameOver() == false)
        {
            Explode();
        }
    }
}