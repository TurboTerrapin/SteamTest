/*
    Button.cs
    - Stores information for a button
    - Handles button visualization
    Contributor(s): Jake Schott
    Last Updated: 8/7/2026
*/

using UnityEngine;
using TMPro;

public class Button
{
    //CLASS CONSTANTS
    private static float COLOR_CHANGE_FACTOR = 10.0f;
    private static Color DARK_GRAY = new Color(0.2f, 0.2f, 0.2f); //default color
    private static Color LIGHT_BLUE = new Color(0.0f, 0.38f, 0.46f); //being pressed

    //PRIVATE DATA MEMBERS
    private string button_desc; //ex. INCREASE
    private int control_index; //ex. 0 = KeyPad.W, based on array in PrimaryScript
    private bool interactable = true;
    private bool togglable = false;
    private bool currently_toggled = false; //used to stay blue during toggles
    private bool currently_visible = false; //used to determine whether to visually update
    private GameObject visual_button = null;
    private float percent_blue = 0.0f;
    private float adjusted_font_size = -1.0f;

    public Button(string button_desc, int control_index, bool interactable, bool togglable)
    {
        this.button_desc = button_desc;
        this.control_index = control_index;
        this.interactable = interactable;
        this.togglable = togglable;
    }

    public int getControlIndex()
    {
        return control_index;
    }

    public bool getInteractable()
    {
        return interactable;
    }

    public bool getTogglable()
    {
        return togglable;
    }

    public bool getToggled()
    {
        return currently_toggled;
    }

    public void updateVisibility(bool visibility)
    {
        currently_visible = visibility;
    }

    public void updateDesc(string new_desc)
    {
        button_desc = new_desc;
        if (visual_button != null && currently_visible == true)
        {
            string key = PrimaryScript.input_options[control_index][0].ToString();
            if (key == "Mouse0")
            {
                key = "LMB";
            }
            if (key.Contains("Alpha"))
            {
                key = key.Substring(5);
            }
            if (visual_button.transform.childCount > 2) //default view
            {
                visual_button.transform.GetChild(4).GetComponent<TMP_Text>().SetText(button_desc); 
            }
            else //list view
            {
                visual_button.transform.GetChild(0).GetComponent<TMP_Text>().SetText(button_desc);
            }
        }
    }

    public void updateInteractable(bool interactable)
    {
        this.interactable = interactable;
        if (visual_button != null && currently_visible == true)
        {
            if (visual_button.transform.childCount > 2) //default view
            {
                if (this.interactable == true)
                {
                    currently_toggled = false;
                    visual_button.transform.GetChild(4).GetComponent<TMP_Text>().alpha = 1.0f;
                    updateColor(1.0f);
                }
                else
                {
                    if (currently_toggled == false)
                    {
                        percent_blue = 0.0f;
                        visual_button.transform.GetChild(4).GetComponent<TMP_Text>().alpha = 0.2f;
                        updateColor(0.2f);
                    }
                    else
                    {
                        percent_blue = 1.0f;
                        updateColor(1.0f);
                    }
                }
            }
        }
    }

    public void toggle(float toggle_length)
    {
        currently_toggled = true;
        percent_blue = 1.0f;
        updateColor(1.0f);
        updateInteractable(false);
        if (visual_button != null && currently_visible == true)
        {
            if (visual_button.transform.childCount > 0) //ensures default format
            {
                PrimaryScript.Instance.transform.GetComponent<ButtonHelper>().toggleHelper(this, toggle_length);
            }
        }
    }

    public void toggle()
    {
        currently_toggled = true;
        percent_blue = 1.0f;
        updateColor(1.0f);
        updateInteractable(false);
    }

    public void untoggle()
    {
        currently_toggled = false;
        percent_blue = 0.0f;
        updateInteractable(interactable);
    }

    public void createVisual(int HUD_setting, int layout, int order_index, GameObject frame)
    {
        string key = PrimaryScript.input_options[control_index][0].ToString();
        if (key == "Mouse0")
        {
            key = "LMB";
        }
        if (key.Contains("Alpha"))
        {
            key = key.Substring(5);
        }

        //Default/Essential: Rounded format
        if (HUD_setting < 2)
        {
            //identify button
            visual_button = frame.transform.GetChild(4).GetChild(order_index).gameObject;

            //resize
            visual_button.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = HUDInfo.BUTTON_SIZES[layout][order_index] + new Vector2(-100f, 0f);
            float rounded_edge_position = (HUDInfo.BUTTON_SIZES[layout][order_index].x / 2) + 17f;
            visual_button.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(((-1f * (rounded_edge_position - 17)) + (rounded_edge_position - 117f)) / 2, 0f);
            visual_button.transform.GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector2(rounded_edge_position - 67f, 0f);
            visual_button.transform.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(-1f * rounded_edge_position, 0f);
            visual_button.transform.GetChild(3).GetComponent<RectTransform>().anchoredPosition = new Vector2(rounded_edge_position, 0f);
            visual_button.transform.GetChild(4).GetComponent<RectTransform>().anchoredPosition = new Vector2(((-1f * rounded_edge_position) + (rounded_edge_position - 134f)) / 2, 0f);
            visual_button.transform.GetChild(5).GetComponent<RectTransform>().anchoredPosition = new Vector2(rounded_edge_position - 52f, 0f);

            //handle rounded edges
            if (HUDInfo.BUTTON_TEMPLATES[layout][order_index] == 1) //left 
            {
                visual_button.transform.GetChild(2).GetComponent<UnityEngine.UI.Image>().sprite = null;
            }
            else if (HUDInfo.BUTTON_TEMPLATES[layout][order_index] == 2) //right
            {
                visual_button.transform.GetChild(3).GetComponent<UnityEngine.UI.Image>().sprite = null;
            }
            else if (HUDInfo.BUTTON_TEMPLATES[layout][order_index] == 3) //rectangle
            {
                visual_button.transform.GetChild(2).GetComponent<UnityEngine.UI.Image>().sprite = null;
                visual_button.transform.GetChild(3).GetComponent<UnityEngine.UI.Image>().sprite = null;
            }

            //position
            visual_button.GetComponent<RectTransform>().anchoredPosition = new Vector3(HUDInfo.BUTTON_POSITIONS[layout][order_index].x, HUDInfo.BUTTON_POSITIONS[layout][order_index].y);

            //make transparent if non-interactable
            if (interactable == false)
            {
                updateInteractable(false);
            }

            //set text info
            visual_button.transform.GetChild(4).GetComponent<TMP_Text>().SetText(button_desc); //set desc of that control
            visual_button.transform.GetChild(5).GetChild(0).gameObject.SetActive(key.CompareTo("LMB") == 0);
            visual_button.transform.GetChild(5).GetChild(1).gameObject.SetActive(key.CompareTo("LMB") != 0);
            visual_button.transform.GetChild(5).GetChild(1).GetChild(0).GetComponent<TMP_Text>().SetText(key);

            if (adjusted_font_size > 0.0f)
            {
                visual_button.transform.GetChild(4).GetComponent<TMP_Text>().fontSizeMax = adjusted_font_size;
            }
        }
        //Minimized: List format
        else if (HUD_setting == 2)
        {
            //identify button
            if (key.CompareTo("LMB") == 0)
            {
                visual_button = frame.transform.GetChild(0).gameObject;
            }
            else
            {
                visual_button = frame.transform.GetChild(order_index + 1).gameObject;
                visual_button.transform.GetChild(1).GetComponent<TMP_Text>().SetText(key);
            }

            //position button
            float horizontal_position = visual_button.GetComponent<RectTransform>().anchoredPosition.x;
            visual_button.GetComponent<RectTransform>().anchoredPosition = new Vector2(horizontal_position, (85f * (HUDInfo.BUTTON_POSITIONS[layout].Length - order_index - 1)) - 1025f);
            
            //set text info
            visual_button.transform.GetChild(0).GetComponent<TMP_Text>().SetText(button_desc);
        }
        if (visual_button != null)
        {
            visual_button.SetActive(true);
        }
    }

    public void setMaxFontSize(float new_max)
    {
        adjusted_font_size = new_max;
    }

    //helper method 
    private void updateColor(float transparency)
    {
        if (visual_button != null && currently_visible == true)
        {
            if (visual_button.transform.childCount > 2) //means default format
            {
                Color temp_color =
                    new Color(DARK_GRAY.r * (1.0f - percent_blue),
                              DARK_GRAY.g + (LIGHT_BLUE.g - DARK_GRAY.g) * percent_blue,
                              DARK_GRAY.b + (LIGHT_BLUE.b - DARK_GRAY.b) * percent_blue,
                              transparency);
                visual_button.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().color = temp_color;
                visual_button.transform.GetChild(2).GetComponent<UnityEngine.UI.Image>().color = temp_color;
            }
        }
    }

    public void highlight(float delta_time)
    {
        if (interactable == true)
        {
            percent_blue = Mathf.Min(1.0f, percent_blue + delta_time * COLOR_CHANGE_FACTOR);
            updateColor(1.0f);
        }
    }

    public void darken(float delta_time)
    {
        if (interactable == true)
        {
            percent_blue = Mathf.Max(0.0f, percent_blue - delta_time * COLOR_CHANGE_FACTOR);
            updateColor(1.0f);
        }
    }
}