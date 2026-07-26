/*
    BWBeam.cs
    - Used to destroy collectible items
    Contributor(s): Jake Schott
    Last Updated: 7/3/2026
*/

using Unity.Netcode;
using UnityEngine;

public class BWBeam : MonoBehaviour
{
    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CollectibleItem>() != null) //Destroy collectible items
        {
            other.GetComponent<IDamageable>().damage(99999.9f, IDamageable.DamageType.Explosive);
        }
    }
}
