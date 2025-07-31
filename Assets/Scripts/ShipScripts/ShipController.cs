using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PilotingSystem))]
[RequireComponent(typeof(WeaponsSystem))]
public class ShipController : NetworkBehaviour
{
    private PilotingSystem pilotingSystem;
    private WeaponsSystem weaponsSystem;

    private GameObject controlHandler;
    private bool shipReady = false;

    public GameObject worldRoot; 

    private void Awake()
    {
        pilotingSystem = GetComponent<PilotingSystem>();
        weaponsSystem = GetComponent<WeaponsSystem>();

    }

    void Start()
    {
        controlHandler = GameObject.FindGameObjectWithTag("ControlHandler");
        worldRoot = GameObject.FindGameObjectWithTag("WorldRoot");

        if (controlHandler != null && worldRoot != null && pilotingSystem.AssignControlReferences(controlHandler)
            && weaponsSystem.AssignControlReferences(controlHandler))
        {
            shipReady = true;
        }

        transform.position = Vector3.zero;
    }

    void Update()
    {
        if (!shipReady) return;

        pilotingSystem.UpdateInput();
        weaponsSystem.UpdateInput();
    }

    private void FixedUpdate()
    {
        if (!shipReady) return;

        if (NetworkManager.Singleton.IsHost == true)
        {
            weaponsSystem.UpdateWeapons();
            pilotingSystem.UpdateMovement(worldRoot.transform);
        }
    }
}