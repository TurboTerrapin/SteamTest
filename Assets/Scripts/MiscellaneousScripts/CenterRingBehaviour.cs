using UnityEngine;

public class CenterRingBehaviour : MonoBehaviour
{
    float Speed = 15f;
    float MaxScale = 550f;
    float MinScale = 500f;
    bool isScaling = true;

    void Update()
    {
        if (isScaling)
        {
            transform.localScale += Vector3.one * Speed * Time.deltaTime;

            if (transform.localScale.x >= MaxScale)
            {
                isScaling = false;
            }
        }
        else
        {
            transform.localScale -= Vector3.one * Speed * Time.deltaTime;

            if (transform.localScale.x <= MinScale)
            {
                isScaling = true;
            }
        }

    }
}
