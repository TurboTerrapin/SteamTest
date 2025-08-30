/*
    PilotTractorBeamProgress.cs
    - Displays tractor beam progress (how close an item is to capture)
    Contributor(s): Jake Schott
    Last Updated: 8/19/2025
*/

using UnityEngine;

public class PilotTractorBeamProgress : MonoBehaviour, IPowerable
{
    public Material lit_green;
    public Material lit_red;
    public Material unlit_green;
    public Material unlit_red;

    public GameObject tractor_beam_percentage;
    public GameObject tractor_beam_distance;
    public GameObject tractor_beam_active_indicator;
    public GameObject tractor_beam_inactive_indicator;

    public void powerOn(int position)
    {
        tractor_beam_percentage.SetActive(true);
        tractor_beam_distance.SetActive(true);
        tractor_beam_active_indicator.GetComponent<Renderer>().material = unlit_green;
        tractor_beam_inactive_indicator.GetComponent<Renderer>().material = lit_red;
    }

    public void powerOff(int position, float time)
    {
        tractor_beam_percentage.SetActive(false);
        tractor_beam_distance.SetActive(false);
        tractor_beam_active_indicator.GetComponent<Renderer>().material = unlit_green;
        tractor_beam_inactive_indicator.GetComponent<Renderer>().material = unlit_red;
    }
}
