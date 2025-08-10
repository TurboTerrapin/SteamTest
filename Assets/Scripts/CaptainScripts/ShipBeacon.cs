/*
    ShipBeacon.cs
    - Handles inputs for ship beacon
    - Turns dial, changes screen
    - Handles flashing of circle
    Contributor(s): Jake Schott
    Last Updated: 8/9/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ShipBeacon : NetworkBehaviour, IControllable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 1.0f;
    private static float FLASH_TIME = 2.0f;

    private string CONTROL_NAME = "SHIP BEACON";
    private List<string> CONTROL_DESCS = new List<string> { "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button> BUTTONS = new List<Button>();

    public Material lit_neon;
    public Material unlit_neon;

    public GameObject dial;
    public GameObject display_canvas; //used to display the circle/flashing circle

    private bool beacon_enabled = true;
    private Coroutine beacon_switch_coroutine = null;
    private Coroutine beacon_flash_coroutine = null;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME);
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[0], true, true)); //enable button
        hud_info.setButtons(BUTTONS);

        displayAdjustment();
    }
    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    public bool getBeaconEnabled()
    {
        return beacon_enabled;
    }
    private void displayAdjustment()
    {
        //update switch light
        if (beacon_enabled == true)
        {
            dial.transform.GetChild(0).GetComponent<Renderer>().material = lit_neon;
        }
        else
        {
            dial.transform.GetChild(0).GetComponent<Renderer>().material = unlit_neon;
        }

        //update screen
        if (beacon_flash_coroutine != null)
        {
            StopCoroutine(beacon_flash_coroutine);
        }
        beacon_flash_coroutine = null;
        if (beacon_enabled == true)
        {
            beacon_flash_coroutine = StartCoroutine(beaconFlasher());
        }
        else
        {
            //hide flashing beacon if not active
            display_canvas.transform.GetChild(1).gameObject.SetActive(false);
            display_canvas.transform.GetChild(2).gameObject.SetActive(false);
        }
    }

    //infinite loop that runs when the beacon is active
    IEnumerator beaconFlasher()
    {
        display_canvas.transform.GetChild(2).gameObject.SetActive(true);
        GameObject flashing_beacon = display_canvas.transform.GetChild(1).gameObject;
        GameObject cover_up = flashing_beacon.transform.GetChild(0).gameObject;
        flashing_beacon.SetActive(true);
        while (true)
        {
            float anim_time = FLASH_TIME;
            while (anim_time > 0.0f)
            {
                float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
                anim_time = Mathf.Max(0.0f, anim_time - dt);

                float percent_to_full = 1.0f - (anim_time / FLASH_TIME);
                float dot_size = Mathf.Lerp(0.0f, 0.03f, percent_to_full);

                flashing_beacon.GetComponent<RectTransform>().sizeDelta = new Vector2(dot_size, dot_size);
                cover_up.GetComponent<RectTransform>().sizeDelta = new Vector2(dot_size - (0.01f * (1.0f - percent_to_full)), dot_size - (0.01f * (1.0f - percent_to_full)));

                flashing_beacon.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, anim_time / FLASH_TIME);
                yield return null;
            }
        }
    }

    IEnumerator beaconSwitch()
    {
        bool enabling = !beacon_enabled;
        if (enabling == false)
        {
            beacon_enabled = false;
            displayAdjustment();
        }

        float anim_time = SWITCH_TIME;
        while (anim_time > 0.0f)
        {
            float dt = Time.deltaTime;
            anim_time = Mathf.Max(0.0f, anim_time - dt);

            float switch_percentage = anim_time / SWITCH_TIME;
            if (enabling == true)
            {
                switch_percentage = 1.0f - switch_percentage;
            }

            dial.transform.localRotation =
                Quaternion.Euler(dial.transform.localEulerAngles.x,
                            dial.transform.localEulerAngles.y,
                            Mathf.Lerp(0.0f, 90.0f, switch_percentage));

            yield return null;
        }

        if (enabling == true)
        {
            beacon_enabled = true;
            displayAdjustment();
            BUTTONS[0].updateDesc(CONTROL_DESCS[1]);
        }
        else
        {
            BUTTONS[0].updateDesc(CONTROL_DESCS[0]);
        }
        BUTTONS[0].updateInteractable(true);

        beacon_switch_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //click to enable/disable
        {
            if (beacon_switch_coroutine == null)
            {
                BUTTONS[0].toggle(0.2f);
                BUTTONS[0].updateInteractable(false);
                transmitEmergencyLightAdjustmentRPC(beacon_enabled);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEmergencyLightAdjustmentRPC(bool be)
    {
        beacon_enabled = be;
        if (beacon_switch_coroutine != null)
        {
            StopCoroutine(beacon_switch_coroutine);
        }

        beacon_switch_coroutine = StartCoroutine(beaconSwitch());
        displayAdjustment();
    }
}
