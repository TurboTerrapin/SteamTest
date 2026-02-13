/*
    ChildSlideshow.cs
    - Sets the active state of only one child at a time to create a slideshow effect indefinitely
    Contributor(s): Jake Schott
    Last Updated: 11/6/2025
*/

using UnityEngine;

public class ChildSlideshow : MonoBehaviour, IAnimable
{
    [SerializeField]
    private float period = 1.0f;
    [SerializeField]
    private bool transparency_mode = false;

    private float slideshow_percentage;

    private float[] slideshow_bottom_thresholds;
    private float[] slideshow_top_thresholds;

    private void Start()
    {
        slideshow_bottom_thresholds = new float[transform.childCount];
        slideshow_top_thresholds = new float[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            slideshow_bottom_thresholds[i] = (i / (transform.childCount * 1.0f));
            slideshow_top_thresholds[i] = ((i + 1) / (transform.childCount * 1.0f));
        }
    }

    private void transparencyAdjustment(Transform to_adjust, float a)
    {
        foreach (Transform t in to_adjust)
        {
            Color c = t.GetComponent<UnityEngine.UI.RawImage>().color;
            t.GetComponent<UnityEngine.UI.RawImage>().color = new Color(c.r, c.g, c.b, a);
        }
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
        for (int i = 0; i < transform.childCount; i++)
        {
            bool active = slideshow_percentage >= slideshow_bottom_thresholds[i] && slideshow_percentage <= slideshow_top_thresholds[i];
            if (transparency_mode == false)
            {
                transform.GetChild(i).gameObject.SetActive(active);
            }
            else
            {
                if (active == true)
                {
                    transparencyAdjustment(transform.GetChild(i), 1.0f);
                }
                else
                {
                    transparencyAdjustment(transform.GetChild(i), 0.2f);
                }
            }
        }
    }
}
