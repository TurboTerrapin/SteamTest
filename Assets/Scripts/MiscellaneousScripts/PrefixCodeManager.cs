/*
    PrefixCodeManager.cs
    - Used to update the prefix codes on all four positions after a certain amount of time
    - Runs on host client
    Contributor(s): Jake Schott
    Last Updated: 5/27/2025
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PrefixCodeManager : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static int LOOP_TIME = 30;

    public List<GameObject> code_labels = null;
    public List<GameObject> progress_bars = null;

    private int[] prefix_codes = new int[] { 0, 0, 0, 0 };
    private Coroutine progress_bars_coroutine;

    private void displayCodes()
    {
        for (int i = 0; i < 4; i++)
        {
            if (code_labels[i] != null)
            {
                string to_display = prefix_codes[i].ToString();
                if (to_display.Length < 2)
                {
                    to_display = "0" + to_display;
                }
                code_labels[i].GetComponent<TMP_Text>().SetText(to_display);
            }
        }
    }

    private void generateNewCodes()
    {
        for (int i = 0; i < 4; i++)
        {
            prefix_codes[i] = Random.Range(0, 100);
        }
        transmitNewCodesRPC(prefix_codes[0], prefix_codes[1], prefix_codes[2], prefix_codes[3]);
        transmitNewLoopRPC();
    }


    IEnumerator progressBarsUpdater()
    {
        //reset all bars
        for (int i = 0; i < 4; i++)
        {
            progress_bars[i].GetComponent<UnityEngine.UI.Image>().fillAmount = 1.0f;
        }

        //loop through each bar
        for (int i = 0; i < 4; i++)
        {
            float fill_time = LOOP_TIME * 0.25f;
            while (fill_time > 0.0f)
            {
                float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
                fill_time = Mathf.Max(0.0f, fill_time - dt);
                progress_bars[i].GetComponent<UnityEngine.UI.Image>().fillAmount = fill_time / (LOOP_TIME * 0.25f);
                yield return null;
            }
        }

        //if host, start the loop again
        if (NetworkManager.Singleton.IsHost)
        {
            generateNewCodes();
        }
    }

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            generateNewCodes();
            transmitNewLoopRPC();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitNewCodesRPC(int a, int b, int c, int d)
    {
        prefix_codes[0] = a;
        prefix_codes[1] = b;
        prefix_codes[2] = c;
        prefix_codes[3] = d;
        displayCodes();
    }

    [Rpc(SendTo.Everyone)]
    private void transmitNewLoopRPC()
    {
        if (progress_bars_coroutine != null)
        {
            StopCoroutine(progress_bars_coroutine);
        }
        progress_bars_coroutine = StartCoroutine(progressBarsUpdater());
    }
}
