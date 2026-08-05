/*
    SecondaryScript.cs
    - Helps with secondary info that isn't primary control interactions
    Contributor(s): Jake Schott
    Last Updated: 8/3/2026
*/

using System.Collections;
using TMPro;
using UnityEngine;

public class SecondaryScript : MonoBehaviour
{
    //CLASS CONSTANTS
    private static Color DEFAULT_BORDER_CORDER = new Color(0.12f, 0.12f, 0.12f, 1.0f);

    public GameObject secondary_info;

    private GameObject permanent_overlay;
    private GameObject stations_button;
    private GameObject current_station_indicator;
    private GameObject station_functions;
    private GameObject mission_objective;
    private GameObject sitting_overlay;
    private GameObject sitting_left_side;
    private GameObject shift_button;
    private GameObject sitting_right_side;
    private GameObject primary_default_power_circles;

    private float displayed_power = 0.0f;
    private Coroutine mission_objective_display_coroutine = null;

    private void Awake()
    {
        permanent_overlay = secondary_info.transform.GetChild(0).gameObject;
        stations_button = permanent_overlay.transform.GetChild(0).gameObject;
        current_station_indicator = permanent_overlay.transform.GetChild(1).gameObject;
        station_functions = permanent_overlay.transform.GetChild(2).gameObject;
        mission_objective = permanent_overlay.transform.GetChild(3).gameObject;
        sitting_overlay = secondary_info.transform.GetChild(1).gameObject;
        sitting_left_side = sitting_overlay.transform.GetChild(0).gameObject;
        shift_button = sitting_left_side.transform.GetChild(2).gameObject;
        sitting_right_side = sitting_overlay.transform.GetChild(1).gameObject;
        primary_default_power_circles = transform.GetChild(1).GetChild(0).GetChild(3).GetChild(1).gameObject;
    }

    public void setSecondaryInfoVisibility(bool active)
    {
        secondary_info.SetActive(active);
    }

    public void setPermanentOverlayVisibility(bool active)
    {
        permanent_overlay.SetActive(active);
    }

    public void setSittingOverlayVisibility(bool active)
    {
        sitting_overlay.SetActive(active);
    }

    public void setSittingRightSideVisibility(bool active)
    {
        sitting_right_side.SetActive(active);
    }

    //updates station indicator in top right as well as colors on top
    public void onStationChange(int pos)
    {
        //show/hide position icon
        current_station_indicator.transform.GetChild(2).gameObject.SetActive(pos >= 0);
        current_station_indicator.transform.GetChild(3).gameObject.SetActive(pos < 0);
        if (pos >= 0) //set permanent overlay color to color of position
        {
            for (int i = 0; i < 4; i++)
            {
                current_station_indicator.transform.GetChild(1).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = ReferenceAssistor.COLOR_OPTIONS[pos];
            }
            foreach (Transform t in stations_button.transform.GetChild(1))
            {
                t.GetComponent<UnityEngine.UI.RawImage>().color = ReferenceAssistor.COLOR_OPTIONS[pos];
            }
            current_station_indicator.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().texture = ReferenceAssistor.Instance.position_icons[pos];
            current_station_indicator.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = ReferenceAssistor.COLOR_OPTIONS[pos];
        }
        else //set default border color to permanent overlay borders
        {
            foreach (Transform t in current_station_indicator.transform.GetChild(1))
            {
                t.GetComponent<UnityEngine.UI.RawImage>().color = DEFAULT_BORDER_CORDER;
            }
            foreach (Transform t in stations_button.transform.GetChild(1))
            {
                t.GetComponent<UnityEngine.UI.RawImage>().color = DEFAULT_BORDER_CORDER;
            }
        }

        //do nothing more if not sitting
        if (pos < 0)
        {
            return;
        }

        //update sitting overlay if sitting
        Color c = ReferenceAssistor.COLOR_OPTIONS[pos];
        foreach (Transform t in transform.GetChild(1).GetChild(0).GetChild(1))
        {
            foreach (Transform b in t)
            {
                if (b.GetComponent<UnityEngine.UI.Image>() != null)
                {
                    b.GetComponent<UnityEngine.UI.Image>().color = c;
                }
                else
                {
                    foreach (Transform l in b)
                    {
                        l.GetComponent<UnityEngine.UI.Image>().color = c;
                    }
                }
            }
        }
        foreach (Transform t in transform.GetChild(1).GetChild(0).GetChild(2).GetChild(1))
        {
            t.GetComponent<UnityEngine.UI.Image>().color = c;
        }
        foreach (Transform t in sitting_left_side.transform)
        {
            foreach (Transform b in t.GetChild(1))
            {
                b.GetComponent<UnityEngine.UI.RawImage>().color = c;
            }
        }
        foreach (Transform t in sitting_right_side.transform)
        {
            foreach (Transform b in t.GetChild(1))
            {
                if (b.GetComponent<UnityEngine.UI.RawImage>() != null)
                {
                    b.GetComponent<UnityEngine.UI.RawImage>().color = c;
                }
                else
                {
                    foreach (Transform l in b)
                    {
                        l.GetComponent<UnityEngine.UI.RawImage>().color = c;
                    }
                }
            }
        }
    }

    //shows/hides the information on the right side on tab press
    public void toggleControlInformationVisibility(HUDInfo temp_info)
    {
        bool currently_visible = sitting_right_side.transform.GetChild(1).gameObject.activeSelf;
        sitting_right_side.transform.GetChild(0).GetChild(2).gameObject.SetActive(currently_visible);
        sitting_right_side.transform.GetChild(0).GetChild(3).gameObject.SetActive(!currently_visible);
        sitting_right_side.transform.GetChild(1).gameObject.SetActive(!currently_visible);
    }

    //updates shift direction UI indicator and get up indicator
    public void updateShiftIndicators(bool is_shifting, int curr_pos, SeatManager seat_manager)
    {
        sitting_left_side.transform.GetChild(1).GetChild(2).GetChild(0).gameObject.SetActive(!is_shifting);
        sitting_left_side.transform.GetChild(1).GetChild(3).GetChild(0).gameObject.SetActive(!is_shifting);
        sitting_left_side.transform.GetChild(2).gameObject.SetActive(curr_pos != 3);
        sitting_left_side.transform.GetChild(2).GetChild(2).GetChild(0).gameObject.SetActive(seat_manager.canShiftLeft(curr_pos) && !is_shifting);
        sitting_left_side.transform.GetChild(2).GetChild(3).GetChild(0).gameObject.SetActive(seat_manager.canShiftRight(curr_pos) && !is_shifting);
        sitting_left_side.transform.GetChild(2).GetChild(4).GetChild(0).gameObject.SetActive(!is_shifting);
    }

    //helper method that estimates the length of a control description based on the length of the description of that control's description
    private int getControlInfoOffset(HUDInfo temp_info)
    {
        return Mathf.Max(280, temp_info.getInfo().Length * 4);
    }

    //updates right side control info (description)
    public void updateSecondaryControlInformation(HUDInfo temp_info)
    {
        //determine whether to show or hide right side
        sitting_right_side.SetActive(temp_info.hasInfo());

        //set info frame title and description
        sitting_right_side.transform.GetChild(1).GetChild(2).GetComponent<TMP_Text>().SetText(temp_info.getName());
        sitting_right_side.transform.GetChild(1).GetChild(3).GetComponent<TMP_Text>().SetText(temp_info.getInfo());
        
        //resize based on length of control description
        int offset = getControlInfoOffset(temp_info);
        Transform control_info_frame = sitting_right_side.transform.GetChild(1);

        //background
        control_info_frame.GetChild(0).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(-285f, -360f + offset);
        control_info_frame.GetChild(0).GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector2(50f, -360f + offset);
        control_info_frame.GetChild(0).GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -410f + (offset / 2));
        control_info_frame.GetChild(0).GetChild(2).GetComponent<RectTransform>().sizeDelta = new Vector2(670f, offset);

        //border/divider
        control_info_frame.GetChild(1).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(-290f, -355f + offset);
        control_info_frame.GetChild(1).GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -305f + offset);
        control_info_frame.GetChild(1).GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -440f + offset);
        control_info_frame.GetChild(1).GetChild(3).GetComponent<RectTransform>().anchoredPosition = new Vector2(-340f, -475f + (offset / 2));
        control_info_frame.GetChild(1).GetChild(3).GetComponent<RectTransform>().sizeDelta = new Vector2(10f, offset - 180f);
        control_info_frame.GetChild(1).GetChild(6).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -520f + offset);

        //text
        control_info_frame.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -420f + offset);
        control_info_frame.GetChild(3).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -515f + (offset / 2));
    }
    
    //updates the four blue dots in bottom right corner
    public void updatePowerConsumption(HUDInfo temp_info)
    {
        float power_consumption = temp_info.getPowerConsumption();
        if (power_consumption == displayed_power)
        {
            return;
        }

        //make power icon blue if consuming any power
        primary_default_power_circles.transform.GetChild(0).GetChild(0).gameObject.SetActive(power_consumption > 0.0f);

        //adjust circles
        float tmp_pwr = (power_consumption * 2.0f);
        for (int i = 0; i < 5; i++)
        {
            tmp_pwr = (power_consumption* 2.0f) - (0.2f * i);
            float a = tmp_pwr / 0.2f;
            primary_default_power_circles.transform.GetChild(i + 1).GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = a;
        }
        displayed_power = power_consumption;
    }

    public void updateInfoOverlayOffset(float offset)
    {
        station_functions.transform.localPosition = new Vector3(0.0f, offset, 0.0f);
    }

    public void checkStationFunctionsInput(bool force_hide)
    {
        bool inputted = PrimaryScript.checkInputIndexDown(15);

        if (inputted == false && force_hide == false)
        {
            return;
        }

        bool hide = station_functions.activeSelf;
        station_functions.SetActive(!hide && !force_hide);
        string button_desc = "SHOW (V)";
        if (station_functions.activeSelf == true)
        {
            button_desc = "HIDE (V)";
        }
        stations_button.transform.GetChild(3).GetComponent<TMP_Text>().SetText(button_desc);
        GetComponent<PrimaryScript>().setCursorVisibility(hide && !force_hide);
    }

    public void displayMissionObjective(float delay)
    {
        if (mission_objective_display_coroutine != null)
        {
            StopCoroutine(mission_objective_display_coroutine);
        }

        mission_objective_display_coroutine = StartCoroutine(missionObjectiveReveal(delay));
    }

    public bool isDisplayingIntro()
    {
        return (mission_objective_display_coroutine != null);
    }

    public void endMissionObjectiveReveal()
    {
        //show stations button and current station indicator
        stations_button.SetActive(true);
        current_station_indicator.SetActive(true);

        //end intro and hide mission objective
        if (mission_objective_display_coroutine != null)
        {
            StopCoroutine(mission_objective_display_coroutine);
            mission_objective_display_coroutine = null;
        }
        mission_objective.SetActive(false);
    }

    IEnumerator missionObjectiveReveal(float delay)
    {
        //set transparency to 0
        mission_objective.transform.GetChild(0).GetComponent<CanvasGroup>().alpha = 0.0f;
        mission_objective.transform.GetChild(1).GetComponent<CanvasGroup>().alpha = 0.0f;
        for (int i = 0; i < 5; i++)
        {
            mission_objective.transform.GetChild(i + 2).GetComponent<TMP_Text>().alpha = 0.0f;
        }
        foreach (Transform t in mission_objective.transform.GetChild(7))
        {
            t.GetComponent<CanvasGroup>().alpha = 0.0f;
        }
        mission_objective.transform.GetChild(8).GetComponent<TMP_Text>().alpha = 0.0f;
        mission_objective.transform.GetChild(9).GetComponent<CanvasGroup>().alpha = 0.0f;

        yield return new WaitForSeconds(delay);

        secondary_info.SetActive(true);
        permanent_overlay.SetActive(true);
        stations_button.SetActive(false);
        current_station_indicator.SetActive(false);
        station_functions.SetActive(false);
        mission_objective.SetActive(true);

        //background, border, dividers, circles, and "MISSION OBJECTIVE"
        float anim_time = 1.0f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            float a = Mathf.Lerp(1.0f, 0.0f, anim_time);
            mission_objective.transform.GetChild(0).GetComponent<CanvasGroup>().alpha = a;
            mission_objective.transform.GetChild(1).GetComponent<CanvasGroup>().alpha = a;
            mission_objective.transform.GetChild(2).GetComponent<TMP_Text>().alpha = a;

            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        //bullet points
        for (int i = 0; i < 4; i++)
        {
            anim_time = 0.5f;
            while (anim_time > 0.0f)
            {
                anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

                mission_objective.transform.GetChild(i + 3).GetComponent<TMP_Text>().alpha = Mathf.Lerp(1.0f, 0.0f, anim_time / 0.5f);

                yield return null;
            }
        }

        //position icons
        for (int i = 0; i < 4; i++)
        {
            anim_time = 0.5f;
            while (anim_time > 0.0f)
            {
                anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

                mission_objective.transform.GetChild(7).GetChild(i).GetComponent<CanvasGroup>().alpha = Mathf.Lerp(1.0f, 0.0f, anim_time / 0.5f);

                yield return null;
            }
        }

        //"USE STATION CONTROLS..."
        anim_time = 1.0f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            mission_objective.transform.GetChild(8).GetComponent<TMP_Text>().alpha = Mathf.Lerp(1.0f, 0.0f, anim_time);

            yield return null;
        }

        //show exit button
        anim_time = 0.5f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            mission_objective.transform.GetChild(9).GetComponent<CanvasGroup>().alpha = Mathf.Lerp(1.0f, 0.0f, anim_time / 0.5f);

            yield return null;
        }

        mission_objective_display_coroutine = null;
    }
}