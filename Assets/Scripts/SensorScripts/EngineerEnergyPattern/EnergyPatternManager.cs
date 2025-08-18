/*
    EnergyPatternManager.cs
    - Handles energy patterns for ship, probe, and tractor beam
    Contributor(s): Jake Schott
    Last Updated: 8/17/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyPatternManager : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float BAR_ANIMATION_TIME = 0.2f; //bars change every 0.2 seconds
    private static float ENABLED_BLINKER_REFRESH = 0.25f;

    public GameObject enabled_indicator;
    public GameObject selection_indicator;
    public List<GameObject> pattern_source_indicators = null; //ship, probe, and tractor beam (the radio line things)
    public List<GameObject> patterns = null; //ship, probe, and tractor beam
    public PatternData[] corresponding_pattern_data = new PatternData[3] {null, null, null};

    private EnergyPattern energy_pattern_control_script; //the script that handles enabling/disabling/shifting
    private Coroutine alert_flasher_coroutine = null;
    private Coroutine[] source_indicator_coroutines = { null, null, null };

    private void Start()
    {
        energy_pattern_control_script = GameObject.FindGameObjectWithTag("ControlHandler").GetComponent<EnergyPattern>();
    }

    //called by EnergyPattern (the engineer control that handles enabling/disabling/shifting)
    public void updateDisplay(bool is_enabled, int to_show)
    {   
        //reset all
        for (int i = 0; i < 3; i++)
        {
            patterns[i].transform.GetChild(0).gameObject.SetActive(false);
            pattern_source_indicators[i].transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.08f);
        }

        //update signal screen
        selection_indicator.transform.localPosition = new Vector3(pattern_source_indicators[to_show].transform.GetChild(0).localPosition.x, selection_indicator.transform.localPosition.y,  0.0f);
        pattern_source_indicators[to_show].transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);

        //update energy pattern display
        patterns[to_show].transform.GetChild(0).gameObject.SetActive(is_enabled && corresponding_pattern_data[to_show] != null);

        //update enabled indicator
        if (is_enabled == true)
        {
            if (alert_flasher_coroutine != null)
            {
                StopCoroutine(alert_flasher_coroutine);
                alert_flasher_coroutine = null;
            }
            enabled_indicator.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        }
        else if (is_enabled == false && (corresponding_pattern_data[0] != null || corresponding_pattern_data[1] != null || corresponding_pattern_data[2] != null))
        {
            alert_flasher_coroutine = StartCoroutine(alertFlasher());
        }
        else
        {
            enabled_indicator.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.0f);
        }
    }

    //establishes the pattern and sets
    public void setPattern(int index, PatternData pd)
    {
        corresponding_pattern_data[index] = pd;
        patterns[index].GetComponent<PatternVisualizer>().displayPattern(corresponding_pattern_data[index]);
        updateSourceIndicators();
        patterns[index].transform.GetChild(0).gameObject.SetActive(energy_pattern_control_script.getDisplayEnabled() && energy_pattern_control_script.getCurrentlyViewing() == index);

        //handle orange blinker
        if (energy_pattern_control_script.getDisplayEnabled() == false && alert_flasher_coroutine == null)
        {
            alert_flasher_coroutine = StartCoroutine(alertFlasher());
        }
    }

    //clears the pattern
    public void clearPattern(int index)
    {
        patterns[index].transform.GetChild(0).gameObject.SetActive(false);
        //resets to default state
        patterns[index].GetComponent<PatternVisualizer>().resetPattern();
        corresponding_pattern_data[index] = null;
        updateSourceIndicators();

        //handle orange blinker
        if (energy_pattern_control_script.getDisplayEnabled() == false)
        {
            if (corresponding_pattern_data[0] == null && corresponding_pattern_data[1] == null && corresponding_pattern_data[2] == null)
            {
                if (alert_flasher_coroutine != null)
                {
                    StopCoroutine(alert_flasher_coroutine);
                    alert_flasher_coroutine = null;
                }
            }
            enabled_indicator.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.0f);
        }
    }

    //updates the colors in the PatternData and corresponding visualizer
    public void updateColors(int index, List<Color> new_colors, float anim_time)
    {
        corresponding_pattern_data[index].setRingColors(new_colors);
        patterns[index].GetComponent<PatternVisualizer>().changeColors(new_colors, anim_time);
    }

    //resizes the pattern in the corresponding index to either contracted (true) or expanded (false) in time_interval time
    public void resizePattern(int index, bool shrink, float time_interval)
    {
        if (shrink == true)
        {
            patterns[index].GetComponent<PatternVisualizer>().contractPattern(time_interval);
        }
        else
        {
            patterns[index].GetComponent<PatternVisualizer>().expandPattern(time_interval);
        }
    }

    //returns the data that informs the energy pattern
    public PatternData getPatternData(int index)
    {
        return corresponding_pattern_data[index];
    }

    //sizes the bar at the index to the to_size_to input
    private void resizeBar(Transform line, float to_size_to)
    {
        line.GetComponent<RectTransform>().sizeDelta = new Vector2(0.004f + to_size_to * 2, 0.01f);
        line.GetChild(0).localPosition = new Vector3(0.002f + to_size_to, 0.0f, 0.0f);
        line.GetChild(1).localPosition = new Vector3(-0.002f - to_size_to, 0.0f, 0.0f);
    }

    IEnumerator sourceIndicatorAnimator(int index)
    {
        int num_lines = pattern_source_indicators[index].transform.GetChild(1).childCount - 1;
        float[] starting_sizes = new float[num_lines];
        float[] sizes = new float[num_lines];
        GameObject lines = pattern_source_indicators[index].transform.GetChild(1).gameObject;
        bool reset = false;

        while (reset == false)
        {
            for (int i = 0; i < num_lines; i++)
            {
                starting_sizes[i] = lines.transform.GetChild(i + 1).GetChild(0).localPosition.x - 0.002f;
                if (corresponding_pattern_data[index] != null)
                {
                    sizes[i] = Random.Range(0.0f, 1.0f) * 0.01f;
                }
                else
                {
                    sizes[i] = 0.0f;
                }
            }

            float anim_time = BAR_ANIMATION_TIME;
            while (anim_time > 0.0f)
            {
                float dt = Time.deltaTime;
                anim_time = Mathf.Max(0.0f, anim_time - dt);
                for (int i = 0; i < num_lines; i++)
                {
                    float to_size_to = Mathf.Lerp(starting_sizes[i], sizes[i], 1.0f - (anim_time / BAR_ANIMATION_TIME));
                    resizeBar(lines.transform.GetChild(i + 1), to_size_to);
                }
                yield return null;
            }

            //end loop if not active
            if (corresponding_pattern_data[index] == null)
            {
                reset = true;
                for (int i = 0; i < num_lines; i++)
                {
                    if (sizes[i] != 0.0f)
                    {
                        reset = false;
                        break;
                    }
                }
            }
        }

        source_indicator_coroutines[index] = null;
    }

    IEnumerator alertFlasher()
    {
        while (true)
        {
            for (int i = 0; i < 2; i++)
            {
                float anim_time = ENABLED_BLINKER_REFRESH;
                while (anim_time > 0.0f)
                {
                    anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
                    if (i == 0)
                    {
                        enabled_indicator.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.84f, 0.62f, 0.0f, 1.0f - (0.8f * (1.0f - (anim_time / ENABLED_BLINKER_REFRESH))));
                    }
                    else
                    {
                        enabled_indicator.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.84f, 0.62f, 0.0f, 0.2f + (0.8f * (1.0f - (anim_time / ENABLED_BLINKER_REFRESH))));
                    }
                    yield return null;
                }
            }
        }
    }

    private void updateSourceIndicators()
    {
        for (int i = 0; i < 3; i++)
        {
            if (corresponding_pattern_data[i] != null && source_indicator_coroutines[i] == null)
            {
                source_indicator_coroutines[i] = StartCoroutine(sourceIndicatorAnimator(i));
            }
        }
    }
}