/*
    PowerRegulationModuleD.cs
    - Handles the horizontal slider mini-game in the engineer position
    Contributor(s): Jake Schott
    Last Updated: 9/15/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerRegulationModuleD : NetworkBehaviour, IControllable, IPowerRegulable
{
    //CLASS CONSTANTS
    private static float STATE_CHANGE_TIME = 0.5f;
    private static float SLIDE_SPEED = 0.35f;

    private string[] CONTROL_NAMES = new string[3] { "PRIMARY IMPULSE ENERGIZER", "SECONDARY IMPULSE ENERGIZER", "TERTIARY IMPULSE ENERGIZER" };
    private List<string> CONTROL_DESCS = new List<string> { "DECREASE", "INCREASE" };
    private List<int> CONTROL_INDEXES = new List<int>() { 4, 5 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    private List<string> ray_targets = new List<string> { "prsd_slider_green", "prsd_slider_purple", "prsd_slider_orange" };

    public GameObject prsd_display;
    public List<GameObject> prsd_color_identifiers = null;
    public List<GameObject> prsd_sliders = null;

    private float FILL_BAR_REAL_SIZE = 0.0f;
    private float FILL_BAR_X_POS = 0.0f;
    private float TARGET_INDICATOR_SIZE = 0.0f;
    private List<UnityEngine.UI.RawImage> prsd_success_indicators = new List<UnityEngine.UI.RawImage>();
    private List<UnityEngine.UI.Image> prsd_fill_bars = new List<UnityEngine.UI.Image>();
    private List<UnityEngine.UI.RawImage> prsd_target_indicators = new List<UnityEngine.UI.RawImage>();

    private bool currently_active = false;
    private Vector3[] initial_positions = new Vector3[3];
    private Vector3 slide_direction = new Vector3(-0.13f, 0.0f, -0.13f);
    private float[] slider_percentages = new float[3]{ 0.0f, 0.0f, 0.0f };
    private Coroutine state_change_coroutine = null;

    private static HUDInfo hud_info = null;

    private void Start()
    {
        FILL_BAR_REAL_SIZE = prsd_display.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().sizeDelta.y;
        FILL_BAR_X_POS = prsd_display.transform.GetChild(0).GetChild(1).transform.localPosition.x;
        TARGET_INDICATOR_SIZE = prsd_display.transform.GetChild(0).GetChild(2).GetComponent<RectTransform>().sizeDelta.y;

        for (int i = 0; i < 3; i++)
        {
            initial_positions[i] = prsd_sliders[i].transform.localPosition;

            prsd_success_indicators.Add(prsd_display.transform.GetChild(i).GetChild(0).GetComponent<UnityEngine.UI.RawImage>());
            prsd_fill_bars.Add(prsd_display.transform.GetChild(i).GetChild(1).GetComponent<UnityEngine.UI.Image>());
            prsd_target_indicators.Add(prsd_display.transform.GetChild(i).GetChild(2).GetComponent<UnityEngine.UI.RawImage>());
            
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        }

        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        hud_info.setButtons(BUTTON_LISTS[0], 7);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 7);

        return hud_info;
    }

    private bool checkIfBarIsCorrect(int bar)
    {
        float fill_point = FILL_BAR_REAL_SIZE * prsd_fill_bars[bar].fillAmount + (FILL_BAR_X_POS - (FILL_BAR_REAL_SIZE * 0.5f));
        float target_point_low = prsd_target_indicators[bar].transform.localPosition.x - (TARGET_INDICATOR_SIZE * 0.5f);
        float target_point_high = prsd_target_indicators[bar].transform.localPosition.x + (TARGET_INDICATOR_SIZE * 0.5f);
        return ((fill_point > target_point_low) && (fill_point < target_point_high));
    }

    private void changeCorrectIndicator(int bar, bool correct)
    {
        Color circle_color = prsd_success_indicators[bar].color;
        float a = 1.0f;
        if (correct == false)
        {
            a = 0.2f;
        }
        prsd_fill_bars[bar].color = new Color(circle_color.r, circle_color.g, circle_color.b, a);
        prsd_success_indicators[bar].color = new Color(circle_color.r, circle_color.g, circle_color.b, a);
    }

    private void displayAdjustment(int bar)
    {
        //adjust fill bar
        prsd_fill_bars[bar].fillAmount = Mathf.Max(0.01f, slider_percentages[bar]);

        //adjust slider
        prsd_sliders[bar].transform.localPosition = Vector3.Lerp(initial_positions[bar], initial_positions[bar] + slide_direction, slider_percentages[bar]);

        //adjust correct indicator
        changeCorrectIndicator(bar, checkIfBarIsCorrect(bar));
    }

    //sets the state 
    IEnumerator stateChangeHelper(bool to_change_to)
    {
        float anim_time = STATE_CHANGE_TIME;
        float[] starting_positions = new float[3] { slider_percentages[0], slider_percentages[1], slider_percentages[2] };
        float[] destination_positions = new float[3] { 0.0f, 0.0f, 0.0f };

        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            for (int i = 0; i < 3; i++)
            {
                //adjust slider
                float slide_percentage = Mathf.Lerp(destination_positions[i], starting_positions[i], (anim_time / STATE_CHANGE_TIME));
                slider_percentages[i] = slide_percentage;
                prsd_sliders[i].transform.localPosition = Vector3.Lerp(initial_positions[i], initial_positions[i] + slide_direction, slider_percentages[i]);
            }

            yield return null;
        }

        prsd_display.SetActive(to_change_to);

        for (int i = 0; i < 3; i++)
        {
            slider_percentages[i] = 0.0f;
            displayAdjustment(i);
        }
        currently_active = to_change_to;

        state_change_coroutine = null;
    }
    private void resetStateChangeCoroutine()
    {
        if (state_change_coroutine != null)
        {
            StopCoroutine(state_change_coroutine);
            state_change_coroutine = null;
        }
    }

    public void resetToDefault()
    {
        if (currently_active == false)
        {
            return;
        }
        currently_active = false;
        prsd_display.SetActive(false);
        for (int i = 0; i < 3; i++)
        {
            prsd_color_identifiers[i].SetActive(false);
            BUTTON_LISTS[i][0].updateInteractable(false);
            BUTTON_LISTS[i][1].updateInteractable(false);
        }
        resetStateChangeCoroutine();
        state_change_coroutine = StartCoroutine(stateChangeHelper(false));
    }

    public void unlockControl()
    {
        if (currently_active == true)
        {
            return;
        }
        currently_active = true;

        if (NetworkManager.Singleton.IsHost == true)
        {
            float[] new_target_positions = new float[3]{ 0.0f, 0.0f, 0.0f };
            for (int i = 0; i < 3; i++)
            {
                new_target_positions[i] = (Random.Range(0.05f, 0.95f) * FILL_BAR_REAL_SIZE) + (FILL_BAR_X_POS - (FILL_BAR_REAL_SIZE * 0.5f));
            }
            newTargetsRPC(new_target_positions[0], new_target_positions[1], new_target_positions[2]);
        }
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (currently_active == false)
        {
            return;
        }

        int target_index = ray_targets.IndexOf(current_target.name);

        int slide_direction = 0;
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs)) //E to increment
        {
            slide_direction += 1;
        }
        if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs)) //Q to decrement
        {
            slide_direction -= 1;
        }
        if (slide_direction != 0)
        {
            float pos = slider_percentages[target_index];
            if (slide_direction > 0)
            {
                pos = Mathf.Min(1.0f, pos + (dt * SLIDE_SPEED));
            }
            else
            {
                pos = Mathf.Max(0.0f, pos - (dt * SLIDE_SPEED));
            }
            BUTTON_LISTS[target_index][0].updateInteractable(pos > 0.0f);
            BUTTON_LISTS[target_index][1].updateInteractable(pos < 1.0f);
            slideChangeRPC(target_index, pos);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void newTargetsRPC(float targ_a, float targ_b, float targ_c)
    {
        float[] targ_positions = new float[3] { targ_a, targ_b, targ_c };

        prsd_display.SetActive(true);
        for (int i = 0; i < 3; i++)
        {
            prsd_target_indicators[i].transform.localPosition = new Vector3(targ_positions[i], prsd_target_indicators[i].transform.localPosition.y, 0.0f);
            slider_percentages[i] = 0.0f;
            displayAdjustment(i);
            prsd_color_identifiers[i].SetActive(true);
            BUTTON_LISTS[i][0].updateInteractable(slider_percentages[i] > 0.0f);
            BUTTON_LISTS[i][1].updateInteractable(slider_percentages[i] < 1.0f);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void slideChangeRPC(int bar, float new_position)
    {
        slider_percentages[bar] = new_position;
        displayAdjustment(bar);

        if (NetworkManager.Singleton.IsHost == true)
        {
            bool minigame_completed = true;
            for (int i = 0; i < 3; i++)
            {
                if (checkIfBarIsCorrect(i) == false)
                {
                    minigame_completed = false;
                    break;
                }
            }

            if (minigame_completed == true)
            {
                transmitModuleCompletionRPC();
            }
        }
    }

    //called by host when mini-game completed
    [Rpc(SendTo.Everyone)]
    private void transmitModuleCompletionRPC()
    {
        GameObject.Find("PowerHandler").GetComponent<PowerRegulator>().moduleCompleted(this.GetType().Name);
    }
}