/*
    PrefixCodeManager.cs
    - Used to update the prefix codes on all four positions after a certain amount of time
    Contributor(s): Jake Schott
    Last Updated: 4/24/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PrefixCodeManager : NetworkBehaviour, IPowerable
{
    //CLASS CONSTANTS
    private static int LOOP_TIME = 30;

    public List<GameObject> code_labels = null;
    public GameObject progress_bar;

    private bool[] is_powered = new bool[4] { false, true, true, true };
    private int[] prefix_codes = new int[] { 0, 0, 0, 0 };
    private Coroutine progress_bar_coroutine;

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
    }

    IEnumerator progressBarUpdater()
    {
        //reset bar
        progress_bar.GetComponent<UnityEngine.UI.Image>().fillAmount = 1.0f;

        //decrease bar
        float fill_time = LOOP_TIME;
        while (fill_time > 0.0f)
        {
            fill_time = Mathf.Max(0.0f, fill_time - Time.deltaTime);
            progress_bar.GetComponent<UnityEngine.UI.Image>().fillAmount = fill_time / LOOP_TIME;
            yield return null;
        }

        //if host, start the loop again
        if (NetworkManager.Singleton.IsHost)
        {
            generateNewCodes();
        }
    }

    public void initiatePrefixCodeManager()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            generateNewCodes();
        }
    }

    public void powerOn(int position)
    {
        is_powered[position] = true;
        code_labels[position].SetActive(true);
        if (position == 3)
        {
            progress_bar.SetActive(true);
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered[position] = false;
        code_labels[position].SetActive(false);
        if (position == 3)
        {
            progress_bar.SetActive(false);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitNewCodesRPC(int a, int b, int c, int d)
    {
        //set new codes
        prefix_codes[0] = a;
        prefix_codes[1] = b;
        prefix_codes[2] = c;
        prefix_codes[3] = d;
        displayCodes();

        //start bar loop
        if (progress_bar_coroutine != null)
        {
            StopCoroutine(progress_bar_coroutine);
        }
        progress_bar_coroutine = StartCoroutine(progressBarUpdater());

        //update self destruct code
        int[] destruct_code = new int[4];
        for (int i = 0; i < 2; i++)
        {
            string pos_code = prefix_codes[i].ToString();
            if (pos_code.Length < 2)
            {
                pos_code = "0" + pos_code;
            }
            char[] pos_code_as_chars = pos_code.ToCharArray();
            destruct_code[i * 2] = int.Parse(pos_code.Substring(0, 1));
            destruct_code[(i * 2) + 1] = int.Parse(pos_code.Substring(1, 1));
        }

        ReferenceAssistor.Instance.module_handlers[3].GetComponent<SelfDestruct>().setNewCode(DataConverter.arrayToString(destruct_code));
    }
}
