/*
    TractorBeamOptions.cs
    - Meant to handle items brought in from tractor beam
    - Does nothing
    Contributor(s): Jake Schott
    Last Updated: 5/7/2025
*/

using System.Collections.Generic;
using UnityEngine;

public class TractorBeamOptions : MonoBehaviour, IControllable
{
    private string CONTROL_NAME = "TRACTOR BEAM OPTIONS";
    private List<string> CONTROL_DESCS = new List<string>() { "SCAN", "COLLECT", "DESTROY" };
    private List<int> CONTROL_INDEXES = new List<int>() {4, 0, 5};
    private List<Button> BUTTONS = new List<Button>();

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        BUTTONS.Add(new Button(CONTROL_DESCS[2], CONTROL_INDEXES[2], false, false));
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
}