/*
    AnomalyPanelInfo.cs
    - Used to hold anomaly info to avoid reusing UI elements
    Contributor(s): Jake Schott
    Last Updated: 8/21/2025
*/

using UnityEngine;

public class AnomalyPanelInfo : PanelInfo
{
    public string anomaly_id = "";
    public Texture anomaly_icon = null;
    public string anomaly_observation_info = "";
    public string step_number = "";
    public string step_title = "";
    public GameObject first_step_destination = null;
}