/*
    ManualButtonRerouter.cs
    - Used for rerouting a different button to this parent upon clicking
    Contributor(s): Jake Schott
    Last Updated: 8/23/2026
*/

using UnityEngine;

public class ManualButtonRerouter : MonoBehaviour, IManualLinker
{
    [SerializeField]
    private GameObject button_to_reroute;
    [SerializeField]
    private GameObject reroute_destination;

    public void link()
    {
        if (button_to_reroute.GetComponent<ManualButton>() != null)
        {
            button_to_reroute.GetComponent<ManualButton>().select_panel = reroute_destination;
        }
        else if (button_to_reroute.GetComponent<ManualButtonOptions>() != null)
        {
            button_to_reroute.GetComponent<ManualButtonOptions>().button_info[4] = reroute_destination;
        }
    }
}