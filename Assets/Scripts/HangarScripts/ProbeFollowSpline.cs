using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

public class ProbeFollowSpline : MonoBehaviour
{

    [SerializeField]
    private bool probe_active = false;
    public int probe_num = 0;

    public SplineContainer current_spline = null;

    public float repair_duration = 0;


    private Vector3 initial_pos = Vector3.zero;
    private Quaternion initial_rot = Quaternion.identity;


    public float slider = 0;

    private float MIN_REPAIR_DURATION = 5f;
    private float MAX_REPAIR_DURATION = 20f;



    void Start()
    {
        GetComponent<ParticleSystem>().Stop();
        initial_pos = transform.position;
        initial_rot = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.DrawRay(transform.position, transform.forward, Color.blue);
        if (!probe_active)
        {
            return;
        }
        

        if (slider < 1)
        {
            slider += 20 * Time.deltaTime / current_spline.CalculateLength();
            //Debug.Log("Attempting to move probe " + probe_num + " along spline " + current_spline.name);
            MoveAlongSpline();
            return;
        }
        else if (slider >= 1 && current_spline == null)
        {
            DeactivateParticles();
            ReturnToStart();
        }
        else if (slider >= 1 && current_spline.name.Equals("EndSpline"))
        {
            //Debug.Log("Returning to start");
            DeactivateParticles();
            current_spline = null;
            return;
        }
        else if (repair_duration < 0)
        {
            //Debug.Log("Probe " + probe_num + " choosing new spline");
            DeactivateParticles();
            ChooseNewSpline();
        }
        else
        {
            ActivateParticles();
            //Debug.Log("Probe " + probe_num + " is currently repairing");
            repair_duration -= Time.deltaTime;
        }
    }

    public void SetProbeActive(bool act)
    {
        probe_active = act;
    }

    void ChooseNewSpline()
    {
        current_spline.GetComponent<ProbeRepairDetails>().setOccupied(false);


        repair_duration = UnityEngine.Random.Range(MIN_REPAIR_DURATION, MAX_REPAIR_DURATION);
        int dir = UnityEngine.Random.Range(0, current_spline.transform.childCount);
        current_spline = current_spline.transform.GetChild(dir).GetComponent<SplineContainer>();
        //Debug.Log("Probe " + probe_num + " chose " + current_spline.name + " for " + repair_duration + " seconds");
        slider = 0;
    }

    void MoveAlongSpline()
    {
        float3 pos, forw, up;
        current_spline.Evaluate(slider, out pos, out forw, out up);
        
        Debug.DrawRay(pos, forw, Color.red);
        transform.position = pos;
        //transform.forward = forw;
        //transform.up = Vector3.up;

        transform.rotation = Quaternion.LookRotation(forw, up);
        //transform.rotation = Quaternion.LookRotation(forw, Vector3.up);
    }

    private float lerp_slider = 0;
    [SerializeField]
    private Vector3 initialPos = Vector3.zero;
    void ReturnToStart()
    {
        if (Vector3.Distance(transform.position, initial_pos) <= 0.01f)
        {
            slider = 0;
            probe_active = false;
            return;
        }

        //Debug.Log("Returning to start");
        lerp_slider += Time.deltaTime;
        transform.position = Vector3.Lerp(initialPos, initial_pos, lerp_slider);
        transform.rotation = Quaternion.Lerp(transform.rotation, initial_rot, lerp_slider);

        //transform.position = initial_pos;
        //transform.rotation = initial_rot;
        //current_spline = null;
    }

    void ActivateParticles()
    {
        ParticleSystem particles = GetComponent<ParticleSystem>();

        if (particles.isPlaying) return;

        particles.Play();
    }

    void DeactivateParticles()
    {
        ParticleSystem particles = GetComponent<ParticleSystem>();

        if (particles.isStopped) return;

        particles.Stop();
    }



    /*
    IEnumerator Return2Start()
    {
        lerp_slider += Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, initial_pos, lerp_slider);
        transform.rotation = Quaternion.Lerp(transform.rotation, initial_rot, lerp_slider);

        //transform.position = initial_pos;
        //transform.rotation = initial_rot;
        current_spline = null;
        yield return null;
    }
    */
}
