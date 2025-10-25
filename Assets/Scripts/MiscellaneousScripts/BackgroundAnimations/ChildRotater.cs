/*
    ChildRotater.cs
    - Rotates all the children of the component based on rotate_speed and rotate_clockwise indefinitely
    Contributor(s): Jake Schott
    Last Updated: 10/24/2025
*/


using UnityEngine;

public class ChildRotater : MonoBehaviour, IAnimable
{
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
        foreach (Transform r in transform)
        {
            r.Rotate(new Vector3(0.0f, 0.0f, rotate_speed * dt));
        }
    }
}
