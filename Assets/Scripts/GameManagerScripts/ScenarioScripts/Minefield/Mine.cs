/*
    Mine.cs
    Contributor(s): Henryk Musial
*/


/*
using Unity.Netcode;
using UnityEngine;

public class Mine : NetworkBehaviour
{
    private float ROTATION_SPEED = 1.5f;
    private float DETONATION_RANGE = 100.0f;
    private float DETECTION_RANGE = 1000.0f;

    private Transform target_ship;


    void Start()
    {
        // If not the host, strip the collider so clients don't calculate local physics
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(transform.GetComponent<Collider>());
        }

        // Find & ref the spaceship in scene
        GameObject spaceship_obj = GameObject.FindGameObjectWithTag("Spaceship");
        if (spaceship_obj != null)
        {
            target_ship = spaceship_obj.transform;
        }
    }

    void Update()
    {
        // Movement and rotation logic should only run on the Host
        if (!IsHost || target_ship == null)
        {
            return;
        }

        float distance_to_ship = Vector3.Distance(transform.position, target_ship.position);

        if (distance_to_ship <= DETECTION_RANGE)
        {
            float dynamic_detection_range = CalculateDetectionRange();

            if(distance_to_ship < dynamic_detection_range)
            {
                LookAtShip();
            }
        }
    }

    private float CalculateDetectionRange()
    {
        EmissionReducers reducers = ReferenceAssistor.Instance.module_handlers[0].GetComponent<EmissionReducers>();
        if (reducers == null)
        {
            return DETECTION_RANGE;
        }
        int active_count = 0;

        // Check port and starboard reducers
        if (reducers.enabled_reducers[0]) active_count++;
        if (reducers.enabled_reducers[1]) active_count++;

        if(active_count == 0)
        {
            return DETECTION_RANGE;
        }
        else
        {
            return DETECTION_RANGE - (active_count * 200.0f);
        }
    }

    private void LookAtShip()
    {
        // Determine direction to ship
        Vector3 target_direction = (target_ship.position - transform.position).normalized;

        Quaternion target_rotation = Quaternion.LookRotation(target_direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, target_rotation, Time.deltaTime * ROTATION_SPEED);

    }

    private void Detonate()
    {

    }
}

*/

/*
    Mine.cs
    Contributor(s): Henryk Musial
*/

using Unity.Netcode;
using UnityEngine;

public class Mine : NetworkBehaviour
{
    private float ROTATION_SPEED = 1.5f;
    private float DETONATION_RANGE = 100.0f;
    private float LASER_RANGE = 500.0f; // Distance threshold to fire the laser
    private float DETECTION_RANGE = 1000.0f;

    public Transform laser_aperture;
    public LineRenderer line_renderer;

    private Transform target_ship;

    void Start()
    {
        // If not the host, strip the collider so clients don't calculate local physics
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(transform.GetComponent<Collider>());
        }

        // Find & ref the spaceship in scene
        GameObject spaceship_obj = GameObject.FindGameObjectWithTag("Spaceship");
        if (spaceship_obj != null)
        {
            target_ship = spaceship_obj.transform;
        }

        // Initialize the laser to off
        if (line_renderer != null)
        {
            line_renderer.positionCount = 2;
            line_renderer.enabled = false;
        }
    }

    void Update()
    {
        if (target_ship == null)
        {
            return;
        }

        float distance_to_ship = Vector3.Distance(transform.position, target_ship.position);

        // Visual logic (Laser) runs on Host AND Clients so everyone sees it
        if (distance_to_ship <= LASER_RANGE)
        {
            //FireLaser();
        }
        else
        {
            //StopLaser();
        }

        if (!IsHost)
        {
            return;
        }

        if (distance_to_ship <= DETECTION_RANGE)
        {
            float dynamic_detection_range = CalculateDetectionRange();

            if (distance_to_ship < dynamic_detection_range)
            {
                LookAtShip();
            }
        }
    }

    private float CalculateDetectionRange()
    {
        EmissionReducers reducers = ReferenceAssistor.Instance.module_handlers[0].GetComponent<EmissionReducers>();
        if (reducers == null)
        {
            return DETECTION_RANGE;
        }
        int active_count = 0;

        // Check port and starboard reducers
        if (reducers.enabled_reducers[0]) active_count++;
        if (reducers.enabled_reducers[1]) active_count++;

        if (active_count == 0)
        {
            return DETECTION_RANGE;
        }
        else
        {
            return DETECTION_RANGE - (active_count * 200.0f);
        }
    }

    private void LookAtShip()
    {
        // Determine direction to ship
        Vector3 target_direction = (target_ship.position - transform.position).normalized;

        Quaternion target_rotation = Quaternion.LookRotation(target_direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, target_rotation, Time.deltaTime * ROTATION_SPEED);

    }

    private void FireLaser()
    {
        if (line_renderer == null || laser_aperture == null) return;

        if (!line_renderer.enabled) line_renderer.enabled = true;

        // Update beam positions
        line_renderer.SetPosition(0, laser_aperture.position);
        line_renderer.SetPosition(1, target_ship.position);
    }

    private void StopLaser()
    {
        if (line_renderer != null && line_renderer.enabled)
        {
            line_renderer.enabled = false;
        }
    }

    private void Detonate()
    {

    }
}