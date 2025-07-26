/*
    Probe.cs
    - Handles distance to ship
    - Handles probe health
    Contributor(s): Jake Schott
    Last Updated: 7/25/2025
*/

using System.Collections;
using UnityEngine;

public class Probe : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float RANGE = 300.0f; //how far the probe can be from the ship while still being in contact

    private float probe_health;
    private bool connected;
    private GameObject control_handler;
    private GameObject sensor_handler;
    private GameObject ship;
    private Coroutine out_of_range_coroutine = null;

    void Start()
    {
        //always start connected
        connected = true;

        //health goes from 0 to 100
        probe_health = 100.0f;

        control_handler = GameObject.FindGameObjectWithTag("ControlHandler");
        sensor_handler = GameObject.FindGameObjectWithTag("SensorHandler");
        ship = GameObject.FindGameObjectWithTag("Spaceship");
    }

    public bool inRange()
    {
        return (Mathf.Min(RANGE, Vector3.Distance(transform.position, ship.transform.position)) < RANGE);
    }

    public void damageProbe(float dam)
    {
        probe_health = Mathf.Max(0.0f, probe_health - dam);
        sensor_handler.GetComponent<TacticianProbeInfo>().displayHealth(probe_health);
        if (probe_health <= 0.0f)
        {
            unlink();
            control_handler.GetComponent<ProbeOptions>().onProbeDestroyed(); //only necessary in cases where probe is not self-destructed
            GameObject.Destroy(transform.gameObject);
        }
    }

    IEnumerator outOfRangeHelper()
    {
        yield return new WaitForSeconds(5.0f);
        if (Mathf.Min(RANGE, Vector3.Distance(transform.position, ship.transform.position)) >= RANGE)
        {
            connected = false;
            unlink();
        }
        out_of_range_coroutine = null;
    }

    private void unlink()
    {
        sensor_handler.GetComponent<TacticianProbeInfo>().disconnectProbe();
        sensor_handler.GetComponent<TacticianProbeInfo>().displayRange(0.0f);
        control_handler.GetComponent<ProbeLateralMovement>().unlinkProbe();
        control_handler.GetComponent<ProbeVerticalMovement>().unlinkProbe();
        control_handler.GetComponent<ProbeOrientation>().unlinkProbe();
        control_handler.GetComponent<ProbeOptions>().unlinkProbe();
    }

    private void link()
    {
        sensor_handler.GetComponent<TacticianProbeInfo>().connectProbe();
        control_handler.GetComponent<ProbeLateralMovement>().linkProbe(transform.gameObject);
        control_handler.GetComponent<ProbeVerticalMovement>().linkProbe(transform.gameObject);
        control_handler.GetComponent<ProbeOrientation>().linkProbe(transform.gameObject);
        control_handler.GetComponent<ProbeOptions>().linkProbe();
    }

    public void updateDistance()
    {
        float distance = Mathf.Min(RANGE, Vector3.Distance(transform.position, ship.transform.position));
        if (distance >= RANGE)
        {
            if (connected == true && out_of_range_coroutine == null)
            {
                //attempt disconnect
                out_of_range_coroutine = StartCoroutine(outOfRangeHelper());
            }
        }
        else
        {
            if (connected == false)
            {
                //reconnect
                connected = true;
                link();
            }
        }
        sensor_handler.GetComponent<TacticianProbeInfo>().displayRange(1.0f - Mathf.Max(0.0f, (distance - 25.0f) / (RANGE - 25.0f)));
    }
}
