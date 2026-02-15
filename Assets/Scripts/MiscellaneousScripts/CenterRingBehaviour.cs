using System.Diagnostics;
using UnityEngine;

public class CenterRingBehaviour : MonoBehaviour
{

    [SerializeField]
    private bool doesPulse = false;
    [SerializeField]
    float Speed = 15f;
    [SerializeField]
    float MaxScale = 550f;
    [SerializeField]
    float MinScale = 500f;
    [SerializeField]
    float ScaleStep = 0f;
    [SerializeField]
    float time = 0f;
    [SerializeField]
    bool isScaling = true;
    [SerializeField]
    private AnimationCurve curve;

    [SerializeField]
    private bool doesRotate = false;
    [SerializeField]
    private float rotationSpeed = 18f;
    [SerializeField]
    private bool rotatesUsingGlobalAxis = true;
    [SerializeField]
    private Vector3 axis = new Vector3(0, 1, 0);

    void Update()
    {
        if (doesPulse)
        {
            if (isScaling)
            {
                time += Time.deltaTime * Speed / 60;


                if (time >= 1)
                {
                    isScaling = false;
                }

                //transform.localScale += Vector3.one * Speed * Time.deltaTime;

                //if (transform.localScale.x >= MaxScale)
                //{
                //    isScaling = false;
                //}
            }
            else
            {
                time -= Time.deltaTime * Speed / 60;

                if (time <= 0)
                {
                    isScaling = true;
                }

                //transform.localScale -= Vector3.one * Speed * Time.deltaTime;

                //if (transform.localScale.x <= MinScale)
                //{
                //    isScaling = true;
                //}
            }

            ScaleStep = MaxScale - MinScale;


            transform.localScale = Vector3.one * (MinScale + (curve.Evaluate(time) * ScaleStep));
        }

        if (doesRotate)
        {
            axis.Normalize();
            if (rotatesUsingGlobalAxis)
            {
                transform.localRotation *= Quaternion.AngleAxis(rotationSpeed * Time.deltaTime, transform.InverseTransformDirection(axis));
            }
            else
            {
                transform.localRotation *= Quaternion.AngleAxis(rotationSpeed * Time.deltaTime, axis);
            }
            //transform.InverseTransformDirection(Vector3.up);

        }






    }
}

