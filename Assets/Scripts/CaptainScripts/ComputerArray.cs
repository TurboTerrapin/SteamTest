/*
    ComputerArray.cs
    - Displays nodes and edges
    Contributor(s): Jake Schott
    Last Updated: 4/25/2026
*/

using UnityEngine;
using Unity.Netcode;

public class ComputerArray : NetworkBehaviour, IPowerable
{
    public GameObject computer_array_display;

    //private bool is_powered = false;

    private void Start()
    {
        resetToDefault();
    }

    public void resetToDefault()
    {

    }

    private void displayComputerArray()
    {

    }

    public void powerOn(int position)
    {
        //is_powered = true;
        computer_array_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        //is_powered = false;
        computer_array_display.SetActive(false);
    }
}