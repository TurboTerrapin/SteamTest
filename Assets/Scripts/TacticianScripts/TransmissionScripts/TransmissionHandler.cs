/*
    TransmissionHandler.cs
    - Moves the waves
    - Switches waves
    - Updates frequency text
    Contributor(s): Jake Schott
    Last Updated: 7/28/2025
*/

using UnityEngine;
using System.Collections.Generic;
using TMPro;
public class TransmissionHandler : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float WAVE_SPEED = 0.05f;

    public GameObject frequency_text;
    public GameObject transmission_canvas;
    public List<GameObject> waves = null;

    private List<string> frequencies = new List<string>() { "120.5", "126.1", "129.4", "129.8", "134.3", "139.9" };
    private List<int> corresponding_waves = new List<int>() { 4, 4, 4, 4, 5, 4 };
    private int frequency_index = 0;
    private float shift = 0.0f;

    private void displayAdjustment()
    {
        frequency_text.GetComponent<TMP_Text>().SetText(frequencies[frequency_index] + "MH");
        for (int i = 0; i < waves.Count; i++)
        {
            waves[i].GetComponent<UnityEngine.UI.RawImage>().texture = transmission_canvas.transform.GetChild(corresponding_waves[frequency_index]).gameObject.GetComponent<UnityEngine.UI.RawImage>().mainTexture;
        }
    }

    public void updateFrequency(int freq)
    {
        if (freq > frequencies.Count - 1)
        {
            freq = 0;
        }
        else if (freq < 0)
        {
            freq = frequencies.Count - 1;
        }
        frequency_index = freq;
        displayAdjustment();
    }

    public int getCurrentFrequencyIndex()
    {
        return frequency_index;
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
        shift += dt * WAVE_SPEED;
        if (shift > 0.08f)
        {
            shift -= 0.08f;
        }
        for (int i = 0; i < waves.Count; i++)
        {
            waves[i].transform.localPosition = new Vector3(0f, -0.04f + (0.08f * i) - shift, 0f);
        }
    }
}
