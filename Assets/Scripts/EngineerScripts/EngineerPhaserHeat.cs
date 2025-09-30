/*
    EngineerPhaserHeat.cs
    - Currently only enables/disables phaser heat screen
    Contributor(s): Jake Schott
    Last Updated: 9/3/2025
*/

using UnityEngine;

public class EngineerPhaserHeat : MonoBehaviour, IPowerable
{
    public GameObject phaser_heat_display;

    public void powerOn(int position)
    {
        phaser_heat_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        phaser_heat_display.SetActive(false);
    }
}