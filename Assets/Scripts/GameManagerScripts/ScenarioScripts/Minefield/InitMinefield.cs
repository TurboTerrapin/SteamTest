/*
    InitMinefield
    - Used for initializing permanent information on Minefield (override combinations and phaser frequencies)
    Contributor(s): Jake Schott
    Last Updated: 6/28/2026
*/

using Unity.Netcode;
using UnityEngine;
using TMPro;

public class InitMinefield : NetworkBehaviour, IScenarioInitialization
{
    public static int SCENARIO_DATABASE_INDEX = 1;

    private GameObject scenario_database_MF;

    public TMP_Text manual_desc_text;

    private void Awake()
    {
        scenario_database_MF = transform.GetChild(0).GetChild(SCENARIO_DATABASE_INDEX).gameObject;
    }

    private void Start()
    {
        manual_desc_text.SetText(manual_desc_text.text + Minefield.WARNING_SIGNAL_PERIOD_TIMES[GetComponent<ScenarioManager>().getDifficulty()] + " SECONDS WHICH CAN BE <color=#AF00AF>DECODED</color> (PAGE 3)");
    }

    public void initializeDatabaseInformation()
    {
        for (int i = 0; i < 6; i++)
        {
            int[] phaser_frequencies = new int[2] { 0, 0 };
            for (int p = 0; p < 2; p++)
            {
                phaser_frequencies[p] = Random.Range(PhaserFrequency.MIN_FREQUENCIES[p], PhaserFrequency.MAX_FREQUENCIES[p] + 1);
            }

            int[] override_switches = new int[6] { 0, 0, 0, 0, 0, 0 };
            for (int s = 0; s < 6; s++)
            {
                override_switches[s] = Random.Range(0, 2);
            }

            transmitTransmissionOptionInitializationRPC(i, phaser_frequencies[0], phaser_frequencies[1], DataConverter.arrayToString(override_switches));
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitTransmissionOptionInitializationRPC(int index, int long_range_frequency, int short_range_frequency, string string_override_switches)
    {
        scenario_database_MF.transform.GetChild(index).GetComponent<TransmissionWaveData>().setTransmissionWave(index + 1); //don't start at 0 because 0 is empty wave
        scenario_database_MF.transform.GetChild(index).GetComponent<PhaserFrequencyData>().setPhaserFrequency(0, long_range_frequency);
        scenario_database_MF.transform.GetChild(index).GetComponent<PhaserFrequencyData>().setPhaserFrequency(1, short_range_frequency);
        scenario_database_MF.transform.GetChild(index).GetComponent<OverrideSwitchesData>().setSwitchConfigurations(DataConverter.stringToArray(string_override_switches));
    }
}