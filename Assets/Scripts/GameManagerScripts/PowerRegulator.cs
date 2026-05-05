/*
    PowerRegulator.cs
    - Handles the six power sources (minigames)
    - Handles the power status screen and its six bars
    Contributor(s): Jake Schott
    Last Updated: 2/23/2026
*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PowerRegulator : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float[] DEPLETION_TIME = new float[] { 40.0f, 35.0f, 28.0f, 20.0f }; //how long it takes for a single control to go from enabled to disabled
    private static float[] NEUTRAL_TIME = new float[] { 30.0f, 25.0f, 20.0f, 10.0f }; //randomizes between this number and 5 seconds less
    private static float POWER_BAR_UPDATE_SPEED = 1.0f; //how fast the power bars update
    private static float DEPLETION_WARNING_FLASH_SPEED = 2.0f; //how fast the warning light flashes
    private static Color[] POWER_STATUS_COLORS = new Color[3]{ new Color(0.0f, 0.84f, 1.0f), new Color(1.0f, 0.47f, 0.0f), new Color(1.0f, 0.0f, 0.0f) };
    private static string[] POWER_STATUS_MESSAGES = new string[3]{ "NOMINAL", "CRITICAL", "OFFLINE" };

    public GameObject power_status;
    public List<GameObject> power_regulation_modules = null;

    //power status screen UI components
    private GameObject power_bars;
    private GameObject power_restoration_message;
    private TMP_Text power_status_label;
    private TMP_Text power_status_message;

    //power regulation module UI components
    private List<UnityEngine.UI.Image> time_bars = new List<UnityEngine.UI.Image>();
    private List<UnityEngine.UI.RawImage> warning_dots = new List<UnityEngine.UI.RawImage>();

    //auxiliary power
    private AuxiliaryPower auxiliary_power;

    private bool[] enabled_power_sources = new bool[6] { true, true, true, true, true, true };
    private IPowerRegulable[] power_regulation_components = new IPowerRegulable[6] { null, null, null, null, null, null };
    private List<string> power_regulation_component_names = new List<string>() { "PowerRegulationModuleA", "PowerRegulationModuleB", "PowerRegulationModuleC", "PowerRegulationModuleD", "PowerRegulationModuleE", "PowerRegulationModuleF" };
    private Coroutine[] power_source_depletion_coroutines = new Coroutine[6] { null, null, null, null, null, null };
    private Coroutine neutral_state_coroutine = null;
    private Coroutine depletion_warning_flasher_coroutine = null;
    private Coroutine power_bar_update_coroutine = null;

    private void Start()
    {
        power_status_label = power_status.transform.GetChild(0).GetComponent<TMP_Text>();
        power_status_message = power_status.transform.GetChild(1).GetComponent<TMP_Text>();
        power_restoration_message = power_status.transform.GetChild(2).gameObject;
        power_bars = power_status.transform.GetChild(3).gameObject;

        auxiliary_power = ReferenceAssistor.Instance.module_handlers[2].GetComponent<AuxiliaryPower>();

        restartPowerBarUpdater();
    
        for (int i = 0; i < 6; i++)
        {
            power_regulation_components[i] = (IPowerRegulable)(ReferenceAssistor.Instance.module_handlers[2].GetComponent(power_regulation_component_names[i]));

            time_bars.Add(power_regulation_modules[i].transform.GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>());
            warning_dots.Add(power_regulation_modules[i].transform.GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.RawImage>());
        }
    }

    //resets enabled_power_sources to all true and begins the depletion process
    public void initializePowerRegulator()
    {
        //enable all power sources, reset all modules to default (unplayable) state
        for (int i = 0; i < 6; i++)
        {
            enabled_power_sources[i] = true;
            power_regulation_components[i].resetToDefault();
        }

        //if host, begin "neutral state"
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (neutral_state_coroutine != null)
            {
                StopCoroutine(neutral_state_coroutine);
            }
            neutral_state_coroutine = StartCoroutine(neutralState());
        }
    }

    //resets all modules and freezes depletion (called by PowerManager on scenario completion) or by useAuxiliaryPower()
    public void resetPowerRegulator()
    {
        //stop neutral state depletion delay
        if (neutral_state_coroutine != null)
        {
            StopCoroutine(neutral_state_coroutine);
            neutral_state_coroutine = null;
        }

        //stop warning flasher
        if (depletion_warning_flasher_coroutine != null)
        {
            StopCoroutine(depletion_warning_flasher_coroutine);
            depletion_warning_flasher_coroutine = null;
        }

        //reset each module
        for (int i = 0; i < 6; i++)
        {
            //stop depletion coroutines
            if (power_source_depletion_coroutines[i] != null)
            {
                StopCoroutine(power_source_depletion_coroutines[i]);
                power_source_depletion_coroutines[i] = null;
            }

            //adjust blue/orange UI
            time_bars[i].fillAmount = 1.0f;
            time_bars[i].color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
            warning_dots[i].color = new Color(0.0f, 0.84f, 1.0f, 1.0f);

            enabled_power_sources[i] = true;

            power_regulation_components[i].resetToDefault();
        }

        //update power status screen
        updatePowerStatus(getPowerStatusState());
    }

    //resets all non-depleted power sources (called by PowerManager)
    public void disableAllPowerSources()
    {
        //disable power sources, stop depletion timers, and reset bars to zero progress
        for (int i = 0; i < 6; i++)
        {
            if (power_source_depletion_coroutines[i] != null)
            {
                StopCoroutine(power_source_depletion_coroutines[i]);
                power_source_depletion_coroutines[i] = null;
            }
            else
            {
                power_regulation_components[i].unlockControl();
            }
            time_bars[i].fillAmount = 0.0f;
            enabled_power_sources[i] = false;
        }

        //reset neutral state
        if (neutral_state_coroutine != null)
        {
            StopCoroutine(neutral_state_coroutine);
            neutral_state_coroutine = null;
        }

        //begin orange flashing (if not already started)
        if (depletion_warning_flasher_coroutine == null)
        {
            depletion_warning_flasher_coroutine = StartCoroutine(depletionWarningFlasher());
        }

        //change state to offline
        restartPowerBarUpdater();
        updatePowerStatus(getPowerStatusState());

        auxiliary_power.activate(transform.GetComponent<PowerManager>().getShipHasPower());
    }

    //enables all depleted power sources (and stops depletion on all depleting sources), restores power
    public void useAuxiliaryPower()
    {
        resetPowerRegulator();
        restartPowerBarUpdater();

        if (NetworkManager.Singleton.IsHost == true)
        {
            neutral_state_coroutine = StartCoroutine(neutralState());
            if (transform.GetComponent<PowerManager>().getShipHasPower() == false)
            {
                transform.GetComponent<PowerManager>().restorePower();
            }
        }
    }

    //display power restoration
    public void displayPowerRestoration()
    {
        //change state to online
        restartPowerBarUpdater();
        updatePowerStatus(0);
        auxiliary_power.activate(true);
    }

    //called when a power regulation module "minigame" has been completed
    public void moduleCompleted(string module_completed)
    {
        //get the corresponding minigame
        int module_index = power_regulation_component_names.IndexOf(module_completed);

        if (power_source_depletion_coroutines[module_index] != null)
        {
            StopCoroutine(power_source_depletion_coroutines[module_index]);
            power_source_depletion_coroutines[module_index] = null;
        }

        //enable and reset flasher/progress bar
        enabled_power_sources[module_index] = true;
        time_bars[module_index].fillAmount = 1.0f;
        time_bars[module_index].color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        warning_dots[module_index].color = new Color(0.0f, 0.84f, 1.0f, 1.0f);

        power_regulation_components[module_index].resetToDefault();

        restartPowerBarUpdater();
        updatePowerStatus(getPowerStatusState());

        if (getPowerSourcesEnabled() < 6)
        {
            auxiliary_power.activate(transform.GetComponent<PowerManager>().getShipHasPower());
        }
        else
        {
            auxiliary_power.deactivate();
        }

        //check if power is off and at least three modules have been powered (if so, restore power)
        if (NetworkManager.Singleton.IsHost == true)
        {
            //restore power if three sources enabled and power is disabled
            if (transform.GetComponent<PowerManager>().getShipHasPower() == false && getPowerSourcesEnabled() >= 3)
            {
                transform.GetComponent<PowerManager>().restorePower();
                if (neutral_state_coroutine != null)
                {
                    StopCoroutine(neutral_state_coroutine);
                }
                neutral_state_coroutine = StartCoroutine(neutralState());
            }

            //if power enabled, check to see what to do next
            if (getPowerSourcesEnabled() > 0 && transform.GetComponent<PowerManager>().getShipHasPower() == true)
            {
                //stop neutral state no matter what
                if (neutral_state_coroutine != null)
                {
                    StopCoroutine(neutral_state_coroutine);
                }

                //if no sources depleting, then trigger neutral state
                if (getPowerSourcesDepleting() == 0)
                {
                    neutral_state_coroutine = StartCoroutine(neutralState());
                }
            }
        }
    }

    //returns a random enabled_power_sources index that is equal to false
    public int getRandomPowerSourceForDepletion()
    {
        int to_deplete = -1;

        List<int> possible_depletion_indexes = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            if (enabled_power_sources[i] == true)
            {
                possible_depletion_indexes.Add(i);
            }
        }

        if (possible_depletion_indexes.Count > 0)
        {
            return possible_depletion_indexes[Random.Range(0, possible_depletion_indexes.Count)];
        }

        return to_deplete;
    }

    //returns how many power sources are enabled
    public int getPowerSourcesEnabled()
    {
        int sources_enabled = 0;
        for (int i = 0; i < 6; i++)
        {
            if (enabled_power_sources[i] == true)
            {
                sources_enabled += 1;
            }
        }
        return sources_enabled;
    }

    //returns how many power sources are currently depleting
    public int getPowerSourcesDepleting()
    {
        int sources_depleting = 0;
        for (int i = 0; i < 6; i++)
        {
            if (power_source_depletion_coroutines[i] != null)
            {
                sources_depleting += 1;
            }
        }
        return sources_depleting;
    }

    //waits a random amount of time (NEUTRAL_TIME - 5 seconds to NEUTRAL_TIME) to deplete a random power source
    IEnumerator neutralState()
    {
        float neutral_time = NEUTRAL_TIME[GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().getDifficulty()];
        yield return new WaitForSeconds(Random.Range(neutral_time - 5.0f, neutral_time));

        //begin depleting a new power source
        if (getPowerSourcesEnabled() > 0)
        {
            initiateDepletionRPC(getRandomPowerSourceForDepletion());
        }
        else //if all depleted, stop
        {
            neutral_state_coroutine = null;
        }
    }

    //flashes the orange lights for all power sources that are depleted/depleting
    IEnumerator depletionWarningFlasher()
    {
        float elapsed_time = 0.0f;
        while (true)
        {
            elapsed_time += Time.deltaTime * DEPLETION_WARNING_FLASH_SPEED;
            float a = Mathf.Lerp(0.2f, 1.0f, Mathf.PingPong(elapsed_time, 1.0f));
            Color warning_color = new Color(1.0f, 0.47f, 0.0f, a);
            for (int x = 0; x < 6; x++)
            {
                if (enabled_power_sources[x] == false || power_source_depletion_coroutines[x] != null)
                {
                    warning_dots[x].color = warning_color;
                }
            }

            yield return null;
        }
    }

    //updates the power bars for all power sources
    IEnumerator powerBarsUpdater()
    {
        while (true)
        {
            List<float> starting_fill_amounts = new List<float>();
            List<float> desired_fill_amounts = new List<float>();
            for (int i = 0; i < 6; i++)
            {
                starting_fill_amounts.Add(power_bars.transform.GetChild(i).GetComponent<UnityEngine.UI.Image>().fillAmount);
                float desired_fill_amount = Random.Range(0.9f, 1.0f);

                if (enabled_power_sources[i] == false)
                {
                    desired_fill_amount = Random.Range(0.01f, 0.05f);
                }
                desired_fill_amounts.Add(desired_fill_amount);
            }

            float anim_time = POWER_BAR_UPDATE_SPEED;
            while (anim_time > 0.0f)
            {
                anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

                for (int i = 0; i < 6; i++)
                {
                    power_bars.transform.GetChild(i).GetComponent<UnityEngine.UI.Image>().fillAmount = Mathf.Lerp(starting_fill_amounts[i], desired_fill_amounts[i], 1.0f - (anim_time / POWER_BAR_UPDATE_SPEED));
                }
                yield return null;
            }
        }
    }

    //reduces the time bar and handles what happens at expiration
    IEnumerator powerDepletion(int source_index)
    {
        UnityEngine.UI.Image time_bar = power_regulation_modules[source_index].transform.GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>();
        time_bar.color = new Color(1.0f, 0.47f, 0.0f);
        time_bar.fillAmount = 1.0f;

        float depletion_time = DEPLETION_TIME[GameObject.FindGameObjectWithTag("ScenarioManager").GetComponent<ScenarioManager>().getDifficulty()];
        float anim_time = depletion_time;

        while (anim_time > 0.0f)
        {
            time_bar.fillAmount = (anim_time / depletion_time);
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            yield return null;
        }

        //if time runs out, then send out disable RPC (if host)
        if (NetworkManager.Singleton.IsHost == true)
        {
            terminateDepletionRPC(source_index, false);
        }
    }

    private int getPowerStatusState()
    {
        //determine power status state
        int state = 0;
        if (transform.GetComponent<PowerManager>().getShipHasPower() == false)
        {
            state = 2;
        }
        else
        {
            if (getPowerSourcesEnabled() < 4)
            {
                state = 1;
            }
            else
            {
                state = 0;
            }
        }
        return state;
    }

    //purely visual update to the power status screen in the engineer position
    private void updatePowerStatus(int state)
    {
        //change color of divider bar, POWER STATUS label
        if (state == 2)
        {
            power_status_label.color = POWER_STATUS_COLORS[2];
        }
        else
        {
            power_status_label.color = POWER_STATUS_COLORS[0];
        }

        //update status text
        power_status_message.color = POWER_STATUS_COLORS[state];
        power_status_message.text = "STATUS: " + POWER_STATUS_MESSAGES[state];

        //update power bars color
        for (int i = 0; i < 6; i++)
        {
            power_bars.transform.GetChild(i).GetComponent<UnityEngine.UI.Image>().color = POWER_STATUS_COLORS[state];
            power_bars.transform.GetChild(i).GetChild(0).GetComponent<TMP_Text>().color = POWER_STATUS_COLORS[state];
        }

        //show/hide power restoration message
        power_restoration_message.SetActive(state == 2);
        if (state == 2)
        {
            power_restoration_message.GetComponent<TMP_Text>().text = "RESTORATION PROGRESS: " + Mathf.Min(3, getPowerSourcesEnabled()) + "/3";
        }
    }

    //called by initiateDepletionRPC() and terminateDepletionRPC()
    private void restartPowerBarUpdater()
    {
        if (power_bar_update_coroutine != null)
        {
            StopCoroutine(power_bar_update_coroutine);
        }
        power_bar_update_coroutine = StartCoroutine(powerBarsUpdater());
    }

    [Rpc(SendTo.Everyone)]
    private void initiateDepletionRPC(int to_deplete)
    {
        auxiliary_power.activate(transform.GetComponent<PowerManager>().getShipHasPower());

        if (power_source_depletion_coroutines[to_deplete] != null)
        {
            StopCoroutine(power_source_depletion_coroutines[to_deplete]);
        }
        else
        {
            power_regulation_components[to_deplete].unlockControl();
        }
        power_source_depletion_coroutines[to_deplete] = StartCoroutine(powerDepletion(to_deplete));

        if (depletion_warning_flasher_coroutine == null)
        {
            depletion_warning_flasher_coroutine = StartCoroutine(depletionWarningFlasher());
        }
    }

    [Rpc(SendTo.Everyone)]
    private void terminateDepletionRPC(int to_terminate, bool enabled)
    {
        if (power_source_depletion_coroutines[to_terminate] != null)
        {
            StopCoroutine(power_source_depletion_coroutines[to_terminate]);
            power_source_depletion_coroutines[to_terminate] = null;
        }

        enabled_power_sources[to_terminate] = enabled;

        if (getPowerSourcesEnabled() < 6)
        {
            auxiliary_power.activate(transform.GetComponent<PowerManager>().getShipHasPower());
        }
        else
        {
            auxiliary_power.deactivate();
        }

        updatePowerStatus(getPowerStatusState());
        restartPowerBarUpdater();

        if (NetworkManager.Singleton.IsHost == true)
        {
            //if no sources left
            if (getPowerSourcesEnabled() == 0)
            {
                if (transform.GetComponent<PowerManager>().getShipHasPower() == true)
                {
                    transform.GetComponent<PowerManager>().totalShutdown();
                }
            }
            else
            {
                initiateDepletionRPC(getRandomPowerSourceForDepletion());
            }
        }
    }
}