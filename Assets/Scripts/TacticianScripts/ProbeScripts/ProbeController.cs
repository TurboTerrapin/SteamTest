/*
    ProbeController.cs
    - Handles launching of probe
    - Handles destroying of probe
    Contributor(s): Jake Schott
    Last Updated: 12/29/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ProbeController : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float RANGE = 1250.0f; //how far the probe can be from the ship while still being in contact
    private static float DEFAULT_PROBE_HEALTH = 150.0f; //starting/max health for probe
    private static float TURN_TIME = 0.5f;
    private static float FUNCTION_TIME = 2.0f; //how long it takes to launch or self-destruct the probe
    private static float MAX_POWER_CONSUMPTION = 0.5f; //equates to 5 circles

    private string[] CONTROL_NAMES = new string[2] { "LAUNCH PROBE", "DESTROY PROBE" };
    private List<string> INFO_MESSAGES = new List<string>() { "Launches a probe if available in inventory and none currently active. Probes can only be recollected through the tractor beam.", "Destroys launched probe in a controlled explosion (only if ship is within range). Probe inventory is limited." };
    private List<string> CONTROL_DESCS = new List<string> { "ACTIVATE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public List<GameObject> probe_dials = null;
    public List<GameObject> probe_dial_displays = null;
    public GameObject probe_controller_display;
    public GameObject probe_actual_prefab;

    private Transform ship = null;
    private GameObject current_probe = null;
    private EngineerInventory engineer_inventory = null;
    private TacticianProbeInfo tactician_probe_info = null;

    private bool is_powered = false;
    private bool probe_connected = false;
    private float probe_health = 100.0f;
    private int active_dial = 0;
    private float[] dial_turn_percentages = new float[2] { 0.0f, 0.0f };
    private Coroutine dial_turn_coroutine = null;
    private Coroutine probe_function_coroutine = null;
    private Coroutine probe_out_of_range_coroutine = null;

    private List<string> ray_targets = new List<string> { "probe_launch_dial", "probe_destruct_dial" };
    private int ray_target_index = -1;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;

    private void Start()
    {
        ship = GameObject.FindGameObjectWithTag("Spaceship").transform;
        tactician_probe_info = GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<TacticianProbeInfo>();
        engineer_inventory = GameObject.FindGameObjectWithTag("SensorHandler").GetComponent<EngineerInventory>();

        hud_info = new HUDInfo(CONTROL_NAMES[0], true);

        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));

        hud_info.setButtons(BUTTON_LISTS[0]);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setInfo(INFO_MESSAGES[index]);

        hud_info.setButtons(BUTTON_LISTS[index]);
        return hud_info;
    }

    //called any time a probe is destroyed
    public void onProbeDestroyed()
    {
        transform.GetComponent<PowerControl>().power_manager.controlPowerChange(1, this.GetType().Name, 0.0f);
        hud_info.setPowerConsumption(0.0f);
    }

    //spawns a probe, links probe to probe controls
    private void spawnProbe()
    {
        //spawn probe as a NetworkObject if host
        if (NetworkManager.Singleton.IsHost == true)
        {
            //ensure there is only one probe
            if (current_probe != null)
            {
                current_probe.GetComponent<NetworkObject>().Despawn(true);
            }
            Transform world_root = GameObject.FindGameObjectWithTag("WorldRoot").transform;
            Transform spaceship = GameObject.FindGameObjectWithTag("Spaceship").transform;
            current_probe = GameObject.Instantiate(probe_actual_prefab, world_root);
            current_probe.transform.position = new Vector3(spaceship.position.x, spaceship.position.y + 8.0f, spaceship.position.z);
            current_probe.transform.rotation = spaceship.rotation;
            current_probe.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
            current_probe.GetComponent<NetworkObject>().TrySetParent(world_root);
            transmitProbeConnectionChangeRPC(true, true);
        }

        //set health to default
        probe_health = DEFAULT_PROBE_HEALTH;

        //handle new power consumption
        if (is_powered == true)
        {
            transform.GetComponent<PowerControl>().power_manager.controlPowerChange(1, this.GetType().Name, MAX_POWER_CONSUMPTION);
            hud_info.setPowerConsumption(MAX_POWER_CONSUMPTION);
        }
    }

    //turns corresponding dial based on dial_turn_percentage
    private void displayDialAdjustment(int index)
    {
        //turn corresponding dial
        probe_dials[index].transform.localRotation =
            Quaternion.Euler(probe_dials[index].transform.localEulerAngles.x,
                             probe_dials[index].transform.localEulerAngles.y,
                             Mathf.Lerp(90.0f, 180.0f, dial_turn_percentages[index]));
    }

    IEnumerator dialActivation()
    {
        int dial_to_check = active_dial;
        do
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            bool able_to_turn = (is_powered == true && ray_target_index == dial_to_check);
            if (dial_to_check == 0)
            {
                able_to_turn = (able_to_turn == true && engineer_inventory.getItemQuantity(0, 0) > 0);
            }
            if (dial_to_check == 1)
            {
                able_to_turn = (able_to_turn == true && probe_connected == true);
            }
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && able_to_turn) //check if turning
            {
                dial_turn_percentages[dial_to_check] = Mathf.Min(1.0f, dial_turn_percentages[dial_to_check] + (dt / TURN_TIME));
            }
            else
            {
                dial_turn_percentages[dial_to_check] = Mathf.Max(0.0f, dial_turn_percentages[dial_to_check] - (dt / TURN_TIME));
            }

            transmitDialTurnAdjustmentRPC(dial_to_check, dial_turn_percentages[dial_to_check]);

            keys_down.Clear();
            ray_target_index = -1;

            int iterator = 0; //counts frames
            while (keys_down.Count == 0 && iterator < 2)
            {
                yield return null;
                iterator++;
            }
        } while (dial_turn_percentages[dial_to_check] > 0.0f && dial_turn_percentages[dial_to_check] < 1.0f);

        if (dial_turn_percentages[dial_to_check] == 1.0f)
        {
            BUTTON_LISTS[dial_to_check][0].updateInteractable(false);
            transmitFunctionActivationRPC(dial_to_check);
        }
        else
        {
            dial_turn_coroutine = null;
        }
    }

    IEnumerator dialReturn()
    {
        float anim_time = TURN_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            for (int i = 0; i < 2; i++)
            {
                dial_turn_percentages[i] = Mathf.Max(0.0f, dial_turn_percentages[i] - (Time.deltaTime / TURN_TIME));
                displayDialAdjustment(i);
            }

            yield return null;
        }

        dial_turn_coroutine = null;
    }

    private void activateProbeControlSwitches()
    {
        transform.GetComponent<ProbeLateralMovement>().linkProbe(current_probe);
        transform.GetComponent<ProbeVerticalMovement>().linkProbe(current_probe);
        transform.GetComponent<ProbeOrientation>().linkProbe(current_probe);
    }

    public void linkProbe()
    {
        BUTTON_LISTS[1][0].updateInteractable(true);
        activateProbeControlSwitches();
        tactician_probe_info.onProbeLinked();
        onProbeDistanceChange();
        tactician_probe_info.displayProbeAltitude(current_probe.transform.position.y);
        active_dial = 1;
        updateDialDisplays();
    }

    private void deactivateProbeControlSwitches()
    {
        transform.GetComponent<ProbeLateralMovement>().unlinkProbe();
        transform.GetComponent<ProbeVerticalMovement>().unlinkProbe();
        transform.GetComponent<ProbeOrientation>().unlinkProbe();
    }

    public void unlinkProbe()
    {
        BUTTON_LISTS[1][0].updateInteractable(false);
        deactivateProbeControlSwitches();
        tactician_probe_info.onProbeUnlinked();

        if (current_probe == null && is_powered == true)
        {
            if (engineer_inventory.getItemQuantity("Probe") > 0)
            {
                active_dial = 0;
            }
            else
            {
                active_dial = -1;
            }
        }
        else
        {
            active_dial = -1;
        }
        updateDialDisplays();
    }

    //play animation then launch probe
    IEnumerator probeLaunchSequence()
    {
        dial_turn_coroutine = StartCoroutine(dialReturn());
        engineer_inventory.removeItem("Probe");

        float anim_time = FUNCTION_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            float percent_loaded = 1.0f - (anim_time / FUNCTION_TIME);
            tactician_probe_info.displayProbeLaunchProgress(percent_loaded);

            yield return null;
        }

        spawnProbe();
        updateDialDisplays();

        probe_function_coroutine = null;
    }

    IEnumerator probeDestructSequence()
    {
        dial_turn_coroutine = StartCoroutine(dialReturn());
        deactivateProbeControlSwitches();
        probe_connected = false;

        if (current_probe != null)
        {
            current_probe.GetComponent<Probe>().toggleSelfDestructVisual();
        }

        float anim_time = FUNCTION_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            float percent_loaded = 1.0f - (anim_time / FUNCTION_TIME);
            tactician_probe_info.displayProbeDestructProgress(percent_loaded);

            yield return null;
        }

        if (NetworkManager.Singleton.IsHost == true)
        {
            damageProbe(9999.9f);
        }
        current_probe = null;
        for (int i = 0; i < 2; i++)
        {
            probe_dial_displays[i].transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount = 0.05f;
        }

        if (engineer_inventory.getItemQuantity("Probe") > 0)
        {
            BUTTON_LISTS[0][0].updateInteractable(true);
            active_dial = 0;
        }
        else
        {
            BUTTON_LISTS[0][0].updateInteractable(false);
            active_dial = -1;
        }
        updateDialDisplays();

        probe_function_coroutine = null;
    }

    public void damageProbe(float dam)
    {
        if (current_probe == null)
        {
            return;
        }

        //handle damage change if host
        if (NetworkManager.Singleton.IsHost == true)
        {
            probe_health = Mathf.Max(0.0f, probe_health - dam);
            transmitProbeHealthChangeRPC(probe_health);

            //handle destruction
            if (probe_health <= 0.0f) 
            {
                current_probe.GetComponent<NetworkObject>().Despawn(true);
                transmitProbeConnectionChangeRPC(false, false);
            }
        }
    }

    //returns true if probe is connected and in range
    public bool probeInRange()
    {
        if (current_probe == null)
        {
            return false;
        }
        return (Mathf.Min(RANGE, Vector3.Distance(current_probe.transform.position, ship.transform.position)) < RANGE);
    }

    //only run by host
    IEnumerator outOfRangeHelper()
    {
        yield return new WaitForSeconds(5.0f);
        if (Mathf.Min(RANGE, Vector3.Distance(current_probe.transform.position, ship.position)) >= RANGE)
        {
            transmitProbeConnectionChangeRPC(false, true);
        }
        probe_out_of_range_coroutine = null;
    }

    //called when either the probe has changed positions or the ship has
    public void onProbeDistanceChange()
    {
        if (current_probe == null)
        {
            return;
        }

        float bounded_distance = Mathf.Min(RANGE, Vector3.Distance(current_probe.transform.position, ship.transform.position));
        if (probe_connected == true)
        {
            if (probeInRange() == true)
            {
                //update screens
                tactician_probe_info.disableProbeOutOfRangeWarning();
                tactician_probe_info.displayProbeRange(1.0f - Mathf.Max(0.0f, (bounded_distance - 25.0f) / (RANGE - 25.0f)));
            }
            else
            {
                tactician_probe_info.enableProbeOutOfRangeWarning();
            }
        }

        //check for disconnect/reconnect if host
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (bounded_distance >= RANGE)
            {
                if (probe_connected == true && probe_out_of_range_coroutine == null)
                {
                    //attempt disconnect
                    probe_out_of_range_coroutine = StartCoroutine(outOfRangeHelper());
                }
            }
            else
            {
                if (probe_connected == false)
                {
                    //reconnect
                    transmitProbeConnectionChangeRPC(true, true);
                }
            }
        }
    }

    private void updateDialDisplays()
    {
        //update dial display colors
        if (current_probe != null)
        {
            if (probe_connected == true)
            {
                tactician_probe_info.setDialDisplayColor(probe_dial_displays[0].transform, 0, 1.0f);
                tactician_probe_info.setDialDisplayColor(probe_dial_displays[1].transform, 1, 1.0f);
            }
            else
            {
                tactician_probe_info.setDialDisplayColor(probe_dial_displays[0].transform, 2, 0.2f);
                tactician_probe_info.setDialDisplayColor(probe_dial_displays[1].transform, 2, 0.2f);
            }
        }
        else
        {
            if (engineer_inventory.getItemQuantity("Probe") > 0)
            {
                tactician_probe_info.setDialDisplayColor(probe_dial_displays[0].transform, 0, 1.0f);
            }
            else
            {
                tactician_probe_info.setDialDisplayColor(probe_dial_displays[0].transform, 0, 0.2f);
            }
            probe_dial_displays[1].SetActive(false);
        }

        //if not powered, hide both
        if (is_powered == false)
        {
            for (int i = 0; i < 2; i++)
            {
                probe_dial_displays[i].SetActive(false);
            }
        }
    }

    //used to update when probe is now available or unavailable
    public void onInventoryChange(int new_probe_quantity)
    {
        //update launch UI immediately if no probes available
        if (new_probe_quantity == 0)
        {
            BUTTON_LISTS[0][0].updateInteractable(false);
        }

        //only update if powered and not currently functioning
        if (is_powered == false || probe_function_coroutine != null || dial_turn_coroutine != null)
        {
            return;
        }

        //only update if in launch mode
        if (current_probe == null && probe_connected == false)
        {
            if (new_probe_quantity > 0)
            {
                active_dial = 0;
                BUTTON_LISTS[0][0].updateInteractable(true);
                updateDialDisplays();
            }
            else
            {
                active_dial = -1;
                BUTTON_LISTS[0][0].updateInteractable(false);
                updateDialDisplays();
            }
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        probe_controller_display.SetActive(true);
        if (current_probe != null)
        {
            if (probeInRange() == true)
            {
                activateProbeControlSwitches();
                active_dial = 1;
            }
            else
            {
                active_dial = -1;
            }
            BUTTON_LISTS[1][0].updateInteractable(probeInRange());
            transform.GetComponent<PowerControl>().power_manager.controlPowerChange(1, this.GetType().Name, MAX_POWER_CONSUMPTION);
            hud_info.setPowerConsumption(MAX_POWER_CONSUMPTION);
        }
        else
        {
            if (engineer_inventory.getItemQuantity("Probe") > 0)
            {
                active_dial = 0;
                BUTTON_LISTS[0][0].updateInteractable(true);
                BUTTON_LISTS[1][0].updateInteractable(false);
            }
            else
            {
                active_dial = -1;
                BUTTON_LISTS[0][0].updateInteractable(false);
                BUTTON_LISTS[1][0].updateInteractable(false);
            }
        }
        updateDialDisplays();
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        probe_controller_display.SetActive(false);
        updateDialDisplays();
        deactivateProbeControlSwitches();
        for (int i = 0; i < 2; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(false);
        }
        transform.GetComponent<PowerControl>().power_manager.controlPowerChange(1, this.GetType().Name, 0.0f);
        hud_info.setPowerConsumption(0.0f);
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        ray_target_index = ray_targets.IndexOf(current_target.name);
        keys_down = inputs;

        if (dial_turn_coroutine == null && probe_function_coroutine == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && ray_target_index == active_dial)
            {
                dial_turn_coroutine = StartCoroutine(dialActivation());
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitProbeHealthChangeRPC(float new_health)
    {
        probe_health = new_health;
        tactician_probe_info.displayProbeHealth(probe_health / DEFAULT_PROBE_HEALTH);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitProbeConnectionChangeRPC(bool connected, bool probe_intact)
    {
        probe_connected = connected;
        if (probe_intact == true)
        {
            current_probe = GameObject.FindGameObjectWithTag("Probe");
        }
        else
        {
            current_probe = null;
        }
        if (probe_connected == true)
        {
            linkProbe();
        }
        else
        {
            unlinkProbe();
            if (probe_intact == true)
            {
                tactician_probe_info.onProbeOutOfRangeDisconnect();
            }
            else
            {
                transform.GetComponent<PowerControl>().power_manager.controlPowerChange(1, this.GetType().Name, 0.0f);
                hud_info.setPowerConsumption(0.0f);
            }
        }
        updateDialDisplays();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDialTurnAdjustmentRPC(int index, float percent)
    {
        dial_turn_percentages[index] = percent;
        displayDialAdjustment(active_dial);
        probe_dial_displays[active_dial].transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount = Mathf.Max(0.05f, dial_turn_percentages[active_dial]);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitFunctionActivationRPC(int index)
    {
        //make buttons uninteractable
        for (int i = 0; i <= 1; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(false);
        }

        //stop coroutines
        if (probe_function_coroutine != null)
        {
            StopCoroutine(probe_function_coroutine);
        }
        if (probe_out_of_range_coroutine != null)
        {
            StopCoroutine(probe_out_of_range_coroutine);
            probe_out_of_range_coroutine = null;
        }
        
        //start launch
        if (index == 0)
        {
            probe_function_coroutine = StartCoroutine(probeLaunchSequence());
        }
        else //start destruct
        {
            probe_function_coroutine = StartCoroutine(probeDestructSequence());
        }
    }
}