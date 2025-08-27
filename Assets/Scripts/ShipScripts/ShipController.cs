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

    private GameObject worldRoot = null;

    private void Awake()
    {
        pilotingSystem = GetComponent<PilotingSystem>();
        weaponsSystem = GetComponent<WeaponsSystem>();
    }

    void Start()
    {
        controlHandler = GameObject.FindGameObjectWithTag("ControlHandler");

        if (controlHandler != null && pilotingSystem.AssignControlReferences(controlHandler)
            && weaponsSystem.AssignControlReferences(controlHandler))
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