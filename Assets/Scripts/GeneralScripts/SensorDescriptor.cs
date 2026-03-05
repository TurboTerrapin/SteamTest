/*
    SensorDescriptor.cs
    - Used to give UI indicators for non-controllable screens
    Contributor(s): Jake Schott
    Last Updated: 3/2/2026
*/

using UnityEngine;
using System.Collections.Generic;

public class SensorDescriptor : MonoBehaviour, IDescribable
{
    //list of all ray target names
    private List<string> RAY_TARGETS = new List<string>() 
    { 
        "power_consumption",
        "prefix_code",
        "proximity_map",
        "scenario_map",
        "boundary_countdown",
        "navigation_heading_display",
        "power_distribution",
        "phaser_heat",
        "power_overview",
        "ship_overview",
        "ship_health",
        "ship_inventory"
    };

    //module titles 
    private static string[] INFO_TITLES = new string[]
    {
        "STATION POWER CONSUMPTION",
        "PARTIAL PREFIX CODE",
        "PROXIMITY MAP",
        "NAVIGATION MAP",
        "BOUNDARY COUNTDOWN",
        "NAVIGATION HEADING",
        "POWER DISTRIBUTION",
        "PHASER TEMPERATURES",
        "POWER OVERVIEW",
        "SHIP OVERVIEW",
        "SHIP HEALTH",
        "SHIP INVENTORY"
    };

    //module additional info, or "" if none
    private static string[] INFO_DESCS = new string[]
    {
        "Describes positional power consumption on a scale of 0 to 10 power units. Power units are allocated in the engineer position.",
        "",
        "",
        "Shows detection boundary, entrance channel, and destination exit channel. Also shows items of interest.",
        "Shows boundary countdown before mission failure.",
        "",
        "Shows power allocation and usage across all four positions. If usage exceeds allocation, ship will shut down.",
        "Shows short-range and long-range phaser temperatures. Overheating causes ship shutdown.",
        "Describes ship power status according to the six corresponding power regulation modules.",
        "",
        "",
        "Shows ship inventory for probes, escape pods, shield batteries, cargo, and torpedoes."
    };

    private List<HUDInfo> corresponding_infos = new List<HUDInfo>();

    private void Start()
    {
        for (int i = 0; i < INFO_TITLES.Length; i++)
        {
            corresponding_infos.Add(new HUDInfo(INFO_TITLES[i]));
            if (INFO_DESCS[i].CompareTo("") != 0)
            {
                corresponding_infos[i].setInfo(INFO_DESCS[i]);
            }
        }
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return corresponding_infos[RAY_TARGETS.IndexOf(current_target.name)];
    }
}