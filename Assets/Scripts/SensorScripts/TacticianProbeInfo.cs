/*
    TacticianProbe.cs
    - Handles probe range and health screens
    Contributor(s): Jake Schott
    Last Updated: 7/25/2025
*/

using UnityEngine;

public class TacticianProbeInfo : MonoBehaviour
{
    public GameObject probe_health_canvas;
    public GameObject probe_range_canvas;
    public GameObject probe_feed_canvas;

    public void displayRange(float dist)
    {
        float tmp_dist = dist;
        for (int i = 0; i <= 10; i++)
        {
            tmp_dist = dist - (0.091f * i);
            float a = Mathf.Max(0.196f, tmp_dist / 0.091f);
            probe_range_canvas.transform.GetChild(1 + i).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
        }
    }

    public void connectProbe()
    {
        //update probe icon
        probe_range_canvas.transform.GetChild(12).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        //show probe feed
        probe_feed_canvas.transform.GetChild(1).gameObject.SetActive(true);
        probe_feed_canvas.transform.GetChild(2).gameObject.SetActive(true);
        probe_feed_canvas.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        probe_feed_canvas.transform.GetChild(5).gameObject.SetActive(false);
        //show health bar
        probe_health_canvas.transform.GetChild(1).gameObject.SetActive(true);
        probe_health_canvas.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
    }

    public void disconnectProbe()
    {
        //update probe icon
        probe_range_canvas.transform.GetChild(12).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.196f);
        //hide probe feed
        probe_feed_canvas.transform.GetChild(1).gameObject.SetActive(false);
        probe_feed_canvas.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        probe_feed_canvas.transform.GetChild(5).gameObject.SetActive(true);
        //hide health bar
        probe_health_canvas.transform.GetChild(1).gameObject.SetActive(false);
        probe_health_canvas.transform.GetChild(2).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 0.196f);
    }

    public void displayHealth(float health)
    {
        Color probe_color = ShipHealth.getDesiredColor(health);
        probe_health_canvas.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().color = probe_color;
        probe_health_canvas.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount = health / 100.0f;
    }
}
