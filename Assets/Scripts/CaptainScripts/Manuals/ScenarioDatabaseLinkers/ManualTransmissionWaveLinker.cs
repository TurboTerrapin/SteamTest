/*
    ManualTransmissionWaveLinker.cs
    - Used for linking scenario database entries to a transmission wave image
    Contributor(s): Jake Schott
    Last Updated: 6/28/2026
*/

using UnityEngine;

public class ManualPhaserTransmissionWaveLinker : MonoBehaviour, IManualLinker
{
    [SerializeField]
    private GameObject corresponding_database_entry;
    [SerializeField]
    private GameObject corresponding_transmission_wave_icon;

    public void link()
    {
        TransmissionWaveData data = corresponding_database_entry.GetComponent<TransmissionWaveData>();
        TransmissionHandler transmission_handler = ReferenceAssistor.Instance.module_handlers[1].GetComponent<TransmissionHandler>();

        corresponding_transmission_wave_icon.GetComponent<UnityEngine.UI.RawImage>().texture = transmission_handler.getWaveTextureFromIndex(data.getTransmissionWave());
    }
}