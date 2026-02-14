using UnityEngine;

public class VisualSpectacleLighting : MonoBehaviour
{
    public GameObject Sphere;
    public GameObject Beam;
    public GameObject UpperCenterRing;
    public GameObject LowerCenterRing;

    public Material litRed;
    public Material litGreen;

    public void SetRedLight()
    {
        Sphere.GetComponent<Renderer>().material = litRed;
        Beam.GetComponent<Renderer>().material = litRed;
        UpperCenterRing.GetComponent<Renderer>().material = litRed;
        LowerCenterRing.GetComponent<Renderer>().material = litRed;
    }

    public void SetGreenLight()
    {
        Sphere.GetComponent<Renderer>().material = litGreen;
        Beam.GetComponent<Renderer>().material = litGreen;
        UpperCenterRing.GetComponent<Renderer>().material = litGreen;
        LowerCenterRing.GetComponent<Renderer>().material = litGreen;
    }
}
