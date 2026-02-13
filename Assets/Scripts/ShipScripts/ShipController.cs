using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PilotingSystem))]
[RequireComponent(typeof(WeaponsSystem))]
public class ShipController : NetworkBehaviour
{
    private PilotingSystem pilotingSystem;
    private WeaponsSystem weaponsSystem;

    private bool shipReady = false;

    private GameObject worldRoot = null;

    private void Awake()
    {
        pilotingSystem = GetComponent<PilotingSystem>();
        weaponsSystem = GetComponent<WeaponsSystem>();
        //collisionSystem = GetComponent<CollisionSystem>();
    }

    void Start()
    {
        if (pilotingSystem.AssignControlReferences(ReferenceAssistor.Instance.module_handlers[0].gameObject)
            && weaponsSystem.AssignControlReferences(ReferenceAssistor.Instance.module_handlers[1].gameObject))
        {
            shipReady = true;
        }

        transform.position = Vector3.zero;
    }

    public void assignWorldRoot(GameObject wr)
    {
        worldRoot = wr;
    }

    void Update()
    {
        if (!shipReady) return;

        pilotingSystem.UpdateInput();
        weaponsSystem.UpdateInput();

        weaponsSystem.UpdateWeapons();

        if (NetworkManager.Singleton.IsHost == true)
        {

            if (worldRoot == null)
            {
                return;
            }

            pilotingSystem.UpdateMovement(worldRoot.transform);
        }
    }
}