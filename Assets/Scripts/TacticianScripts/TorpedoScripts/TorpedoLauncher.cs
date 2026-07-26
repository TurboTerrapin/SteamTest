/*
    TorpedoLauncher.cs
    - Spawns torpedoes at the correct bay
    - Configures torpedo stats on spawn
    Last Updated: 3/6/2026
*/

using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class TorpedoLauncher : NetworkBehaviour
{
    public List<GameObject> torpedoPrefabs;

    // Forward [0], Port [1], Starboard [2], Aft [3] 
    public Transform[] spawn_points = new Transform[4];

    // Can be called by TorpedoTrigger.cs inside transmitTorpedoFireRPC
    public void fireTorpedo(int bay_index, int torpedo_type)
    {
        if (NetworkManager.Singleton.IsHost == false) return; // Only spawn on server

        // Instantiate
        if (bay_index < spawn_points.Length)
        {
            // Find the floating origin so the torpedo shares the same coordinate space as the targets
            GameObject world_root = ReferenceAssistor.Instance.world_root;
            Transform parent_transform = world_root != null ? world_root.transform : null;

            // Instantiate as a child of the world root
            GameObject new_torpedo = GameObject.Instantiate(torpedoPrefabs[torpedo_type], parent_transform);
            new_torpedo.transform.position = spawn_points[bay_index].position;
            new_torpedo.transform.rotation = spawn_points[bay_index].rotation;

            // Launch
            new_torpedo.GetComponent<Torpedo>().Initialize();

            // Spawn over network
            NetworkObject net_obj = new_torpedo.GetComponent<NetworkObject>();
            net_obj.Spawn(true); // Spawns the object across the network
            if (parent_transform != null && parent_transform.GetComponent<NetworkObject>() != null)
            {
                net_obj.TrySetParent(parent_transform);
            }
        }
    }
}