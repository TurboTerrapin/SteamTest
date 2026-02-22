/*
    DefaultButton.cs
    - Default button
    Contributor(s): Jake Schott
    Last Updated: 2/19/2026
*/

using System.Collections;
using TMPro;
using UnityEngine;

public class DefaultButton : ManualButton, IManualButton
{
    private static float FLASH_TIME = 2.0f;

    public GameObject selected_indicator;
    public Sprite selected;
    public Sprite unselected;

    private Color background_color;
    private Coroutine highlight_loop_coroutine = null;

    IEnumerator highlightLoop()
    {
        GameObject background = selected_indicator.transform.GetChild(0).gameObject;
        Color c = background_color;
        c.a = 0.0f;
        background.GetComponent<UnityEngine.UI.Image>().color = c;
        float elapsed_time = 0.0f;
        while (true)
        {
            elapsed_time += Mathf.Min(Time.deltaTime, 1.0f / 30.0f) * FLASH_TIME;
            float a = Mathf.Lerp(0.0f, 0.5f, Mathf.PingPong(elapsed_time, 1.0f));
            c.a = a;
            background.GetComponent<UnityEngine.UI.Image>().color = c;

            yield return null;
        }
    }

    private void alphaAdjustment(float a)
    {
        Transform[] to_adjust = new Transform[] { transform, selected_indicator.transform };
        for (int t = 0; t < to_adjust.Length; t++)
        {
            Color c = to_adjust[t].GetComponent<UnityEngine.UI.Image>().color;
            c.a = a;
            to_adjust[t].GetComponent<UnityEngine.UI.Image>().color = c;
            for (int i = 0; i < to_adjust[t].childCount; i++)
            {
                GameObject go = to_adjust[t].transform.GetChild(i).gameObject;
                if (go.GetComponent<UnityEngine.UI.Image>() != null)
                {
                    c = go.GetComponent<UnityEngine.UI.Image>().color;
                    c.a = a;
                    go.GetComponent<UnityEngine.UI.Image>().color = c;
                }
                else if (go.GetComponent<UnityEngine.UI.RawImage>() != null)
                {
                    c = go.GetComponent<UnityEngine.UI.RawImage>().color;
                    c.a = a;
                    go.GetComponent<UnityEngine.UI.RawImage>().color = c;
                }
                else if (go.GetComponent<TMP_Text>() != null)
                {
                    c = go.GetComponent<TMP_Text>().color;
                    c.a = a;
                    go.GetComponent<TMP_Text>().color = c;
                }
            }
        }
    }

    public void select()
    {
        //get color from the border image
        background_color = GetComponent<UnityEngine.UI.Image>().color;
        background_color = new Color(Mathf.Max(0.0f, background_color.r - 0.4f), Mathf.Max(0.0f, background_color.g - 0.4f), Mathf.Max(0.0f, background_color.b - 0.4f), 0.0f);

        alphaAdjustment(1.0f);
        selected_indicator.transform.GetComponent<UnityEngine.UI.Image>().sprite = selected;
        selected_indicator.transform.GetChild(1).GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        if (highlight_loop_coroutine != null)
        {
            StopCoroutine(highlight_loop_coroutine);
        }
        highlight_loop_coroutine = StartCoroutine(highlightLoop());
    }

    public void deselect()
    {
        alphaAdjustment(0.2f);
        selected_indicator.transform.GetComponent<UnityEngine.UI.Image>().sprite = unselected;
        if (highlight_loop_coroutine != null)
        {
            StopCoroutine(highlight_loop_coroutine);
        }
        selected_indicator.transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().color = background_color;
        selected_indicator.transform.GetChild(1).GetComponent<TMP_Text>().fontStyle = FontStyles.Normal;
    }
}
