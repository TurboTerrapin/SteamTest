/*
    AuxiliaryPower.cs
    - Can only be used once per scenario
    - Restores power to any disabled power regulation modules (can restart power on the ship)
    Contributor(s): Jake Schott
    Last Updated: 2/12/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using static AnimatorHandler;

public class AuxiliaryPower : NetworkBehaviour, IControllable, IIKTargetable
{
    //CLASS CONSTANTS
    private static float LEVER_PULL_TIME = 0.5f;
    private static float CIRCLE_ANIMATION_TIME = 5.0f;

    private string CONTROL_NAME = "AUXILIARY POWER";
    private static string INFO_MESSAGE = "Completes all modules and restores power immediately (does not recharge).";
    private List<string> CONTROL_DESCS = new List<string>() { "ACTIVATE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject auxiliary_power_lever;
    public GameObject auxiliary_power_display;
    public List<GameObject> auxiliary_power_arrows = null;
    public GameObject IK_target;

    private UnityEngine.UI.RawImage auxiliary_power_outer_circle;
    private UnityEngine.UI.Image auxiliary_power_fill_circle;
    private TMP_Text auxiliary_power_available_label;

    private bool auxiliary_power_available = true;
    private bool currently_available = false;
    private Coroutine auxiliary_power_emergency_flasher_coroutine = null;
    private Coroutine auxiliary_power_activation_coroutine = null;

    private static HUDInfo hud_info = null;
    public AnimatorHandler.HandInteractionType hand_interaction_type = AnimatorHandler.HandInteractionType.Grasp;
    public float hand_pose = 0;
    public bool does_right_hand_flip = false;
    private void Start()
    {
        auxiliary_power_outer_circle = auxiliary_power_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>();
        auxiliary_power_fill_circle = auxiliary_power_display.transform.GetChild(1).GetChild(1).GetComponent<UnityEngine.UI.Image>();
        auxiliary_power_available_label = auxiliary_power_display.transform.GetChild(2).GetComponent<TMP_Text>();

        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        hud_info.setButtons(BUTTONS, 6);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }
    public Transform getIKTarget(GameObject current_target)
    {
        return IK_target.transform;
    }
    public AnimatorHandler.HandInteractionType getHandInteractionType()
    {
        return hand_interaction_type;
    }
    public float getHandPose()
    {
        return hand_pose;
    }
    public bool getRightHandFlip()
    {
        return does_right_hand_flip;
    }
    public void resetAuxiliaryPower()
    {
        if (auxiliary_power_activation_coroutine != null)
        {
            StopCoroutine(auxiliary_power_activation_coroutine);
            auxiliary_power_activation_coroutine = null;
        }

        if (auxiliary_power_emergency_flasher_coroutine != null)
        {
            StopCoroutine(auxiliary_power_emergency_flasher_coroutine);
            auxiliary_power_emergency_flasher_coroutine = null;
        }

        //set lever to default position
        auxiliary_power_lever.transform.localRotation = Quaternion.Euler(-84.0f, -45.0f, 90.0f);

        //turn blue
        auxiliary_power_fill_circle.fillAmount = 1.0f;
        auxiliary_power_outer_circle.color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        auxiliary_power_available_label.color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        auxiliary_power_available_label.SetText("AVAILABLE");
        displayArrowAdjustment(new Color(0.0f, 0.84f, 1.0f, 0.2f));

        //reset state variables
        auxiliary_power_available = true;
        currently_available = false;
    }

    private void displayArrowAdjustment(Color c)
    {
        for (int i = 0; i < 2; i++)
        {
            auxiliary_power_arrows[i].GetComponent<UnityEngine.UI.RawImage>().color = c;
        }
    }

    public void activate(bool power_online)
    {
        if (auxiliary_power_available == true)
        {
            if (power_online == false)
            {
                if (auxiliary_power_emergency_flasher_coroutine == null)
                {
                    auxiliary_power_emergency_flasher_coroutine = StartCoroutine(auxiliaryPowerEmergencyFlasher());
                }
            }
            else
            {
                if (auxiliary_power_emergency_flasher_coroutine != null)
                {
                    StopCoroutine(auxiliary_power_emergency_flasher_coroutine);
                    auxiliary_power_emergency_flasher_coroutine = null;
                }
                displayArrowAdjustment(new Color(0.0f, 0.84f, 1.0f, 1.0f));
            }
        }

        currently_available = true;
        BUTTONS[0].updateInteractable(auxiliary_power_available);
    }

    public void deactivate()
    {
        if (auxiliary_power_available == true)
        {
            displayArrowAdjustment(new Color(0.0f, 0.84f, 1.0f, 0.08f));
        }
        currently_available = false;
        BUTTONS[0].updateInteractable(false);
    }

    IEnumerator auxiliaryPowerEmergencyFlasher()
    {
        float elapsed_time = 0.0f;
        while (true)
        {
            elapsed_time += Time.deltaTime;
            float a = 1.0f;
            if (Mathf.PingPong(elapsed_time, 0.4f) > 0.2f)
            {
                a = 0.2f;
            }
            displayArrowAdjustment(new Color(1.0f, 0.47f, 0.0f, a));

            yield return null;
        }
    }

    IEnumerator auxiliaryPowerActivation()
    {
        //pull lever
        float anim_time = LEVER_PULL_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            auxiliary_power_lever.transform.localRotation = Quaternion.Euler(Mathf.Lerp(-34.0f, -84.0f, anim_time / LEVER_PULL_TIME), -45.0f, 90.0f);

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

        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs) == true)
        {
            BUTTONS[0].toggle(0.25f);
            transmitAuxiliaryPowerUsageRPC();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitAuxiliaryPowerUsageRPC()
    {
        if (auxiliary_power_emergency_flasher_coroutine != null)
        {
            StopCoroutine(auxiliary_power_emergency_flasher_coroutine);
            auxiliary_power_emergency_flasher_coroutine = null;
        }
        displayArrowAdjustment(new Color(1.0f, 0.0f, 0.0f, 0.08f));

        if (auxiliary_power_available == true)
        {
            auxiliary_power_available = false;
            BUTTONS[0].updateInteractable(false);

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