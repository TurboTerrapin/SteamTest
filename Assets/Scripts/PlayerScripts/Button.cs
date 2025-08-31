/*
    Button.cs
    - Stores information for a button
    - Handles button, divider GUI
    Contributor(s): Jake Schott
    Last Updated: 8/31/2025
*/

/*
    ***READ ME!***
    
    This script handles UI layouts for buttons and the trapezoid. Every button is fed the layout index (LAYOUT #),
    the index of the button within that layout (left-to-right, top-bottom), and the corresponding frame (trapezoid or not).

    The layout descriptions are listed below:

    LAYOUT 0: 1 BUTTON, CENTERED (ex. character select)

    LAYOUT 1: 2 TOUCHING BUTTONS, BOTH CENTERED, DIVIDED BY A DIVIDER (ex. impulse throttle)

    LAYOUT 2: 3 BUTTONS, ALL SEPARATED (ex. inertial dampeners)

    LAYOUT 3: 4 BUTTONS, ALL SEPARATED (ex. hangar clamps)

    LAYOUT 4: 4 BUTTONS ALL CONNECTED BOTTOM ROW, 2 BUTTONS SEPARATED TOP ROW (ex. regulations manual)

    LAYOUT 5: 3 BUTTONS, 1 SEPARATED ON LEFT, 2 TOUCHING ON RIGHT (ex. map options)

    LAYOUT 6: 1 BUTTON, CENTERED, ELONGATED (ex. tractor beam incinerator, used for extra long titles)

    LAYOUT 7: 2 TOUCHING BUTTONS, BOTH CENTERED, DIVIDED BY A DIVER, ELONGATED (ex. engineer power allocation)
*/

using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class Button
{
    //CLASS CONSTANTS
    private static float COLOR_CHANGE_FACTOR = 10.0f;
    private static Color DARK_GRAY = new Color(0.22f, 0.22f, 0.22f, 0.36f); //default color
    private static Color LIGHT_BLUE = new Color(0.12f, 0.57f, 0.69f, 0.36f); //being pressed

    //BUTTON LAYOUT INFORMATION
    private static Vector2[] trapezoid_sizes = new Vector2[]
    {
        new Vector2(1100f, 250f),
        new Vector2(1250f, 250f),
        new Vector2(1800f, 250f),
        new Vector2(2300f, 250f),
        new Vector2(1600f, 350f),
        new Vector2(1600f, 250f),
        new Vector2(1400f, 250f),
        new Vector2(1400f, 250f)
    };

    private static float[] title_sizes = new float[]
    {
        700f,
        700f,
        700f,
        700f,
        900f,
        700f,
        1200f,
        1150f
    };

    private static List<Vector2[]> button_positions = new List<Vector2[]>
    {
        new Vector2[] {new Vector2(0f, -45f)},
        new Vector2[] {new Vector2(-294f, -45f), new Vector2(294f, -45f)},
        new Vector2[] {new Vector2(-600f, -45f), new Vector2(0f, -45f), new Vector2(600f, -45f)},
        new Vector2[] {new Vector2(-863f, -45f), new Vector2(-288f, -45f), new Vector2(288f, -45f), new Vector2(863f, -45f)},
        new Vector2[] {new Vector2(-315f, 15f), new Vector2(315f, 15f), new Vector2(-582f, -90f), new Vector2(-194f, -90f), new Vector2(194f, -90f), new Vector2(582f, -90f)},
        new Vector2[] {new Vector2(-510f, -45f), new Vector2(48f, -45f), new Vector2(536f, -45f)},
        new Vector2[] {new Vector2(0f, -45f)},
        new Vector2[] {new Vector2(-294f, -45f), new Vector2(294f, -45f)}
    };

    private static List<int[]> button_templates = new List<int[]>
    {
        new int[] {0},
        new int[] {1, 2},
        new int[] {0, 0, 0},
        new int[] {0, 0, 0, 0},
        new int[] {0, 0, 1, 3, 3, 2},
        new int[] {0, 1, 2},
        new int[] {0},
        new int[] {1, 2}
    };

    private static List<Vector2[]> button_sizes = new List<Vector2[]>
    {
        new Vector2[] {new Vector2(600f, 80f)},
        new Vector2[] {new Vector2(500f, 80f), new Vector2(500f, 80f)},
        new Vector2[] {new Vector2(500f, 80f), new Vector2(500f, 80f), new Vector2(500f, 80f)},
        new Vector2[] {new Vector2(450f, 80f), new Vector2(450f, 80f), new Vector2(450f, 80f), new Vector2(450f, 80f)},
        new Vector2[] {new Vector2(500f, 80f), new Vector2(500f, 80f), new Vector2(300f, 80f), new Vector2(300f, 80f), new Vector2(300f, 80f), new Vector2(300f, 80f)},
        new Vector2[] {new Vector2(450f, 80f), new Vector2(400f, 80f), new Vector2(400f, 80f)},
        new Vector2[] {new Vector2(600f, 80f)},
        new Vector2[] {new Vector2(500f, 80f), new Vector2(500f, 80f)}
    };

    private static List<Vector2[]> divider_positions = new List<Vector2[]>
    {
        new Vector2[] {},
        new Vector2[] {new Vector2(0f, -45f)},
        new Vector2[] {},
        new Vector2[] {},
        new Vector2[] {new Vector2(-388f, -90f), new Vector2(0f, -90f), new Vector2(388f, -90f)},
        new Vector2[] {new Vector2(292f, -45f)},
        new Vector2[] {},
        new Vector2[] {new Vector2(0f, -45f)}
    };

    //PRIVATE DATA MEMBERS
    private string button_desc; //ex. INCREASE
    private int control_index; //ex. 0 = KeyPad.W, based on array in ControlScript
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
            string key = ControlScript.input_options[control_index][0].ToString();
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
                    updateColor(0.36f);
                }
                else
                {
                    if (currently_toggled == false)
                    {
                        percent_blue = 0.0f;
                        visual_button.transform.GetChild(2).GetComponent<TMP_Text>().color = new Color(1f, 1f, 1f, 0.05f);
                        updateColor(0.05f);
                    }
                    else
                    {
                        percent_blue = 1.0f;
                        updateColor(0.36f);
                    }
                }
            }
        }
    }
    public void toggle(float toggle_length)
    {
        currently_toggled = true;
        percent_blue = 1.0f;
        updateColor(0.36f);
        updateInteractable(false);
        if (visual_button != null)
        {
            if (visual_button.transform.childCount > 0) //ensures trapezoid format
            {
                visual_button.transform.parent.GetComponent<ButtonHelper>().toggleHelper(this, toggle_length);
            }
        }
    }
    public void toggle()
    {
        currently_toggled = true;
        percent_blue = 1.0f;
        updateColor(0.36f);
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
        string key = ControlScript.input_options[control_index][0].ToString();
        if (key == "Mouse0")
        {
            key = "LMB";
        }
        if (key.Contains("Alpha"))
        {
            key = key.Substring(5);
        }

        //Default: Trapezoidal format
        if (HUD_setting == 0)
        {
            //define buttons panel
            GameObject buttons_panel = frame.transform.GetChild(4).gameObject;

            //copy button
            visual_button = UnityEngine.Object.Instantiate(buttons_panel.transform.GetChild(0).gameObject, buttons_panel.transform);

            //resize
            visual_button.GetComponent<RectTransform>().sizeDelta = button_sizes[layout][order_index];
            visual_button.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector3(-1f * (button_sizes[layout][order_index].x / 2 + 20f), 0f, 0f);
            visual_button.transform.GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector3(button_sizes[layout][order_index].x / 2 + 20f, 0f, 0f);
            visual_button.transform.GetChild(2).GetComponent<RectTransform>().sizeDelta = button_sizes[layout][order_index];

            //handle rounded edges
            if (button_templates[layout][order_index] == 1) //left 
            {
                visual_button.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().sprite = null;
            }
            else if (button_templates[layout][order_index] == 2) //right
            {
                visual_button.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().sprite = null;
            }
            else if (button_templates[layout][order_index] == 3) //rectangle
            {
                visual_button.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().sprite = null;
                visual_button.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().sprite = null;
            }

            //position
            visual_button.GetComponent<RectTransform>().anchoredPosition = new Vector3(button_positions[layout][order_index].x, button_positions[layout][order_index].y, 0f);

            //handle trapezoid size, positioning, and dividers
            if (order_index == 0)
            {
                //trapezoid height/vertical position
                frame.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(0, trapezoid_sizes[layout].y);
                frame.transform.GetComponent<RectTransform>().anchoredPosition = new Vector3(0f, -1080f + trapezoid_sizes[layout].y / 2, 0f);
                //trapezoid center
                frame.transform.GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(trapezoid_sizes[layout].x, 0f);
                //trapezoid edge triangles
                frame.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector3(-1f * (trapezoid_sizes[layout].x / 2 + 75f), 0f, 0f);
                frame.transform.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector3(trapezoid_sizes[layout].x / 2 + 75f, 0f, 0f);
                if (divider_positions[layout].Length > 0)
                {
                    for (int i = 0; i < divider_positions[layout].Length; i++)
                    {
                        //copy divider
                        GameObject divider = UnityEngine.Object.Instantiate(buttons_panel.transform.GetChild(1).gameObject, buttons_panel.transform);

                        //position
                        divider.GetComponent<RectTransform>().anchoredPosition = new Vector3(divider_positions[layout][i].x, divider_positions[layout][i].y, 0f);

                        divider.name = "DIVIDER" + i;
                        divider.SetActive(true);
                    }
                }
                //handle title size
                frame.transform.GetChild(3).GetComponent<RectTransform>().sizeDelta = new Vector2(title_sizes[layout], 80f);
            }

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
        else if (HUD_setting == 1)
        {
            //copy button
            visual_button = UnityEngine.Object.Instantiate(frame.transform.GetChild(0).gameObject, frame.transform);

            //position button
            visual_button.GetComponent<RectTransform>().anchoredPosition = new Vector3(-1655f, (40f * (button_positions[layout].Length - order_index - 1)) - 1050f, 0f);
            
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
                    new Color(DARK_GRAY.r * (1 - percent_blue),
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
            updateColor(0.36f);
        }
    }
    public void darken(float delta_time)
    {
        if (interactable == true)
        {
            percent_blue = Mathf.Max(0.0f, percent_blue - delta_time * COLOR_CHANGE_FACTOR);
            updateColor(0.36f);
        }
    }
}
