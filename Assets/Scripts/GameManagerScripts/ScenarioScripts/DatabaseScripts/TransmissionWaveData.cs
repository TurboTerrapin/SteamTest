/*
    TransmissionWaveData.cs
    - Data holder for transmission waves
    Contributor(s): Jake Schott
    Last Updated: 6/28/2026
*/

using UnityEngine;

public class TransmissionWaveData : MonoBehaviour
{
    private int transmission_wave_index;

    public void setTransmissionWave(int wave)
    {
        transmission_wave_index = wave;
    }

    public int getTransmissionWave()
    {
        return transmission_wave_index;
    }
}