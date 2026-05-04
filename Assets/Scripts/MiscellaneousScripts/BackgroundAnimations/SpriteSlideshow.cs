/*
    SpriteSlideshow.cs
    - Sets the active state of only one items_to_slideshow at a time to create a slideshow effect indefinitely
    Contributor(s): Jake Schott
    Last Updated: 5/3/2026
*/

using System.Collections.Generic;
using UnityEngine;

public class SpriteSlideshow : MonoBehaviour, IAnimable
{
    [SerializeField]
    private List<GameObject> items_to_slideshow;
    [SerializeField]
    private float period = 1.0f;
    [SerializeField]
    private bool transparency_mode = false;

    private float slideshow_percentage;

    private float[] slideshow_bottom_thresholds;
    private float[] slideshow_top_thresholds;

    private void Start()
    {
        slideshow_bottom_thresholds = new float[items_to_slideshow.Count];
        slideshow_top_thresholds = new float[items_to_slideshow.Count];

        for (int i = 0; i < items_to_slideshow.Count; i++)
        {
            slideshow_bottom_thresholds[i] = (i / (items_to_slideshow.Count * 1.0f));
            slideshow_top_thresholds[i] = ((i + 1) / (items_to_slideshow.Count * 1.0f));
        }
    }

    private void transparencyAdjustment(Transform to_adjust, float a)
    {
        Color c = to_adjust.GetComponent<SpriteRenderer>().color;
        c.a = a;
        to_adjust.GetComponent<SpriteRenderer>().color = c;
    }

    public void animate(float dt)
    {
        slideshow_percentage += (dt / period);
        if (slideshow_percentage < 0.0f)
        {
            slideshow_percentage *= -1f;
            slideshow_percentage = 1.0f - slideshow_percentage;
        }
        else if (slideshow_percentage > 1.0f)
        {
            slideshow_percentage -= 1.0f;
        }
        for (int i = 0; i < items_to_slideshow.Count; i++)
        {
            bool active = slideshow_percentage >= slideshow_bottom_thresholds[i] && slideshow_percentage <= slideshow_top_thresholds[i];
            if (transparency_mode == false)
            {
                items_to_slideshow[i].SetActive(active);
            }
            else
            {
                if (active == true)
                {
                    transparencyAdjustment(items_to_slideshow[i].transform, 1.0f);
                }
                else
                {
                    transparencyAdjustment(items_to_slideshow[i].transform, 0.2f);
                }
            }
        }
    }
}