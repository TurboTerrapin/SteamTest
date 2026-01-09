/*
    HUDInfo.cs
    - Stores information for the onscreen UI indicator that appears when facing a control
        - Includes control title and button information
    Contributor(s): Jake Schott
    Last Updated: 1/4/2025
*/

using System.Collections.Generic;
using UnityEngine;

public class HUDInfo
{
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

    public void applyDescriptor(Transform frame)
    {
        if (numOptions() > 0)
        {
            return;
        }

        float trapezoid_width = 50f + (control_name.Length * 50f);

        //trapezoid height/vertical position
        frame.transform.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 200f);
        frame.transform.GetComponent<RectTransform>().anchoredPosition = new Vector3(0f, -980f, 0f);
        //trapezoid center
        frame.transform.GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(trapezoid_width, 0f);
        //trapezoid edge triangles
        frame.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector3(-1f * (trapezoid_width / 2 + 75f), 0f, 0f);
        frame.transform.GetChild(2).GetComponent<RectTransform>().anchoredPosition = new Vector3(trapezoid_width / 2 + 75f, 0f, 0f);
        //handle title size
        frame.transform.GetChild(3).GetComponent<RectTransform>().sizeDelta = new Vector2(trapezoid_width, 80f);
        frame.transform.GetChild(3).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -100f);
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
