/*
    AuxiliaryPower.cs
    - Can only be used once per scenario
    - Restores power to any disabled power regulation modules (can restart power on the ship)
    Contributor(s): Jake Schott
    Last Updated: 9/16/2025
*/

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AuxiliaryPower : NetworkBehaviour, IControllable
{
    private string CONTROL_NAME = "AUXILIARY POWER";
    private List<string> CONTROL_DESCS = new List<string>() { "ACTIVATE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button> BUTTONS = new List<Button>();

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        hud_info.setButtons(BUTTONS, 6);
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {

    }
}
