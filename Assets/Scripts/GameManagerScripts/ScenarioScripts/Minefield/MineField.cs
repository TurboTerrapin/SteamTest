/*
    MineField.cs
    Contributor(s): Henryk Musial
    Last Updated: 4/1/2026

    NETWORKING NOTES:
    - Mines are spawned as ROOT (unparented) NetworkObjects. Netcode for GameObjects
      replicates root NetworkObjects cleanly; parented spawns require both peers to
      have a matching parent NetworkObject and add complexity that we don't need
      since WorldRoot already drives motion via networked offset/heading.
    - World-space position and rotation are set on the transform BEFORE calling
      Spawn(), so when Mine.OnNetworkSpawn runs on the host it captures the
      correct pose into the replicated NetworkVariables for clients to mirror.
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MineField : NetworkBehaviour, IScenario
{
    //CLASS CONSTANTS
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

        // Build obstacle list (positions are in the same local space as the generated
        // points, so subtract world_root_center from any world-space placements)
        var obstacles = new List<SpawnPointGenerator.Obstacle>
        {
            // object at world origin with 300m clearance
            new SpawnPointGenerator.Obstacle(Vector3.zero - world_root_center, 300f),
        };

        List<Vector3> positions = SpawnPointGenerator.GenerateSpawnLocations(
            radius, height, minDistance, totalMines, obstacles);

        for (int i = 0; i < MINE_QUANTITY; i++)
        {
            // Compute the world-space spawn pose first.
            Vector3 spawn_location = positions[i] + world_root_center;
            Quaternion spawn_rotation = Random.rotation;

            // Instantiate UNPARENTED at the desired world-space pose so the
            // Rigidbody's internal physics position is set correctly from the start.
            // Setting position via Instantiate's overload is the most reliable way
            // to get both transform.position and body.position in sync before the
            // first FixedUpdate.
            GameObject curr_mine = GameObject.Instantiate(mine, spawn_location, spawn_rotation);
            curr_mine.name = "Mine_" + i;

            // Spawn AFTER position is set. Mine.OnNetworkSpawn will read
            // transform.position into the replicated NetworkVariable so clients
            // can mirror it. No reparenting needed ? motion is driven by WorldRoot
            // via networked offset/heading on every peer.
            curr_mine.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
        }
    }

    public string getDeathMessage()
    {
        return DEATH_MESSAGE;
    }
}