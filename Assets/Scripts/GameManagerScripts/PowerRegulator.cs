/*
    PowerRegulator.cs
    - Handles the six power sources (minigames)
    - Handles the power status screen and its six bars
    Contributor(s): Jake Schott
    Last Updated: 9/13/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerRegulator : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float DEPLETION_TIME = 20.0f; //how long it takes for a single control to go from enabled to disabled
    private static float NEUTRAL_TIME = 15.0f; //randomizes between this number and 5 seconds less
    private static float POWER_BAR_UPDATE_SPEED = 1.0f; //how fast the power bars update
    private static float DEPLETION_WARNING_FLASH_SPEED = 0.25f; //how often the warning light flashes

    public GameObject power_bars_display;
    public List<GameObject> power_regulation_modules = null;

    private List<UnityEngine.UI.Image> time_bars = new List<UnityEngine.UI.Image>();
    private List<UnityEngine.UI.RawImage> warning_dots = new List<UnityEngine.UI.RawImage>();

    private bool[] enabled_power_sources = new bool[6] { true, true, true, true, true, true };
    private IPowerRegulable[] power_regulation_components = new IPowerRegulable[6] { null, null, null, null, null, null };
    private List<string> power_regulation_component_names = new List<string>() { "PowerRegulationModuleA", "PowerRegulationModuleB", "PowerRegulationModuleC", "PowerRegulationModuleD", "PowerRegulationModuleE", "PowerRegulationModuleF" };
    private Coroutine[] power_source_depletion_coroutines = new Coroutine[6] { null, null, null, null, null, null };
    private Coroutine neutral_state_coroutine = null;
    private Coroutine depletion_warning_flasher_coroutine = null;
    private Coroutine power_bar_update_coroutine = null;

    private void Start()
    {
        restartPowerBarUpdater();
    
        for (int i = 0; i < 6; i++)
        {
            power_regulation_components[i] = (IPowerRegulable)GameObject.FindGameObjectWithTag("ControlHandler").GetComponent(power_regulation_component_names[i]);

            time_bars.Add(power_regulation_modules[i].transform.GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>());
            warning_dots.Add(power_regulation_modules[i].transform.GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.RawImage>());
        }
    }

    //resets enabled_power_sources to all true and begins the depletion process
    public void initializePowerRegulator()
    {
        for (int i = 0; i < 6; i++)
        {
            enabled_power_sources[i] = true;
        }

        if (NetworkManager.Singleton.IsHost == true)
        {
            if (neutral_state_coroutine != null)
            {
                StopCoroutine(neutral_state_coroutine);
            }
            neutral_state_coroutine = StartCoroutine(neutralState());
        }
    }

    //resets all non-depleted power sources (called by PowerManager)
    public void disableAllPowerSources()
    {
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

        if (neutral_state_coroutine != null)
        {
            StopCoroutine(neutral_state_coroutine);
            neutral_state_coroutine = null;
        }

        if (depletion_warning_flasher_coroutine == null)
        {
            depletion_warning_flasher_coroutine = StartCoroutine(depletionWarningFlasher());
        }
    }

    //called when a power regulation module "mini-game" has been completed
    public void moduleCompleted(string module_completed)
    {
        int module_index = power_regulation_component_names.IndexOf(module_completed);

        if (power_source_depletion_coroutines[module_index] != null)
        {
            StopCoroutine(power_source_depletion_coroutines[module_index]);
            power_source_depletion_coroutines[module_index] = null;
        }

        enabled_power_sources[module_index] = true;
        time_bars[module_index].fillAmount = 1.0f;
        time_bars[module_index].color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        warning_dots[module_index].color = new Color(0.0f, 0.84f, 1.0f, 1.0f);

        power_regulation_components[module_index].resetToDefault();

        if (NetworkManager.Singleton.IsHost == true)
        {
            if (transform.GetComponent<PowerManager>().getShipHasPower() == false && getPowerSourcesEnabled() >= 3)
            {
                transform.GetComponent<PowerManager>().restorePower();
            }
            if (getPowerSourcesEnabled() > 0 && transform.GetComponent<PowerManager>().getShipHasPower() == true)
            {
                if (neutral_state_coroutine != null)
                {
                    StopCoroutine(neutral_state_coroutine);
                }
                neutral_state_coroutine = StartCoroutine(neutralState());
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

    IEnumerator neutralState()
    {
        yield return new WaitForSeconds(Random.Range(NEUTRAL_TIME - 5.0f, NEUTRAL_TIME));
        initiateDepletionRPC(getRandomPowerSourceForDepletion());
    }

    IEnumerator depletionWarningFlasher()
    {
        while (true)
        {
            for (int i = 0; i < 2; i++)
            {
                float anim_time = DEPLETION_WARNING_FLASH_SPEED;
                while (anim_time > 0.0f)
                {
                    anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
                    Color warning_color = new Color(0.84f, 0.62f, 0.0f, 1.0f - (0.8f * (1.0f - (anim_time / DEPLETION_WARNING_FLASH_SPEED))));
                    if (i == 1)
                    {
                        warning_color = new Color(0.84f, 0.62f, 0.0f, 0.2f + (0.8f * (1.0f - (anim_time / DEPLETION_WARNING_FLASH_SPEED))));
                    }
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
        }
    }

    IEnumerator powerBarsUpdater()
    {
        while (true)
        {
            List<float> starting_fill_amounts = new List<float>();
            List<float> desired_fill_amounts = new List<float>();
            for (int i = 0; i < 6; i++)
            {
                starting_fill_amounts.Add(power_bars_display.transform.GetChild(i).GetComponent<UnityEngine.UI.Image>().fillAmount);
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
                    power_bars_display.transform.GetChild(i).GetComponent<UnityEngine.UI.Image>().fillAmount = Mathf.Lerp(starting_fill_amounts[i], desired_fill_amounts[i], 1.0f - (anim_time / POWER_BAR_UPDATE_SPEED));
                }
                yield return null;
            }
        }
    }

    IEnumerator powerDepletion(int source_index)
    {
        UnityEngine.UI.Image time_bar = power_regulation_modules[source_index].transform.GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetComponent<UnityEngine.UI.Image>();
        time_bar.color = new Color(0.84f, 0.62f, 0f);
        time_bar.fillAmount = 1.0f;

        float anim_time = DEPLETION_TIME;

        while (anim_time > 0.0f)
        {
            time_bar.fillAmount = (anim_time / DEPLETION_TIME);
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            yield return null;
        }

        //if time runs out, then send out disable RPC (if host)
        if (NetworkManager.Singleton.IsHost == true)
        {
            terminateDepletionRPC(source_index, false);
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
        if (to_deplete < 0 || to_deplete > 5)
        {
            return;
        }

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
        restartPowerBarUpdater();

        if (NetworkManager.Singleton.IsHost == true)
        {
            if (getPowerSourcesEnabled() == 0)
            {
                transform.GetComponent<PowerManager>().totalShutdown();
            }
            else
            {
                initiateDepletionRPC(getRandomPowerSourceForDepletion());
            }
        }
    }
}