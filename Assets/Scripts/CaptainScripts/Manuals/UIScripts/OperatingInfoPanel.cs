/*
    OperatingPanelInfo.cs
    - Used to hold operating info to avoid reusing UI elements
    Contributor(s): Jake Schott
    Last Updated: 8/21/2025
*/

using UnityEngine;

public class OperatingPanelInfo : PanelInfo
{
    public Color header_color = Color.white;
    public bool header_line = false;
    public string page_name = "";
    public Texture page_icon = null;
    public bool general_overview = false;
    public int max_power_usage = -1;
}