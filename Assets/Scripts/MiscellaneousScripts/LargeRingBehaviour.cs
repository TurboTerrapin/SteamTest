using UnityEngine;

public class LargeRingBehaviour : MonoBehaviour
{
    float Speed = 15f;
    float MaxScale = 200f;
    float MinScale = 150f;
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