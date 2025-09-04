/*
    EngineerInventory.cs
    - Currently only enables/disables inventory screen
    Contributor(s): Jake Schott
    Last Updated: 9/3/2025
*/

using UnityEngine;

public class EngineerInventory : MonoBehaviour, IPowerable
{
    public GameObject inventory_display;

    public void powerOn(int position)
    {
        inventory_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        inventory_display.SetActive(false);
    }
}