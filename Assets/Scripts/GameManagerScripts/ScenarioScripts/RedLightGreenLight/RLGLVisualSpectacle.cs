using Unity.Netcode;
using UnityEngine;

public class RLGLVisualSpectacle : MonoBehaviour
{
    public GameObject Sphere;
    public GameObject Beam;
    public GameObject UpperCenterRing;
    public GameObject LowerCenterRing;
    public RedLightGreenLight RedLightGreenLight;

    private bool Active = false;

    private void Start()
    {
        SetRedLight();
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }
    }

    public void Activate()
    {
        Active = true;
    }

    public void SetRedLight()
    {
        Sphere.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_red;
        Beam.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_red;
        UpperCenterRing.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_red;
        LowerCenterRing.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_red;
    }

    public void SetGreenLight()
    {
        Sphere.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_green;
        Beam.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_green;
        UpperCenterRing.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_green;
        LowerCenterRing.GetComponent<Renderer>().material = ReferenceAssistor.Instance.lit_green;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Active == false)
        {
            return;
        }

        if (other.gameObject.layer == 9) //Stun ship
        {
            RedLightGreenLight.shipEnteredSpectacle();
        }
        else if (other.GetComponent<Probe>() != null) //Destroy probes
        {
            other.GetComponent<Probe>().damage(99999.9f, IDamageable.DamageType.Explosive);
        }
    }
}
