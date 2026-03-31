/*
    SecondaryScript.cs
    - Helps with secondary info that isn't primary control interactions
    Contributor(s): Jake Schott
    Last Updated: 2/25/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SecondaryScript : MonoBehaviour
{
    //CLASS CONSTANTS
    private static KeyCode[][] INFO_OVERLAY_INPUT_OPTIONS = new KeyCode[][] { 
        new KeyCode[] { KeyCode.Alpha1, KeyCode.Keypad1 } ,                                                                      
        new KeyCode[] { KeyCode.Alpha2, KeyCode.Keypad2 } ,
        new KeyCode[] { KeyCode.Alpha3, KeyCode.Keypad3 } ,
        new KeyCode[] { KeyCode.Alpha4, KeyCode.Keypad4 } ,
        new KeyCode[] { KeyCode.Alpha5, KeyCode.Keypad5 }
    };
    private static Color DEFAULT_BORDER_COLOR = new Color(0.4f, 0.4f, 0.4f);
    
    public GameObject secondary_info;

    private GameObject station_overlay;
    private GameObject info_overlay;
    private GameObject left_side;
    private GameObject right_side;
    private GameObject intro_graphic_overlay;
    private GameObject station_indicator;
    private GameObject primary_default_power_circles;

    private float displayed_power = 0.0f;
    private Coroutine intro_graphic_display_coroutine = null;

    private void Awake()
    {
        info_overlay = secondary_info.transform.GetChild(1).gameObject;
        station_overlay = secondary_info.transform.GetChild(0).gameObject;
        left_side = station_overlay.transform.GetChild(0).gameObject;
        right_side = station_overlay.transform.GetChild(1).gameObject;
        station_indicator = secondary_info.transform.GetChild(2).gameObject;
        primary_default_power_circles = transform.GetChild(1).GetChild(0).GetChild(2).GetChild(1).gameObject;
    }

    public void toggleSecondaryInfoVisibility(bool active)
    {
        secondary_info.SetActive(active);
    }

    public void toggleInfoOverlaysVisibility(bool active)
    {
        info_overlay.SetActive(active);
    }

    public void toggleStationIndicatorVisibility(bool active)
    {
        station_indicator.SetActive(active);
    }

    public void toggleStationOverlayVisibility(bool active)
    {
        station_overlay.SetActive(active);
    }

    public void toggleRightSideVisibility(bool active)
    {
        right_side.SetActive(active);
    }

    //updates station indicator in top right
    public void onStationChange(int pos)
    {
        station_indicator.transform.GetChild(2).gameObject.SetActive(pos >= 0);
        station_indicator.transform.GetChild(3).gameObject.SetActive(pos == -1);
        Color c = new Color(0.4f, 0.4f, 0.4f, 0.84f);
        if (pos >= 0)
        {
            c = ReferenceAssistor.COLOR_OPTIONS[pos];
            c.a = 1.0f;
            station_indicator.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().texture = ReferenceAssistor.Instance.position_icons[pos];
        }

        //update left side color
        foreach (Transform t in info_overlay.transform.GetChild(0).GetChild(1))
        {
            t.GetComponent<UnityEngine.UI.RawImage>().color = c;
        }
        Color bc = c;
        bc.a = info_overlay.transform.GetChild(0).GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color.a;
        info_overlay.transform.GetChild(0).GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = bc;
        station_indicator.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = c;
        foreach (Transform t in info_overlay.transform.GetChild(0).GetChild(3))
        {
            foreach (Transform b in t.GetChild(0))
            {
                bc = c;
                bc.a = b.GetComponent<UnityEngine.UI.RawImage>().color.a;
                b.GetComponent<UnityEngine.UI.RawImage>().color = bc;
            }
        }

        //update right side color
        foreach (Transform t in station_indicator.transform.GetChild(1))
        {
            t.GetComponent<UnityEngine.UI.RawImage>().color = c;
        }

        //update info overlay borders and circles and dividers
        foreach (Transform t in info_overlay.transform.GetChild(1))
        {
            foreach (Transform b in t.GetChild(1))
            {
                b.GetComponent<UnityEngine.UI.RawImage>().color = c;
            }
            for (int i = 0; i < 5; i++)
            {
                bc = c;
                bc.a = t.GetChild(2).GetChild(0).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color.a;
                t.GetChild(2).GetChild(0).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = bc;
            }
            foreach (Transform d in t.GetChild(3))
            {
                d.GetComponent<UnityEngine.UI.RawImage>().color = c;
            }
            foreach (Transform b in t.GetChild(t.transform.childCount - 1).GetChild(0))
            {
                b.GetComponent<UnityEngine.UI.RawImage>().color = c;
            }
        }

        //update default UI items
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
        foreach (Transform t in left_side.transform)
        {
            foreach (Transform b in t.GetChild(1))
            {
                b.GetComponent<UnityEngine.UI.RawImage>().color = c;
            }
        }
        foreach (Transform t in right_side.transform)
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
        bool currently_visible = right_side.transform.GetChild(1).gameObject.activeSelf;
        right_side.transform.GetChild(0).GetChild(2).gameObject.SetActive(currently_visible);
        right_side.transform.GetChild(0).GetChild(3).gameObject.SetActive(!currently_visible);
        right_side.transform.GetChild(1).gameObject.SetActive(!currently_visible);
    }

    //updates shift direction UI indicator and get up indicator
    public void updateShiftIndicators(bool is_shifting, int curr_pos, SeatManager seat_manager)
    {
        left_side.transform.GetChild(1).GetChild(2).GetChild(0).gameObject.SetActive(!is_shifting);
        left_side.transform.GetChild(1).GetChild(3).GetChild(0).gameObject.SetActive(!is_shifting);
        left_side.transform.GetChild(2).gameObject.SetActive(curr_pos != 3);
        left_side.transform.GetChild(2).GetChild(2).GetChild(0).gameObject.SetActive(seat_manager.canShiftLeft(curr_pos) && !is_shifting);
        left_side.transform.GetChild(2).GetChild(3).GetChild(0).gameObject.SetActive(seat_manager.canShiftRight(curr_pos) && !is_shifting);
        left_side.transform.GetChild(2).GetChild(4).GetChild(0).gameObject.SetActive(!is_shifting);
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
        right_side.SetActive(temp_info.hasInfo());

        //set info frame title and description
        right_side.transform.GetChild(1).GetChild(2).GetComponent<TMP_Text>().SetText(temp_info.getName());
        right_side.transform.GetChild(1).GetChild(3).GetComponent<TMP_Text>().SetText(temp_info.getInfo());
        
        //resize based on length of control description
        int offset = getControlInfoOffset(temp_info);
        Transform control_info_frame = right_side.transform.GetChild(1);

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
        if (temp_info.getPowerConsumption() == displayed_power)
        {
            return;
        }

        float tmp_pwr = (temp_info.getPowerConsumption() * 2.0f);
        for (int i = 0; i <= 4; i++)
        {
            tmp_pwr = (temp_info.getPowerConsumption() * 2.0f) - (0.2f * i);
            float a = tmp_pwr / 0.2f;
            primary_default_power_circles.transform.GetChild(i).GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = a;
        }
        displayed_power = temp_info.getPowerConsumption();
    }

    public void updateInfoOverlayOffset(float offset)
    {
        info_overlay.transform.GetChild(1).localPosition = new Vector3(0.0f, offset, 0.0f);
    }

    public void checkInfoOverlayInputs(bool force_hide)
    {
        bool inputted = false;
        int input = -1;
        while (inputted == false && input < 4)
        {
            input++;
            foreach (KeyCode kc in INFO_OVERLAY_INPUT_OPTIONS[input])
            {
                if (Input.GetKeyDown(kc) == true)
                {
                    inputted = true;
                    break;
                }
            }
        }

        if (inputted == false && force_hide == false)
        {
            return;
        }

        Color c = info_overlay.transform.GetChild(0).GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color;
        c.a = 0.1f;
        bool hide = info_overlay.transform.GetChild(1).GetChild(input).gameObject.activeSelf;
        for (int i = 0; i < 5; i++)
        {
            info_overlay.transform.GetChild(1).GetChild(i).gameObject.SetActive(false);
            foreach (Transform t in info_overlay.transform.GetChild(0).GetChild(3).GetChild(i).GetChild(0))
            {
                t.GetComponent<UnityEngine.UI.RawImage>().color = c;
            }
            info_overlay.transform.GetChild(0).GetChild(3).GetChild(i).GetChild(1).GetComponent<TMP_Text>().color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
        }

        info_overlay.transform.GetChild(1).GetChild(input).gameObject.SetActive(!hide && !force_hide);
        GetComponent<PrimaryScript>().setCursorVisibility(hide && !force_hide);
        info_overlay.transform.GetChild(0).GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = c;
        if (hide == false && force_hide == false)
        {
            c.a = 1.0f;
            info_overlay.transform.GetChild(0).GetChild(3).GetChild(input).GetChild(1).GetComponent<TMP_Text>().color = Color.white;
            info_overlay.transform.GetChild(0).GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = c;
        }
        foreach (Transform t in info_overlay.transform.GetChild(0).GetChild(3).GetChild(input).GetChild(0))
        {
            t.GetComponent<UnityEngine.UI.RawImage>().color = c;
        }
    }

    public void displayIntroGraphic(float delay)
    {
        if (intro_graphic_display_coroutine != null)
        {
            StopCoroutine(intro_graphic_display_coroutine);
        }

        intro_graphic_display_coroutine = StartCoroutine(introGraphicReveal(delay));
    }

    public bool isDisplayingIntroGraphic()
    {
        return (intro_graphic_display_coroutine != null);
    }

    public void endIntroGraphicReveal()
    {
        //show info buttons
        info_overlay.transform.GetChild(0).gameObject.SetActive(true);

        //end intro and delete cloned graphic
        if (intro_graphic_display_coroutine != null)
        {
            StopCoroutine(intro_graphic_display_coroutine);
            intro_graphic_display_coroutine = null;
        }
        if (intro_graphic_overlay != null)
        {
            GameObject.Destroy(intro_graphic_overlay);
        }
    }

    IEnumerator introGraphicReveal(float delay)
    {
        //hide info buttons
        info_overlay.transform.GetChild(0).gameObject.SetActive(false);

        //component assignment and transparency setting to 0
        intro_graphic_overlay = GameObject.Instantiate(info_overlay.transform.GetChild(1).GetChild(1).gameObject, info_overlay.transform.GetChild(1));
        intro_graphic_overlay.name = "Temporary";
        intro_graphic_overlay.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        intro_graphic_overlay.transform.GetChild(12).gameObject.SetActive(false); //hide (2) indicator
        List<UnityEngine.UI.RawImage> background_elements = new List<UnityEngine.UI.RawImage>();
        foreach (Transform t in intro_graphic_overlay.transform.GetChild(0))
        {
            background_elements.Add(t.GetComponent<UnityEngine.UI.RawImage>());
            Color c = t.GetComponent<UnityEngine.UI.RawImage>().color;
            c.a = 0.0f;
            t.GetComponent<UnityEngine.UI.RawImage>().color = c;
        }

        List<UnityEngine.UI.RawImage> borders = new List<UnityEngine.UI.RawImage>();
        foreach (Transform t in intro_graphic_overlay.transform.GetChild(1))
        {
            borders.Add(t.GetComponent<UnityEngine.UI.RawImage>());
            Color c = t.GetComponent<UnityEngine.UI.RawImage>().color;
            c.a = 0.0f;
            t.GetComponent<UnityEngine.UI.RawImage>().color = c;
        }

        List<UnityEngine.UI.RawImage> order_circles = new List<UnityEngine.UI.RawImage>();
        for (int i = 0; i < 5; i++)
        {
            order_circles.Add(intro_graphic_overlay.transform.GetChild(2).GetChild(0).GetChild(i).GetComponent<UnityEngine.UI.RawImage>());
            Color c = order_circles[i].color;
            c.a = 0.0f;
            if (i > 0)
            {
                order_circles[i].transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            }
            order_circles[i].color = c;
        }

        List<UnityEngine.UI.RawImage> position_circles = new List<UnityEngine.UI.RawImage>();
        for (int i = 0; i < 4; i++)
        {
            position_circles.Add(intro_graphic_overlay.transform.GetChild(2).GetChild(1).GetChild(i).GetComponent<UnityEngine.UI.RawImage>());
            Color c = position_circles[i].color;
            c.a = 0.0f;
            position_circles[i].color = c;
        }

        List<UnityEngine.UI.RawImage> divider_elements = new List<UnityEngine.UI.RawImage>();
        for (int i = 0; i < 3; i++)
        {
            divider_elements.Add(intro_graphic_overlay.transform.GetChild(3).GetChild(i).GetComponent<UnityEngine.UI.RawImage>());
            Color c = divider_elements[i].color;
            c.a = 0.0f;
            divider_elements[i].color = c;
        }

        TMP_Text mission_objective_text = intro_graphic_overlay.transform.GetChild(4).GetComponent<TMP_Text>();
        mission_objective_text.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);

        List<TMP_Text> bullet_points = new List<TMP_Text>();
        for (int i = 0; i < 4; i++)
        {
            bullet_points.Add(intro_graphic_overlay.transform.GetChild(5 + i).GetComponent<TMP_Text>());
            Color c = bullet_points[i].color;
            c.a = 0.0f;
            bullet_points[i].color = c;
        }

        List<UnityEngine.UI.RawImage> position_icons = new List<UnityEngine.UI.RawImage>();
        for (int i = 0; i < 4; i++)
        {
            position_icons.Add(intro_graphic_overlay.transform.GetChild(9).GetChild(i).GetComponent<UnityEngine.UI.RawImage>());
            Color c = position_icons[i].color;
            c.a = 0.0f;
            position_icons[i].color = c;
            position_icons[i].transform.GetChild(0).GetComponent<TMP_Text>().color = c;
        }

        TMP_Text station_controls_text = intro_graphic_overlay.transform.GetChild(10).GetComponent<TMP_Text>();
        station_controls_text.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);

        UnityEngine.UI.RawImage exit_button = intro_graphic_overlay.transform.GetChild(11).GetComponent<UnityEngine.UI.RawImage>();
        exit_button.gameObject.SetActive(false);

        yield return new WaitForSeconds(delay);

        intro_graphic_overlay.SetActive(true);

        //background, border, dividers, circles, and "MISSION OBJECTIVE"
        float anim_time = 1.0f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            float background_a = Mathf.Lerp(0.9f, 0.0f, anim_time / 1.0f);
            float divider_a = Mathf.Lerp(0.84f, 0.0f, anim_time / 1.0f);
            float border_a = Mathf.Lerp(0.84f, 0.0f, anim_time / 1.0f);
            float pos_circle_a = Mathf.Lerp(1.0f, 0.0f, anim_time / 1.0f);
            float order_circle_a = Mathf.Lerp(0.04f, 0.0f, anim_time / 1.0f);

            foreach (UnityEngine.UI.RawImage component in background_elements)
            {
                component.color = new Color(0.0f, 0.0f, 0.0f, background_a);
            }

            foreach (UnityEngine.UI.RawImage divider in divider_elements)
            {
                Color c = divider.color;
                c.a = divider_a;
                divider.color = c;
            }

            foreach (UnityEngine.UI.RawImage pos_circle in position_circles)
            {
                Color c = pos_circle.color;
                c.a = pos_circle_a;
                pos_circle.color = c;
            }

            for (int i = 0; i < 5; i++)
            {
                Color c = order_circles[i].color;
                c.a = pos_circle_a;
                if (i > 0)
                {
                    c.a = order_circle_a;
                    order_circles[i].transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.0f, 0.0f, pos_circle_a);
                }
                order_circles[i].color = c;
            }

            foreach (UnityEngine.UI.RawImage border in borders)
            {
                Color c = border.color;
                c.a = border_a;
                border.color = c;
            }

            mission_objective_text.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(1.0f, 0.0f, anim_time / 1.0f));

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

                Color c = bullet_points[i].color;
                c.a = Mathf.Lerp(1.0f, 0.0f, anim_time / 0.5f);
                bullet_points[i].color = c;

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

                Color c = position_icons[i].color;
                c.a = Mathf.Lerp(1.0f, 0.0f, anim_time / 0.5f);
                position_icons[i].color = c;
                position_icons[i].transform.GetChild(0).GetComponent<TMP_Text>().color = c;

                yield return null;
            }
        }

        //"USE STATION CONTROLS..."
        anim_time = 1.0f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            station_controls_text.color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(1.0f, 0.0f, anim_time / 1.0f));

            yield return null;
        }

        //exit button
        exit_button.gameObject.SetActive(true);
        anim_time = 0.5f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            Color c = new Color(0.0f, 0.0f, 0.0f, Mathf.Lerp(0.9f, 0.0f, anim_time / 0.5f));
            exit_button.GetComponent<UnityEngine.UI.RawImage>().color = c;
            exit_button.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = c;
            exit_button.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = c;

            float border_a = Mathf.Lerp(0.84f, 0.0f, anim_time / 0.5f);
            foreach (Transform t in exit_button.transform.GetChild(2))
            {
                Color b = t.GetComponent<UnityEngine.UI.RawImage>().color;
                b.a = border_a;
                t.GetComponent<UnityEngine.UI.RawImage>().color = b;
            }

            exit_button.transform.GetChild(3).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(1.0f, 0.0f, anim_time / 0.5f));
            exit_button.transform.GetChild(4).GetComponent<TMP_Text>().color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(1.0f, 0.0f, anim_time / 0.5f));

            yield return null;
        }

        intro_graphic_display_coroutine = null;
    }
}