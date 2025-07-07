/*
    TractorBeamOptions.cs
    - Meant to handle items brought in from tractor beam
    - Does nothing
    Contributor(s): Jake Schott
    Last Updated: 7/6/2025
*/

using System.Collections.Generic;
using UnityEngine;

public class TractorBeamOptions : MonoBehaviour, IControllable
{
    private List<string> CONTROL_NAMES = new List<string>() { "ITEM INCINERATOR", "ITEM COLLECTOR" };
    private List<string> CONTROL_DESCS = new List<string>() {"DESTROY", "COLLECT"};
    private List<int> CONTROL_INDEXES = new List<int>() {6};
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    private List<string> ray_targets = new List<string> { "tractor_beam_destroy", "tractor_beam_collect" };

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[0], false, false));
        hud_info.setButtons(BUTTON_LISTS[0]);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index]);

        return hud_info;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        //does nothing
    }
}