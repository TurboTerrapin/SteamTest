
using UnityEngine;

public class BlackHole : MonoBehaviour
{
    public Transform accretion_disk_vertical;
    public Transform accretion_disk_horizontal;

    private float spin_speed = 720.0f;
    private float pulse_speed = 1.5f;    // Speed of gravitational shimmer
    private float pulse_amount = 0.0025f;  // Scale variance 

    private float accumulated_spin = 0.0f;
    private Vector3 vertical_base_scale;

    private void Start()
    {
        if (accretion_disk_vertical != null)
        {
            vertical_base_scale = accretion_disk_vertical.localScale;
        }
    }

    private void Update()
    {
        accretion_disk_horizontal.Rotate(0.0f, 0.0f, -spin_speed * Time.deltaTime, Space.Self);

        // Vertical Photon Ring Behavior
        if (Camera.main != null && accretion_disk_vertical != null)
        {
            accretion_disk_vertical.LookAt(Camera.main.transform);
            // pulsing
            float pulse = Mathf.Sin(Time.time * pulse_speed) * pulse_amount;
            accretion_disk_vertical.localScale = vertical_base_scale + new Vector3(pulse, pulse, 0f);
        }

        // Spin logic
        accumulated_spin = (accumulated_spin - (spin_speed * Time.deltaTime)) % 360.0f;
        accretion_disk_vertical.Rotate(0.0f, 0.0f, accumulated_spin, Space.Self);
    }
}