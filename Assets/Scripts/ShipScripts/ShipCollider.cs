/*
    ShipCollider.cs
    - Forwards collision events to CollisionHandler
    - Forwards damage to ShipHealth
    Contributor(s): Henryk Musial
    Last Updated: 3/21/2026
*/

using Unity.Netcode;
using UnityEngine;

public class ShipCollider : MonoBehaviour, IDamageable
{
    public CollisionHandler collisionHandler;
    [SerializeField]
    private int section = -1;
    private ShipHealth shipHealth;

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Destroy(this);
        }
        shipHealth = GameObject.FindGameObjectWithTag("Spaceship").GetComponent<ShipHealth>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        collisionHandler.HandleCollision(GetComponent<Collider>(), collision.collider);
    }

    public void damage(float damage)
    {
        shipHealth.damageSection(damage, section);
    }
}