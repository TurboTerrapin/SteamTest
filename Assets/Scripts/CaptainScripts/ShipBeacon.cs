/*
    ShipBeacon.cs
    - Handles inputs for ship beacon
    - Turns dial, changes screen
    - Handles flashing of circle
    - Illuminates collectible item in space when ship beacon is active
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ShipBeacon : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float SWITCH_TIME = 1.0f;
    private static float FLASH_TIME = 1.0f;
    private static float MAX_POWER_CONSUMPTION = 0.2f; //equates to 2 circles

    private string CONTROL_NAME = "SHIP BEACON";
    private static string INFO_MESSAGE = "Enables/disables transponder used for ship identification by foreign vessels. Also illuminates collectible items.";
    private List<string> CONTROL_DESCS = new List<string> { "ENABLE", "DISABLE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button> BUTTONS = new List<Button>();

    public GameObject ship_beacon_dial;
    public GameObject ship_beacon_display; //used to display the circle/flashing circle

    private bool is_powered = false;
    private Coroutine power_loss_coroutine = null;
    private bool beacon_enabled = false;
    private Coroutine beacon_switch_coroutine = null;
    private Coroutine beacon_flash_coroutine = null;

    private static HUDInfo hud_info = null;
    private void Start()
    {
        hud_info = new HUDInfo(CONTROL_NAME, true);
        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true)); //enable button
        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);

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
            ship_beacon_dial.transform.GetChild(0).GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_neon;
        }
        else
        {
            ship_beacon_dial.transform.GetChild(0).GetComponent<Renderer>().material = ReferenceAssistor.Instance.unlit_neon;
            displayCollectiblesLightChange(0.0f);
        }

        //update screen
        if (beacon_flash_coroutine != null)
        {
            StopCoroutine(beacon_flash_coroutine);
        }
        beacon_flash_coroutine = null;
        ship_beacon_display.transform.GetChild(0).gameObject.SetActive(beacon_enabled);
        if (beacon_enabled == true)
        {
            ship_beacon_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
            beacon_flash_coroutine = StartCoroutine(beaconFlasher());
        }
        else
        {
            ship_beacon_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.2f);
        }
    }

    private void displayCollectiblesLightChange(float intensity)
    {
        //update items in space
        GameObject world_root = GameObject.FindGameObjectWithTag("WorldRoot");
        if (world_root == null)
        {
            return;
        }

        foreach (Transform i in world_root.transform)
        {
            Component[] item_components = i.GetComponents<Component>();
            for (int c = 0; c < item_components.Length; c++)
            {
                CollectibleItem test_collectible_item = item_components[c] as CollectibleItem;
                if (test_collectible_item != null)
                {
                    test_collectible_item.setIlluminationIntensity(intensity);
                }
            }
        }
    }

    //infinite loop that runs when the beacon is active
    IEnumerator beaconFlasher()
    {
        float elapsed_time = 0.0f;

        ship_beacon_display.transform.GetChild(1).gameObject.SetActive(true);
        GameObject flashing_beacon = ship_beacon_display.transform.GetChild(0).gameObject;
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
                float dot_size = Mathf.Lerp(0.002f, 0.015f, percent_to_full);

                flashing_beacon.GetComponent<RectTransform>().sizeDelta = new Vector2(dot_size, dot_size);
                cover_up.GetComponent<RectTransform>().sizeDelta = new Vector2(dot_size - (0.012f * (1.0f - percent_to_full)), dot_size - (0.012f * (1.0f - percent_to_full)));

                flashing_beacon.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, anim_time / FLASH_TIME);

                elapsed_time += dt;
                displayCollectiblesLightChange(Mathf.PingPong(elapsed_time, 1.0f));

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
            ReferenceAssistor.Instance.power_manager.controlPowerChange(3, this.GetType().Name, 0.0f);
            hud_info.setPowerConsumption(0.0f);
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

            ship_beacon_dial.transform.localRotation =
                Quaternion.Euler(ship_beacon_dial.transform.localEulerAngles.x, 
                                 ship_beacon_dial.transform.localEulerAngles.y, 
                                 Mathf.Lerp(0.0f, 90.0f, switch_percentage));

            yield return null;
        }

        if (enabling == true)
        {
            beacon_enabled = true;
            ReferenceAssistor.Instance.power_manager.controlPowerChange(3, this.GetType().Name, MAX_POWER_CONSUMPTION);
            hud_info.setPowerConsumption(MAX_POWER_CONSUMPTION);
            displayAdjustment();
            BUTTONS[0].updateDesc(CONTROL_DESCS[1]);
        }
        else
        {
            BUTTONS[0].updateDesc(CONTROL_DESCS[0]);
        }
        BUTTONS[0].updateInteractable(is_powered);

        beacon_switch_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //click to enable/disable
        {
            if (beacon_switch_coroutine == null)
            {
                BUTTONS[0].toggle(0.2f);
                BUTTONS[0].updateInteractable(false);
                transmitEmergencyLightAdjustmentRPC(beacon_enabled);
            }
        }
    }

    //used by powerOff
    IEnumerator returnToZero(float power_off_time)
    {
        float starting_rotation = ship_beacon_dial.transform.localRotation.eulerAngles.z;

        float anim_time = power_off_time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            ship_beacon_dial.transform.localRotation =
                Quaternion.Euler(ship_beacon_dial.transform.localEulerAngles.x, 
                                 ship_beacon_dial.transform.localEulerAngles.y, 
                                 Mathf.Lerp(starting_rotation, 0.0f, 1.0f - (anim_time / power_off_time)));

            yield return null;
        }

        power_loss_coroutine = null;
    }

    public void powerOn(int position)
    {
        is_powered = true;
        ship_beacon_display.SetActive(true);
        BUTTONS[0].updateInteractable(true);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        beacon_enabled = false;
        ship_beacon_display.SetActive(false);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[0].untoggle();
        BUTTONS[0].updateDesc(CONTROL_DESCS[0]);
        displayAdjustment();
        hud_info.setPowerConsumption(0.0f);

        if (beacon_flash_coroutine != null)
        {
            StopCoroutine(beacon_flash_coroutine);
            beacon_flash_coroutine = null;
        }
        if (beacon_switch_coroutine != null)
        {
            StopCoroutine(beacon_switch_coroutine);
            beacon_switch_coroutine = null;
        }

        //return dial to off position
        if (power_loss_coroutine != null)
        {
            StopCoroutine(power_loss_coroutine);
        }
        power_loss_coroutine = StartCoroutine(returnToZero(time));
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