/*
    SecondaryScript.cs
    - Helps with secondary info that isn't primary control interactions
    Contributor(s): Jake Schott
    Last Updated: 2/5/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SecondaryScript : MonoBehaviour
{
    //CLASS CONSTANTS
    private static KeyCode[][] INFO_OVERLAY_INPUT_OPTIONS = new KeyCode[3][] { 
        new KeyCode[] { KeyCode.Alpha1, KeyCode.Keypad1 } ,                                                                      
        new KeyCode[] { KeyCode.Alpha2, KeyCode.Keypad2 } ,
        new KeyCode[] { KeyCode.Alpha3, KeyCode.Keypad3 } };
    
    public GameObject secondary_info;

    private GameObject position_overlay;
    private GameObject left_side;
    private GameObject right_side;
    private GameObject info_overlays;
    private GameObject intro_graphic_overlay;

    private float displayed_power = 0.0f;

    private Coroutine intro_graphic_display_coroutine = null;

    private void Start()
    {
        info_overlays = secondary_info.transform.GetChild(1).gameObject;
        position_overlay = secondary_info.transform.GetChild(0).gameObject;
        left_side = position_overlay.transform.GetChild(0).gameObject;
        right_side = position_overlay.transform.GetChild(1).gameObject;
    }

    public void toggleSecondaryInfoVisibility(bool active)
    {
        secondary_info.SetActive(active);
    }

    public void togglePositionOverlayVisibility(bool active)
    {
        position_overlay.SetActive(active);
    }

    public void toggleRightSideVisibility(bool active)
    {
        right_side.SetActive(active);
    }

    //shows/hides the information on the right side on tab press
    public void toggleControlInformationVisibility(HUDInfo temp_info)
    {
        bool currently_visible = right_side.transform.GetChild(1).GetChild(0).gameObject.activeSelf;
        right_side.transform.GetChild(1).GetChild(0).gameObject.SetActive(!currently_visible);
        right_side.transform.GetChild(1).GetChild(1).gameObject.SetActive(currently_visible);

        if (currently_visible == true)
        {
            right_side.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(1595f, -890f);
        }
        else
        {
            right_side.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(1595f, -530f + getControlInfoOffset(temp_info));
        }
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
        return Mathf.Max(100, temp_info.getInfo().Length * 4);
    }

    //updates right side control info (description and power consumption)
    public void updateSecondaryControlInformation(HUDInfo temp_info)
    {
        //determine whether to show or hide right side
        right_side.SetActive(temp_info.hasInfo());

        //determine whether to show or hide the power indicator
        right_side.transform.GetChild(0).gameObject.SetActive(temp_info.getConsumesPower());

        //set info frame title and description
        right_side.transform.GetChild(1).GetChild(0).GetChild(3).GetComponent<TMP_Text>().SetText(temp_info.getName());
        right_side.transform.GetChild(1).GetChild(0).GetChild(5).GetComponent<TMP_Text>().SetText(temp_info.getInfo());

        //resize based on length of control description
        int offset = getControlInfoOffset(temp_info);
        right_side.transform.GetChild(1).GetChild(0).GetChild(5).GetComponent<RectTransform>().sizeDelta = new Vector2(535f, offset);
        right_side.transform.GetChild(1).GetChild(0).GetChild(5).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -322f + (offset / 2));
        right_side.transform.GetChild(1).GetChild(0).GetChild(4).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -284f + offset);
        right_side.transform.GetChild(1).GetChild(0).GetChild(4).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -284f + offset);
        right_side.transform.GetChild(1).GetChild(0).GetChild(3).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -145f + offset);
        right_side.transform.GetChild(1).GetChild(0).GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -230f + (offset / 2));
        right_side.transform.GetChild(1).GetChild(0).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 365f + offset);
        right_side.transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -23f + offset);
        if (right_side.transform.GetChild(1).GetChild(0).gameObject.activeSelf == true)
        {
            right_side.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(1595f, -530f + offset);
        }
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
            right_side.transform.GetChild(0).GetChild(4).GetChild(i).GetChild(0).GetComponent<UnityEngine.UI.Image>().fillAmount = a;
        }
        displayed_power = temp_info.getPowerConsumption();
    }

    public void checkInfoOverlayInputs(bool force_hide)
    {
        bool inputted = false;
        int input = -1;
        while (inputted == false && input < 2)
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

        bool hide = info_overlays.transform.GetChild(1 + input).gameObject.activeSelf;
        for (int i = 0; i < 3; i++)
        {
            info_overlays.transform.GetChild(1 + i).gameObject.SetActive(false);
            info_overlays.transform.GetChild(0).GetChild(3).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
            info_overlays.transform.GetChild(0).GetChild(3).GetChild(i).GetChild(0).GetComponent<TMP_Text>().color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
        }

        info_overlays.transform.GetChild(1 + input).gameObject.SetActive(!hide && !force_hide);
        GetComponent<PrimaryScript>().setCursorVisibility(hide && !force_hide);
        float a = 0.1f;
        if (hide == false && force_hide == false)
        {
            a = 1.0f;
        }
        info_overlays.transform.GetChild(0).GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 1.0f, 1.0f, a);
        info_overlays.transform.GetChild(0).GetChild(3).GetChild(input).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 1.0f, 1.0f, a);
        info_overlays.transform.GetChild(0).GetChild(3).GetChild(input).GetChild(0).GetComponent<TMP_Text>().color = new Color(1.0f, 1.0f, 1.0f, a);
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
        info_overlays.transform.GetChild(0).gameObject.SetActive(true);

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
        info_overlays.transform.GetChild(0).gameObject.SetActive(false);

        //component assignment and transparency setting to 0
        intro_graphic_overlay = GameObject.Instantiate(info_overlays.transform.GetChild(1).gameObject, info_overlays.transform);
        intro_graphic_overlay.name = "Temporary";
        List<UnityEngine.UI.RawImage> background_elements = new List<UnityEngine.UI.RawImage>();
        for (int i = 0; i < 3; i++)
        {
            background_elements.Add(intro_graphic_overlay.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>());
            Color c = background_elements[i].color;
            c.a = 0.0f;
            background_elements[i].color = c;
        }

        List<UnityEngine.UI.RawImage> divider_elements = new List<UnityEngine.UI.RawImage>();
        for (int i = 0; i < 3; i++)
        {
            divider_elements.Add(intro_graphic_overlay.transform.GetChild(3 + i).GetComponent<UnityEngine.UI.RawImage>());
            Color c = divider_elements[i].color;
            c.a = 0.0f;
            divider_elements[i].color = c;
        }

        TMP_Text mission_objective_text = intro_graphic_overlay.transform.GetChild(6).GetComponent<TMP_Text>();
        mission_objective_text.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);

        List<TMP_Text> bullet_points = new List<TMP_Text>();
        for (int i = 0; i < 4; i++)
        {
            bullet_points.Add(intro_graphic_overlay.transform.GetChild(7 + i).GetComponent<TMP_Text>());
            Color c = bullet_points[i].color;
            c.a = 0.0f;
            bullet_points[i].color = c;
        }

        List<UnityEngine.UI.RawImage> position_icons = new List<UnityEngine.UI.RawImage>();
        for (int i = 0; i < 4; i++)
        {
            position_icons.Add(intro_graphic_overlay.transform.GetChild(11 + i).GetComponent<UnityEngine.UI.RawImage>());
            Color c = position_icons[i].color;
            c.a = 0.0f;
            position_icons[i].color = c;
            position_icons[i].transform.GetChild(0).GetComponent<TMP_Text>().color = c;
        }

        TMP_Text station_controls_text = intro_graphic_overlay.transform.GetChild(15).GetComponent<TMP_Text>();
        station_controls_text.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);

        UnityEngine.UI.RawImage exit_button = intro_graphic_overlay.transform.GetChild(16).GetComponent<UnityEngine.UI.RawImage>();
        exit_button.gameObject.SetActive(false);

        yield return new WaitForSeconds(delay);

        intro_graphic_overlay.SetActive(true);

        //background and "MISSION OBJECTIVE"
        float anim_time = 1.0f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            float background_a = Mathf.Lerp(0.96f, 0.0f, anim_time / 1.0f);
            float divider_a = Mathf.Lerp(0.84f, 0.0f, anim_time / 1.0f);

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

            Color c = new Color(0.0f, 0.0f, 0.0f, Mathf.Lerp(0.96f, 0.0f, anim_time / 0.5f));
            exit_button.GetComponent<UnityEngine.UI.RawImage>().color = c;
            exit_button.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = c;
            exit_button.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = c;

            exit_button.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(1.0f, 0.0f, anim_time / 0.5f));
            exit_button.transform.GetChild(3).GetComponent<TMP_Text>().color = new Color(1.0f, 1.0f, 1.0f, Mathf.Lerp(1.0f, 0.0f, anim_time / 0.5f));

            yield return null;
        }

        intro_graphic_display_coroutine = null;
    }
}