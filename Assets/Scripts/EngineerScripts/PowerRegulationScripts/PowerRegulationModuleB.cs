/*
    PowerRegulationModuleB.cs
    - Handles the lever pushing mini-game in the engineer position
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PowerRegulationModuleB : NetworkBehaviour, IControllable, IPowerRegulable
{
    //CLASS CONSTANTS
    private static float STATE_CHANGE_TIME = 0.5f;
    private static Vector3 SLIDER_PUSH_DIRECTION = new Vector3(0.027f, 0.027f, -0.027f);

    private string[] CONTROL_NAMES = new string[3] { "PRIMARY ANTI-MATTER INDUCER", "SECONDARY ANTI-MATTER INDUCER", "TERTIARY ANTI-MATTER INDUCER" };
    private static string INFO_MESSAGE = "Prime each slider until it is at its max setting to complete the module.";
    private List<string> CONTROL_DESCS = new List<string> { "POWER" };
    private List<int> CONTROL_INDEXES = new List<int>() { 11 };
    private List<Button>[] BUTTON_LISTS = new List<Button>[3] { new List<Button>(), new List<Button>(), new List<Button>() };

    public GameObject prsb_display;
    public List<GameObject> prsb_sliders = null;

    private bool currently_active = false;

    private Vector3[] initial_positions = new Vector3[3];
    private float[] slide_percentages = new float[3] { 1.0f, 1.0f, 1.0f };
    private Coroutine[] slide_increase_coroutines = new Coroutine[3] { null, null, null };
    private Coroutine state_change_coroutine = null;

    private List<string> ray_targets = new List<string> { "prsb_slider_a", "prsb_slider_b", "prsb_slider_c" };

    private static HUDInfo hud_info = null;

    private void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            initial_positions[i] = prsb_sliders[i].transform.localPosition;
            prsb_sliders[i].transform.localPosition = prsb_sliders[i].transform.localPosition + SLIDER_PUSH_DIRECTION;
            BUTTON_LISTS[i].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        }

        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        hud_info.setButtons(BUTTON_LISTS[0]);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index], 6);

        return hud_info;
    }

    private void displayAdjustment()
    {
        for (int i = 0; i < 3; i++)
        {
            //adjust slider
            prsb_sliders[i].transform.localPosition = initial_positions[i] + (SLIDER_PUSH_DIRECTION * slide_percentages[i]);
            
            //adjust bar
            prsb_display.transform.GetChild(i).GetComponent<UnityEngine.UI.Image>().fillAmount = Mathf.Max(0.02f, slide_percentages[i]);

            //adjust completion circle
            Color circle_color = new Color(0.0f, 0.84f, 1.0f, 0.2f);
            if (slide_percentages[i] >= 1.0f)
            {
                circle_color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
            }
            prsb_display.transform.GetChild(i).transform.GetChild(0).GetChild(0).gameObject.SetActive(slide_percentages[i] < 1.0f);
            prsb_display.transform.GetChild(i).transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = circle_color;
        }
    }

    //sets the state 
    IEnumerator stateChangeHelper(bool to_change_to)
    {
        float anim_time = STATE_CHANGE_TIME;
        float[] starting_percentage = new float[] { slide_percentages[0], slide_percentages[1], slide_percentages[2] };
        float destination_percentage = 1.0f;

        if (to_change_to == true)
        {
            destination_percentage = 0.0f;
        }

        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            for (int i = 0; i < 3; i++)
            {
                //adjust slider
                float move_percentage = Mathf.Lerp(destination_percentage, starting_percentage[i], (anim_time / STATE_CHANGE_TIME));
                slide_percentages[i] = move_percentage;
                prsb_sliders[i].transform.localPosition = initial_positions[i] + (SLIDER_PUSH_DIRECTION * move_percentage);
            }

            yield return null;
        }

        displayAdjustment();
        prsb_display.SetActive(to_change_to);

        for (int i = 0; i < 3; i++)
        {
            BUTTON_LISTS[i][0].updateInteractable(to_change_to);
        }
        currently_active = to_change_to;

        state_change_coroutine = null;
    }

    //used to create a slight delay between spam clicks, also brings back slider when idle
    IEnumerator slideIncrease(int index)
    {
        if (slide_percentages[index] < 1.0f)
        {
            yield return new WaitForSeconds(0.1f);
            BUTTON_LISTS[index][0].untoggle();
            BUTTON_LISTS[index][0].updateInteractable(true);
            while (slide_percentages[index] > 0.0f)
            {
                yield return new WaitForSeconds(0.15f);
                if (NetworkManager.Singleton.IsHost == true)
                {
                    transmitSlideAdjustmentRPC(index, Mathf.Max(0.0f, slide_percentages[index] - 0.03f));
                }
            }
        }
        else
        {
            BUTTON_LISTS[index][0].untoggle();
        }
        slide_increase_coroutines[index] = null;
    }

    private void resetStateChangeCoroutine()
    {
        if (state_change_coroutine != null)
        {
            StopCoroutine(state_change_coroutine);
            state_change_coroutine = null;
        }
    }

    //if not locked already, sets to completed control state and disables interaction
    public void resetToDefault()
    {
        if (currently_active == false)
        {
            return;
        }
        resetStateChangeCoroutine();
        currently_active = false;
        prsb_display.SetActive(false);
        state_change_coroutine = StartCoroutine(stateChangeHelper(false));
    }

    //if not unlocked already, sets to default control state and allows for interaction
    public void unlockControl()
    {
        if (currently_active == true)
        {
            return;
        }
        resetStateChangeCoroutine();
        state_change_coroutine = StartCoroutine(stateChangeHelper(true));
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (currently_active == false)
        {
            return;
        }

        int target_index = ray_targets.IndexOf(current_target.name);
        if (slide_percentages[target_index] < 1.0f)
        {
            if (PrimaryScript.checkInputIndex(CONTROL_INDEXES[0], inputs) && BUTTON_LISTS[target_index][0].getInteractable() == true) //push slider up
            {
                BUTTON_LISTS[target_index][0].toggle();
                BUTTON_LISTS[target_index][0].updateInteractable(false);
                transmitSlideAdjustmentRPC(target_index, Mathf.Min(1.0f, slide_percentages[target_index] + 0.1f));
            }
        }
    }

    //updates slide percentage for the given index
    [Rpc(SendTo.Everyone)]
    private void transmitSlideAdjustmentRPC(int index, float new_percentage)
    {
        float old_percentage = slide_percentages[index];
        slide_percentages[index] = new_percentage;
        displayAdjustment();
        
        if (new_percentage < old_percentage) //means an automatic decrease
        {
            return;
        }
        
        //stop current debounce/automatic reduction
        if (slide_increase_coroutines[index] != null)
        {
            StopCoroutine(slide_increase_coroutines[index]);
        }

        slide_increase_coroutines[index] = StartCoroutine(slideIncrease(index));
        
        //check if all three are at full (if host)
        if (NetworkManager.Singleton.IsHost == true)
        {
            bool completed = true;
            for (int i = 0; i < 3; i++)
            {
                if (slide_percentages[i] < 1.0f)
                {
                    completed = false;
                    break;
                }
            }
            
            if (completed == true)
            {
                transmitModuleCompletionRPC();
            }
        }
    }

    //called by host when all three sliders are at full
    [Rpc(SendTo.Everyone)]
    private void transmitModuleCompletionRPC()
    {
        for (int i = 0; i < 3; i++)
        {
            if (slide_increase_coroutines[i] != null)
            {
                StopCoroutine(slide_increase_coroutines[i]);
                slide_increase_coroutines[i] = null;
            }
        }

        GameObject.Find("PowerHandler").GetComponent<PowerRegulator>().moduleCompleted(this.GetType().Name);
    }
}