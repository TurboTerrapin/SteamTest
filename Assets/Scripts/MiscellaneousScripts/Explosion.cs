/*
    Explosion.cs
    - Utilizes sprite renderers to create an explosion effect that can be spawned
    Contributor(s): Jake Schott
    Last Updated: 3/20/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Explosion : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float MAX_EXPLOSION_DAMAGE = 10.0f;
    private static float EXPLOSION_SPHERE_FACTOR = 7.5f;
    private Vector2[] RESIZE_FACTORS = new Vector2[]
    {
        new Vector2(0.4f, 0.4f), //background
        new Vector2(0.2f, 0.2f), //base
        new Vector2(0.1f, 0.16f), //center
        new Vector2(0.21f, 0.21f) //ring
    };
    private Vector3[] COLOR_ADJUSTMENTS = new Vector3[]
    {
        new Vector3(0.0f, 0.0f, 0.0f), //background, leave as this.color
        new Vector3(-0.11f, -0.05f, 0.0f), //base
        new Vector3(-0.25f, -0.07f, 0.0f), //center
        new Vector3(-0.46f, -0.1f, 0.0f), //ring
        new Vector3(0.0f, 0.04f, 0.0f) //offshoot
    };
    private float[] ALPHAS = new float[]
    {
        0.04f, //background
        0.45f, //base
        0.55f, //center
        0.4f, //ring
        0.55f, //offshoot
    };

    public List<GameObject> explosion_sections; //background, base, center, ring, offshoots
    public GameObject light_source;
    public List<AudioClip> sound_options;

    private float size = 1.0f;
    private Color primary_color = ReferenceAssistor.COLOR_OPTIONS[2];
    private Color secondary_color = ReferenceAssistor.COLOR_OPTIONS[2];

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            Component.Destroy(GetComponent<Collider>());
        }
    }

    //explodes on default size (1) and default color (orange)
    public void explode()
    {
        beginAnimation();
    }

    //explodes on default size (1) and default color (orange)
    public void explode(float explosion_size)
    {
        size = Mathf.Max(1.0f, explosion_size);
        beginAnimation();
    }

    //explodes on size and color
    public void explode(float explosion_size, Color explosion_color)
    {
        size = Mathf.Max(1.0f, explosion_size);
        primary_color = explosion_color;
        secondary_color = explosion_color;

        beginAnimation();
    }

    //explodes on size, primary color, and secondary color
    public void explode(float explosion_size, Color base_color, Color accent_color)
    {
        size = Mathf.Max(1.0f, explosion_size);
        primary_color = base_color;
        secondary_color = accent_color;

        beginAnimation();
    }

    private void updateSize()
    {
        for (int i = 0; i < 4; i++)
        {
            explosion_sections[i].transform.localScale = new Vector3(0.0f, 0.0f, 1.0f);
        }
        light_source.GetComponent<Light>().range = size * 4.0f;
    }

    private void updateColors()
    {
        explosion_sections[0].GetComponent<SpriteRenderer>().color = primary_color;
        explosion_sections[1].GetComponent<SpriteRenderer>().color = new Color(primary_color.r + COLOR_ADJUSTMENTS[1].x, primary_color.g + COLOR_ADJUSTMENTS[1].y, primary_color.b + COLOR_ADJUSTMENTS[1].z);
        explosion_sections[2].GetComponent<SpriteRenderer>().color = new Color(primary_color.r + COLOR_ADJUSTMENTS[2].x, primary_color.g + COLOR_ADJUSTMENTS[2].y, primary_color.b + COLOR_ADJUSTMENTS[2].z);
        explosion_sections[3].GetComponent<SpriteRenderer>().color = new Color(secondary_color.r + COLOR_ADJUSTMENTS[3].x, secondary_color.g + COLOR_ADJUSTMENTS[3].y, secondary_color.b + COLOR_ADJUSTMENTS[3].z);
        explosion_sections[4].transform.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(secondary_color.r + COLOR_ADJUSTMENTS[4].x, secondary_color.g + COLOR_ADJUSTMENTS[4].y, secondary_color.b + COLOR_ADJUSTMENTS[4].z);

        light_source.GetComponent<Light>().color = primary_color;
    }

    private void beginAnimation()
    {
        //damage nearby items (including ship)
        if (NetworkManager.Singleton.IsHost == true)
        {
            Collider[] explosion_targets = Physics.OverlapSphere(transform.position, size * EXPLOSION_SPHERE_FACTOR);
            foreach (Collider et in explosion_targets)
            {
                if (et.GetComponent<Explosion>() == null)
                {
                    IDamageable[] damage_targets = et.GetComponents<IDamageable>();
                    foreach (IDamageable damage_target in damage_targets)
                    {
                        if (damage_target != null)
                        {
                            damage_target.damage(MAX_EXPLOSION_DAMAGE * (1.0f - (Vector3.Distance(transform.position, et.ClosestPoint(transform.position)) / (size * EXPLOSION_SPHERE_FACTOR))));
                        }
                    }
                }
            }
        }

        //play explosion sound
        GetComponent<AudioSource>().clip = sound_options[Random.Range(0, sound_options.Count)];
        GetComponent<AudioSource>().Play();

        //display animation
        updateSize();
        updateColors();
        StartCoroutine(explosionAnimation());
        StartCoroutine(centerRotator());
        StartCoroutine(offshootSender());
    }

    //creates 10-15 offshoot particles and sends them in random directions 
    IEnumerator offshootSender()
    {
        //initialize
        List<GameObject> explosion_offshoots = new List<GameObject>();
        List<Vector2> final_positions = new List<Vector2>();
        for (int i = 0; i < Random.Range(10, 15); i++)
        {
            //create
            explosion_offshoots.Add(GameObject.Instantiate(explosion_sections[4].transform.GetChild(0).gameObject, explosion_sections[4].transform));
            //resize
            float offshoot_size = Random.Range(0.05f, 0.1f);
            explosion_offshoots[i].transform.localScale = new Vector3(size * offshoot_size, size * offshoot_size, 1.0f);
            //determine final position
            final_positions.Add(Random.insideUnitCircle * size * 5.0f);
            //set active
            explosion_offshoots[i].SetActive(true);
        }

        //animate
        float anim_time = 1.0f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            //determine color based on alpha
            Color c = explosion_offshoots[0].GetComponent<SpriteRenderer>().color;
            c.a = Mathf.Lerp(0.0f, ALPHAS[4], Mathf.PingPong(anim_time, 0.5f) / 0.5f);

            //launch and recolor
            for (int i = 0; i <  explosion_offshoots.Count; i++)
            {
                explosion_offshoots[i].GetComponent<SpriteRenderer>().color = c;
                explosion_offshoots[i].transform.localPosition = Vector3.Lerp(Vector3.zero, new Vector3(final_positions[i].x, final_positions[i].y, 0.0f), Mathf.Lerp(1.0f, 0.0f, anim_time / 1.0f)); 
            }

            yield return null;
        }

        if (NetworkManager.Singleton.IsHost == true)
        {
            yield return new WaitForSeconds(5.0f);
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    //handles alpha adjustment on background (brightest flash)
    IEnumerator backgroundFlasher()
    {
        float anim_time = 0.0f;
        Color c = explosion_sections[0].GetComponent<SpriteRenderer>().color;
        while (anim_time < 0.3f)
        {
            anim_time = Mathf.Min(0.3f, anim_time + Time.deltaTime);

            c.a = Mathf.Lerp(0.0f, 0.55f, Mathf.PingPong(anim_time, 0.15f) / 0.15f);
            explosion_sections[0].GetComponent<SpriteRenderer>().color = c; 

            yield return null;
        }
    }

    //continually rotates center, faces towards camera
    IEnumerator centerRotator()
    {
        //randomize initial rotation
        explosion_sections[2].transform.localRotation = Quaternion.Euler(0.0f, 0.0f, Random.Range(0.0f, 360.0f));

        //rotate perpetually
        while (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            explosion_sections[2].transform.Rotate(0.0f, 0.0f, 240.0f * Time.deltaTime);
            yield return null;
        }
    }

    //primary animation
    IEnumerator explosionAnimation()
    {
        //initial expansion and fade in
        float anim_time = 0.3f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            float animation_progress = Mathf.Lerp(1.0f, 0.0f, anim_time / 0.3f);

            for (int i = 0; i < 4; i++)
            {
                explosion_sections[i].transform.localScale = new Vector3(animation_progress * size * RESIZE_FACTORS[i].x, animation_progress * size * RESIZE_FACTORS[i].y, 1.0f);
                Color c = explosion_sections[i].GetComponent<SpriteRenderer>().color;
                c.a = animation_progress * ALPHAS[i];
                explosion_sections[i].GetComponent<SpriteRenderer>().color = c;
            }
            light_source.GetComponent<Light>().intensity = Mathf.Lerp(0.0f, 10000.0f, animation_progress);

            yield return null;
        }

        //flash bright background
        StartCoroutine(backgroundFlasher());

        //fade away while continuing expansion
        anim_time = 0.5f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            float animation_progress = Mathf.Lerp(1.0f, 0.0f, anim_time / 0.5f);

            for (int i = 0; i < 4; i++)
            {
                explosion_sections[i].transform.localScale = 
                    new Vector3(Mathf.Lerp(RESIZE_FACTORS[i].x, RESIZE_FACTORS[i].x * 2.0f, animation_progress) * size,
                                Mathf.Lerp(RESIZE_FACTORS[i].y, RESIZE_FACTORS[i].y * 2.0f, animation_progress) * size, 
                                1.0f);

                if (i != 0)
                {
                    Color c = explosion_sections[i].GetComponent<SpriteRenderer>().color;
                    c.a = Mathf.Lerp(ALPHAS[i], 0.0f, animation_progress);
                    explosion_sections[i].GetComponent<SpriteRenderer>().color = c;
                }
            }
            light_source.GetComponent<Light>().intensity = Mathf.Lerp(10000.0f, 0.0f, animation_progress);

            yield return null;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void transmitExplosionRPC()
    {
        explode();
    }

    [Rpc(SendTo.Everyone)]
    public void transmitExplosionRPC(float s)
    {
        explode(s);
    }

    [Rpc(SendTo.Everyone)]
    public void transmitExplosionRPC(float s, Color c)
    {
        explode(s, c);
    }

    [Rpc(SendTo.Everyone)]
    public void transmitExplosionRPC(float s, Color b, Color a)
    {
        explode(s, b, a);
    }
}