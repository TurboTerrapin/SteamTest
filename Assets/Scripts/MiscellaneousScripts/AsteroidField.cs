using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AsteroidField : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static int ASTEROID_QUANTITY = 100;
    private static string DEATH_MESSAGE = "Encompassed in a large field of asteroids, stolen ship NCC-3002 was found adrift with no obvious sign of distress. Investigations will continue to identify possible causes of failure.";

    public GameObject asteroid;
    public Transform visual_spectacle;

    private Vector3 world_root_center;
    private List<Vector3> spawned_locations = new List<Vector3>();

    private float vs_radius = 600f;
    private float vs_height = 1200f;

    private Vector3 getRandomSpawnLocation()
    {
        bool found_valid_location = false;
        Vector3 valid_location = Vector3.zero;

        while (found_valid_location == false)
        {
            Vector2 x_and_z = Random.insideUnitCircle * (ScenarioManager.BOUNDARY_SIZE * 0.5f);

            float x_coordinate = x_and_z.x;
            float y_coordinate = Random.Range(-(ScenarioManager.BOUNDARY_ALTITUDE + 20.0f), ScenarioManager.BOUNDARY_ALTITUDE + 20.0f);
            float z_coordinate = x_and_z.y;

            valid_location =
                new Vector3(x_coordinate, y_coordinate, z_coordinate);

            if (isInsideSpectacle(valid_location)) 
            {
                    continue;
            }

            found_valid_location = true;

            foreach (Vector3 existing_location in spawned_locations)
            {
                if (Vector3.Distance(existing_location, valid_location) < 40.0f)
                {
                    found_valid_location = false;
                    break;
                }
            }
        }

        spawned_locations.Add(valid_location);

        return valid_location;
         }

    
    private bool isInsideSpectacle(Vector3 spawn_position)
    {
        Vector3 vs_pos = visual_spectacle.localPosition - world_root_center;

        float vs_half_height = vs_height / 2f;
        float cylinder_height = vs_half_height - vs_radius;

        Vector3 local = spawn_position - vs_pos;

        float clampedY = Mathf.Clamp(local.y, -cylinder_height, cylinder_height);
        Vector3 closestPoint = new Vector3(0, clampedY, 0);

        float distance = Vector3.Distance(local, closestPoint);

        return distance <= vs_radius;
    }

    //only run by the host
    public void initiateScenario()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        world_root_center = new Vector3(0.0f, 0.0f, ScenarioManager.BOUNDARY_SIZE * 0.5f);

        Transform world_root = GameObject.FindGameObjectWithTag("WorldRoot").transform;
        for (int i = 0; i < ASTEROID_QUANTITY; i++)
        {
            GameObject curr_asteroid = GameObject.Instantiate(asteroid, world_root);
            //Debug.Log("asteroid # = " + i);

            float scale = Random.Range(800f, 3000f);
            curr_asteroid.transform.localScale = Vector3.one * scale;

            float rotation = Random.Range(0f, 360f);
            curr_asteroid.transform.localRotation = Random.rotation;

            curr_asteroid.GetComponent<NetworkObject>().SynchronizeTransform = true;
            Vector3 spawn_location = getRandomSpawnLocation() + world_root_center;
            curr_asteroid.transform.localPosition = spawn_location;
            curr_asteroid.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
            curr_asteroid.GetComponent<NetworkObject>().TrySetParent(world_root);
        }
    }

    public string getDeathMessage()
    {
        return DEATH_MESSAGE;
    }
}
       