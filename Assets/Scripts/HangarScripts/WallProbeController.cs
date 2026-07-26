using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class WallProbeController : MonoBehaviour
{

    [SerializeField]
    private List<ProbeFollowSpline> probes = new List<ProbeFollowSpline>();
    int MAX_PROBES_ACTIVE = 4;

    [SerializeField]
    private List<ProbeFollowSpline> active_probes = new List<ProbeFollowSpline>();
    public float timer = 0;
    public float timer_max = 5;
    public float MAX_TIMER_LENGTH = 10;

    [SerializeField]
    private GameObject spline_root = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            probes.Add(transform.GetChild(i).GetComponent<ProbeFollowSpline>());
        }
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > timer_max && active_probes.Count < MAX_PROBES_ACTIVE)
        {
            timer = 0;
            //Debug.Log("Attempting activating a new probe");
            ActivateProbe();
        }

        for (int i = 0; i < active_probes.Count; i++)
        {
            if (active_probes[i].current_spline.name.Equals("EndSpline"))
            {
                //Debug.Log("Attempting removing an old probe");
                active_probes.RemoveAt(i);
            }
        }
    }

    void ActivateProbe()
    {
        bool found = false;
        int probe = 0;
        //Debug.Log("Doing active probe check");
        do
        {
            found = false;
            probe = Random.Range(0, probes.Count);
            
            for (int i = 0; i < active_probes.Count; i++)
            {
                if (probe == active_probes[i].probe_num)
                {
                    Debug.Log("Probe " + probe + " is already active");
                    found = true;
                    continue;
                }
            }
        } while (found);


        List<int> spline_choices = new List<int>();
        //Debug.Log("Filling out current spline choice options");
        for (int i = 0; i < spline_root.transform.childCount; i++)
        {
            if(!spline_root.transform.GetChild(i).GetComponent<ProbeRepairDetails>().getOccupied())
            {
                spline_choices.Add(i);
            }
        }

        if(spline_choices.Count <=0)
        {
            Debug.Log("All repair spots are filled");
            timer_max = MAX_TIMER_LENGTH;
            return;
        }

        int index = Random.Range(0, spline_choices.Count);
        int dir = spline_choices[index];
        //Debug.Log("Choosing new direction");
        probes[probe].current_spline = spline_root.transform.GetChild(dir).GetComponent<SplineContainer>();
        spline_root.transform.GetChild(dir).GetComponent<ProbeRepairDetails>().setOccupied(true);


        active_probes.Add(probes[probe]);
        float repair_time = Random.Range(5, 20);
        probes[probe].repair_duration = repair_time;
        probes[probe].SetProbeActive(true);
        timer_max = MAX_TIMER_LENGTH;
        Debug.Log("Activating probe " + probe + " on spline " + probes[probe].current_spline.name + " for " + repair_time + " seconds");

    }
}
