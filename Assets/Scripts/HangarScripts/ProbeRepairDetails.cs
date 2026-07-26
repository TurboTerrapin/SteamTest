using UnityEngine;

public class ProbeRepairDetails : MonoBehaviour
{
    private bool isOccupied = false;

    public bool getOccupied()
    {
        return isOccupied;
    }
    public void setOccupied(bool value)
    {
        isOccupied = value;
    }
}
