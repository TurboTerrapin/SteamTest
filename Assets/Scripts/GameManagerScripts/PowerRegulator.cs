/*
    PowerRegulator.cs
    - Handles the six power sources (minigames)
    - Handles the power status screen and its six bars
    Contributor(s): Jake Schott
    Last Updated: 9/11/2025
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerRegulator : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float DEPLETION_TIME = 20.0f; //how long it takes for a single control to go from enabled to disabled
    private static float POWER_BAR_UPDATE_SPEED = 1.0f; //how fast the power bars update

    public GameObject power_bars_display;

    private bool[] enabled_power_sources = new bool[6] { true, true, true, true, true, true };
    private Component[] power_regulation_components = new Component[6] { null, null, null, null, null, null }; //private PowerRegulationControl[]
    private string[] power_regulation_component_names = new string[6] { "PowerRegulationA", "PowerRegulationB", "PowerRegulationC", "PowerRegulationD", "PowerRegulationE", "PowerRegulationF" };
    private Coroutine[] power_source_depletion_coroutines = new Coroutine[6] { null, null, null, null, null, null };
    private Coroutine power_bar_update_coroutine = null;

    private void Start()
    {
        restartPowerBarUpdater();
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
            initiateDepletionRPC(Random.Range(0, 6));
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
        float anim_time = DEPLETION_TIME;
        while (anim_time > 0.0f)
        {
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
        if (power_source_depletion_coroutines[to_deplete] != null)
        {
            StopCoroutine(power_source_depletion_coroutines[to_deplete]);
        }
        power_source_depletion_coroutines[to_deplete] = StartCoroutine(powerDepletion(to_deplete));
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
