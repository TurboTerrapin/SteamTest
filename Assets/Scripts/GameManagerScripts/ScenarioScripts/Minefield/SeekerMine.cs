/*
    SeekerMine.cs
    Contributor(s): Henryk Musial
*/

using Unity.Netcode;
using UnityEngine;

public class SeekerMine : NetworkBehaviour
{
    private float detection_range = 1000.0f;
    private float move_speed = 12.0f;
    private float rotation_speed = 1.5f;

    private Transform target_ship;

    void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(transform.GetComponent<Collider>());
        }

        GameObject spaceship_obj = GameObject.FindGameObjectWithTag("Spaceship");
        if (spaceship_obj != null)
        {
            target_ship = spaceship_obj.transform;
        }
    }

    void Update()
    {
        if (!IsHost || target_ship == null)
        {
            return;
        }

        float distance_to_ship = Vector3.Distance(transform.position, target_ship.position);

        if (distance_to_ship <= detection_range)
        {
            LookAtShip();
            MoveTowardShip();
        }
    }

    private void LookAtShip()
    {
        // Determine direction to ship
        Vector3 target_direction = (target_ship.position - transform.position).normalized;

        // face the ship over time
        Quaternion target_rotation = Quaternion.LookRotation(target_direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, target_rotation, Time.deltaTime * rotation_speed);

    }

    private void MoveTowardShip()
    {
        // move forward based on the current rotation
        //transform.position += transform.forward * move_speed * Time.deltaTime;
    }
}