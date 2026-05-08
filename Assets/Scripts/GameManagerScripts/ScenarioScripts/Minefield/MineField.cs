/*
    MineField.cs
    Contributor(s): Henryk Musial
    Last Updated: 4/1/2026

    Spawns mines as root NetworkObjects at world-space coordinates. Position is
    set BEFORE Spawn() so the host's pose is captured correctly and replicated
    to clients via the prefab's NetworkTransform.
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MineField : NetworkBehaviour, IScenario
{
    private static int MINE_QUANTITY = 100;
    //private static int SEEKER_MINE_QUANTITY = 20;
    private static string DEATH_MESSAGE = "You died buddy get better at this game";

    public GameObject mine;
    public GameObject seekerMine;

    public void initiateScenario()
    {
        if (NetworkManager.Singleton.IsHost == false)
            return;

        float radius = ScenarioManager.BOUNDARY_SIZE * 0.5f;
        float height = ScenarioManager.BOUNDARY_ALTITUDE * 2f;
        float minDistance = 75.0f;

        int totalMines = MINE_QUANTITY;

        Vector3 world_root_center = new Vector3(0.0f, 0.0f, ScenarioManager.BOUNDARY_SIZE * 0.5f);

        var obstacles = new List<SpawnPointGenerator.Obstacle>
        {
            // object at world origin with 300m clearance
            new SpawnPointGenerator.Obstacle(Vector3.zero - world_root_center, 300f),
        };

        List<Vector3> positions = SpawnPointGenerator.GenerateSpawnLocations(
            radius, height, minDistance, totalMines, obstacles);

        for (int i = 0; i < MINE_QUANTITY; i++)
        {
            Vector3 spawn_location = positions[i] + world_root_center;
            Quaternion spawn_rotation = Random.rotation;

            // Instantiate at the desired world-space pose, then Spawn(). The
            // prefab's NetworkTransform will replicate this pose to all clients.
            GameObject curr_mine = GameObject.Instantiate(mine, spawn_location, spawn_rotation);
            curr_mine.name = "Mine_" + i;

            curr_mine.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
        }
    }

    public string getDeathMessage()
    {
        return DEATH_MESSAGE;
    }
}