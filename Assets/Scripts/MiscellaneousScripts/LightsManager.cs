/*
    LightsManager.cs
    - Handles light stuff
    Contributor(s): Jake Schott
    Last Updated: 9/3/2025
*/

using UnityEngine;

public class LightsManager : MonoBehaviour
{
    public void enableDefaultLights()
    {
        transform.GetChild(0).gameObject.SetActive(true);
    }  

    public void disableDefaultLights()
    {
        transform.GetChild(0).gameObject.SetActive(false);
    }
}
