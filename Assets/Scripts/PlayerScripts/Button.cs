/*
    Button.cs
    - Stores information for a button
    - Handles button, divider GUI
    Contributor(s): Jake Schott
    Last Updated: 3/2/2026
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
    private GameObject visual_button;
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

    public void updateDesc(string new_desc)
    {
        button_desc = new_desc;
        if (visual_button != null)
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
            if (visual_button.transform.childCount > 0) //trapezoid view
            {
                visual_button.transform.GetChild(2).gameObject.GetComponent<TMP_Text>().SetText(button_desc + " (" + key + ")"); 
            }
            else //list view
            {
                visual_button.GetComponent<TMP_Text>().SetText(button_desc + " - " + key);
            }
        }
    }
    public void updateInteractable(bool interactable)
    {
        this.interactable = interactable;
        if (visual_button != null)
        {
            if (visual_button.transform.childCount > 0) //trapezoid view
            {
                if (this.interactable == true)
                {
                    currently_toggled = false;
                    visual_button.transform.GetChild(2).GetComponent<TMP_Text>().color = new Color(1f, 1f, 1f, 1f);
                    updateColor(1.0f);
                }
                else
                {
                    if (currently_toggled == false)
                    {
                        percent_blue = 0.0f;
                        visual_button.transform.GetChild(2).GetComponent<TMP_Text>().color = new Color(1f, 1f, 1f, 0.2f);
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
        if (visual_button != null)
        {
            if (visual_button.transform.childCount > 0) //ensures trapezoid format
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

        //Default: Trapezoidal format
        if (HUD_setting < 2)
        {
            //define buttons panel
            GameObject buttons_panel = frame.transform.GetChild(4).gameObject;

            //copy button
            visual_button = UnityEngine.Object.Instantiate(buttons_panel.transform.GetChild(0).gameObject, buttons_panel.transform);

            //resize
            visual_button.GetComponent<RectTransform>().sizeDelta = HUDInfo.BUTTON_SIZES[layout][order_index];
            visual_button.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector3(-1f * (HUDInfo.BUTTON_SIZES[layout][order_index].x / 2 + 17f), 0f, 0f);
            visual_button.transform.GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector3(HUDInfo.BUTTON_SIZES[layout][order_index].x / 2 + 17f, 0f, 0f);
            visual_button.transform.GetChild(2).GetComponent<RectTransform>().sizeDelta = HUDInfo.BUTTON_SIZES[layout][order_index];

            //handle rounded edges
            if (HUDInfo.BUTTON_TEMPLATES[layout][order_index] == 1) //left 
            {
                visual_button.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().sprite = null;
            }
            else if (HUDInfo.BUTTON_TEMPLATES[layout][order_index] == 2) //right
            {
                visual_button.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().sprite = null;
            }
            else if (HUDInfo.BUTTON_TEMPLATES[layout][order_index] == 3) //rectangle
            {
                visual_button.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().sprite = null;
                visual_button.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().sprite = null;
            }

            //position
            visual_button.GetComponent<RectTransform>().anchoredPosition = new Vector3(HUDInfo.BUTTON_POSITIONS[layout][order_index].x, HUDInfo.BUTTON_POSITIONS[layout][order_index].y, 0f);

            //make transparent if non-interactable
            if (interactable == false)
            {
                updateInteractable(false);
            }

            //set text info
            visual_button.transform.GetChild(2).GetComponent<TMP_Text>().SetText(button_desc + " (" + key + ")"); //set desc of that control

            if (adjusted_font_size > 0.0f)
            {
                visual_button.transform.GetChild(2).GetComponent<TMP_Text>().fontSizeMax = adjusted_font_size;
            }
        }
        //Minimized: List format
        else if (HUD_setting == 2)
        {
            //copy button
            visual_button = UnityEngine.Object.Instantiate(frame.transform.GetChild(0).gameObject, frame.transform);

            //position button
            visual_button.GetComponent<RectTransform>().anchoredPosition = new Vector3(-1655f, (40f * (HUDInfo.BUTTON_POSITIONS[layout].Length - order_index - 1)) - 1050f, 0f);
            
            //set text info
            visual_button.GetComponent<TMP_Text>().SetText(button_desc + " - " + key);
        }
        if (visual_button != null)
        {
            visual_button.name = button_desc;
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
        if (visual_button != null)
        {
            if (visual_button.transform.childCount > 0) //means trapezoid format
            {
                Color temp_color =
                    new Color(DARK_GRAY.r * (1.0f - percent_blue),
                              DARK_GRAY.g + (LIGHT_BLUE.g - DARK_GRAY.g) * percent_blue,
                              DARK_GRAY.b + (LIGHT_BLUE.b - DARK_GRAY.b) * percent_blue,
                              transparency);
                visual_button.GetComponent<UnityEngine.UI.Image>().color = temp_color;
                visual_button.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().color = temp_color;
                visual_button.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().color = temp_color;
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