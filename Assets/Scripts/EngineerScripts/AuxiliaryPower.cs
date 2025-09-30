/*
    AuxiliaryPower.cs
    - Can only be used once per scenario
    - Restores power to any disabled power regulation modules (can restart power on the ship)
    Contributor(s): Jake Schott
    Last Updated: 9/17/2025
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class AuxiliaryPower : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float LEVER_PULL_TIME = 0.5f;
    private static float CIRCLE_ANIMATION_TIME = 5.0f;

    private string CONTROL_NAME = "AUXILIARY POWER";
    private List<string> CONTROL_DESCS = new List<string>() { "ACTIVATE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject auxiliary_power_lever;
    public GameObject auxiliary_power_display;

    private TMP_Text auxiliary_power_label;
    private UnityEngine.UI.RawImage auxiliary_power_outer_circle;
    private UnityEngine.UI.Image auxiliary_power_fill_circle;
    private TMP_Text auxiliary_power_available_label;

    private bool auxiliary_power_available = true;
    private bool currently_available = false;
    private Coroutine auxiliary_power_activation_coroutine = null;

    private static HUDInfo hud_info = null;

    private void Start()
    {
        auxiliary_power_outer_circle = auxiliary_power_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>();
        auxiliary_power_fill_circle = auxiliary_power_display.transform.GetChild(1).GetChild(1).GetComponent<UnityEngine.UI.Image>();
        auxiliary_power_available_label = auxiliary_power_display.transform.GetChild(2).GetComponent<TMP_Text>();

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        hud_info.setButtons(BUTTONS, 6);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    public void resetAuxiliaryPower()
    {
        if (auxiliary_power_activation_coroutine != null)
        {
            StopCoroutine(auxiliary_power_activation_coroutine);
            auxiliary_power_activation_coroutine = null;
        }

        //set lever to default position
        auxiliary_power_lever.transform.localRotation = Quaternion.Euler(-84.0f, -45.0f, -90.0f);

        //turn blue
        auxiliary_power_fill_circle.fillAmount = 1.0f;
        auxiliary_power_outer_circle.color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        auxiliary_power_available_label.color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        auxiliary_power_available_label.SetText("AVAILABLE");

        //reset state variables
        auxiliary_power_available = true;
        currently_available = false;
    }

    public void activate()
    {
        currently_available = true;
        BUTTONS[0].updateInteractable(auxiliary_power_available);
    }

    public void deactivate()
    {
        currently_available = false;
        BUTTONS[0].updateInteractable(false);
    }

    IEnumerator auxiliaryPowerActivation()
    {
        //pull lever
        float anim_time = LEVER_PULL_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            auxiliary_power_lever.transform.localRotation = Quaternion.Euler(Mathf.Lerp(-34.0f, -84.0f, anim_time / LEVER_PULL_TIME), -45.0f, -90.0f);

            yield return null;
        }

        yield return new WaitForSeconds(1.0f);

        if (NetworkManager.Singleton.IsHost == true)
        {
            transmitAuxiliaryPowerActivationRPC();
        }

        //empty circle
        anim_time = CIRCLE_ANIMATION_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            auxiliary_power_fill_circle.fillAmount = anim_time / CIRCLE_ANIMATION_TIME;

            yield return null;
        }

        //turn red
        auxiliary_power_outer_circle.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        auxiliary_power_available_label.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        auxiliary_power_available_label.SetText("UNAVAILABLE");

        auxiliary_power_activation_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (auxiliary_power_available == false || currently_available == false)
        {
            return;
        }

        if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs) == true)
        {
            BUTTONS[0].toggle(0.25f);
            BUTTONS[0].updateInteractable(false);
            transmitAuxiliaryPowerUsageRPC();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitAuxiliaryPowerUsageRPC()
    {
        if (auxiliary_power_available == true)
        {
            auxiliary_power_available = false;

            if (auxiliary_power_activation_coroutine != null)
            {
                StopCoroutine(auxiliary_power_activation_coroutine);
            }
            auxiliary_power_activation_coroutine = StartCoroutine(auxiliaryPowerActivation());
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitAuxiliaryPowerActivationRPC()
    {
        GameObject.Find("PowerHandler").GetComponent<PowerRegulator>().useAuxiliaryPower();
    }
}
