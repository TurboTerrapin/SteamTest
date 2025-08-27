using UnityEngine;
using System.Collections;
using TMPro;

public class TransitionHandler : MonoBehaviour
{
    public Transform ScenarioTransitionCamera;

    // Parents
    public Transform CameraPositions;
    public Transform TransitionText;
    public GameObject TransitionCanvas;

    public void ShowTransition(int scenario)
    {
        GameObject localPlayer = GameObject.Find("PlayerManager").GetComponent<PlayerManager>().getLocalPlayer();

        GameObject playerCamera = localPlayer.transform.GetChild(0).gameObject;
        GameObject transitionCamera = transform.GetChild(0).gameObject;

        // switch cameras
        playerCamera.SetActive(false);
        transitionCamera.SetActive(true);

        // show UI
        TransitionCanvas.SetActive(true);

        StartCoroutine(StartTransition(scenario));
    }

    IEnumerator StartTransition(int scenario)
    {
        switch (scenario)
        {
            case 1:
                StartCoroutine(MoveCamera(CameraPositions.GetChild(0), CameraPositions.GetChild(1), 12f));

                yield return new WaitForSeconds(2f);
                StartCoroutine(FadeText(TransitionText.GetChild(0).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(1).GetComponent<TMP_Text>(), 1f, 1.5f));

                yield return new WaitForSeconds(3f);
                StartCoroutine(FadeText(TransitionText.GetChild(2).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(3).GetComponent<TMP_Text>(), 1f, 1.5f));
                break;

            case 2:
                StartCoroutine(MoveCamera(CameraPositions.GetChild(2), CameraPositions.GetChild(3), 12f));

                yield return new WaitForSeconds(2f);
                StartCoroutine(FadeText(TransitionText.GetChild(4).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(5).GetComponent<TMP_Text>(), 1f, 1.5f));

                yield return new WaitForSeconds(3f);
                StartCoroutine(FadeText(TransitionText.GetChild(6).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(7).GetComponent<TMP_Text>(), 1f, 1.5f));
                break;

            case 3:
                StartCoroutine(MoveCamera(CameraPositions.GetChild(4), CameraPositions.GetChild(5), 12f));

                yield return new WaitForSeconds(2f);
                StartCoroutine(FadeText(TransitionText.GetChild(8).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(9).GetComponent<TMP_Text>(), 1f, 1.5f));

                yield return new WaitForSeconds(3f);
                StartCoroutine(FadeText(TransitionText.GetChild(10).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(11).GetComponent<TMP_Text>(), 1f, 1.5f));
                break;

            case 4:
                StartCoroutine(MoveCamera(CameraPositions.GetChild(6), CameraPositions.GetChild(7), 12f));

                yield return new WaitForSeconds(2f);
                StartCoroutine(FadeText(TransitionText.GetChild(12).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(13).GetComponent<TMP_Text>(), 1f, 1.5f));

                yield return new WaitForSeconds(3f);
                StartCoroutine(FadeText(TransitionText.GetChild(14).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(15).GetComponent<TMP_Text>(), 1f, 1.5f));
                break;

            case 5:
                StartCoroutine(MoveCamera(CameraPositions.GetChild(8), CameraPositions.GetChild(9), 12f));

                yield return new WaitForSeconds(2f);
                StartCoroutine(FadeText(TransitionText.GetChild(16).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(17).GetComponent<TMP_Text>(), 1f, 1.5f));

                yield return new WaitForSeconds(3f);
                StartCoroutine(FadeText(TransitionText.GetChild(18).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(19).GetComponent<TMP_Text>(), 1f, 1.5f));
                break;

            case 6:
                StartCoroutine(MoveCamera(CameraPositions.GetChild(10), CameraPositions.GetChild(11), 12f));

                yield return new WaitForSeconds(2f);
                StartCoroutine(FadeText(TransitionText.GetChild(12).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(20).GetComponent<TMP_Text>(), 1f, 1.5f));

                yield return new WaitForSeconds(3f);
                StartCoroutine(FadeText(TransitionText.GetChild(14).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(21).GetComponent<TMP_Text>(), 1f, 1.5f));
                break;


            case 7:
                StartCoroutine(MoveCamera(CameraPositions.GetChild(12), CameraPositions.GetChild(13), 12f));

                yield return new WaitForSeconds(2f);
                StartCoroutine(FadeText(TransitionText.GetChild(4).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(22).GetComponent<TMP_Text>(), 1f, 1.5f));

                yield return new WaitForSeconds(3f);
                StartCoroutine(FadeText(TransitionText.GetChild(6).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(23).GetComponent<TMP_Text>(), 1f, 1.5f));
                break;


            case 8:
                StartCoroutine(MoveCamera(CameraPositions.GetChild(14), CameraPositions.GetChild(15), 12f));

                yield return new WaitForSeconds(2f);
                StartCoroutine(FadeText(TransitionText.GetChild(8).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(24).GetComponent<TMP_Text>(), 1f, 1.5f));

                yield return new WaitForSeconds(3f);
                StartCoroutine(FadeText(TransitionText.GetChild(10).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(25).GetComponent<TMP_Text>(), 1f, 1.5f));
                break;


            case 9:
                StartCoroutine(MoveCamera(CameraPositions.GetChild(16), CameraPositions.GetChild(17), 12f));

                yield return new WaitForSeconds(2f);
                StartCoroutine(FadeText(TransitionText.GetChild(0).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(26).GetComponent<TMP_Text>(), 1f, 1.5f));

                yield return new WaitForSeconds(3f);
                StartCoroutine(FadeText(TransitionText.GetChild(2).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(27).GetComponent<TMP_Text>(), 1f, 1.5f));
                break;

            case 10:
                StartCoroutine(MoveCamera(CameraPositions.GetChild(18), CameraPositions.GetChild(19), 12f));

                yield return new WaitForSeconds(2f);
                StartCoroutine(FadeText(TransitionText.GetChild(16).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(28).GetComponent<TMP_Text>(), 1f, 1.5f));

                yield return new WaitForSeconds(3f);
                StartCoroutine(FadeText(TransitionText.GetChild(18).GetComponent<TMP_Text>(), 1f, 1.5f));
                StartCoroutine(FadeText(TransitionText.GetChild(29).GetComponent<TMP_Text>(), 1f, 1.5f));
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
