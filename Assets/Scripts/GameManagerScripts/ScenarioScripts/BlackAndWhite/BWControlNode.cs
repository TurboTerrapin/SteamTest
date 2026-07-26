/*
    BWControlNode.cs
    - Used to control one of the 63 control nodes
    Contributor(s): Jake Schott
    Last Updated: 7/18/2026
*/

using Unity.Netcode;
using UnityEngine;

public class BWControlNode : NetworkBehaviour, IDamageable, ITorpedoTargetable, IPhaserTargetable
{
    public BlackAndWhite black_and_white;

    private float node_health = 50.0f;
    private bool is_active = true;
    private int node_index = -1;

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }

        for (int i = 0; i < transform.parent.childCount; i++)
        {
            if (transform.parent.GetChild(i).gameObject == gameObject)
            {
                node_index = i;
                break;
            }
        }
    }

    public void damage(float damage, IDamageable.DamageType damage_type)
    {
        if (NetworkManager.Singleton.IsHost == false || node_health <= 0.0f)
        {
            return;
        }

        //adjust damage
        if (is_active == true)
        {
            if (damage_type == IDamageable.DamageType.Collision)
            {
                return;
            }
            node_health = Mathf.Max(0.0f, node_health - damage);
        }

        //handle destruction
        if (node_health <= 0.0f || is_active == false)
        {
            black_and_white.onNodeDestroyed(node_index);
            if (GetComponent<NetworkObject>() != null && GetComponent<NetworkObject>().IsSpawned == true)
            {
                GetComponent<NetworkObject>().Despawn(true);
            }
            ReferenceAssistor.Instance.effects_handler.createExplosion(transform.position, 15.0f, true, Color.white);
            Destroy(this);
        }
    }

    public bool getTorpedoTargetable(IDamageable.DamageType torpedo_type)
    {
        return true;
    }

    public bool getPhaserTargetable(IDamageable.DamageType phaser_type)
    {
        return true;
    }

    public void activate()
    {
        is_active = true;
        transform.GetChild(0).GetComponent<MeshRenderer>().material = ReferenceAssistor.Instance.lit_white;
        if (NetworkManager.Singleton.IsHost == true)
        {
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    public void deactivate()
    {
        is_active = false;
        transform.GetChild(0).GetComponent<MeshRenderer>().material = ReferenceAssistor.Instance.pure_black;
        if (NetworkManager.Singleton.IsHost == true)
        {
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        }
    }
}
