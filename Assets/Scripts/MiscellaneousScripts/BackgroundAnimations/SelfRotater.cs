/*
    SelfRotater.cs
    - Rotates this object
    Contributor(s): Jake Schott
    Last Updated: 5/3/2026
*/

using UnityEngine;

public class SelfRotater : MonoBehaviour, IAnimable
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
        transform.Rotate(new Vector3(0.0f, 0.0f, rotate_speed * dt));
    }
}
