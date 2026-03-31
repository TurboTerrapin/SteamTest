/*
    Cheeseball.cs
    - Removes collider if not host, then destroys self
    Contributor(s): Jake Schott
    Last Updated: 1/23/2026
*/

using Unity.Netcode;
using UnityEngine;

public class Cheeseball : MonoBehaviour
{
    void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(transform.GetComponent<Collider>());
        }
        Destroy(this);
    }
}
