/*
    HUDInfo.cs
    - Stores information for the onscreen UI indicator that appears when facing a control
        - Includes control title and button information
    Contributor(s): Jake Schott
    Last Updated: 8/9/2026
*/

/*
    ***READ ME!***
    
    This script handles UI layouts for buttons and the default frame. Every button is fed the layout index (LAYOUT #),
    the index of the button within that layout (left-to-right, top-bottom), and the corresponding frame (default or not).

    The layout descriptions are listed below:

    LAYOUT 0: 1 BUTTON, CENTERED (ex. character select)

    LAYOUT 1: 2 TOUCHING BUTTONS, BOTH CENTERED, DIVIDED BY A DIVIDER (ex. impulse throttle)

    LAYOUT 2: 3 BUTTONS, ALL SEPARATED (ex. inertial dampeners)

    LAYOUT 3: 4 BUTTONS, ALL SEPARATED (ex. hangar clamps)

    LAYOUT 4: 4 BUTTONS ALL CONNECTED BOTTOM ROW, 2 BUTTONS SEPARATED TOP ROW (ex. regulations manual)

    LAYOUT 5: 3 BUTTONS, 1 SEPARATED ON LEFT, 2 TOUCHING ON RIGHT (ex. map options)

    LAYOUT 6: 1 BUTTON, CENTERED, ELONGATED (ex. tractor beam incinerator, used for extra long titles)

    LAYOUT 7: 2 TOUCHING BUTTONS, BOTH CENTERED, DIVIDED BY A DIVER, ELONGATED (ex. engineer power allocation)

    LAYOUT 8: 2 SETS OF 2 TOUCHING BUTTONS, BOTH SETS DIVIDED BY A DIVIDER, ELONGATED (ex. probe lateral movement)

    LAYOUT 9: 2 SETS OF 2 TOUCHING BUTTONS PLUS ONE CENTER BUTTON, BOTH SETS DIVIDED BY A DIVIDER, ELONGATED (ex. computer regulator)
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUDInfo
{
    //BUTTON LAYOUT INFORMATION
    public static float[] FRAME_WIDTHS = new float[]
    {
        800f,
        950f,
        1500f,
        2000f,
        1100f,
        1450f,
        1100f,
        1100f,
        1650f,
        1800f
    };

    public static float[] FRAME_HEIGHT_OPTIONS = new float[] { 200f, 260f, 360f };

    public static int[] FRAME_HEIGHT_INDEXES = new int[]
    {
        1,
        1,
        1,
        1,
        2,
        1,
        1,
        1,
        1,
        1
    };

    public static float[] TITLE_SIZES = new float[]
    {
        850f,
        850f,
        850f,
        1150f,
        1050f,
        1150f,
        1350f,
        1300f,
        1150f,
        1150f
    };

    public static List<Vector2[]> BUTTON_POSITIONS = new List<Vector2[]>
    {
        new Vector2[] {new Vector2(0f, -65f)},
        new Vector2[] {new Vector2(-294f, -65f), new Vector2(294f, -65f) },
        new Vector2[] {new Vector2(-600f, -65f), new Vector2(0f, -65f), new Vector2(600f, -65f) },
        new Vector2[] {new Vector2(-863f, -65f), new Vector2(-288f, -65f), new Vector2(288f, -65f), new Vector2(863f, -65f) },
        new Vector2[] {new Vector2(-315f, -10f), new Vector2(315f, -10f), new Vector2(-582f, -105f), new Vector2(-194f, -105f), new Vector2(194f, -105f), new Vector2(582f, -105f)},
        new Vector2[] {new Vector2(-520f, -65f), new Vector2(113f, -65f), new Vector2(601f, -65f)},
        new Vector2[] {new Vector2(0f, -65f)},
        new Vector2[] {new Vector2(-294f, -65f), new Vector2(294f, -65f)},
        new Vector2[] {new Vector2(-748f, -65f), new Vector2(-260f, -65f), new Vector2(260f, -65f), new Vector2(748f, -65f)},
        new Vector2[] {new Vector2(-843f, -65f), new Vector2(-475f, -65f), new Vector2(0f, -65f), new Vector2(475f, -65f), new Vector2(843f, -65f)}
    };

    public static List<int[]> BUTTON_TEMPLATES = new List<int[]>
    {
        new int[] {0},
        new int[] {2, 1},
        new int[] {0, 0, 0},
        new int[] {0, 0, 0, 0},
        new int[] {0, 0, 2, 3, 3, 1},
        new int[] {0, 2, 1},
        new int[] {0},
        new int[] {2, 1},
        new int[] {2, 1, 2, 1},
        new int[] {2, 1, 0, 2, 1}
    };

    public static List<Vector2[]> BUTTON_SIZES = new List<Vector2[]>
    {
        new Vector2[] {new Vector2(600f, 75f)},
        new Vector2[] {new Vector2(500f, 75f), new Vector2(500f, 75f) },
        new Vector2[] {new Vector2(500f, 75f), new Vector2(500f, 75f), new Vector2(500f, 75f) },
        new Vector2[] {new Vector2(450f, 75f), new Vector2(450f, 75f), new Vector2(450f, 75f), new Vector2(450f, 75f) },
        new Vector2[] {new Vector2(500f, 75f), new Vector2(500f, 75f), new Vector2(300f, 75f), new Vector2(300f, 75f), new Vector2(300f, 75f), new Vector2(300f, 75f) },
        new Vector2[] {new Vector2(560f, 75f), new Vector2(400f, 75f), new Vector2(400f, 75f) },
        new Vector2[] {new Vector2(600f, 75f) },
        new Vector2[] {new Vector2(500f, 75f), new Vector2(500f, 75f) },
        new Vector2[] {new Vector2(400f, 75f), new Vector2(400f, 75f), new Vector2(400f, 75f), new Vector2(400f, 75f) },
        new Vector2[] {new Vector2(280f, 75f), new Vector2(280f, 75f), new Vector2(400f, 75f), new Vector2(280f, 75f), new Vector2(280f, 75f) },
    };

    public static List<Vector2[]> DIVIDER_POSITIONS = new List<Vector2[]>
    {
        new Vector2[] {},
        new Vector2[] {new Vector2(0f, -65f)},
        new Vector2[] {},
        new Vector2[] {},
        new Vector2[] {new Vector2(-388f, -105f), new Vector2(0f, -105f), new Vector2(388f, -105f)},
        new Vector2[] {new Vector2(357f, -65f)},
        new Vector2[] {},
        new Vector2[] {new Vector2(0f, -65f)},
        new Vector2[] {new Vector2(-504f, -65f), new Vector2(504f, -65f)},
        new Vector2[] {new Vector2(-659f, -65f), new Vector2(659f, -65f)}
    };

    public static List<int[]> POWER_CIRCLE_POSITIONS = new List<int[]>
    {
        new int[] {0, 0, 0, 0, 0, 0},
        new int[] {-50, 0, 0, 0, 0, 0},
        new int[] {-72, -22, 23, 0, 0, 0},
        new int[] {-95, -45, 0, 45, 0, 0},
        new int[] {-117, -67, -22, 23, 68, 0},
        new int[] { -140, -90, -45, 0, 45, 90}
    };

    private string control_name; //ex. "IMPULSE THROTTLE"
    private int layout = -1;
    private bool consumes_power = false;
    private float power_consumption = 0.0f;
    private float maximum_consumption = 0.0f;
    private string info_msg = "";
    private List<Button> buttons = null;

    public HUDInfo(string title)
    {
        control_name = title;
    }

    public HUDInfo(string title, bool is_powerable, float max_possible_consumption)
    {
        control_name = title;
        consumes_power = is_powerable;
        maximum_consumption = max_possible_consumption;
    }

    public void initializeDefaultFrame(Transform frame)
    {
        float frame_width = 50f + (control_name.Length * 50f);
        float frame_height = FRAME_HEIGHT_OPTIONS[0];
        float header_offset = -95f;
        float extension_offset = 0f;
        float title_size = frame_width;
        int height_index = 0;

        if (numOptions() > 0)
        {
            frame_width = HUDInfo.FRAME_WIDTHS[layout];
            frame_height = HUDInfo.FRAME_HEIGHT_OPTIONS[HUDInfo.FRAME_HEIGHT_INDEXES[layout]];
            header_offset = -80f;
            title_size = TITLE_SIZES[layout];
            height_index = HUDInfo.FRAME_HEIGHT_INDEXES[layout];

            //handle dividers
            if (DIVIDER_POSITIONS[layout].Length > 0)
            {
                for (int i = 0; i < HUDInfo.DIVIDER_POSITIONS[layout].Length; i++)
                {
                    //set divider
                    GameObject divider = frame.transform.GetChild(5).GetChild(i).gameObject;
                    divider.GetComponent<RectTransform>().anchoredPosition = new Vector2(HUDInfo.DIVIDER_POSITIONS[layout][i].x, HUDInfo.DIVIDER_POSITIONS[layout][i].y);
                    divider.SetActive(true);
                }
            }
        }

        //set title
        frame.transform.GetChild(3).GetChild(0).GetComponent<TMP_Text>().SetText(control_name);

        //adjust power circles
        frame.transform.GetChild(3).GetChild(1).gameObject.SetActive(consumes_power);
        if (consumes_power == true)
        {
            if (numOptions() > 0)
            {
                header_offset = -25f;
            }
            else
            {
                header_offset = -45f;
            }

            extension_offset = 40f;
            int circles_visible = Mathf.CeilToInt(maximum_consumption * 10.0f);
            for (int i = 0; i < 6; i++)
            {
                frame.transform.GetChild(3).GetChild(1).GetChild(i).gameObject.SetActive(i <= circles_visible);
                frame.transform.GetChild(3).GetChild(1).GetChild(i).GetComponent<RectTransform>().anchoredPosition = new Vector2(POWER_CIRCLE_POSITIONS[circles_visible][i], 0);
            }
        }

        //position frame
        frame.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -1080f + (frame_height / 2));
        //resize background center and corners
        frame.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, extension_offset);
        frame.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(frame_height, frame_height);
        frame.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(frame_width, frame_height);
        frame.transform.GetChild(0).GetChild(2).GetComponent<RectTransform>().sizeDelta = new Vector2(frame_height, frame_height);
        //position background corners
        frame.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(-1f * (frame_width / 2 + (frame_height / 2)), 0f);
        frame.transform.GetChild(0).GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(frame_width / 2 + (frame_height / 2), 0f);
        //handle border
        frame.transform.GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, extension_offset);
        for (int i = 0; i < 3; i++)
        {
            frame.transform.GetChild(1).GetChild(0).GetChild(i).gameObject.SetActive(i == height_index);
            frame.transform.GetChild(1).GetChild(2).GetChild(i).gameObject.SetActive(i == height_index);
        }
        frame.transform.GetChild(1).GetChild(0).GetChild(height_index).GetComponent<RectTransform>().anchoredPosition = new Vector2(-1f * (frame_width / 2 + (frame_height / 2)) - 5f, 5f);
        frame.transform.GetChild(1).GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, (frame_height / 2) + 5f);
        frame.transform.GetChild(1).GetChild(1).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(-(frame_width / 2) + 85f, 0f);
        frame.transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector2(55f, 0f);
        frame.transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(frame_width - 150f, 10f);
        frame.transform.GetChild(1).GetChild(2).GetChild(height_index).GetComponent<RectTransform>().anchoredPosition = new Vector2((frame_width / 2 + (frame_height / 2)) + 5f, 5f);
        //handle extension
        frame.transform.GetChild(2).gameObject.SetActive(consumes_power);
        if (consumes_power == true)
        {
            frame.transform.GetChild(2).GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(frame_width + (frame_height * 2), 50f);
            frame.transform.GetChild(2).GetChild(1).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(-1f * ((frame_width / 2) + frame_height + 5f), -155f);
            frame.transform.GetChild(2).GetChild(1).GetChild(1).GetComponent<RectTransform>().anchoredPosition = new Vector2((frame_width / 2) + frame_height + 5f, -155f);
            frame.transform.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -1f * ((frame_height - 260f) / 2));
        }
        //position header
        frame.transform.GetChild(3).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, (frame_height / 2) + header_offset);
        //handle title size
        frame.transform.GetChild(3).GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(title_size, 80f);
    }

    public void setButtons(List<Button> buttons)
    {
        this.buttons = buttons;
        this.layout = buttons.Count - 1;
    }

    public void adjustButtonFontSizes(float new_font_size)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].setMaxFontSize(new_font_size);
        }
    }

    public void setButtons(List<Button> buttons, int new_layout)
    {
        this.buttons = buttons;
        this.layout = new_layout;
    }

    public void setTitle(string new_title)
    {
        this.control_name = new_title;
    }

    public void setInfo(string control_info)
    {
        this.info_msg = control_info;
    }

    public void setLayout(int new_layout)
    {
        this.layout = new_layout;
    }

    public void setPowerConsumption(float pwr_consumption)
    {
        power_consumption = pwr_consumption;
    }

    public void setMaxPowerConsumption(float max_pwr_consumption)
    {
        maximum_consumption = max_pwr_consumption;
    }

    public List<Button> getButtons()
    {
        return buttons;
    }

    public bool hasInfo()
    {
        return info_msg.CompareTo("") != 0;
    }

    public bool getConsumesPower()
    {
        return consumes_power;
    }

    public float getPowerConsumption()
    {
        return power_consumption;
    }

    public string getName()
    {
        return control_name;
    }

    public string getInfo()
    {
        return info_msg;
    }

    public int numOptions()
    {
        if (buttons == null)
        {
            return 0;
        }
        return buttons.Count;
    }

    public int getLayout()
    {
        return layout;
    }
}