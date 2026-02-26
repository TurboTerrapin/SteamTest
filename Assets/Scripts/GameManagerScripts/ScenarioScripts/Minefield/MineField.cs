/*
    MineField.cs
    - Spawns a bunch of mines that do nothing
    Contributor(s): Henryk Musial
    Last Updated: 2/25/2026
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MineField : NetworkBehaviour, IScenario
{
    //CLASS CONSTANTS
    private static int MINE_QUANTITY = 100;
    private static string DEATH_MESSAGE = "Encompassed in a large field of glowing orange orbs, stolen ship NCC-3002 was found adrift with no obvious sign of distress. Investigations will continue to identify possible causes of failure.";

    public GameObject mine;

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
        for (int i = 0; i < MINE_QUANTITY; i++)
        {
            GameObject curr_mine = GameObject.Instantiate(mine, world_root);
            curr_mine.name = "Mine_" + i;
            curr_mine.GetComponent<NetworkObject>().SynchronizeTransform = true;
            Vector3 spawn_location = getRandomSpawnLocation() + world_root_center;
            curr_mine.transform.localPosition = spawn_location;

            curr_mine.transform.localRotation = Random.rotation; // Applies random rotation to mines

            curr_mine.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
            curr_mine.GetComponent<NetworkObject>().TrySetParent(world_root);
        }
    }

    public string getDeathMessage()
    {
        return DEATH_MESSAGE;
    }
}
