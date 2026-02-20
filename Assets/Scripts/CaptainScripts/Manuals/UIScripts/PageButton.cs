/*
    PageButton.cs
    - Page button for next/back
    Contributor(s): Jake Schott
    Last Updated: 2/19/2026
*/

using System.Collections;
using TMPro;
using UnityEngine;

public class PageButton : ManualButton, IManualButton
{
    private static float FLASH_TIME = 2.0f;

    private Color background_color;
    private Coroutine highlight_loop_coroutine = null;

    IEnumerator highlightLoop()
    {
        GameObject background = transform.GetChild(0).gameObject;
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
        Color c = GetComponent<UnityEngine.UI.Image>().color;
        c.a = a;
        GetComponent<UnityEngine.UI.Image>().color = c;
        transform.GetChild(1).GetComponent<TMP_Text>().color = c;
    }

    public void select()
    {
        //get color from the border image
        background_color = GetComponent<UnityEngine.UI.Image>().color;
        background_color = new Color(Mathf.Max(0.0f, background_color.r - 0.4f), Mathf.Max(0.0f, background_color.g - 0.4f), Mathf.Max(0.0f, background_color.b - 0.4f), 0.0f);

        alphaAdjustment(1.0f);
        transform.GetChild(1).GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        if (highlight_loop_coroutine != null)
        {
            StopCoroutine(highlight_loop_coroutine);
        }
        highlight_loop_coroutine = StartCoroutine(highlightLoop());
    }

    public void deselect()
    {
        alphaAdjustment(0.2f);
        if (highlight_loop_coroutine != null)
        {
            StopCoroutine(highlight_loop_coroutine);
        }
        transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().color = background_color;
        transform.GetChild(1).GetComponent<TMP_Text>().fontStyle = FontStyles.Normal;
    }
}
