/*
    CollectibleTest.cs
    - Spawns a bunch of collectible items
    Contributor(s): Jake Schott
    Last Updated: 12/9/2025
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CollectibleTest : NetworkBehaviour, IScenario
{
    //CLASS CONSTANTS
    private static int COLLECTIBLE_QUANTITY = 50;
    private static string DEATH_MESSAGE = "How did you die to a bunch of collectibles?";

    public List<GameObject> possible_collectibles;

    private Vector3 world_root_center;
    private List<Vector3> spawned_locations = new List<Vector3>();

    private Vector3 getRandomSpawnLocation()
    {
        bool found_valid_location = false;
        Vector3 valid_location = Vector3.zero;
        while (found_valid_location == false)
        {
            Vector2 x_and_z = Random.insideUnitCircle * (ScenarioManager.BOUNDARY_SIZE * 0.5f);
            float x_coordinate = x_and_z.x;
            float y_coordinate = Random.Range(-(ScenarioManager.BOUNDARY_ALTITUDE), ScenarioManager.BOUNDARY_ALTITUDE);
            float z_coordinate = x_and_z.y;
            valid_location =
                new Vector3(x_coordinate, y_coordinate, z_coordinate);

            found_valid_location = true;
            foreach (Vector3 existing_location in spawned_locations)
            {
                if (Vector3.Distance(existing_location, valid_location) < 50.0f)
                {
                    found_valid_location = false;
                    break;
                }
            }
        }

        spawned_locations.Add(valid_location);

        return valid_location;
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
        for (int i = 0; i < COLLECTIBLE_QUANTITY; i++)
        {
            GameObject curr_collectible = GameObject.Instantiate(possible_collectibles[Random.Range(0, possible_collectibles.Count)], world_root);
            curr_collectible.GetComponent<NetworkObject>().SynchronizeTransform = true;
            Vector3 spawn_location = getRandomSpawnLocation() + world_root_center;
            curr_collectible.transform.localPosition = spawn_location;
            curr_collectible.transform.localRotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
            curr_collectible.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
            curr_collectible.GetComponent<NetworkObject>().TrySetParent(world_root);
        }
    }

    public string getDeathMessage()
    {
        return DEATH_MESSAGE;
    }
}
