/*
    AsteroidField.cs
    - Spawns a field of asteroids for scenario purposes
    Contributor(s): Jake Schott
    Last Updated: 6/24/2026
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AsteroidField : NetworkBehaviour
{
    public GameObject asteroid;

    //only run by the host
    public void spawnField(List<Vector3> asteroid_spawn_locations)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        Transform world_root = GameObject.FindGameObjectWithTag("WorldRoot").transform;
        for (int i = 0; i < asteroid_spawn_locations.Count; i++)
        {
            GameObject curr_asteroid = GameObject.Instantiate(asteroid, world_root);

            float scale = Random.Range(3.0f, 40.0f);
            curr_asteroid.transform.localScale = Vector3.one * scale;
            curr_asteroid.transform.localRotation = Random.rotation;

            curr_asteroid.GetComponent<NetworkObject>().SynchronizeTransform = true;
            curr_asteroid.transform.localPosition = asteroid_spawn_locations[i];
            curr_asteroid.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
            curr_asteroid.GetComponent<NetworkObject>().TrySetParent(world_root);
        }
    }
}