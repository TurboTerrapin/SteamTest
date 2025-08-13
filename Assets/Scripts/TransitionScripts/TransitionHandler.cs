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
    public Transform StartingCameraPosition6;
    public Transform EndingCameraPosition6;
    public Transform StartingCameraPosition7;
    public Transform EndingCameraPosition7;
    public Transform StartingCameraPosition8;
    public Transform EndingCameraPosition8;
    public Transform StartingCameraPosition9;
    public Transform EndingCameraPosition9;
    public Transform StartingCameraPosition10;
    public Transform EndingCameraPosition10;

    public TMP_Text StarDateText1;
    public TMP_Text DistanceToDSFText1;
    public TMP_Text StarDateText2;
    public TMP_Text DistanceToDSFText2;
    public TMP_Text StarDateText3;
    public TMP_Text DistanceToDSFText3;
    public TMP_Text StarDateText4;
    public TMP_Text DistanceToDSFText4;
    public TMP_Text StarDateText5;
    public TMP_Text DistanceToDSFText5;
    public TMP_Text StarDateText6;
    public TMP_Text DistanceToDSFText6;
    public TMP_Text StarDateText7;
    public TMP_Text DistanceToDSFText7;
    public TMP_Text StarDateText8;
    public TMP_Text DistanceToDSFText8;
    public TMP_Text StarDateText9;
    public TMP_Text DistanceToDSFText9;
    public TMP_Text StarDateText10;
    public TMP_Text DistanceToDSFText10;

    // Start() for testing purposes
    void Start()
    {
        ShowTransition(10);
    }

    public void ShowTransition(int scenario)
    {
        StartCoroutine(StartTransition(scenario));
    }

    IEnumerator StartTransition(int scenario)
    {
        //    //--SWITCH CAMERAS--
        //    ProbeCamera.gameObject.SetActive(false);
        //    // turn off player prefab cam
        //    ScenarioTransitionCamera.gameObject.SetActive(true);

        switch (scenario)
        {
            case 1:
                StartCoroutine(MoveCamera(StartingCameraPosition1, EndingCameraPosition1, 12f));

                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(FadeText(StarDateText1, 1f, 1.5f));
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(FadeText(DistanceToDSFText1, 1f, 1.5f));
                break;

            case 2:
                StartCoroutine(MoveCamera(StartingCameraPosition2, EndingCameraPosition2, 12f));

                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(FadeText(StarDateText2, 1f, 1.5f));
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(FadeText(DistanceToDSFText2, 1f, 1.5f));
                break;

            case 3:
                StartCoroutine(MoveCamera(StartingCameraPosition3, EndingCameraPosition3, 12f));

                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(FadeText(StarDateText3, 1f, 1.5f));
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(FadeText(DistanceToDSFText3, 1f, 1.5f));
                break;

            case 4:
                StartCoroutine(MoveCamera(StartingCameraPosition4, EndingCameraPosition4, 12f));

                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(FadeText(StarDateText4, 1f, 1.5f));
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(FadeText(DistanceToDSFText4, 1f, 1.5f));
                break;

            case 5:
                StartCoroutine(MoveCamera(StartingCameraPosition5, EndingCameraPosition5, 12f));

                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(FadeText(StarDateText5, 1f, 1.5f));
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(FadeText(DistanceToDSFText5, 1f, 1.5f));
                break;

            case 6:
                StartCoroutine(MoveCamera(StartingCameraPosition6, EndingCameraPosition6, 12f));

                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(FadeText(StarDateText6, 1f, 1.5f));
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(FadeText(DistanceToDSFText6, 1f, 1.5f));
                break;

            case 7:
                StartCoroutine(MoveCamera(StartingCameraPosition7, EndingCameraPosition7, 12f));

                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(FadeText(StarDateText7, 1f, 1.5f));
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(FadeText(DistanceToDSFText7, 1f, 1.5f));
                break;

            case 8:
                StartCoroutine(MoveCamera(StartingCameraPosition8, EndingCameraPosition8, 12f));

                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(FadeText(StarDateText8, 1f, 1.5f));
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(FadeText(DistanceToDSFText8, 1f, 1.5f));
                break;

            case 9:
                StartCoroutine(MoveCamera(StartingCameraPosition9, EndingCameraPosition9, 12f));

                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(FadeText(StarDateText9, 1f, 1.5f));
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(FadeText(DistanceToDSFText9, 1f, 1.5f));
                break;

            case 10:
                StartCoroutine(MoveCamera(StartingCameraPosition10, EndingCameraPosition10, 12f));

                yield return new WaitForSeconds(2f);
                yield return StartCoroutine(FadeText(StarDateText10, 1f, 1.5f));
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(FadeText(DistanceToDSFText10, 1f, 1.5f));
                break;
        }
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
