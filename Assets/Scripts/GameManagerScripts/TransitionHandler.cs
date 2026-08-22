using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TransitionHandler : MonoBehaviour
{
    //CLASS CONSTANTS
    private static int[] TRANSITION_OPTIONS = new int[10] { 0, 0, 2, 4, 1, 4, 3, 2, 0, 1 }; //0 is BL, 1 is BM, 2 is BR, 3 is TL, 4 is TR

    public Transform ScenarioTransitionCamera;

    // Parents
    public Transform CameraPositions;
    public Transform TransitionText;
    public GameObject TransitionCanvas;
    public GameObject FakeShip;
    private List<int> availableTransitionOptions = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

    public void ShowTransition(int transitionOption, string starDate, string distanceToDSF)
    {
        GameObject localPlayer = ReferenceAssistor.Instance.player_manager.getLocalPlayer();

        GameObject transitionCamera = transform.GetChild(0).gameObject;

        // switch cameras
        localPlayer.GetComponent<CameraMove>().DeactivateCamera();
        transitionCamera.SetActive(true);

        // show fake ship
        FakeShip.SetActive(true);

        // show UI
        TransitionCanvas.SetActive(true);

        StartCoroutine(StartTransition(transitionOption, starDate, distanceToDSF));
    }

    public void EndTransition()
    {
        StopAllCoroutines();
        TransitionCanvas.SetActive(false);
        foreach (Transform option in TransitionText)
        {
            option.gameObject.SetActive(false);
        }

        GameObject localPlayer = ReferenceAssistor.Instance.player_manager.getLocalPlayer();

        GameObject playerCamera = localPlayer.transform.GetChild(0).gameObject;
        GameObject transitionCamera = transform.GetChild(0).gameObject;

        // switch cameras
        playerCamera.SetActive(true);
        transitionCamera.SetActive(false);

        // hide fake ship
        FakeShip.SetActive(false);
    }

    // randomly returns an option from 0-9 to inclusive (used to avoid repeats)
    public int GetTransitionOption()
    {
        if (availableTransitionOptions.Count == 0)
        {
            for (int i = 0; i < 10; i++)
            {
                availableTransitionOptions.Add(i);
            }
        }
        int optionToReturn = availableTransitionOptions[Random.Range(0, availableTransitionOptions.Count)];
        availableTransitionOptions.Remove(optionToReturn);
        return optionToReturn;
    }

    IEnumerator StartTransition(int transitionOption, string starDate, string distanceToDSF)
    {
        // hide all options
        foreach (Transform option in TransitionText)
        {
            option.gameObject.SetActive(false);
        }

        // reset selected option
        foreach (Transform text in TransitionText.GetChild(TRANSITION_OPTIONS[transitionOption]))
        {
            text.GetComponent<TMP_Text>().alpha = 0.0f;
        }
        TransitionText.GetChild(TRANSITION_OPTIONS[transitionOption]).gameObject.SetActive(true);

        // set text
        TransitionText.GetChild(TRANSITION_OPTIONS[transitionOption]).GetChild(1).GetComponent<TMP_Text>().text = starDate;
        TransitionText.GetChild(TRANSITION_OPTIONS[transitionOption]).GetChild(3).GetComponent<TMP_Text>().text = distanceToDSF + " lightyears";

        StartCoroutine(MoveCamera(CameraPositions.GetChild(transitionOption * 2), CameraPositions.GetChild((transitionOption * 2) + 1), 12f));

        yield return new WaitForSeconds(2f);

        StartCoroutine(FadeText(TransitionText.GetChild(TRANSITION_OPTIONS[transitionOption]).GetChild(0).GetComponent<TMP_Text>(), 1f, 1.5f));
        StartCoroutine(FadeText(TransitionText.GetChild(TRANSITION_OPTIONS[transitionOption]).GetChild(1).GetComponent<TMP_Text>(), 1f, 1.5f));

        yield return new WaitForSeconds(3f);

        StartCoroutine(FadeText(TransitionText.GetChild(TRANSITION_OPTIONS[transitionOption]).GetChild(2).GetComponent<TMP_Text>(), 1f, 1.5f));
        StartCoroutine(FadeText(TransitionText.GetChild(TRANSITION_OPTIONS[transitionOption]).GetChild(3).GetComponent<TMP_Text>(), 1f, 1.5f));
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
        ScenarioTransitionCamera.transform.position = start.position;
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

        EndTransition();
    }
}