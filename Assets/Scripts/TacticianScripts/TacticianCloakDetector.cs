/*
    TacticianCloakDetector.cs
    - Handles tactician cloak detector
    - Has no interaction with the player
    Contributor(s): Jake Schott
    Last Updated: 1/3/2026
*/

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TacticianCloakDetector : MonoBehaviour, IPowerable
{
    //CLASS CONSTANTS
    private static float ANIMATION_PERIOD = 0.5f;

    public Material lit_purple;
    public Material unlit_purple;

    public List<GameObject> cloak_detector_displays;
    public GameObject cloak_indicator;

    private bool cloaked_ship_detected = false;
    private Coroutine cloak_indicator_coroutine = null;

    public bool getCloakedShipDetected()
    {
        return cloaked_ship_detected;
    }

    public void setCloakedShipDetected(bool detected)
    {
        cloaked_ship_detected = detected;
    }

    private void barTransparencyAdjustment(int index, float a)
    {
        for (int i = 0; i < 2; i++)
        {
            cloak_detector_displays[i].transform.GetChild(index).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.69f, 0.0f, 0.69f, a);
        }
    }

    private float handleAnimPercentage(float ap)
    {
        if (ap < 0.0f)
        {
            ap *= -1.0f;
            ap = 1.0f - ap;
        }
        else if (ap > 1.0f)
        {
            ap -= 1.0f;
        }
        return ap;
    }

    IEnumerator searchingState()
    {
        float anim_percentage = 0.0f;
        while (true)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            anim_percentage += (dt / ANIMATION_PERIOD);
            anim_percentage = handleAnimPercentage(anim_percentage);
            for (int i = 0; i < 5; i++)
            {
                bool active = anim_percentage >= (i / 5.0f) && anim_percentage <= ((i + 1) / 5.0f);
                if (active == true)
                {
                    barTransparencyAdjustment(i, 1.0f);
                }
                else
                {
                    barTransparencyAdjustment(i, 0.2f);
                }
            }

            yield return null;
        }
    }

    IEnumerator cloakDetectedState()
    {
        float anim_percentage = 0.0f;
        while (true)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            anim_percentage += (dt / ANIMATION_PERIOD) * 1.5f;
            anim_percentage = handleAnimPercentage(anim_percentage);
            bool active = (anim_percentage >= 0.5f);
            for (int i = 0; i < 5; i++)
            {
                if (active == true)
                {
                    barTransparencyAdjustment(i, 1.0f);
                }
                else
                {
                    barTransparencyAdjustment(i, 0.2f);
                }
            }

            if (active == true)
            {
                cloak_indicator.GetComponent<Renderer>().material = lit_purple;
            }
            else
            {
                cloak_indicator.GetComponent<Renderer>().material = unlit_purple;
            }

            yield return null;
        }
    }


    public void powerOn(int pos)
    {
        if (cloak_indicator_coroutine == null)
        {
            if (cloaked_ship_detected == false)
            {
                cloak_indicator_coroutine = StartCoroutine(searchingState());
            }
            else
            {
                cloak_indicator_coroutine = StartCoroutine(cloakDetectedState());
            }
        }
        for (int i = 0; i < 2; i++)
        {
            cloak_detector_displays[i].SetActive(true);
        }
    }

    public void powerOff(int pos, float time)
    {
        if (cloak_indicator_coroutine != null)
        {
            StopCoroutine(cloak_indicator_coroutine);
            cloak_indicator_coroutine = null;
        }
        cloak_indicator.GetComponent<Renderer>().material = unlit_purple;
        for (int i = 0; i < 2; i++)
        {
            cloak_detector_displays[i].SetActive(false);
        }
    }
}
