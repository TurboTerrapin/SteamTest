/*
    MineField.cs
    Contributor(s): Henryk Musial
    Last Updated: 4/1/2026
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MineField : NetworkBehaviour, IScenario
{
    //CLASS CONSTANTS
    private static int MINE_QUANTITY = 30;
    //private static int SEEKER_MINE_QUANTITY = 20;
    private static string DEATH_MESSAGE = "You died buddy get better at this game";

    public GameObject mine;
    public GameObject seekerMine;

    public void initiateScenario()
    {
        if (NetworkManager.Singleton.IsHost == false)
            return;

        float minDistance = 50.0f;

        int totalMines = MINE_QUANTITY;

        List<Vector3> positions = GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().generateSpawnLocations(
            minDistance, totalMines, null);

        Transform world_root = GameObject.FindGameObjectWithTag("WorldRoot").transform;

        for (int i = 0; i < MINE_QUANTITY; i++)
        {
            GameObject curr_mine = GameObject.Instantiate(mine, world_root);
            curr_mine.name = "Mine_" + i;
            curr_mine.GetComponent<NetworkObject>().SynchronizeTransform = true;

            Vector3 spawn_location = positions[i];
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