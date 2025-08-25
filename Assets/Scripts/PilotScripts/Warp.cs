/*
    Warp.cs
    - Handles warp throttle
    - Does nothing
    Contributor(s): Jake Schott
    Last Updated: 8/19/2025
*/


using System.Collections.Generic;
using UnityEngine;

public class Warp : MonoBehaviour, IControllable, IPowerable
{
    private string CONTROL_NAME = "WARP THROTTLE";
    private List<string> CONTROL_DESCS = new List<string>() { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5, 6 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject warp_factor_display;
    public GameObject warp_power_display;

    //private bool is_powered = false;
    
    private static HUDInfo hud_info = null;

    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);

        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        hud_info.setButtons(BUTTONS);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }
  
    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        //does nothing
    }

    public void powerOn(int position)
    {
        warp_factor_display.SetActive(true);
        warp_power_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        warp_factor_display.SetActive(false);
        warp_power_display.SetActive(false);
    }
}
