/*
    HUDInfo.cs
    - Stores information for the onscreen UI indicator that appears when facing a control
        - Includes control title and button information
    Contributor(s): Jake Schott
    Last Updated: 10/21/2025
*/

using System.Collections.Generic;

public class HUDInfo
{
    private string control_name; //ex. "IMPULSE THROTTLE"
    private int layout = -1;
    private bool consumes_power = false;
    private float power_consumption = 0.0f;
    private string info_msg = "";
    private List<Button> buttons;

    public HUDInfo(string title)
    {
        control_name = title;
    }

    public HUDInfo(string title, bool is_powerable)
    {
        control_name = title;
        consumes_power = is_powerable;
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
        return buttons.Count;
    }

    public int getLayout()
    {
        return layout;
    }
}
