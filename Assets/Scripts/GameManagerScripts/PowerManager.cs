/*
    PowerManager.cs
    - Handles powering on/off each of the positions
    - Handles power consumption
    Contributor(s): Jake Schott
    Last Updated: 8/29/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerManager : MonoBehaviour, IPowerable
{
    //CLASS CONSTANTS
    private static float POWER_ON_TIME = 1.0f; //how long it takes to power on a position
    private static float POWER_OFF_TIME = 1.0f; //how long it takes to power down a position
    private static float POWER_UPDATE_TIME = 0.5f; //how often the power consumption displays update

    public List<GameObject> position_power_displays = null;
    public List<GameObject> engineer_power_displays = null;
    public List<GameObject> power_warnings = null;

    private GameObject control_handler;
    private GameObject sensor_handler;

    private List<Component> pilot_modules = new List<Component>();
    private List<Component> tactician_modules = new List<Component>();
    private List<Component> engineer_modules = new List<Component>();
    private List<Component> captain_modules = new List<Component>();

    private bool[] powered_positions = new bool[] { false, false, false, false }; //corresponds to pilot, tactician, engineer, captain
    private float[] power_consumptions = new float[] { 1.0f, 0.4f, 0.8f, 0.1f }; //corresponds to pilot, tactician, engineer, captain
    private Coroutine[] power_change_coroutines = new Coroutine[] { null, null, null, null };

    private void Start()
    {
        control_handler = GameObject.FindGameObjectWithTag("ControlHandler");
        sensor_handler = GameObject.FindGameObjectWithTag("SensorHandler");

        addPilotModules();
        addTacticianModules();
        addEngineerModules();
        addCaptainModules();

        StartCoroutine(powerUpdater());
    }

    private void addPilotModules()
    {
        pilot_modules.Add(control_handler.GetComponent("SignalJammer")); //1
        pilot_modules.Add(this); //2
        pilot_modules.Add(control_handler.GetComponent("Shields")); //3
        pilot_modules.Add(sensor_handler.GetComponent("PrefixCodeManager")); //4
        pilot_modules.Add(control_handler.GetComponent("DirectionalShifter")); //5
        pilot_modules.Add(control_handler.GetComponent("TractorBeamOptions")); //6
        pilot_modules.Add(sensor_handler.GetComponent("PilotTractorBeamProgress")); //7
        pilot_modules.Add(sensor_handler.GetComponent("PilotSCA")); //8
        pilot_modules.Add(control_handler.GetComponent("ShipStatus")); //9
        pilot_modules.Add(this); //10
        pilot_modules.Add(control_handler.GetComponent("TractorBeamPower")); //11
        pilot_modules.Add(control_handler.GetComponent("InertialDampeners")); //12
        pilot_modules.Add(control_handler.GetComponent("Headlights")); //13
        pilot_modules.Add(control_handler.GetComponent("Warp")); //14
        pilot_modules.Add(control_handler.GetComponent("VerticalThrusters")); //15
        pilot_modules.Add(sensor_handler.GetComponent("PilotNavigation")); //16
        pilot_modules.Add(control_handler.GetComponent("CourseHeading")); //17
        pilot_modules.Add(control_handler.GetComponent("HorizontalThrusters")); //18
        pilot_modules.Add(sensor_handler.GetComponent("PilotNavigation")); //19
        pilot_modules.Add(control_handler.GetComponent("ImpulseThrottle")); //20
    }

    private void addTacticianModules()
    {
        tactician_modules.Add(control_handler.GetComponent("TorpedoPower")); //1
        tactician_modules.Add(this); //2
        tactician_modules.Add(control_handler.GetComponent("ProbeOrientation")); //3
        tactician_modules.Add(sensor_handler.GetComponent("PrefixCodeManager")); //4
        tactician_modules.Add(control_handler.GetComponent("TransmissionHandler")); //5
        tactician_modules.Add(sensor_handler.GetComponent("TacticianProbeInfo")); //6
        tactician_modules.Add(control_handler.GetComponent("ShipStatus")); //7
        tactician_modules.Add(this); //8
        tactician_modules.Add(control_handler.GetComponent("ProbeVerticalMovement")); //9
        tactician_modules.Add(control_handler.GetComponent("ProbeLateralMovement")); //10
        tactician_modules.Add(control_handler.GetComponent("PhaserTemperatures")); //11
        tactician_modules.Add(control_handler.GetComponent("UniversalCommunicator")); //12
        tactician_modules.Add(control_handler.GetComponent("LongRangeDirection")); //13
        tactician_modules.Add(control_handler.GetComponent("TorpedoSelector")); //14
        tactician_modules.Add(sensor_handler.GetComponent("TacticianMap")); //15
        tactician_modules.Add(control_handler.GetComponent("MapOptions")); //16
        tactician_modules.Add(control_handler.GetComponent("TorpedoTrigger")); //17
        tactician_modules.Add(control_handler.GetComponent("ProbeOptions")); //18
        tactician_modules.Add(control_handler.GetComponent("PhaserPowers")); //19
    }

    private void addEngineerModules()
    {
        engineer_modules.Add(control_handler.GetComponent("PhaserFrequency")); //1
        engineer_modules.Add(control_handler.GetComponent("EnergyPattern")); //2
        engineer_modules.Add(this); //3
        engineer_modules.Add(sensor_handler.GetComponent("PrefixCodeManager")); //4
    }

    private void addCaptainModules()
    {
        captain_modules.Add(control_handler.GetComponent("ShipStatus")); //1
        captain_modules.Add(control_handler.GetComponent("SelfDestruct")); //2
        captain_modules.Add(control_handler.GetComponent("ShipManual")); //3
        captain_modules.Add(this); //4
        captain_modules.Add(sensor_handler.GetComponent("PrefixCodeManager")); //5
        captain_modules.Add(control_handler.GetComponent("CommunicationsManual")); //6
        captain_modules.Add(control_handler.GetComponent("CargoJettison")); //7
        captain_modules.Add(control_handler.GetComponent("ShipBeacon")); //8
        captain_modules.Add(control_handler.GetComponent("ShipOverride")); //9
        captain_modules.Add(control_handler.GetComponent("EmergencyLights")); //10
    }

    IEnumerator modulePowerSequence(List<Component> to_power_on, int position)
    {
        for (int i = 0; i < to_power_on.Count; i++)
        {
            IPowerable current_module = (IPowerable)to_power_on[i];
            current_module.powerOn(position);
            yield return new WaitForSeconds(POWER_ON_TIME / to_power_on.Count);
        }

        control_handler.GetComponent<PowerControl>().enableDial(position, true);
        power_change_coroutines[position] = null;
    }

    IEnumerator powerDownSequence(List<Component> to_disable, int position)
    {
        control_handler.GetComponent<PowerControl>().turnDial(position, false);
        for (int i = 0; i < to_disable.Count; i++)
        {
            IPowerable current_module = (IPowerable)to_disable[i];
            current_module.powerOff(position, POWER_OFF_TIME);
        }

        yield return new WaitForSeconds(POWER_OFF_TIME);

        control_handler.GetComponent<PowerControl>().enableDial(position, false);
        power_change_coroutines[position] = null;
    }

    //called by PowerControl
    public bool getPowerEnabled(int position)
    {
        return powered_positions[position];
    }

    //called by PowerControl
    public void powerStation(int position)
    {
        if (powered_positions[position] == true)
        {
            return;
        }
        powered_positions[position] = true;
        List<Component> to_enable = null;
        if (position == 0)
        {
            to_enable = pilot_modules;
        }
        else if (position == 1)
        {
            to_enable = tactician_modules;
        }
        else if (position == 2)
        {
            to_enable = engineer_modules;
        }
        else
        {
            to_enable = captain_modules;
        }
        if (power_change_coroutines[position] != null)
        {
            StopCoroutine(power_change_coroutines[position]);
        }
        power_change_coroutines[position] = StartCoroutine(modulePowerSequence(to_enable, position));
    }

    //called by PowerControl
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
        List<Component> to_disable = null;
        if (position == 0)
        {
            to_disable = pilot_modules;
        }
        else if (position == 1)
        {
            to_disable = tactician_modules;
        }
        else if (position == 2)
        {
            to_disable = engineer_modules;
        }
        else
        {
            to_disable = captain_modules;
        }
        power_change_coroutines[position] = StartCoroutine(powerDownSequence(to_disable, position));
    }

    //called by this script to display the position's power circles and the warning indicator only
    public void powerOn(int position)
    {
        if (position <= 1)
        {
            if (position_power_displays[position].activeSelf == true)
            {
                power_warnings[position].SetActive(true);
            }
        }
        position_power_displays[position].SetActive(true);
    }

    //called by this script to hide the position's power circles and the warning indicator only
    public void powerOff(int position, float time)
    {
        if (position <= 1)
        {
            power_warnings[position].SetActive(false);
        }
        position_power_displays[position].SetActive(false);
    }

    private void powerIconHelper(GameObject to_change, float a)
    {
        Color icon_color = to_change.GetComponent<UnityEngine.UI.RawImage>().color;
        to_change.GetComponent<UnityEngine.UI.RawImage>().color = new Color(icon_color.r, icon_color.g, icon_color.b, a);
    }

    private void animationProgressHelper(GameObject display, int power_level, float percent)
    {
        float tmp_prcnt = percent;
        for (int i = 0; i < power_level; i++)
        {
            tmp_prcnt = percent - ((1.0f / power_level) * i);
            float a = Mathf.Max(0.2f, tmp_prcnt / (1.0f / power_level));
            powerIconHelper(display.transform.GetChild(i + 1).gameObject, a);
        }
    }

    IEnumerator powerUpdater()
    {
        while (true)
        {
            int[] power_levels = new int[4] { 0, 0, 0, 0 };
            for (int i = 0; i < 4; i++)
            {
                power_levels[i] = (int)Mathf.Floor(power_consumptions[i] * 10.0f);
                for (int k = 1; k <= 10; k++)
                {
                    position_power_displays[i].transform.GetChild(k).GetChild(0).gameObject.SetActive(!(k <= power_levels[i]));
                    powerIconHelper(position_power_displays[i].transform.GetChild(k).gameObject, 0.2f);

                    engineer_power_displays[i].transform.GetChild(k).GetChild(0).gameObject.SetActive(!(k <= power_levels[i]));
                    powerIconHelper(engineer_power_displays[i].transform.GetChild(k).gameObject, 0.2f);
                }
            }

            float anim_time = POWER_UPDATE_TIME;
            while (anim_time > 0.0f)
            {
                anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

                for (int i = 0; i < 4; i++)
                {
                    animationProgressHelper(position_power_displays[i], power_levels[i], 1.0f - (anim_time / POWER_UPDATE_TIME));
                    animationProgressHelper(engineer_power_displays[i], power_levels[i], 1.0f - (anim_time / POWER_UPDATE_TIME));
                }

                yield return null;
            }
        }
    }














}
