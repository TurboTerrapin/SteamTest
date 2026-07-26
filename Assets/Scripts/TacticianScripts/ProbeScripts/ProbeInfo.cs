/*
    ProbeInfo.cs
    - Handles animating probe screens
    Contributor(s): Jake Schott
    Last Updated: 2/3/2026
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProbeInfo : MonoBehaviour, IPowerable, IDescribable
{
    //CLASS CONSTANTS
    private Color BLUE = new Color(0.0f, 0.84f, 1.0f);
    private Color RED = new Color(1.0f, 0.0f, 0.0f);
    private Color ORANGE = new Color(1.0f, 0.47f, 0.0f);

    //list of all ray target names
    private List<string> RAY_TARGETS = new List<string>()
    {
        "probe_feed",
        "probe_range",
        "probe_altimeter",
        "probe_health"
    };

    //module titles 
    private static string[] INFO_TITLES = new string[]
    {
        "PROBE FEED",
        "PROBE RANGE",
        "PROBE ALTIMETER",
        "PROBE HEALTH"
    };

    //module additional info, or "" if none
    private static string[] INFO_DESCS = new string[]
    {
        "",
        "",
        "",
        ""
    };

    public GameObject probe_controller_display;

    public GameObject probe_health_display;
    public GameObject probe_signal_display;
    public GameObject probe_feed_display;
    public GameObject probe_altimeter_display;
    public GameObject probe_range_display;

    private TMP_Text probe_in_range;
    private UnityEngine.UI.RawImage probe_render_texture_image;
    private UnityEngine.UI.RawImage probe_overlay;
    private UnityEngine.UI.RawImage probe_load_outer_circle;
    private UnityEngine.UI.Image probe_load_fill_circle;
    private UnityEngine.UI.RawImage probe_icon;
    private UnityEngine.UI.RawImage probe_disconnected_icon;
    private List<HUDInfo> corresponding_infos = new List<HUDInfo>();

    private void Start()
    {
        probe_in_range = probe_range_display.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>();
        probe_render_texture_image = probe_feed_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>();
        probe_overlay = probe_feed_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>();
        probe_load_outer_circle = probe_feed_display.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>();
        probe_load_fill_circle = probe_feed_display.transform.GetChild(2).GetChild(1).GetComponent<UnityEngine.UI.Image>();
        probe_icon = probe_feed_display.transform.GetChild(3).GetComponent<UnityEngine.UI.RawImage>();
        probe_disconnected_icon = probe_feed_display.transform.GetChild(3).GetChild(0).GetComponent<UnityEngine.UI.RawImage>();

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

    //enable probe feed view
    public void onProbeLinked()
    {
        //show feed
        probe_render_texture_image.gameObject.SetActive(true);

        //update center logo
        probe_icon.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        probe_disconnected_icon.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);

        //update probe controller screen
        probe_controller_display.transform.GetChild(0).GetComponent<RawImage>().color = new Color(BLUE.r, BLUE.g, BLUE.b, 1.0f);

        //show directional arcs
        for (int i = 1; i <= 4; i++)
        {
            probe_controller_display.transform.GetChild(i).gameObject.SetActive(true);
            probe_controller_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);
        }

        //hide load circle
        probe_load_outer_circle.gameObject.SetActive(false);

        //update overlay
        probe_overlay.color = BLUE;

        //show health bar
        probe_health_display.transform.GetChild(0).gameObject.SetActive(true);
        probe_health_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = BLUE;

        //show signal
        probe_signal_display.transform.GetChild(0).gameObject.SetActive(true);
        foreach (Transform child in probe_signal_display.transform.GetChild(0))
        {
            child.GetComponent<UnityEngine.UI.RawImage>().color = BLUE;
        }

        //show range
        probe_range_display.transform.GetChild(0).gameObject.SetActive(true);
        probe_in_range.color = BLUE;
        probe_in_range.SetText("IN RANGE");
        probe_range_display.transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);
        probe_range_display.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>().color = BLUE;

        //show altimeter
        probe_altimeter_display.transform.GetChild(0).gameObject.SetActive(true);
    }

    public void onProbeUnlinked()
    {
        //hide feed
        probe_render_texture_image.gameObject.SetActive(false);

        //update center icon
        probe_icon.color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);
        probe_disconnected_icon.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);

        //update probe controller screen
        probe_controller_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);

        //fade directional arcs
        for (int i = 1; i < 5; i++)
        {
            probe_controller_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);
        }

        //hide load circle
        probe_load_outer_circle.gameObject.SetActive(false);

        //update overlay
        probe_overlay.color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);

        //hide health bar
        probe_health_display.transform.GetChild(0).gameObject.SetActive(false);
        probe_health_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);

        //fade signals
        foreach (Transform t in probe_signal_display.transform.GetChild(0))
        {
            t.GetComponent<UnityEngine.UI.RawImage>().color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);
        }

        //fade range bar
        probe_range_display.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().SetText("INACTIVE");
        probe_range_display.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);
        probe_range_display.transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);
        probe_range_display.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>().color = BLUE;
        probe_range_display.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = 0.0f;

        //hide altimeter, show blank one
        probe_altimeter_display.transform.GetChild(0).gameObject.SetActive(false);
        probe_altimeter_display.transform.GetChild(1).gameObject.SetActive(true);
        foreach (Transform t in probe_altimeter_display.transform.GetChild(1))
        {
            t.GetComponent<UnityEngine.UI.RawImage>().color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);
        }
    }

    public void enableProbeOutOfRangeWarning()
    {
        //update signal screen
        foreach (Transform child in probe_signal_display.transform.GetChild(0))
        {
            child.GetComponent<UnityEngine.UI.RawImage>().color = new Color(RED.r, RED.g, RED.b, 0.08f);
        }
        probe_signal_display.transform.GetChild(0).GetChild(12).GetComponent<UnityEngine.UI.RawImage>().color = RED;
        probe_signal_display.transform.GetChild(0).gameObject.SetActive(true);

        //update range text
        probe_in_range.SetText("OUT OF RANGE");
        probe_in_range.color = RED;
        probe_range_display.transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(RED.r, RED.g, RED.b, 0.08f);
        probe_range_display.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>().color = RED;
        probe_range_display.transform.GetChild(0).gameObject.SetActive(true);
    }

    public void disableProbeOutOfRangeWarning()
    {
        //update range screen
        foreach (Transform child in probe_signal_display.transform.GetChild(0))
        {
            child.GetComponent<UnityEngine.UI.RawImage>().color = BLUE;
        }

        //update range text
        probe_in_range.SetText("IN RANGE");
        probe_in_range.color = BLUE;
        probe_range_display.transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);
        probe_range_display.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>().color = BLUE;
    }

    public void onProbeOutOfRangeDisconnect()
    {
        //hide feed
        probe_render_texture_image.gameObject.SetActive(false);

        //update center icon
        probe_icon.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        probe_disconnected_icon.color = RED;

        //update probe controller screen
        probe_controller_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = RED;
        for (int i = 1; i < 5; i++)
        {
            probe_controller_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(RED.r, RED.g, RED.b, 0.08f);
        }

        //hide load circle
        probe_load_outer_circle.gameObject.SetActive(false);

        //update overlay
        probe_overlay.color = RED;

        //hide health bar
        probe_health_display.transform.GetChild(0).gameObject.SetActive(false);
        probe_health_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = RED;

        //update signal
        enableProbeOutOfRangeWarning();

        //hide altimeter
        //hide altimeter, show blank one as orange
        probe_altimeter_display.transform.GetChild(0).gameObject.SetActive(false);
        probe_altimeter_display.transform.GetChild(1).gameObject.SetActive(true);
        foreach (Transform t in probe_altimeter_display.transform.GetChild(1))
        {
            t.GetComponent<UnityEngine.UI.RawImage>().color = new Color(RED.r, RED.g, RED.b, 0.08f);
        }
    }

    public void displayProbeLaunchProgress(float percent_loaded)
    {
        //show/hide feed
        probe_render_texture_image.gameObject.SetActive(percent_loaded == 1.0f);

        //update probe controller screen
        probe_controller_display.transform.GetChild(0).localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f * percent_loaded);
        probe_controller_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = BLUE;

        //update/spin center icon
        probe_icon.color = BLUE;
        probe_icon.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f * percent_loaded);
        probe_disconnected_icon.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);

        //show load circle
        probe_load_outer_circle.gameObject.SetActive(percent_loaded < 1.0f);
        probe_load_outer_circle.color = BLUE;
        probe_load_fill_circle.fillAmount = percent_loaded;
        probe_load_fill_circle.color = new Color(BLUE.r, BLUE.g, BLUE.b, 0.08f);

        //update overlay
        probe_overlay.color = BLUE;

        //highlight the different scan waves for the probe distance screen
        probe_range_display.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().SetText("LAUNCHING");
        probe_range_display.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().color = BLUE;
        probe_signal_display.transform.GetChild(0).gameObject.SetActive(true);
        displayProbeRange(percent_loaded);

        //increase probe health and highlight the border
        probe_health_display.transform.GetChild(0).gameObject.SetActive(true);
        probe_health_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = BLUE;
        displayProbeHealth(percent_loaded);
    }

    public void displayProbeDestructProgress(float percent_to_destruct)
    {
        //hide feed
        probe_render_texture_image.gameObject.SetActive(false);

        //update probe controller screen
        probe_controller_display.transform.GetChild(0).localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f * percent_to_destruct);
        probe_controller_display.transform.GetChild(0).GetComponent<RawImage>().color = ORANGE;
        float tmp_percent = percent_to_destruct;
        int[] adjusted_indexes = new int[] { 0, 2, 1, 3 };
        for (int i = 0; i < 4; i++)
        {
            tmp_percent = percent_to_destruct - (0.25f * i);
            float a = Mathf.Max(0.08f, tmp_percent / 0.25f);
            probe_controller_display.transform.GetChild(adjusted_indexes[i] + 1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(ORANGE.r, ORANGE.g, ORANGE.b, a);
        }

        //update/spin center icon
        probe_icon.color = ORANGE;
        probe_icon.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 90.0f * percent_to_destruct);
        probe_disconnected_icon.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);

        //show load circle
        probe_load_outer_circle.gameObject.SetActive(percent_to_destruct < 1.0f);
        probe_load_outer_circle.color = ORANGE;
        probe_load_fill_circle.fillAmount = 1.0f - percent_to_destruct;
        probe_load_fill_circle.color = new Color(ORANGE.r, ORANGE.g, ORANGE.b, 0.08f);

        //update overlay
        probe_overlay.color = ORANGE;

        //change the signal colors
        probe_signal_display.transform.GetChild(0).gameObject.SetActive(true);
        foreach (Transform child in probe_signal_display.transform.GetChild(0))
        {
            child.GetComponent<UnityEngine.UI.RawImage>().color = ORANGE;
        }

        //update health
        probe_health_display.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().color = ORANGE;
        probe_health_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = ORANGE;

        //update range as destruction progress
        probe_range_display.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().SetText("DESTRUCTING");
        probe_range_display.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().color = ORANGE;
        probe_range_display.transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(ORANGE.r, ORANGE.g, ORANGE.b, 0.08f);
        probe_range_display.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>().color = ORANGE;
        probe_range_display.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = 1.0f - percent_to_destruct;

        //hide altimeter, show blank one as orange
        probe_altimeter_display.transform.GetChild(0).gameObject.SetActive(false);
        probe_altimeter_display.transform.GetChild(1).gameObject.SetActive(true);
        foreach (Transform t in probe_altimeter_display.transform.GetChild(1))
        {
            t.GetComponent<UnityEngine.UI.RawImage>().color = ORANGE;
        }
    }

    public void setDialDisplayColor(Transform dial_display, int state, float a)
    {
        Color display_color = BLUE;
        if (state == 1)
        {
            display_color = ORANGE;
        }
        else if (state == 2)
        {
            display_color = RED;
        }
        display_color.a = a;
        dial_display.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = display_color;
        dial_display.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.Image>().color = display_color;
        dial_display.gameObject.SetActive(true);
    }

    public void powerOn(int position)
    {
        probe_health_display.SetActive(true);
        probe_signal_display.SetActive(true);
        probe_feed_display.SetActive(true);
        probe_altimeter_display.SetActive(true);
        probe_range_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        probe_health_display.SetActive(false);
        probe_signal_display.SetActive(false);
        probe_feed_display.SetActive(false);
        probe_altimeter_display.SetActive(false);
        probe_range_display.SetActive(false);
    }

    public void displayProbeRange(float percent)
    {
        //update signal visualization
        GameObject signal_visualization = probe_signal_display.transform.GetChild(0).gameObject;
        float tmp_dist = percent;
        for (int i = 0; i < 11; i++)
        {
            tmp_dist = percent - (0.091f * i);
            float a = Mathf.Max(0.08f, tmp_dist / 0.091f);
            signal_visualization.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(BLUE.r, BLUE.g, BLUE.b, a);
        }
        signal_visualization.transform.GetChild(11).GetComponent<UnityEngine.UI.RawImage>().color = BLUE;
        signal_visualization.transform.GetChild(12).GetComponent<UnityEngine.UI.RawImage>().color = BLUE;

        //update range bar
        probe_range_display.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = percent;
    }

    public void displayProbeHealth(float health)
    {
        Color probe_color = ShipHealth.getHealthColor(health * 100.0f);
        probe_health_display.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().color = probe_color;
        probe_health_display.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = health;
    }

    //takes in probe.transform.position.y
    public void displayProbeAltitude(float pos)
    {
        GameObject altimeter = probe_altimeter_display.transform.GetChild(0).GetChild(2).gameObject;

        //get current altitude
        float current_altitude = pos;

        //get number markers
        int smallest_number = (((int)(current_altitude)) / 10) * 10;
        int next_number = smallest_number + 10;
        if (current_altitude < 0.0f)
        {
            next_number = smallest_number - 10;
        }

        //define order of markers
        List<GameObject> bars = new List<GameObject>();
        int[] marker_indices = new int[4];
        int[] corresponding_markers = new int[4];
        int marker_index = 18 - (int)((current_altitude % 5.0f) / 1.0f); //defines top marker

        for (int i = 0; i < 4; i++) //define other markers (every 5th marker)
        {
            marker_indices[i] = marker_index - (i * 5);
            if (current_altitude < 0.0f)
            {
                marker_indices[i] -= 5;
            }
        }

        bool lower_half = true;

        if ((Mathf.Abs(current_altitude) % 10.0f < 5.0f)) //swap between number/midpoint halfway
        {
            lower_half = true;
            if (current_altitude < 0.0f)
            {
                lower_half = false;
            }
        }
        else
        {
            lower_half = false;
            if (current_altitude < 0.0f)
            {
                lower_half = true;
            }
        }

        if (lower_half == true)
        {
            corresponding_markers[0] = 0;
            corresponding_markers[1] = 1;
            corresponding_markers[2] = 2;
            corresponding_markers[3] = 3;
        }
        else
        {
            corresponding_markers[0] = 1;
            corresponding_markers[1] = 0;
            corresponding_markers[2] = 3;
            corresponding_markers[3] = 2;
        }

        if (current_altitude < 0.0f)
        {
            int temp = smallest_number;
            smallest_number = next_number;
            next_number = temp;
        }

        //set text for text markers
        altimeter.transform.GetChild(0).transform.GetChild(0).GetComponent<TMP_Text>().SetText(next_number.ToString() + "m");
        altimeter.transform.GetChild(2).transform.GetChild(0).GetComponent<TMP_Text>().SetText(smallest_number.ToString() + "m");

        //define order of markers
        for (int i = 0; i < 17; i++)
        {
            bool marked = false;
            for (int x = 0; x < 4; x++)
            {
                if (i == marker_indices[x])
                {
                    bars.Add(altimeter.transform.GetChild(corresponding_markers[x]).gameObject);
                    marked = true;
                    break;
                }
            }
            if (marked == false)
            {
                bars.Add(altimeter.transform.GetChild(i + 4).gameObject);
            }
        }
        //hide all markers to start
        for (int i = 0; i < 21; i++)
        {
            altimeter.transform.GetChild(i).gameObject.SetActive(false);
        }
        //set positions and active state of each marker
        float shift = ((-current_altitude % 1.0f) / 1.0f) * 0.01f; //0.01 in distance between markers equals 1 meter
        for (int i = 0; i < 17; i++)
        {
            bars[i].SetActive(true);
            bars[i].transform.localPosition = new Vector3(bars[i].transform.localPosition.x, (0.01f * i) - 0.08f + shift, 0.0f);
        }
    }
}