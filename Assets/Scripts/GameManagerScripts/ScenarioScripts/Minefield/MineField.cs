/*
    Minefield.cs
    Contributor(s): Henryk Musial
    Last Updated: 6/29/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Minefield : NetworkBehaviour, IScenario, IEmissionSusceptible, IBroadcastable, IShipBeaconCommunicable, IOverrideSwitchCommunicable
{
    //CLASS CONSTANTS
    private static int MINE_QUANTITY = 55;
    public static float DETECTION_RANGE = 600.0f;
    private static float EMISSION_REDUCER_EFFECT = 75.0f; // Each reducer reduces detection range by this much
    private static IDamageable.DamageType[] VALID_TORPEDO_TYPES = new IDamageable.DamageType[] { IDamageable.DamageType.IonTorpedo, IDamageable.DamageType.SuperluminalTorpedo, IDamageable.DamageType.ChronitonTorpedo }; // Torpedo types that disable/destroy the mines
    public static int[] WARNING_SIGNAL_PERIOD_TIMES = new int[] { 120, 90, 60, 45 }; // Easy, medium, hard, expert
    private static int[] WARNING_SIGNAL_INDEXES = new int[8] { 10, 10, 10, 10, 10, 10, 10, 10 };
    private static bool[] WARNING_SIGNAL_IS_NUMERIC = new bool[8] { false, false, false, false, false, false, false, false };
    private static int[] WARNING_SIGNAL_COLORS = new int[8] { 2, 4, 2, 4, 2, 4, 2, 4 };
    private static string DEATH_MESSAGE = "Stolen ship SEACC-3002 was found adrift within a field of mines. Crew was unable to maneuver around or disable the mines and sustained extensive hull damage. No survivors were found.";

    public GameObject normalMine;
    public GameObject seekerMine;
    public Material mineLitGreen;
    private Transform scenarioDatabaseMF;
    private ShipBeacon shipBeacon;
    private OverrideSwitches overrideSwitches;
    private PhaserFrequency phaserFrequency;
    private UniversalCommunicatorCodeData warningSignalCodeData;

    private bool minesCurrentlyEnabled = true;
    private float currentTransmissionFrequency = 0.0f;
    private int currentTransmissionIndex = -1;
    private float detectionRange = DETECTION_RANGE;
    private Coroutine warningSignalCoroutine;

    private void Start()
    {
        shipBeacon = ReferenceAssistor.Instance.module_handlers[3].GetComponent<ShipBeacon>();
        overrideSwitches = ReferenceAssistor.Instance.module_handlers[3].GetComponent<OverrideSwitches>();
        phaserFrequency = ReferenceAssistor.Instance.module_handlers[2].GetComponent<PhaserFrequency>();
        scenarioDatabaseMF = ReferenceAssistor.Instance.scenario_manager.transform.GetChild(0).GetChild(InitMinefield.SCENARIO_DATABASE_INDEX);

        warningSignalCodeData = GetComponent<UniversalCommunicatorCodeData>();
        warningSignalCodeData.setCodeIndexes(WARNING_SIGNAL_INDEXES);
        warningSignalCodeData.setCodeIsNumeric(WARNING_SIGNAL_IS_NUMERIC);
        warningSignalCodeData.setCodeColors(WARNING_SIGNAL_COLORS);
    }

    public void initiateScenario()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        // Spawn mines (half normal, half seeker)
        float minDistance = 250.0f;
        List<Vector3> positions =
            ReferenceAssistor.Instance.scenario_manager.generateSpawnLocations(minDistance, MINE_QUANTITY, null);

        Transform world_root = GameObject.FindGameObjectWithTag("WorldRoot").transform;

        for (int i = 0; i < MINE_QUANTITY; i++)
        {
            bool isSeekerMine = (Random.Range(0, 2) == 0);
            GameObject curr_mine;
            if (isSeekerMine == true)
            {
                curr_mine = GameObject.Instantiate(seekerMine, world_root);
            }
            else
            {
                curr_mine = GameObject.Instantiate(normalMine, world_root);
            }

            curr_mine.name = "Mine" + i;
            curr_mine.GetComponent<NetworkObject>().SynchronizeTransform = true;

            Vector3 spawn_location = positions[i];
            curr_mine.transform.localPosition = spawn_location;
            curr_mine.transform.localRotation = Random.rotation;

            curr_mine.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
            curr_mine.GetComponent<NetworkObject>().TrySetParent(world_root);
        }

        // Set initial frequency and wave combination
        warningSignalCoroutine = StartCoroutine(WarningSignalLoop());
    }

    public string getDeathMessage()
    {
        return DEATH_MESSAGE;
    }

    public bool torpedoTracksMine(IDamageable.DamageType torpedo_type)
    {
        for (int i = 0; i < VALID_TORPEDO_TYPES.Length; i++)
        {
            if (VALID_TORPEDO_TYPES[i] == torpedo_type)
            {
                return true;
            }
        }
        return false;
    }

    IEnumerator WarningSignalLoop()
    {
        while (true)
        {
            // Wipe current frequency and index combination
            if (currentTransmissionFrequency != 0.0f)
            {
                ReferenceAssistor.Instance.module_handlers[1].GetComponent<FrequencyAdjuster>().frequencyReplacement(currentTransmissionFrequency, 0);
            }

            // Identify new frequency and index combination
            float newTransmissionFrequency = currentTransmissionFrequency;
            while (newTransmissionFrequency == currentTransmissionFrequency)
            {
                newTransmissionFrequency = UnityEngine.Random.Range(FrequencyAdjuster.FREQUENCY_RANGES[0], FrequencyAdjuster.FREQUENCY_RANGES[1] + 1) / 10.0f;
            }
            List<int> possibleTransmissionIndexes = new List<int>() { 0, 1, 2, 3, 4, 5 };
            possibleTransmissionIndexes.Remove(currentTransmissionIndex);
            int newTransmissionIndex = possibleTransmissionIndexes[Random.Range(0, possibleTransmissionIndexes.Count)];

            // Set and automatically transmit new frequency and index combination
            currentTransmissionFrequency = newTransmissionFrequency;
            currentTransmissionIndex = newTransmissionIndex;
            ReferenceAssistor.Instance.module_handlers[1].GetComponent<FrequencyAdjuster>().frequencyReplacement(currentTransmissionFrequency, currentTransmissionIndex + 1);

            // Check enabled status
            checkMinesEnabledStatus();

            yield return new WaitForSeconds(WARNING_SIGNAL_PERIOD_TIMES[ReferenceAssistor.Instance.scenario_manager.getDifficulty()]);
        }
    }

    public float getMineDetectionRange()
    {
        return detectionRange;
    }

    public void onEmissionChange()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        bool[] enabledReducers = ReferenceAssistor.Instance.module_handlers[0].GetComponent<EmissionReducers>().getEnabledReducers();
        float updatedDetectionRange = DETECTION_RANGE;
        for (int i = 0; i < 2; i++)
        {
            if (enabledReducers[i] == true)
            {
                updatedDetectionRange -= EMISSION_REDUCER_EFFECT;
            }
        }
        detectionRange = updatedDetectionRange;
    }

    private void checkMinesEnabledStatus()
    {
        // Ship beacon must be enabled
        bool new_status = false;
        if (shipBeacon.getBeaconEnabled() == false)
        {
            new_status = true;
        }

        // Override switches must match database based on current transmission wave index
        bool[] currentOverrideSwitches = overrideSwitches.getOverrideSwitches();
        for (int i = 0; i < 6; i++)
        {
            if (currentOverrideSwitches[i] != scenarioDatabaseMF.transform.GetChild(currentTransmissionIndex).GetComponent<OverrideSwitchesData>().getSwitchEnabled(i))
            {
                new_status = true;
            }
        }

        // Update if necessary
        if (new_status != minesCurrentlyEnabled)
        {
            updateMineEnabledStatusRPC(new_status);
        }
    }

    public void onShipBeaconChange()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        checkMinesEnabledStatus();
    }

    public void onOverrideSwitchChange()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        checkMinesEnabledStatus();
    }

    public bool damageTypeBypassesMineShields(IDamageable.DamageType damage_type)
    {
        if (damage_type == IDamageable.DamageType.LongRangePhaser)
        {
            return (scenarioDatabaseMF.transform.GetChild(currentTransmissionIndex).GetComponent<PhaserFrequencyData>().getPhaserFrequency(0) == phaserFrequency.getCurrentPhaserFrequency(0));
        }
        else if (damage_type == IDamageable.DamageType.ShortRangePhaser)
        {
            return (scenarioDatabaseMF.transform.GetChild(currentTransmissionIndex).GetComponent<PhaserFrequencyData>().getPhaserFrequency(1) == phaserFrequency.getCurrentPhaserFrequency(1));
        }
        else if (damage_type == IDamageable.DamageType.Explosive || damage_type == IDamageable.DamageType.Collision || damage_type == IDamageable.DamageType.PhotonTorpedo || damage_type == IDamageable.DamageType.ProtonTorpedo || damage_type == IDamageable.DamageType.QuantumTorpedo)
        {
            return false;
        }
        return true;
    }

    public bool canFetchTransmission(float frequency)
    {
        return (frequency == currentTransmissionFrequency);
    }

    public UniversalCommunicatorCodeData fetchTransmission(float frequency)
    {
        if (canFetchTransmission(frequency) == true)
        {
            return warningSignalCodeData;
        }
        return null;
    }

    [Rpc(SendTo.Everyone)]
    private void updateMineEnabledStatusRPC(bool enabled)
    {
        minesCurrentlyEnabled = enabled;
        foreach (Transform t in ReferenceAssistor.Instance.world_root.transform)
        {
            if (t.GetComponent<NormalMine>() != null)
            {
                t.GetComponent<NormalMine>().UpdateEnabledStatus(enabled);
            }
            else if (t.GetComponent<SeekerMine>() != null)
            {
                t.GetComponent<SeekerMine>().UpdateEnabledStatus(enabled);
            }
        }
    }
}