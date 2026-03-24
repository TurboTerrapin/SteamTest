/*
    HUDInfo.cs
    - Stores information for the onscreen UI indicator that appears when facing a control
        - Includes control title and button information
    Contributor(s): Jake Schott
    Last Updated: 3/2/2026
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
        1700f
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
        new Vector2[] {new Vector2(-798f, -65f), new Vector2(-460f, -65f), new Vector2(0f, -65f), new Vector2(460f, -65f), new Vector2(798f, -65f)}
    };

    public static List<int[]> BUTTON_TEMPLATES = new List<int[]>
    {
        new int[] {0},
        new int[] {1, 2},
        new int[] {0, 0, 0},
        new int[] {0, 0, 0, 0},
        new int[] {0, 0, 1, 3, 3, 2},
        new int[] {0, 1, 2},
        new int[] {0},
        new int[] {1, 2},
        new int[] {1, 2, 1, 2},
        new int[] {1, 2, 0, 1, 2}
    };

    public static List<Vector2[]> BUTTON_SIZES = new List<Vector2[]>
    {
        new Vector2[] {new Vector2(600f, 68f)},
        new Vector2[] {new Vector2(500f, 68f), new Vector2(500f, 68f) },
        new Vector2[] {new Vector2(500f, 68f), new Vector2(500f, 68f), new Vector2(500f, 68f) },
        new Vector2[] {new Vector2(450f, 68f), new Vector2(450f, 68f), new Vector2(450f, 68f), new Vector2(450f, 68f) },
        new Vector2[] {new Vector2(500f, 68f), new Vector2(500f, 68f), new Vector2(300f, 68f), new Vector2(300f, 68f), new Vector2(300f, 68f), new Vector2(300f, 68f) },
        new Vector2[] {new Vector2(560f, 68f), new Vector2(400f, 68f), new Vector2(400f, 68f) },
        new Vector2[] {new Vector2(600f, 68f) },
        new Vector2[] {new Vector2(500f, 68f), new Vector2(500f, 68f) },
        new Vector2[] {new Vector2(400f, 68f), new Vector2(400f, 68f), new Vector2(400f, 68f), new Vector2(400f, 68f) },
        new Vector2[] {new Vector2(250f, 68f), new Vector2(250f, 68f), new Vector2(400f, 68f), new Vector2(250f, 68f), new Vector2(250f, 68f) },
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
        new Vector2[] {new Vector2(-629f, -65f), new Vector2(629f, -65f)}
    };

    private string control_name; //ex. "IMPULSE THROTTLE"
    private int layout = -1;
    private bool consumes_power = false;
    private float power_consumption = 0.0f;
    private string info_msg = "";
    private List<Button> buttons = null;

    public HUDInfo(string title)
    {
        control_name = title;
    }

    public HUDInfo(string title, bool is_powerable)
    {
        control_name = title;
        consumes_power = is_powerable;
    }

    public void initializeDefaultFrame(Transform frame)
    {
        float frame_width = 50f + (control_name.Length * 50f);
        float frame_height = FRAME_HEIGHT_OPTIONS[0];
        float header_offset = -80f;
        float title_size = frame_width;
        int height_index = 0;

        if (numOptions() > 0)
        {
            frame_width = HUDInfo.FRAME_WIDTHS[layout];
            frame_height = HUDInfo.FRAME_HEIGHT_OPTIONS[HUDInfo.FRAME_HEIGHT_INDEXES[layout]];
            header_offset = -70f;
            title_size = TITLE_SIZES[layout];
            height_index = HUDInfo.FRAME_HEIGHT_INDEXES[layout];

            //handle dividers
            if (DIVIDER_POSITIONS[layout].Length > 0)
            {
                for (int i = 0; i < HUDInfo.DIVIDER_POSITIONS[layout].Length; i++)
                {
                    //copy divider
                    GameObject divider = UnityEngine.Object.Instantiate(frame.transform.GetChild(3).GetChild(1).gameObject, frame.transform.GetChild(3).transform);
                    divider.name = "DIVIDER" + i;
                    divider.SetActive(true);

                    //position
                    divider.GetComponent<RectTransform>().anchoredPosition = new Vector3(HUDInfo.DIVIDER_POSITIONS[layout][i].x, HUDInfo.DIVIDER_POSITIONS[layout][i].y, 0f);
                }
            }
        }

        //position frame
        frame.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -1080f + (frame_height / 2));
        //resize center and corners
        frame.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(frame_height, frame_height);
        frame.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(frame_width, frame_height);
        frame.transform.GetChild(0).GetChild(2).GetComponent<RectTransform>().sizeDelta = new Vector2(frame_height, frame_height);
        //position corners
        frame.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(-1f * (frame_width / 2 + (frame_height / 2)), 0f);
        frame.transform.GetChild(0).GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(frame_width / 2 + (frame_height / 2), 0f);
        //handle border
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
        //position header
        frame.transform.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, (frame_height / 2) + header_offset);
        //handle title size
        frame.transform.GetChild(2).GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(title_size, 80f);
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