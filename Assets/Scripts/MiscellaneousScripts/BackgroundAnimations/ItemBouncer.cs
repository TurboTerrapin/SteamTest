/*
    ItemBouncer.cs
    - Bounces items in items_to_bounce from starting position to negative starting position and back indefinitely
    Contributor(s): Jake Schott
    Last Updated: 11/3/2025
*/

using UnityEngine;
using System.Collections.Generic;

public class ItemBouncer: MonoBehaviour, IAnimable
{
    [SerializeField]
    private List<GameObject> items_to_bounce;
    [SerializeField]
    private float period = 2.0f; //seconds

    private float bounce_percentage = 0.0f;
    private float dir = 1.0f;

    private List<Vector3> starting_positions = new List<Vector3>();
    private List<Vector3> final_positions = new List<Vector3>();

    private void Start()
    {
        foreach (GameObject item in items_to_bounce)
        {
            starting_positions.Add(item.transform.localPosition);
            final_positions.Add(item.transform.localPosition * -1f);
        }    
    }

    public void animate(float dt)
    {
        bounce_percentage += (dt * dir) / period;
        if (bounce_percentage < 0.0f)
        {
            bounce_percentage *= -1f;
            dir *= -1f;
        }
        else if (bounce_percentage > 1.0f)
        {
            bounce_percentage -= 1.0f;
            bounce_percentage = 1.0f - bounce_percentage;
            dir *= -1f;
        }
        for (int i = 0; i < items_to_bounce.Count; i++)
        {
            items_to_bounce[i].transform.localPosition = Vector3.Lerp(starting_positions[i], final_positions[i], bounce_percentage);
        }
    }
}
