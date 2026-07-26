/*
    ShipCollisionHandler.cs
    - Deals with collision impacts
    - Communicates to ShipHealth 
    Contributor(s): Henryk Musial
    Last Updated: 6/26/2026
*/

using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ShipCollisionHandler : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float COLLISION_DAMAGE_INFLICTION = 25.0f; //does 25 damage to object if traveling at max speed
    private static float NOT_FORWARD_SECTION_COLLISION_MODIFIER = 0.5f; //does half as much damage if not the forward section

    public List<Collider> shipColliders = new List<Collider>();

    private ShipHealth shipHealth;
    private ImpulseThrottle impulseThrottle;
    private EngineCoolantSupply engineCoolantSupply;

    private void Start()
    {
        shipHealth = GetComponent<ShipHealth>();
        impulseThrottle = ReferenceAssistor.Instance.module_handlers[0].GetComponent<ImpulseThrottle>();
        engineCoolantSupply = ReferenceAssistor.Instance.module_handlers[2].GetComponent<EngineCoolantSupply>();

        //if not host, destroy this and colliders
        if (NetworkManager.Singleton.IsHost == false)
        {
            foreach (Collider collider in shipColliders)
            {
                GameObject.Destroy(collider.gameObject);
            }
            Destroy(this);
        }
    }

    public void HandleCollision(Collider shipSectionCollider, Collider impactObjectCollider)
    {
        int sectionIndex = shipColliders.IndexOf(shipSectionCollider);
        float damage_to_deal = impulseThrottle.getCurrentImpulse() * engineCoolantSupply.getMaxImpulseSpeedBasedOnEngineTemperature() * COLLISION_DAMAGE_INFLICTION;

        if (damage_to_deal <= 0.0f)
        {
            return;
        }

        if (sectionIndex > 0)
        {
            damage_to_deal *= NOT_FORWARD_SECTION_COLLISION_MODIFIER; //adjust damage (not a head-on collision)
        }

        //damage the object being impacted
        IDamageable[] damage_targets = impactObjectCollider.GetComponents<IDamageable>();
        foreach (IDamageable damage_target in damage_targets)
        {
            damage_target.damage(damage_to_deal, IDamageable.DamageType.Collision);
        }

        //damage the ship directly
        shipHealth.damageSection(damage_to_deal * 0.5f, sectionIndex);
    }
}