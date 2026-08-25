/*
    ItemFiller.cs
    - Fills UnityEngine.UI elements fillAmounts from random intervals over a period of time
    Contributor(s): Jake Schott
    Last Updated: 11/7/2025
*/

using UnityEngine;
using System.Collections.Generic;

public class ItemFiller : MonoBehaviour, IAnimable
{
    [SerializeField]
    private List<GameObject> items_to_fill;
    [SerializeField]
    private float period = 2.0f; //seconds

    private float fill_percentage = 0.0f;
    private float dir = 1.0f;

    private List<UnityEngine.UI.Image> images = new List<UnityEngine.UI.Image>();
    private List<float> starting_amounts = new List<float>();
    private List<float> final_amounts = new List<float>();

    private void generateNewFinalAmounts()
    {
        final_amounts.Clear();
        foreach (GameObject item in items_to_fill)
        {
            final_amounts.Add(Random.Range(0.5f, 1.0f));
        }
    }

    private void generateNewStartingAmounts()
    {
        starting_amounts.Clear();
        foreach (GameObject item in items_to_fill)
        {
            starting_amounts.Add(Random.Range(0.05f, 1.0f));
        }
    }

    private void Start()
    {
        foreach (GameObject image in items_to_fill)
        {
            images.Add(image.GetComponent<UnityEngine.UI.Image>());
        }
        generateNewStartingAmounts();
        generateNewFinalAmounts();
    }

    public void animate(float dt)
    {
        fill_percentage += (dt * dir) / period;
        if (fill_percentage < 0.0f)
        {
            generateNewFinalAmounts();
            fill_percentage *= -1f;
            dir *= -1f;
        }
        else if (fill_percentage > 1.0f)
        {
            generateNewStartingAmounts();
            fill_percentage -= 1.0f;
            fill_percentage = 1.0f - fill_percentage;
            dir *= -1f;
        }
        for (int i = 0; i < items_to_fill.Count; i++)
        {
            images[i].fillAmount = Mathf.Lerp(starting_amounts[i], final_amounts[i], fill_percentage);
        }
    }
}