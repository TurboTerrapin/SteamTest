/*
    StatusIndicators.cs
    - Handles enabling/disabling the blue/yellow/red alert circles in pilot and tactician position
    - Handles enabling/disabling the overconsumption circles in pilot and tactician position
    - Handles coloring blue/yellow/red alert circles across the ship
    Contributor(s): Jake Schott
    Last Updated: 1/5/2026
*/

using System.Collections.Generic;
using UnityEngine;

public class StatusIndicators : MonoBehaviour, IPowerable, IDescribable
{
    //list of all ray target names
    private List<string> RAY_TARGETS = new List<string>()
    {
        "overconsumption_warning",
        "red_alert_indicator"
    };

    //module titles 
    private static string[] INFO_TITLES = new string[]
    {
        "POWER OVERCONSUMPTION INDICATOR",
        "SHIP STATUS"
    };

    //module additional info, or "" if none
    private static string[] INFO_DESCS = new string[]
    {
        "Animates when exceeding allocated power units until power overload and shutdown.",
        ""
    };

    public List<GameObject> overconsumption_position_indicators = null;
    public List<GameObject> ship_status_position_indicators = null;
    public List<GameObject> ship_status_indicators = null;

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

    public void displayShipStatus(Color to_display)
    {
        for (int i = 0; i < ship_status_indicators.Count; i++)
        {
            foreach (Transform c in ship_status_indicators[i].transform)
            {
                c.GetComponent<UnityEngine.UI.RawImage>().color = to_display;
            }
        }
    }

    public void powerOn(int position)
    {
        if (overconsumption_position_indicators[position].activeSelf == true)
        {
            ship_status_position_indicators[position].SetActive(true); //second pass
        }
        overconsumption_position_indicators[position].SetActive(true); //first pass
    }

    public void powerOff(int position, float time)
    {
        overconsumption_position_indicators[position].SetActive(false);
        ship_status_position_indicators[position].SetActive(false);
    }

    //the small blue circle on pilot and tactician positions only
    public void displayOverconsumptionPositionIndicator(int position, float percentage)
    {
        //make red
        overconsumption_position_indicators[position].transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        for (int i = 1; i < 6; i++)
        {
            overconsumption_position_indicators[position].transform.GetChild(0).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }

        //expand center
        overconsumption_position_indicators[position].transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Lerp(0.005f, 0.025f, percentage), Mathf.Lerp(0.005f, 0.025f, percentage));

        //contract four dots
        Vector2 other_size = new Vector2(Mathf.Lerp(0.0f, 0.005f, Mathf.Max(0.0f, 1.0f - (percentage * 4.0f))), Mathf.Lerp(0.0f, 0.005f, Mathf.Max(0.0f, 1.0f - (percentage * 4.0f))));
        for (int i = 2; i < 6; i++)
        {
            overconsumption_position_indicators[position].transform.GetChild(0).GetChild(i).GetComponent<RectTransform>().sizeDelta = other_size;
        }
    }

    //the small blue circle on pilot and tactician positions only
    public void resetOverconsumptionPositionIndicator(int position)
    {
        //make blue
        overconsumption_position_indicators[position].transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        for (int i = 1; i < 6; i++)
        {
            overconsumption_position_indicators[position].transform.GetChild(0).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);

            //resize
            overconsumption_position_indicators[position].transform.GetChild(0).GetChild(i).GetComponent<RectTransform>().sizeDelta = new Vector2(0.005f, 0.005f);
        }
    }
}
