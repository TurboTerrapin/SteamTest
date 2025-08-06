using UnityEngine;
using System.Collections;
using TMPro;

public class TransitionHandler : MonoBehaviour
{
    public Camera ProbeCamera;

    public Transform ScenarioTransitionCamera;

    public Transform StartingCameraPosition1;
    public Transform EndingCameraPosition1;

    public Transform StartingCameraPosition2;
    public Transform EndingCameraPosition2;

    public Transform StartingCameraPosition3;
    public Transform EndingCameraPosition3;

    public Transform StartingCameraPosition4;
    public Transform EndingCameraPosition4;

    public Transform StartingCameraPosition5;
    public Transform EndingCameraPosition5;

    public TMP_Text StarDateText;
    public TMP_Text DistanceToDSFText;

    void Start()
    {
        ShowTransition(0);
    }

    public void ShowTransition(int scenario)
    {
        StartCoroutine(StartTransition(1));
    }

    IEnumerator StartTransition(int scenario)
    {
        //    //--SWITCH CAMERAS--
        //    ProbeCamera.gameObject.SetActive(false);
        //    // turn off player prefab cam
        //    ScenarioTransitionCamera.gameObject.SetActive(true);

        //--CAMERA MOVEMENT--
        if (scenario == 1)
        {
            StartCoroutine(MoveCamera(StartingCameraPosition1, EndingCameraPosition1, 12f));
        }

        if (scenario == 2)
        {
            StartCoroutine(MoveCamera(StartingCameraPosition2, EndingCameraPosition2, 12f));
        }

        if (scenario == 3)
        {
            StartCoroutine(MoveCamera(StartingCameraPosition3, EndingCameraPosition3, 12f));
        }

        if (scenario == 4)
        {
            StartCoroutine(MoveCamera(StartingCameraPosition4, EndingCameraPosition4, 12f));
        }

        if (scenario == 5)
        {
            StartCoroutine(MoveCamera(StartingCameraPosition5, EndingCameraPosition5, 12f));
        }


        //--FADE IN/OUT TEXT--
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(FadeText(StarDateText, 1f, 1.5f)); 
        yield return new WaitForSeconds(3f);
        yield return StartCoroutine(FadeText(StarDateText, 0f, 1.5f)); 

        yield return StartCoroutine(FadeText(DistanceToDSFText, 1f, 1.5f)); 
        yield return new WaitForSeconds(3f);
        yield return StartCoroutine(FadeText(DistanceToDSFText, 0f, 1.5f)); 

    }

IEnumerator FadeText(TMP_Text text, float targetAlpha, float duration)
    {
        Color color = text.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            text.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        text.color = new Color(color.r, color.g, color.b, targetAlpha);
    }

    IEnumerator MoveCamera(Transform start, Transform end, float duration)
    {
        yield return new WaitForSeconds(1f);

        ScenarioTransitionCamera.transform.position= start.position;
        ScenarioTransitionCamera.transform.rotation = start.rotation;

        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / duration);

            ScenarioTransitionCamera.transform.position = Vector3.Lerp(start.position, end.position, t);
            ScenarioTransitionCamera.transform.rotation = Quaternion.Lerp(start.rotation, end.rotation, t);

            yield return null;
        }

        ScenarioTransitionCamera.transform.position = end.position;
    }
}
