/*
    ItemRotater.cs
    - Rotates items in items_to_rotate indefinitely
    Contributor(s): Jake Schott
    Last Updated: 11/6/2025
*/

using UnityEngine;
using System.Collections.Generic;

public class ItemRotater : MonoBehaviour, IAnimable
{
    [SerializeField]
    private List<GameObject> items_to_rotate;
    [SerializeField]
    private bool rotate_clockwise = true;
    [SerializeField]
    private float rotate_speed = 50.0f;

    public void animate(float dt)
    {
        float rotation_effect = rotate_speed * dt;
        if (rotate_clockwise == false)
        {
            rotation_effect *= -1;
        }
        foreach (GameObject r in items_to_rotate)
        {
            r.transform.Rotate(new Vector3(0.0f, 0.0f, rotate_speed * dt));
        }
    }
}