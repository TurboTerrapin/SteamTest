/*
    EngineMonitoring.cs
    - Updates speed and engine capacity temperature screens (next to Spatial Composition Analyzer)
    - Adjusts engine sound
    Contributor(s): Jake Schott
    Last Updated: 2/1/2026
*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EngineMonitoring : MonoBehaviour, IPowerable, IDescribable
{
    //CLASS CONSTANTS
    private static Color[] COLOR_OPTIONS = new Color[3] { new Color(0.0f, 0.84f, 1.0f), new Color(1.0f, 0.47f, 0.0f), new Color(1.0f, 0.0f, 0.0f) }; //blue, orange, red
    private static string[] STATE_NAMES = new string[3] { "NOMINAL", "REDUCED", "MINIMAL" };

    //list of all ray target names
    private List<string> RAY_TARGETS = new List<string>()
    {
        "speedometer",
        "engine_performance"
    };

    //module titles 
    private static string[] INFO_TITLES = new string[]
    {
        "SHIP SPEED",
        "ENGINE PERFORMANCE"
    };

    //module additional info, or "" if none
    private static string[] INFO_DESCS = new string[]
    {
        "Describes current ship speed based on impulse percentage and engine capacity.",
        "Describes engine performance based on impulse percentage and engine capacity based on engine temperature."
    };

    public GameObject engine_speed_display;
    public GameObject engine_capacity_display;
    public TMP_Text engineer_speed_text;
    public AudioSource ambient_ship_noise;

    private ImpulseThrottle impulse_throttle;
    private EngineCoolantSupply engine_coolant_supply;

    private int current_state = 0; //0 is nominal, 1 is reduced, 2 is minimal
    private List<HUDInfo> corresponding_infos = new List<HUDInfo>();

    private void Start()
    {
        impulse_throttle = ReferenceAssistor.Instance.module_handlers[0].GetComponent<ImpulseThrottle>();
        engine_coolant_supply = ReferenceAssistor.Instance.module_handlers[2].GetComponent<EngineCoolantSupply>();

        for (int i = 0; i < INFO_TITLES.Length; i++)
        {
            corresponding_infos.Add(new HUDInfo(INFO_TITLES[i]));
            if (INFO_DESCS[i].CompareTo("") != 0)
            {
                corresponding_infos[i].setInfo(INFO_DESCS[i]);
            }
        }
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return corresponding_infos[RAY_TARGETS.IndexOf(current_target.name)];
    }

    private void adjustSpeed()
    {
        float imp = impulse_throttle.getCurrentImpulse();
        float max_speed = engine_coolant_supply.getMaxImpulseSpeedBasedOnEngineTemperature();

        //update speedometer
        engine_speed_display.transform.GetChild(1).transform.localRotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Lerp(150.0f, -150.0f, imp * max_speed));

        //update engine volume
        ambient_ship_noise.volume = Mathf.Lerp(0.1f, 0.5f, imp * max_speed);

        //update speedometer text in engineer position
        string rounded_speed = (Mathf.Round(imp * max_speed * 1000.0f) / 10.0f).ToString();
        if (rounded_speed.Contains(".") == false)
        {
            rounded_speed += ".0";
        }
        engineer_speed_text.SetText("IMPULSE SPEED: " + rounded_speed + "%");
        engineer_speed_text.color = COLOR_OPTIONS[current_state];
    }

    public void impulseAdjustment()
    {
        float imp = impulse_throttle.getCurrentImpulse();
        engine_capacity_display.transform.GetChild(2).GetComponent<UnityEngine.UI.Image>().fillAmount = imp;
        engine_capacity_display.transform.GetChild(3).GetComponent<UnityEngine.UI.Image>().fillAmount = imp;
        adjustSpeed();
    }

    public void displayStateChangeAdjustment()
    {
        //update text
        engine_capacity_display.transform.GetChild(4).GetComponent<TMP_Text>().SetText(STATE_NAMES[current_state]);

        //update colors
        Color status_color = COLOR_OPTIONS[current_state];
        engine_speed_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
        engine_speed_display.transform.GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
        engine_capacity_display.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().color = status_color;
        engine_capacity_display.transform.GetChild(2).GetComponent<UnityEngine.UI.Image>().color = status_color;
        engine_capacity_display.transform.GetChild(3).GetComponent<UnityEngine.UI.Image>().color = status_color;
        engine_capacity_display.transform.GetChild(4).GetComponent<TMP_Text>().color = status_color;

        status_color.a = 0.2f;

        engine_speed_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
        engine_capacity_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
        engine_capacity_display.transform.GetChild(2).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
        engine_capacity_display.transform.GetChild(3).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
        for (int i = 0; i < engine_capacity_display.transform.GetChild(0).childCount; i++)
        {
            engine_capacity_display.transform.GetChild(0).GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
            engine_capacity_display.transform.GetChild(0).GetChild(i).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = status_color;
        }
    }

    public void temperatureAdjustment()
    {
        float temp = engine_coolant_supply.getEngineTemperature();
        adjustSpeed();

        engine_capacity_display.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount = temp;
        int new_state = 0;
        if (temp >= 1.0f)
        {
            new_state = 2;
        }
        else if (temp >= 0.5f)
        {
            new_state = 1;
        }
        if (current_state != new_state)
        {
            current_state = new_state;
            displayStateChangeAdjustment();
        }
    }

    public void powerOn(int position)
    {
        engine_speed_display.SetActive(true);
        engine_capacity_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        engine_speed_display.SetActive(false);
        engine_capacity_display.SetActive(false);
    }
}