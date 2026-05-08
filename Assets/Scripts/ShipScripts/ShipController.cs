using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ShipMovement))]
//[RequireComponent(typeof(WeaponsSystem))]
public class ShipController : NetworkBehaviour
{
    private ShipMovement shipMovement;
    //private WeaponsSystem weaponsSystem;

    private bool shipReady = false;

    private GameObject worldRoot = null;

    private void Awake()
    {
        shipMovement = GetComponent<ShipMovement>();
        //weaponsSystem = GetComponent<WeaponsSystem>();
        //collisionSystem = GetComponent<CollisionSystem>();
    }

    void Start()
    {
        if (shipMovement.AssignControlReferences())
            //&& weaponsSystem.AssignControlReferences(ReferenceAssistor.Instance.module_handlers[1].gameObject))
        {
            shipReady = true;
        }

        transform.position = Vector3.zero;
    }


    public void assignWorldRoot(GameObject wr)
    {
        worldRoot = wr;
    }


    void FixedUpdate()
    {
        if (!shipReady) return;

        shipMovement.UpdateInput();
        //weaponsSystem.UpdateInput();

        //weaponsSystem.UpdateWeapons();

        if (NetworkManager.Singleton.IsHost == true)
        {

            if (worldRoot == null)
            {
                return;
            }

            shipMovement.UpdateMovement(worldRoot.transform);
        }
    }
}