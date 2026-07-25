/*
    Probe.cs
    - Alerts ProbeController.cs when damaged
    - Handles visual things
    Contributor(s): Jake Schott
    Last Updated: 6/26/2026
*/

using System.Collections;
using UnityEngine;

public class Probe : MonoBehaviour, IDamageable
{
    public Material lit_orange;
    public Material pure_black;

    private ProbeController probe_controller;

    private Coroutine self_destruct_coroutine = null;

    private void Start()
    {
        probe_controller = ReferenceAssistor.Instance.module_handlers[1].GetComponent<ProbeController>();
    }

    public void damage(float damage, IDamageable.DamageType damage_type)
    {
        probe_controller.damageProbe(damage);
    }

    //orange flashing
    public void toggleSelfDestructVisual()
    {
        GetComponent<MapItem>().setColor(ReferenceAssistor.COLOR_OPTIONS[2]);
        GetComponent<AudioSource>().Play();
        StopAllCoroutines();
        self_destruct_coroutine = StartCoroutine(selfDestruct());
    }

    IEnumerator selfDestruct()
    {
        //runs until destroyed
        while (true) 
        {
            for (int i = 1; i < 4; i++)
            {
                transform.GetChild(i).GetComponent<Renderer>().material = lit_orange;
            }
            transform.GetChild(0).gameObject.SetActive(true);
            foreach (Transform light in transform.GetChild(0))
            {
                light.GetComponent<Light>().color = new Color(1.0f, 0.47f, 0.0f);
            }
            yield return new WaitForSeconds(0.1f);
            
            for (int i = 1; i < 4; i++)
            {
                transform.GetChild(i).GetComponent<Renderer>().material = pure_black;
            }
            transform.GetChild(0).gameObject.SetActive(false);
            
            yield return new WaitForSeconds(0.1f);
        }
    }
}