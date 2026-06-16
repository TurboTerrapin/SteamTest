/*
    StatusIndicators.cs
    - Handles enabling/disabling the blue/yellow/red alert circles in pilot and tactician position
    - Handles enabling/disabling the overconsumption circles in pilot and tactician position
    - Handles coloring blue/yellow/red alert circles across the ship
    Contributor(s): Jake Schott
    Last Updated: 4/26/2026
*/

using System.Collections.Generic;
using UnityEngine;

public class StatusIndicators : MonoBehaviour, IPowerable, IDescribable
{
    //CLASS CONSTANTS
    private static float[] DOT_SIZES = new float[] { 0.005f, 0.005f, 0.0f, 0.0105f }; //used for animating power consumption

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
    public List<GameObject> ship_status_displays = null;

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
        for (int i = 0; i < ship_status_displays.Count; i++)
        {
            if (ship_status_displays[i].transform.childCount == 1) //pilot, tactician
            {
                ship_status_displays[i].transform.GetChild(0).GetComponent<SpriteRenderer>().color = to_display;
            }
            else if (ship_status_displays[i].transform.childCount > 1) //walls
            {
                foreach (Transform t in ship_status_displays[i].transform)
                {
                    to_display.a = t.GetComponent<SpriteRenderer>().color.a;
                    t.GetComponent<SpriteRenderer>().color = to_display;
                }
            }
            else //SEACC logos
            {
                ship_status_displays[i].GetComponent<SpriteRenderer>().color = to_display;
            }
        }
    }

    public void powerOn(int position)
    {
        if (overconsumption_position_indicators[position].activeSelf == true)
        {
            if (position < 3)
            {
                ship_status_displays[position].SetActive(true); //second pass
            }
        }
        overconsumption_position_indicators[position].SetActive(true); //first pass
    }

    public void powerOff(int position, float time)
    {
        overconsumption_position_indicators[position].SetActive(false);
        if (position < 3)
        {
            ship_status_displays[position].SetActive(false);
        }
    }

    //the small blue circle on pilot, tactician, and captain positions only
    public void displayOverconsumptionPositionIndicator(int position, float percentage)
    {
        //make red
        overconsumption_position_indicators[position].transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        for (int i = 1; i < 6; i++)
        {
            overconsumption_position_indicators[position].transform.GetChild(0).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }

        //expand center
        overconsumption_position_indicators[position].transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Lerp(DOT_SIZES[position], DOT_SIZES[position] * 5.0f, percentage), Mathf.Lerp(DOT_SIZES[position], DOT_SIZES[position] * 5.0f, percentage));

        //contract four dots
        Vector2 other_size = new Vector2(Mathf.Lerp(0.0f, DOT_SIZES[position], Mathf.Max(0.0f, 1.0f - (percentage * 4.0f))), Mathf.Lerp(0.0f, DOT_SIZES[position], Mathf.Max(0.0f, 1.0f - (percentage * 4.0f))));
        for (int i = 2; i < 6; i++)
        {
            overconsumption_position_indicators[position].transform.GetChild(0).GetChild(i).GetComponent<RectTransform>().sizeDelta = other_size;
        }
    }

    //the small blue circle on pilot, tactician, and captain positions only
    public void resetOverconsumptionPositionIndicator(int position)
    {
        //make blue
        overconsumption_position_indicators[position].transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        for (int i = 1; i < 6; i++)
        {
            overconsumption_position_indicators[position].transform.GetChild(0).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);

            //resize
            overconsumption_position_indicators[position].transform.GetChild(0).GetChild(i).GetComponent<RectTransform>().sizeDelta = new Vector2(DOT_SIZES[position], DOT_SIZES[position]);
        }
    }
}
