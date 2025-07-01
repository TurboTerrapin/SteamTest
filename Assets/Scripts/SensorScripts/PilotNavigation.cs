/*
    PilotNavigation.cs
    - Updates course heading text and compass slider
    - Updates ship altimeter
    Contributor(s): Jake Schott
    Last Updated: 6/30/2025
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PilotNavigation : MonoBehaviour
{
    public GameObject heading_text;
    public GameObject compass;
    public GameObject altimeter;
    private GameObject spaceship;
    private GameObject world_root;

    private void Start()
    {
        spaceship = GameObject.FindGameObjectWithTag("Spaceship");
        world_root = GameObject.FindGameObjectWithTag("WorldRoot");
        updateCourseHeadingScreen();
        updateAltimeterScreen();
    }

    public void updateAltimeterScreen()
    {
        //get current altitude (remember, is inverse, so - is positive and + is negative)
        float current_altitude = world_root.transform.position.y;

        //define which markers display text
        int smallest_number = (((int)(current_altitude * -1.0f)) / 10) * 10;
        int next_number = smallest_number + 10;
        if (current_altitude > 0.0f)
        {
            next_number = smallest_number - 10;
        }
        //set text for text markers
        altimeter.transform.GetChild(0).transform.GetChild(0).GetComponent<TMP_Text>().SetText(next_number.ToString() + "m");
        altimeter.transform.GetChild(2).transform.GetChild(0).GetComponent<TMP_Text>().SetText(smallest_number.ToString() + "m");
        List<GameObject> bars = new List<GameObject>();
        int[] marker_indices = new int[4];
        int[] corresponding_markers = new int[4];
        int marker_index = 18 - (int)(Mathf.Abs((current_altitude - 50000.0f) % 5.0f) / 1.0f);
        if (current_altitude > 0.0f)
        {
            marker_index--;
        }
        for (int i = 0; i < 4; i++)
        {
            marker_indices[i] = marker_index - (i * 5);
        }
        if ((Mathf.Abs(current_altitude) % 10.0f < 5.0f))
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
        //if negative altitude then switch the markers around
        if (current_altitude > 0.0f)
        {
            for (int x = 0; x < 2; x++)
            {
                int temp = corresponding_markers[3 - x];
                corresponding_markers[3 - x] = corresponding_markers[x];
                corresponding_markers[x] = temp;
            }
        }
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
        //hide all markers
        for (int i = 0; i < 21; i++)
        {
            altimeter.transform.GetChild(i).gameObject.SetActive(false);
        }
        //set positions and active state of each marker
        float shift = ((current_altitude % 1.0f) / 1.0f) * 0.01f; //0.01 in distance between markers equals 1 meter
        for (int i = 0; i < 17; i++)
        {
            bars[i].SetActive(true);
            bars[i].transform.localPosition = new Vector3(bars[i].transform.localPosition.x, (0.01f * i) - 0.08f + shift, 0.0f);
        }
    }

    public void updateCourseHeadingScreen()
    {
        //get ship rotation to get directional heading
        float current_rotation = spaceship.transform.rotation.eulerAngles.y;
        if (current_rotation < 0.0f)
        {
            current_rotation += 360.0f;
        }
        else if (current_rotation >= 360.0f)
        {
            current_rotation -= 360.0f;
        }

        //adjust course heading text
        float rounded_rotation = Mathf.Round(current_rotation * 10.0f) / 10.0f;
        string display_heading = rounded_rotation.ToString();
        if (display_heading.Contains(".") == false)
        {
            display_heading += ".0";
        }
        heading_text.GetComponent<TMP_Text>().SetText(display_heading + "°");

        //adjust course heading slider
        int marker_index = 18 - (int)((current_rotation % 22.5f) / 2.5f);
        int halfway_index = marker_index - 9;
        int[] corresponding_markers = new int[2];
        if (current_rotation % 45.0f < 22.5f)
        {
            corresponding_markers[0] = 0;
            corresponding_markers[1] = 1;
        }
        else
        {
            corresponding_markers[0] = 1;
            corresponding_markers[1] = 0;
        }
        //set number marker texts
        int[] possible_options = { 315, 270, 225, 180, 135, 90, 45, 0 };
        for (int i = 0; i < possible_options.Length; i++)
        {
            if (Mathf.Abs(current_rotation - possible_options[i]) < 22.5f)
            {
                compass.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().SetText(possible_options[i].ToString());
                break;
            }
            if (i == 6)
            {
                compass.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().SetText("0");
            }
        }
        //define order of markers
        List<GameObject> bars = new List<GameObject>();
        for (int i = 0; i < 19; i++)
        {
            if (i == marker_index)
            {
                bars.Add(compass.transform.GetChild(corresponding_markers[0]).gameObject);
            }
            else if (i == halfway_index)
            {
                bars.Add(compass.transform.GetChild(corresponding_markers[1]).gameObject);
            }
            else
            {
                bars.Add(compass.transform.GetChild(i + 2).gameObject);
            }
        }
        //hide all markers
        for (int i = 0; i < 21; i++)
        {
            compass.transform.GetChild(i).gameObject.SetActive(false);
        }
        //set positions and active state of each marker
        float shift = ((current_rotation % 2.5f) / 2.5f) * -0.01f; //0.01 in distance between markers equals 2.5 degrees
        for (int i = 0; i < 19; i++)
        {
            bars[i].SetActive(true);
            bars[i].transform.localPosition = new Vector3((-0.01f * i) + 0.09f - shift, bars[i].transform.localPosition.y, 0.0f);
        }
    }
}
