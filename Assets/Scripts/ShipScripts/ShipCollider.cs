/*
    ShipCollider.cs
    - Forwards collision events to CollisionHandler
    - Forwards damage to ShipHealth
    Contributor(s): Henryk Musial
    Last Updated: 6/26/2026
*/

using Unity.Netcode;
using UnityEngine;

public class ShipCollider : MonoBehaviour, IDamageable
{
    public ShipCollisionHandler shipCollisionHandler;
    [SerializeField]
    private int section = -1;
    private ShipHealth shipHealth;

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Destroy(this);
        }
        shipHealth = ReferenceAssistor.Instance.spaceship.GetComponent<ShipHealth>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        shipCollisionHandler.HandleCollision(GetComponent<Collider>(), collision.collider);
    }

    public void damage(float damage, IDamageable.DamageType damage_type)
    {
        shipHealth.damageSection(damage, section);
    }
}