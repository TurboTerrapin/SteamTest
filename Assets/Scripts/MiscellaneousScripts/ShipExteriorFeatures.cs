/*
    ShipExteriorFeatures.cs
    - Handles rotating elements and blinking lights on the exterior of the ship
    - Handles/opening closing of cargo door
    Contributor(s): Jake Schott
    Last Updated: 4/11/2026
*/

using System.Collections;
using UnityEngine;

public class ShipExteriorFeatures : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float CARGO_LATCH_CHANGE_TIME = 0.5f;
    private static Vector3 CARGO_LATCH_CLOSED_POSITION = new Vector3(0.0f, -77.6f, -6.7f);
    private static Vector3 CARGO_LATCH_OPEN_POSITION = new Vector3(0.0f, -77.5f, -7.35f);
    private static float FEATURE_ROTATION_SPEED = 30.0f;
    private static float LIGHT_BLINK_DELAY = 0.5f;

    [SerializeField]
    private bool enable_cosmetic_features = true;

    public GameObject ship_engine_circles;
    public GameObject ship_radar_dish;
    public GameObject ship_white_lights;
    public GameObject ship_cargo_latch;
    public Material lit_white;
    public Material black;

    private bool[] open_features = new bool[] { false, false }; //tractor beam, cargo eject
    private float cargo_open_percentage = 0.0f;
    private float target_open_percentage = 0.0f;
    private Coroutine cargo_change_coroutine = null;

    private void Start()
    {
        if (enable_cosmetic_features == true)
        {
            StartCoroutine(lightBlinker());
            StartCoroutine(rotator());
        }
    }

    private void restartCargoDoorChange()
    {
        if (cargo_change_coroutine != null)
        {
            StopCoroutine(cargo_change_coroutine);
        }
        cargo_change_coroutine = StartCoroutine(cargoDoorChange());
    }

    public void adjustCargoDoorOpen(int index, bool needs_open)
    {
        open_features[index] = needs_open;
        if (open_features[0] == true || open_features[1] == true)
        {
            if (cargo_open_percentage != 1.0f && target_open_percentage != 1.0f)
            {
                target_open_percentage = 1.0f;
                restartCargoDoorChange();
            }
        }
        else
        {
            if (cargo_open_percentage != 0.0f && target_open_percentage != 0.0f)
            {
                target_open_percentage = 0.0f;
                restartCargoDoorChange();
            }
        }
    }

    IEnumerator rotator()
    {
        while (true)
        {
            float rotation = Time.deltaTime * FEATURE_ROTATION_SPEED;
            ship_engine_circles.transform.Rotate(rotation, 0.0f, 0.0f);
            ship_radar_dish.transform.Rotate(0.0f, 0.0f, rotation);
            yield return null;
        }
    }

    IEnumerator lightBlinker()
    {
        while (true)
        {
            ship_white_lights.GetComponent<Renderer>().material = lit_white;
            yield return new WaitForSeconds(LIGHT_BLINK_DELAY);
            ship_white_lights.GetComponent<Renderer>().material = black;
            yield return new WaitForSeconds(LIGHT_BLINK_DELAY);
        }
    }

    IEnumerator cargoDoorChange()
    {
        float anim_time = CARGO_LATCH_CHANGE_TIME;
        float starting_cargo_percentage = cargo_open_percentage;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            cargo_open_percentage = Mathf.Lerp(target_open_percentage, starting_cargo_percentage, anim_time / CARGO_LATCH_CHANGE_TIME);

            ship_cargo_latch.transform.localRotation = Quaternion.Euler(Mathf.Lerp(0.0f, 90.0f, cargo_open_percentage), 0.0f, 0.0f);
            ship_cargo_latch.transform.localPosition = Vector3.Lerp(CARGO_LATCH_CLOSED_POSITION, CARGO_LATCH_OPEN_POSITION, cargo_open_percentage);

            yield return null;
        }

        cargo_change_coroutine = null;
    }
}
