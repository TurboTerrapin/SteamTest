/*
    MineField.cs
    Contributor(s): Henryk Musial
    Last Updated: 3/26/2026
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MineField : NetworkBehaviour, IScenario
{
    //CLASS CONSTANTS
    private static int MINE_QUANTITY = 30;
    private static int SEEKER_MINE_QUANTITY = 20;
    private static string DEATH_MESSAGE = "You died buddy get better at this game";

    public GameObject mine;
    public GameObject seekerMine;

    //only run by the host
    public void initiateScenario()
    {
        if (NetworkManager.Singleton.IsHost == false)
            return;

        // Calculate cylinder dimensions
        float radius = ScenarioManager.BOUNDARY_SIZE * 0.5f;   // 2500
        float height = ScenarioManager.BOUNDARY_ALTITUDE * 2f; // 200
        float minDistance = 50.0f; 

        int totalMines = MINE_QUANTITY + SEEKER_MINE_QUANTITY;

        // Generate spawn points
        List<Vector3> positions = SpawnPointGenerator.GenerateSpawnLocations(radius, height, minDistance, totalMines);

        Transform world_root = GameObject.FindGameObjectWithTag("WorldRoot").transform;
        Vector3 world_root_center = new Vector3(0.0f, 0.0f, ScenarioManager.BOUNDARY_SIZE * 0.5f);

        // Spawn regular mines
        for (int i = 0; i < MINE_QUANTITY; i++)
        {
            GameObject curr_mine = GameObject.Instantiate(mine, world_root);
            curr_mine.name = "Mine_" + i;
            curr_mine.GetComponent<NetworkObject>().SynchronizeTransform = true;

            Vector3 spawn_location = positions[i] + world_root_center;
            curr_mine.transform.localPosition = spawn_location;
            curr_mine.transform.localRotation = Random.rotation;

            curr_mine.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
            curr_mine.GetComponent<NetworkObject>().TrySetParent(world_root);
        }

        // Spawn seeker mines
        for (int i = 0; i < SEEKER_MINE_QUANTITY; i++)
        {
            GameObject curr_mine = GameObject.Instantiate(seekerMine, world_root);
            curr_mine.name = "Seeker_Mine_" + i;
            curr_mine.GetComponent<NetworkObject>().SynchronizeTransform = true;

            Vector3 spawn_location = positions[MINE_QUANTITY + i] + world_root_center;
            curr_mine.transform.localPosition = spawn_location;
            curr_mine.transform.localRotation = Random.rotation;

            curr_mine.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
            curr_mine.GetComponent<NetworkObject>().TrySetParent(world_root);
        }
    }

    public string getDeathMessage()
    {
        return DEATH_MESSAGE;
    }
}