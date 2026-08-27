/*
    PhaserHeat.cs
    - Adjusts short-range and long-range heat based on usage and intensity
    Contributor(s): Jake Schott
    Last Updated: 8/25/2026
*/

using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PhaserHeat : NetworkBehaviour, IPowerable
{
    //0 is long-range, 1 is short-range
    private static float[] PHASER_OVERHEAT_TIMES = new float[2] { 40.0f, 30.0f }; //how long it takes to overheat at max intensity
    private static float[] PHASER_NORMAL_COOLDOWN_TIMES = new float[2] { 5.0f, 7.5f }; //how long it takes to cool down normally
    private static float[] PHASER_OVERHEATED_COOLDOWN_TIMES = new float[2] { 20.0f, 25.0f }; //how long it takes to cool down when overheated
    private static string[] PHASER_HEAT_STATE_MESSAGES = new string[3] { "INACTIVE", "ACTIVE", "OVERHEATED" };
    private static Color[] PHASER_CATEGORY_COLORS = new Color[2] { ReferenceAssistor.COLOR_OPTIONS[0], ReferenceAssistor.COLOR_OPTIONS[2] };

    public GameObject phaser_heat_display;
    public List<AudioClip> overheat_notifications;
    private UnityEngine.UI.Image[] phaser_heat_bars = new UnityEngine.UI.Image[2];
    private PhaserIntensities phaser_intensities;

    private float[] phaser_heats = new float[] { 0.0f, 0.0f };
    private int[] phaser_states = new int[] { 0, 0 }; //0 is zero heat, 1 is overheating, 2 is overheated/cooling down
    private Coroutine phaser_heat_adjuster_coroutine = null;

    private void Start()
    {
        phaser_intensities = ReferenceAssistor.Instance.module_handlers[1].GetComponent<PhaserIntensities>();
        phaser_heat_bars[0] = phaser_heat_display.transform.GetChild(1).GetChild(2).GetComponent<UnityEngine.UI.Image>();
        phaser_heat_bars[1] = phaser_heat_display.transform.GetChild(2).GetChild(2).GetComponent<UnityEngine.UI.Image>();
    }

    public void resetToDefault()
    {
        //stop head adjustment
        if (phaser_heat_adjuster_coroutine != null)
        {
            StopCoroutine(phaser_heat_adjuster_coroutine);
            phaser_heat_adjuster_coroutine = null;
        }

        //reset to unheated
        for (int i = 0; i < 2; i++)
        {
            phaser_heats[i] = 0.0f;
            phaser_states[i] = 0;
            phaser_heat_bars[i].fillAmount = 0.01f;
            displayStateAdjustment(i);
        }
    }

    public void onPhaserActivationChange()
    {
        if (phaser_heat_adjuster_coroutine == null)
        {
            phaser_heat_adjuster_coroutine = StartCoroutine(heatAdjuster());
        }
    }

    public bool isOverheated(int phaser_category)
    {
        return (phaser_states[phaser_category] == 2);
    }

    private void displayStateAdjustment(int phaser_category)
    {
        //adjust title
        phaser_heat_display.transform.GetChild(1 + phaser_category).GetChild(1).GetComponent<TMP_Text>().SetText(PHASER_HEAT_STATE_MESSAGES[phaser_states[phaser_category]]);

        //play sound
        if (phaser_states[phaser_category] == 2)
        {
            ReferenceAssistor.Instance.audio_manager.AddNotification(0, overheat_notifications[phaser_category]);
        }

        //find color
        Color c = PHASER_CATEGORY_COLORS[phaser_category];
        if (phaser_states[phaser_category] == 2)
        {
            c = new Color(1.0f, 0.0f, 0.0f);
        }

        //display color
        phaser_intensities.adjustPhaserColor(phaser_category, c);
        phaser_heat_display.transform.GetChild(1 + phaser_category).GetChild(1).GetComponent<TMP_Text>().color = c;
        if (phaser_states[phaser_category] == 0)
        {
            c.a = 0.08f;
        }
        phaser_heat_display.transform.GetChild(1 + phaser_category).GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = c;
        c.a = 0.08f;
        phaser_heat_display.transform.GetChild(1 + phaser_category).GetComponent<UnityEngine.UI.RawImage>().color = c;
        c.a = 0.16f;
        phaser_heat_display.transform.GetChild(1 + phaser_category).GetChild(2).GetComponent<UnityEngine.UI.Image>().color = c;
    }

    IEnumerator heatAdjuster()
    {
        float[] intensities = phaser_intensities.getPhaserIntensities();

        while (intensities[0] > 0.0f || intensities[1] > 0.0f)
        {
            do
            {
                yield return null;

                //get info
                intensities = phaser_intensities.getPhaserIntensities();

                //increase/decrease heat
                for (int i = 0; i < 2; i++)
                {
                    if (phaser_states[i] == 2) //overheated cooldown
                    {
                        phaser_heats[i] = Mathf.Max(0.0f, phaser_heats[i] - (Time.deltaTime / PHASER_OVERHEATED_COOLDOWN_TIMES[i]));
                    }
                    else if (phaser_states[i] < 2 && intensities[i] > 0.0f) //heat increase
                    {
                        phaser_heats[i] = Mathf.Min(1.0f, phaser_heats[i] + ((Time.deltaTime / PHASER_OVERHEAT_TIMES[i]) * (1.0f + intensities[i])));
                    }
                    else if (phaser_states[i] == 1 && intensities[i] == 0.0f) //heat decrease
                    {
                        phaser_heats[i] = Mathf.Max(0.0f, phaser_heats[i] - (Time.deltaTime / PHASER_NORMAL_COOLDOWN_TIMES[i]));
                    }
                }

                //send updates
                phaserHeatUpdateRPC(phaser_heats[0], phaser_heats[1]);
            }
            while (phaser_heats[0] > 0.0f || phaser_heats[1] > 0.0f);
        }

        phaser_heat_adjuster_coroutine = null;
    }

    public void powerOn(int position)
    {
        phaser_heat_display.SetActive(true);
    }

    public void powerOff(int position, float time)
    {
        phaser_heat_display.SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void phaserHeatUpdateRPC(float lr_heat, float sr_heat)
    {
        phaser_heats[0] = lr_heat;
        phaser_heats[1] = sr_heat;
        int[] new_states = new int[2] { -1, -1 };

        //adjust state based on new heat
        for (int i = 0; i < 2; i++)
        {
            if (phaser_states[i] < 2 && phaser_heats[i] == 1.0f)
            {
                new_states[i] = 2;
            }
            else if (phaser_states[i] < 2 && phaser_heats[i] > 0.0f)
            {
                new_states[i] = 1;
            }
            else if (phaser_heats[i] == 0.0f)
            {
                new_states[i] = 0;
            }
        }

        //adjust fill bar amount and check if need for display update
        for (int i = 0; i < 2; i++)
        {
            phaser_heat_bars[i].fillAmount = Mathf.Lerp(0.01f, 1.0f, phaser_heats[i]);
            if (new_states[i] >= 0 && new_states[i] != phaser_states[i])
            {
                phaser_states[i] = new_states[i];
                displayStateAdjustment(i);
            }
        }
    }
}