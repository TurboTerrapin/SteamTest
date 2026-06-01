using UnityEngine;

public class BlackHole : MonoBehaviour
{
    public Transform accretion_disk;
    public float spin_speed = 40.0f; // Degrees per second

    private float accumulated_spin = 0.0f; //total rotation angle

    private void Update()
    {
        if (Camera.main != null && accretion_disk != null)
        {
            accretion_disk.LookAt(Camera.main.transform);
            accumulated_spin = (accumulated_spin - (spin_speed * Time.deltaTime)) % 360.0f;
            accretion_disk.Rotate(0.0f, 0.0f, accumulated_spin, Space.Self);
        }
    }
}