/*
    AsteroidField.cs
    - Spawns a field of asteroids for scenario purposes
    Contributor(s): Jake Schott
    Last Updated: 3/20/2026
*/

using Unity.Netcode;
using UnityEngine;

public class AsteroidField : NetworkBehaviour
{
    public GameObject asteroid;
    private ScenarioManager scenario_manager;

    private void Start()
    {
        scenario_manager = GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>();   
    }

    //only run by the host
    public void spawnField(int asteroid_quantity)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        Transform world_root = GameObject.FindGameObjectWithTag("WorldRoot").transform;
        for (int i = 0; i < asteroid_quantity; i++)
        {
            GameObject curr_asteroid = GameObject.Instantiate(asteroid, world_root);

            float scale = Random.Range(8.0f, 30.0f);
            curr_asteroid.transform.localScale = Vector3.one * scale;
            curr_asteroid.transform.localRotation = Random.rotation;

            curr_asteroid.GetComponent<NetworkObject>().SynchronizeTransform = true;
            Vector3 spawn_location = scenario_manager.getSpawnLocation(scale, false);
            curr_asteroid.transform.localPosition = spawn_location;
            curr_asteroid.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
            curr_asteroid.GetComponent<NetworkObject>().TrySetParent(world_root);
        }
    }
}