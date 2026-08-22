/*
    ManualButtonRerouter.cs
    - Used for rerouting a different button to this parent upon clicking
    Contributor(s): Jake Schott
    Last Updated: 8/22/2026
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
        button_to_reroute.GetComponent<ManualButton>().select_panel = reroute_destination;
    }
}