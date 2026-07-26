/*
    PhaserFrequencyData.cs
    - Data holder for phaser frequency combinations
    Contributor(s): Jake Schott
    Last Updated: 6/27/2026
*/

using UnityEngine;

public class PhaserFrequencyData : MonoBehaviour
{
    private int[] phaser_frequencies = new int[] { 0, 0 };
    
    public void setPhaserFrequencies(int[] new_frequencies)
    {
        phaser_frequencies = new_frequencies;        
    }

    public void setPhaserFrequency(int index, int frequency)
    {
        phaser_frequencies[index] = frequency;
    }

    public int getPhaserFrequency(int index)
    {
        return phaser_frequencies[index];
    }
}