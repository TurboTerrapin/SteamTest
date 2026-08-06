/*
    PowerManager.cs
    - Handles powering on/off each of the positions
    - Records changes in power consumption (as called by the individual controls)
    - Handles overconsumption and complete shutdown
    Contributor(s): Jake Schott
    Last Updated: 7/31/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PowerManager : NetworkBehaviour, IPowerable
{
    //CLASS CONSTANTS
    private static float POWER_ON_TIME = 1.0f; //how long it takes to power on a position
    private static float POWER_OFF_TIME = 1.0f; //how long it takes to power down a position
    private static float POWER_UPDATE_TIME = 0.5f; //how often the power consumption displays update
    private static float TIME_TO_POWER_LOSS = 3.0f; //once a position overconsumes power, how long until ship shutdown
    private static int[] DEFAULT_POWER_ALLOCATIONS = new int[] { 8, 6, 5, 5 }; //communicated to PowerAllocation

    public List<GameObject> position_power_displays = null;
    public List<GameObject> engineer_power_displays = null;
    public List<GameObject> power_warnings = null;

    public LightsManager lights_manager;
    public BackgroundAnimator background_animator;

    //sounds
    public List<AudioSource> overconsumption_warning_sounds = null;
    public AudioSource ship_beeps_sound;
    public AudioSource power_off_sound;
    public AudioSource power_on_sound;
    public List<AudioClip> power_notifications = null;

    private PowerAllocation power_allocation;
    private PowerControl power_control;
    private StatusIndicators status_indicators;

    private bool ship_has_power = true;

    //these three lists correspond to 0-3 pilot, tactician, engineer, captain
    private List<Component>[] positional_modules = new List<Component>[] { null, null, null, null }; //the powerable components
    private List<float>[] power_distributions = new List<float>[] { new List<float>(), new List<float>(), new List<float>(), new List<float>() };
    private List<string>[] associated_controls = new List<string>[] { new List<string>(), new List<string>(), new List<string>(), new List<string>() };

    private bool[] powered_positions = new bool[] { false, false, false, false }; //corresponds to pilot, tactician, engineer, captain
    private float[] power_consumptions = new float[] { 0.0f, 0.0f, 0.0f, 0.0f }; //corresponds to pilot, tactician, engineer, captain
    private Coroutine[] power_change_coroutines = new Coroutine[] { null, null, null, null }; //corresponds to pilot, tactician, engineer, captain
    private Coroutine[] overconsumption_coroutines = new Coroutine[] { null, null, null, null }; //corresponds to pilot, tactician, engineer, captain
    private Coroutine shutdown_coroutine = null;
    private Coroutine power_restart_coroutine = null;
    private Coroutine power_updater_coroutine = null;

    private void Start()
    {
        power_allocation = ReferenceAssistor.Instance.module_handlers[2].GetComponent<PowerAllocation>();
        power_control = ReferenceAssistor.Instance.module_handlers[4].GetComponent<PowerControl>();
        status_indicators = ReferenceAssistor.Instance.module_handlers[4].GetComponent<StatusIndicators>();

        addPilotModules(); //positional_modules[0]
        addTacticianModules(); //positional_modules[1]
        addEngineerModules(); //positional_modules[2]
        addCaptainModules(); //positional_modules[3]

        linkPowerDistributions();

        power_updater_coroutine = StartCoroutine(powerUpdater());
        power_allocation.resetToDefaultAllocation(DEFAULT_POWER_ALLOCATIONS);
    }

    //called by ScenarioManager as part of the BridgeEnvironment reset process prior to starting a new scenario
    public void resetPowerManager()
    {
        //stop any ongoing coroutines
        for (int i = 0; i < 4; i++)
        {
            if (power_change_coroutines[i] != null)
            {
                StopCoroutine(power_change_coroutines[i]);
                power_change_coroutines[i] = null;
            }

            if (overconsumption_coroutines[i] != null)
            {
                StopCoroutine(overconsumption_coroutines[i]);
                overconsumption_coroutines[i] = null;
            }
        }

        if (power_restart_coroutine != null)
        {
            StopCoroutine(power_restart_coroutine);
            power_restart_coroutine = null;
        }

        //stop power loss/restart sounds
        foreach (AudioSource overconsumption_warning_sound in overconsumption_warning_sounds)
        {
            overconsumption_warning_sound.Stop();
        }
        power_on_sound.Stop();
        power_off_sound.Stop();

        //resume beeping sound
        if (ship_beeps_sound.isPlaying == false)
        {
            ship_beeps_sound.Play();
        }

        //if power updater is disabled, reenable
        if (power_updater_coroutine == null)
        {
            power_updater_coroutine = StartCoroutine(powerUpdater());
        }

        //set power allocation to default
        power_allocation.resetToDefaultAllocation(DEFAULT_POWER_ALLOCATIONS);

        //set power to true
        ship_has_power = true;

        //pause regulation
        transform.GetComponent<PowerRegulator>().resetPowerRegulator();

        //reset display
        for (int i = 0; i < 4; i++)
        {
            resetEngineerPositionDisplay(i);
        }
    }

    //called only once by Start() to initialize the tracking of each control's potential power consumption
    private void linkPowerDistributions()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int m = 0; m < positional_modules[i].Count; m++)
            {
                IControllable control_test = positional_modules[i][m] as IControllable;
                if (control_test != null)
                {
                    string control_name = positional_modules[i][m].GetType().Name;

                    if (associated_controls[i].Contains(control_name) == false)
                    {
                        power_distributions[i].Add(0.0f);
                        associated_controls[i].Add(control_name);
                    }
                }
                if (i == 1) //tactician exception for TransmissionHandler since it's not a "control" per se
                {
                    power_distributions[1].Add(0.0f);
                    associated_controls[1].Add("TransmissionHandler");
                }
                else if (i == 3) //captain exception for ManualOnOff since it's not covered by IPowerable
                {
                    power_distributions[3].Add(0.0f);
                    associated_controls[3].Add("ManualOnOff");
                }
            }
        }
    }

    //returns true if there is at least one overconsumption warning in effect
    private bool checkIfOverconsuming()
    {
        for (int i = 0; i < 4; i++)
        {
            if (overconsumption_coroutines[i] != null)
            {
                return true;
            }
        }
        return false;
    }

    //returns the power consumption of a specific position (0 = pilot, 1 = tactician, 2 = engineer, 3 = captain) 
    private float getPowerConsumption(int position)
    {
        float total_power = 0.0f;
        for (int p = 0; p < power_distributions[position].Count; p++)
        {
            total_power += power_distributions[position][p];
        }
        return Mathf.Min(1.05f, total_power);
    }

    //powers every control in a given position in POWER_ON_TIME seconds
    IEnumerator modulePowerSequence(List<Component> to_power_on, int position)
    {
        for (int i = 0; i < to_power_on.Count; i++)
        {
            IPowerable current_module = (IPowerable)to_power_on[i];
            current_module.powerOn(position);
            yield return new WaitForSeconds(POWER_ON_TIME / to_power_on.Count);
        }

        power_control.enableDial(position, true);
        power_change_coroutines[position] = null;
    }

    //powers down every control instantly, finishes in POWER_OFF_TIME (throttles return to 0 position in POWER_OFF_TIME)
    IEnumerator powerDownSequence(List<Component> to_disable, int position)
    {
        power_control.turnDial(position, false);
        for (int i = 0; i < to_disable.Count; i++)
        {
            IPowerable current_module = (IPowerable)to_disable[i];
            current_module.powerOff(position, POWER_OFF_TIME);
        }

        yield return new WaitForSeconds(POWER_OFF_TIME);

        if (ship_has_power == true)
        {
            power_control.enableDial(position, false);
        }
        power_change_coroutines[position] = null;
    }

    //called by IControllables attached to module handlers
    public void controlPowerChange(int position, string control_name, float power_level)
    {
        if (associated_controls[position].Contains(control_name) == false)
        {
            return;
        }

        power_distributions[position][associated_controls[position].IndexOf(control_name)] = power_level;
        powerConsumptionChangeRPC(position, getPowerConsumption(position));
    }

    //returns whether the ship as a whole has power or not
    public bool getShipHasPower()
    {
        return ship_has_power;
    }

    //called by PowerControl
    public bool getPowerEnabled(int position)
    {
        return powered_positions[position];
    }

    //called by PowerControl and ScenarioManager when at the start of a scenario
    public void powerStation(int position)
    {
        if (powered_positions[position] == true)
        {
            return;
        }
        powered_positions[position] = true;

        List<Component> to_enable = positional_modules[position];

        if (power_change_coroutines[position] != null)
        {
            StopCoroutine(power_change_coroutines[position]);
        }
        power_change_coroutines[position] = StartCoroutine(modulePowerSequence(to_enable, position));
    }

    //called by PowerControl (and ScenarioManager at the end of a scenario as a way of resetting every control)
    public void disableStation(int position)
    {
        if (powered_positions[position] == false)
        {
            return;
        }
        if (power_change_coroutines[position] != null)
        {
            StopCoroutine(power_change_coroutines[position]);
            power_change_coroutines[position] = null;
        }
        powered_positions[position] = false;
        
        for (int i = 0; i < power_distributions[position].Count; i++)
        {
            power_distributions[position][i] = 0.0f;
        }
        power_consumptions[position] = getPowerConsumption(position);
        checkForOverconsumption(position, power_consumptions[position]);

        List<Component> to_disable = positional_modules[position];
        
        power_change_coroutines[position] = StartCoroutine(powerDownSequence(to_disable, position));
    }

    //helper method used to set the color of a power icon, called by powerUpdater() and animationProgressHelper()
    private void powerIconHelper(GameObject to_change, float a)
    {
        Color icon_color = to_change.GetComponent<UnityEngine.UI.RawImage>().color;
        to_change.GetComponent<UnityEngine.UI.RawImage>().color = new Color(icon_color.r, icon_color.g, icon_color.b, a);
    }

    //helper method used to set the alphas of each of the green-to-red circles based on a given power level (0-10)
    private void animationProgressHelper(GameObject display, int power_level, float percent, float min_alpha)
    {
        float tmp_prcnt = percent;
        for (int i = 0; i < power_level; i++)
        {
            tmp_prcnt = percent - ((1.0f / power_level) * i);
            float a = Mathf.Max(min_alpha, tmp_prcnt / (1.0f / power_level));
            powerIconHelper(display.transform.GetChild(i + 1).gameObject, a);
        }
    }

    //runs on an infinite loop, updates the green-to-red power consumption screens for all positions
    IEnumerator powerUpdater()
    {
        while (true)
        {
            //start at minimum transparency (0.2f)
            int[] power_levels = new int[4] { 0, 0, 0, 0 };
            for (int i = 0; i < 4; i++)
            {
                power_levels[i] = (int)Mathf.Floor(power_consumptions[i] * 10.0f);
                for (int k = 1; k < 11; k++)
                {
                    position_power_displays[i].transform.GetChild(k).GetChild(0).gameObject.SetActive(k > power_levels[i]);
                    powerIconHelper(position_power_displays[i].transform.GetChild(k).gameObject, 0.2f);

                    engineer_power_displays[i].transform.GetChild(k).GetChild(0).gameObject.SetActive(k > power_levels[i]);
                    powerIconHelper(engineer_power_displays[i].transform.GetChild(k).gameObject, 0.2f);
                }
            }

            //increase alphas based on how power consumption over the course of POWER_UPDATE_TIME
            float anim_time = POWER_UPDATE_TIME;
            while (anim_time > 0.0f)
            {
                anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

                float animation_progress = 1.0f - (anim_time / POWER_UPDATE_TIME);
                for (int i = 0; i < 4; i++)
                {
                    animationProgressHelper(position_power_displays[i], power_levels[i], animation_progress, 0.2f);
                    animationProgressHelper(engineer_power_displays[i], power_levels[i], animation_progress, 0.5f);
                }

                yield return null;
            }
        }
    }

    //used after the conclusion of a power overconsumption sequence (power shutdown)
    private void resetEngineerPositionDisplay(int position)
    {
        //hide warning bar
        engineer_power_displays[position].transform.GetChild(0).gameObject.SetActive(false);

        //change colors of position icon and label
        engineer_power_displays[position].transform.GetChild(11).GetComponent<UnityEngine.UI.RawImage>().color = ReferenceAssistor.COLOR_OPTIONS[position];
        engineer_power_displays[position].transform.GetChild(12).GetComponent<TMP_Text>().color = ReferenceAssistor.COLOR_OPTIONS[position];

        //get power allocation for that position
        int max_allocation = (int)(power_allocation.getPowerAllocation(position) * 10.0f);

        //recolor circles from red to their actual color
        for (int i = 1; i <= 10; i++)
        {
            //recolor circle
            float circle_alpha = engineer_power_displays[position].transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color.a;
            Color corresponding_circle_color = position_power_displays[0].transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color; //use pilot position as a reference
            engineer_power_displays[position].transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(corresponding_circle_color.r, corresponding_circle_color.g, corresponding_circle_color.b, circle_alpha);

            //recolor power allocation circle
            float power_alpha = 0.2f;
            if (i <= max_allocation)
            {
                power_alpha = 1.0f;
            }
            engineer_power_displays[position].transform.GetChild(i).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, power_alpha);
        }
    }

    //used when a position's power consumption exceeds their allocation
    IEnumerator imminentPowerLoss(int index)
    {
        //play engineer warning sound if not playing already
        if (overconsumption_warning_sounds[2].isPlaying == false)
        {
            overconsumption_warning_sounds[2].Play();
        }

        //play corresponding warning sound
        if (overconsumption_warning_sounds[index].isPlaying == false)
        {
            overconsumption_warning_sounds[index].Play();
        }

        //show red warning bar
        GameObject power_loss_bar = engineer_power_displays[index].transform.GetChild(0).gameObject;
        power_loss_bar.SetActive(true);

        //change colors of position icon and label to red
        engineer_power_displays[index].transform.GetChild(11).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        engineer_power_displays[index].transform.GetChild(12).GetComponent<TMP_Text>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);

        //change colors of each circle to red
        for (int i = 1; i <= 10; i++)
        {
            float a = engineer_power_displays[index].transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color.a;
            engineer_power_displays[index].transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 0.0f, 0.0f, a);
            engineer_power_displays[index].transform.GetChild(i).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }

        //animate the progress bar based on TIME_TO_POWER_LOSS and positional indicator (if pilot or tactician)
        float anim_time = TIME_TO_POWER_LOSS;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            if (index != 2)
            {
                status_indicators.displayOverconsumptionPositionIndicator(index, 1.0f - (anim_time / TIME_TO_POWER_LOSS));
            }
            power_loss_bar.GetComponent<UnityEngine.UI.Image>().fillAmount = (anim_time / TIME_TO_POWER_LOSS);

            yield return null;
        }

        //if time runs out, then power shutdown (if host)
        if (NetworkManager.Singleton.IsHost == true)
        {
            totalShutdownRPC(index);
        }
    }

    IEnumerator shutdownProcess(int reason)
    {
        //cache auxiliary power availability
        bool auxiliary_power_available = ReferenceAssistor.Instance.module_handlers[2].GetComponent<AuxiliaryPower>().canUseAuxiliaryPower();

        //handle shutdown effects (lights, sounds)
        lights_manager.setDefaultLights(false);
        lights_manager.setEmergencyLights(false);
        power_off_sound.Play();
        ship_beeps_sound.Stop();
        foreach (AudioSource overconsumption_warning_sound in overconsumption_warning_sounds)
        {
            overconsumption_warning_sound.Stop();
        }
        background_animator.disableAllScreens();
        background_animator.disableEnergyCircles();
        ReferenceAssistor.Instance.seat_manager.reflectPowerChange();

        //stop orange flashing at positions where a player is sitting but power dial is not active
        power_control.updatePlayerNotifiers();

        //stop updating power consumption
        if (power_updater_coroutine != null)
        {
            StopCoroutine(power_updater_coroutine);
            power_updater_coroutine = null;
        }

        //power down all stations
        for (int i = 0; i < 4; i++)
        {
            power_control.disableDial(i);
            disableStation(i);
            if (overconsumption_coroutines[i] != null)
            {
                StopCoroutine(overconsumption_coroutines[i]);
                overconsumption_coroutines[i] = null;
                if (i != 2)
                {
                    status_indicators.resetOverconsumptionPositionIndicator(i);
                }
                resetEngineerPositionDisplay(i);
            }
        }

        //clear out all power sources
        GetComponent<PowerRegulator>().disableAllPowerSources();

        yield return new WaitForSeconds(2.0f);

        //play notification sounds
        ReferenceAssistor.Instance.audio_manager.AddNotification(1, power_notifications[6]);
        ReferenceAssistor.Instance.audio_manager.AddNotification(1, power_notifications[reason]);
        if (auxiliary_power_available == true)
        {
            ReferenceAssistor.Instance.audio_manager.AddNotification(1, power_notifications[8]);
        }

        lights_manager.disableRedAlert();
        lights_manager.setEmergencyLights(true);

        shutdown_coroutine = null;
    }

    //called by PowerRegulator.terminateDepletionRPC() and PilotingSystem.cs
    public void totalShutdown(bool energy_surge)
    {
        if (ship_has_power == true)
        {
            if (energy_surge == true)
            {
                totalShutdownRPC(5);
            }
            else
            {
                totalShutdownRPC(4);
            }
        }
    }

    //called by PowerRegulator.moduleCompleted(), PowerRegulator.useAuxiliaryPower()
    public void restorePower()
    {
        if (shutdown_coroutine != null)
        {
            StopCoroutine(shutdown_coroutine);
            shutdown_coroutine = null;
        }

        if (ship_has_power == false)
        {
            powerRestartRPC();
        }
    }

    //calls overconsumptionRPC() or abortOverconsumptionRPC() if applicable
    private void checkForOverconsumption(int position, float allocation)
    {
        if (power_consumptions[position] > allocation && overconsumption_coroutines[position] == null)
        {
            overconsumptionRPC(position);            
        }
        else if (power_consumptions[position] <= allocation && overconsumption_coroutines[position] != null)
        {
            abortOverconsumptionRPC(position);
        }
    }

    //calls checkForOverConsumption()
    public void allocationChange(int position, float allocation)
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            checkForOverconsumption(position, allocation);
        }
    }

    public void powerOn(int position)
    {
        position_power_displays[position].SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        position_power_displays[position].SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void totalShutdownRPC(int reason)
    {
        //kill power
        ship_has_power = false;

        if (power_restart_coroutine != null)
        {
            StopCoroutine(power_restart_coroutine);
            power_restart_coroutine = null;
        }

        if (shutdown_coroutine == null)
        {
            shutdown_coroutine = StartCoroutine(shutdownProcess(reason));
        }
    }

    IEnumerator restartPower()
    {
        //play sound but wait a delay
        power_on_sound.Play();

        //show power enabled on power status screen in engineer position
        GetComponent<PowerRegulator>().displayPowerRestoration();

        yield return new WaitForSeconds(3.0f);

        //play power restoration notification
        ReferenceAssistor.Instance.audio_manager.AddNotification(0, power_notifications[7]);

        //bring back power
        ship_has_power = true;

        //handle restart effects (lights, sounds)
        lights_manager.setDefaultLights(true);
        lights_manager.setEmergencyLights(false);
        ship_beeps_sound.Play();
        background_animator.enableAllScreens(1.5f);
        background_animator.enableEnergyCircles();
        ReferenceAssistor.Instance.seat_manager.reflectPowerChange();

        //start updating power consumption
        if (power_updater_coroutine == null)
        {
            power_updater_coroutine = StartCoroutine(powerUpdater());
        }

        //power all stations
        for (int i = 0; i < 4; i++)
        {
            powerStation(i);
            power_control.turnDial(i, true);
        }

        //start orange flashing for power dials (if a player is sitting at a position)
        power_control.updatePlayerNotifiers();

        power_restart_coroutine = null;
    }

    [Rpc(SendTo.Everyone)]
    private void powerRestartRPC()
    {
        if (power_restart_coroutine == null)
        {
            power_restart_coroutine = StartCoroutine(restartPower());
        }
    }

    [Rpc(SendTo.Everyone)]
    private void overconsumptionRPC(int index)
    {
        if (overconsumption_coroutines[index] != null)
        {
            StopCoroutine(overconsumption_coroutines[index]);
        }

        overconsumption_coroutines[index] = StartCoroutine(imminentPowerLoss(index));
    }

    [Rpc(SendTo.Everyone)]
    private void abortOverconsumptionRPC(int index)
    {
        if (overconsumption_coroutines[index] != null)
        {
            StopCoroutine(overconsumption_coroutines[index]);
        }
        overconsumption_coroutines[index] = null;

        overconsumption_warning_sounds[index].Stop();
        if (overconsumption_warning_sounds[2].isPlaying == true && checkIfOverconsuming() == false)
        {
            overconsumption_warning_sounds[2].Stop();
        }

        if (index != 2)
        {
            status_indicators.resetOverconsumptionPositionIndicator(index);
        }
        resetEngineerPositionDisplay(index);
    }

    [Rpc(SendTo.Everyone)]
    private void powerConsumptionChangeRPC(int position, float consumption)
    {
        power_consumptions[position] = consumption;
        if (NetworkManager.Singleton.IsHost == true)
        {
            checkForOverconsumption(position, power_allocation.getPowerAllocation(position));
        }
    }

    //CONTROL LINKING AND ORDER (for power-on/power-off purposes)
    private void addPilotModules()
    {
        List<Component> pilot_modules = new List<Component>();
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("SignalJammer")); //1
        pilot_modules.Add(this); //2
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("EmissionReducers")); //3
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[4].GetComponent("PrefixCodeManager")); //4
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("DirectionalShifter")); //5
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("TractorBeamOptions")); //6
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("EngineMonitoring")); //7
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("SpatialCompositionAnalyzer")); //8
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[4].GetComponent("StatusIndicators")); //9
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[4].GetComponent("StatusIndicators")); //10
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("TractorBeamPower")); //11
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("InertialDampener")); //12
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("Headlights")); //13
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("Warp")); //14
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("VerticalThrusters")); //15
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("FlyingInstruments")); //16
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("ShipSteering")); //17
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("HorizontalThrusters")); //18
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("FlyingInstruments")); //19
        pilot_modules.Add(ReferenceAssistor.Instance.module_handlers[0].GetComponent("ImpulseThrottle")); //20
        positional_modules[0] = pilot_modules;
    }

    private void addTacticianModules()
    {
        List<Component> tactician_modules = new List<Component>();
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("EncryptionKeys")); //1
        tactician_modules.Add(this); //2
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("CloakDetector")); //3
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[4].GetComponent("PrefixCodeManager")); //4
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("ProbeController")); //5
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("ProbeInfo")); //6
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[4].GetComponent("StatusIndicators")); //7
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[4].GetComponent("StatusIndicators")); //8
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("UniversalCommunicator")); //9
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("PhaserIntensities")); //10
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("PhaserActivators")); //11
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("LongRangeDirection")); //12
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("ProximityMap")); //13
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("TorpedoTrigger")); //14
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("ProximityMapOptions")); //15
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("TorpedoBaySelector")); //16
        tactician_modules.Add(ReferenceAssistor.Instance.module_handlers[1].GetComponent("ThreatDetectors")); //17
        positional_modules[1] = tactician_modules;
    }

    private void addEngineerModules()
    {
        List<Component> engineer_modules = new List<Component>();
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("ScenarioMap")); //1
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("ScenarioCountdown")); //2
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("PhaserFrequency")); //3
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("EnergyPattern")); //4
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("ScenarioMap")); //5
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("PowerAllocation")); //6
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("PowerAllocation")); //7
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("PhaserHeat")); //8
        engineer_modules.Add(ReferenceAssistor.Instance.spaceship.GetComponent("ShipHealth")); //9
        engineer_modules.Add(ReferenceAssistor.Instance.spaceship.GetComponent("ShipHealth")); //10
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("ShieldStrength")); //11
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("TorpedoLoader")); //12
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("EngineCoolantSupply")); //13
        engineer_modules.Add(ReferenceAssistor.Instance.spaceship.GetComponent("ShipInventory")); //14
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("CargoEject")); //15
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[2].GetComponent("ComputerRegulator")); //16
        engineer_modules.Add(this); //17
        engineer_modules.Add(ReferenceAssistor.Instance.module_handlers[4].GetComponent("PrefixCodeManager")); //18
        positional_modules[2] = engineer_modules;
    }

    private void addCaptainModules()
    {
        List<Component> captain_modules = new List<Component>();
        captain_modules.Add(ReferenceAssistor.Instance.module_handlers[3].GetComponent("ProcedureManual")); //1
        captain_modules.Add(ReferenceAssistor.Instance.module_handlers[4].GetComponent("PrefixCodeManager")); //2
        captain_modules.Add(this); //3
        captain_modules.Add(ReferenceAssistor.Instance.module_handlers[3].GetComponent("SelfDestruct")); //4
        captain_modules.Add(ReferenceAssistor.Instance.module_handlers[3].GetComponent("ShipStatus")); //5
        captain_modules.Add(ReferenceAssistor.Instance.module_handlers[3].GetComponent("ComputerArray")); //6
        captain_modules.Add(ReferenceAssistor.Instance.module_handlers[3].GetComponent("OverrideSwitches")); //7
        captain_modules.Add(ReferenceAssistor.Instance.module_handlers[3].GetComponent("EmergencyLights")); //8
        captain_modules.Add(ReferenceAssistor.Instance.module_handlers[3].GetComponent("ShipBeacon")); //9
        captain_modules.Add(ReferenceAssistor.Instance.module_handlers[4].GetComponent("StatusIndicators")); //10
        captain_modules.Add(ReferenceAssistor.Instance.module_handlers[3].GetComponent("OperatingManual")); //11
        positional_modules[3] = captain_modules;
    }
}