/*
    TorpedoLauncher.cs
    - Spawns torpedoes at the correct bay
    - Configures torpedo stats on spawn
    Last Updated: 2/13/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TorpedoLauncher : NetworkBehaviour
{
    [Header("References")]
    public TorpedoSelector torpedoSelector;
    public TorpedoPowers torpedoPowers;
    public GameObject torpedoPrefab;

    // Forward [0], Port [1], Starboard [2], Aft [3] 
    public Transform[] spawn_points = new Transform[4];

    [Header("Torpedo Configuration")]
    public TorpedoType current_ammo_type = TorpedoType.Photon;
    public float default_angle_delta = 90.0f; // Degrees the torpedo can turn

    // Can be called by TorpedoTrigger.cs inside transmitTorpedoFireRPC
    public void fireTorpedo()
    {
        if (!IsServer) return; // Only spawn on server

        // 1. Get Selected Bay Index (0-3)
        int bay_index = torpedoSelector.getSelectionIndex();

        // 2. Get Power Level for that Bay
        float power_percent = torpedoPowers.getPowerLevel(bay_index);

        // 3. Instantiate
        if (bay_index < spawn_points.Length)
        {
            // Find the floating origin so the torpedo shares the same coordinate space as the targets
            GameObject world_root = GameObject.FindGameObjectWithTag("WorldRoot");
            Transform parent_transform = world_root != null ? world_root.transform : null;

            // Instantiate as a child of the world root
            GameObject new_torpedo = Instantiate(torpedoPrefab, spawn_points[bay_index].position, spawn_points[bay_index].rotation, parent_transform);

            // 4. Configure
            Torpedo torpedo_script = new_torpedo.GetComponent<Torpedo>();
            if (torpedo_script != null)
            {
                torpedo_script.Initialize(current_ammo_type, power_percent, default_angle_delta);
            }

            // 5. Spawn over network
            NetworkObject net_obj = new_torpedo.GetComponent<NetworkObject>();
            if (net_obj != null)
            {
                net_obj.Spawn(true); // Spawns the object across the network

                // Unity Netcode specific: If the WorldRoot has a NetworkObject, 
                // we should explicitly use TrySetParent to ensure clients sync the hierarchy
                if (parent_transform != null && parent_transform.GetComponent<NetworkObject>() != null)
                {
                    net_obj.TrySetParent(parent_transform);
                }
            }
        }
    }

    // Call this to change ammo type (e.g. from UI)
    public void setAmmoType(TorpedoType new_type)
    {
        current_ammo_type = new_type;
    }
}