/*
    TractorBeamOptions.cs
    - Meant to handle items brought in from tractor beam
    - Does nothing
    Contributor(s): Jake Schott
    Last Updated: 10/21/2025
*/

using System.Collections.Generic;
using UnityEngine;

public class TractorBeamOptions : MonoBehaviour, IControllable, IPowerable
{
    private List<string> CONTROL_NAMES = new List<string>() { "TRACTOR BEAM ITEM INCINERATOR", "TRACTOR BEAM ITEM COLLECTOR" };
    private List<string> INFO_MESSAGES = new List<string>() { "Destroys the item held in the tractor beam item storing position.", "Collects and stores the item held in the tractor beam item storing position for later use." };
    private List<string> CONTROL_DESCS = new List<string>() {"DESTROY", "COLLECT"};
    private List<int> CONTROL_INDEXES = new List<int>() {6};
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public GameObject item_display;
    public GameObject serial_display;
    public GameObject destroy_display;
    public GameObject collect_display;
    public GameObject destroy_dial;
    public GameObject collect_dial;

    //private bool is_powered = false;

    private List<string> ray_targets = new List<string> { "tractor_beam_destroy", "tractor_beam_collect" };

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[0], false, false));
        hud_info.setLayout(6);
        hud_info.setButtons(BUTTON_LISTS[0]);
        hud_info.setInfo(INFO_MESSAGES[0]);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 6);
        hud_info.setInfo(INFO_MESSAGES[index]);

        return hud_info;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        //does nothing
    }

    public void powerOn(int position)
    {
        item_display.SetActive(true);
        serial_display.SetActive(true);
        destroy_display.SetActive(true);
        collect_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        item_display.SetActive(false);
        serial_display.SetActive(false);
        destroy_display.SetActive(false);
        collect_display.SetActive(false);
    }
}