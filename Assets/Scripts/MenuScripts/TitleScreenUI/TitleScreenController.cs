using UnityEngine;
using TMPro;
using System.Collections;

public class TitleScreenController : MonoBehaviour
{
    //TitleScreen
    public TextMeshProUGUI PressStartText;
    public float FadeDuration = 1.5f; // Time for a full fade in/out
    public GameObject TitleScreen;

    // Rings
    public GameObject SpinCircle;
    public GameObject SpriteMask;
    public static float[] SPIN_SPEEDS = new float[3] { 12.5f, 50.0f, 22.5f };

    //MainMenu
    public GameObject MainMenu;

    void Start()
    {
        TitleScreen.SetActive(true);
        SpriteMask.SetActive(true);
        SpinCircle.SetActive(true);
        MainMenu.SetActive(false);

        StartCoroutine(FadeText());

        if (SceneData.targetUI == "MainMenu")
        {
            SwitchToMainMenu();
            SceneData.targetUI = null; 
        }
    }

    // Call SwitchCanvas() if any key is pressed
    void Update()
    {
        spinRings();

        if (Input.anyKeyDown && TitleScreen.activeSelf)
        {
            SwitchToMainMenu();
        }
    }

    IEnumerator FadeText()
    {
        while (true)
        {
            yield return StartCoroutine(FadeTo(0f, FadeDuration)); // Fade out
            yield return StartCoroutine(FadeTo(1.3f, FadeDuration)); // Fade in
        }
    }

    private void spinRings()
    {
        for (int i = 0; i < 3; i++)
        {
            float z = SpinCircle.transform.GetChild(i).GetComponent<Transform>().rotation.eulerAngles.z + SPIN_SPEEDS[i] * Time.deltaTime;
            SpinCircle.transform.GetChild(i).GetComponent<Transform>().rotation = Quaternion.Euler(0.0f, 0.0f, z);
        }
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        Color color = PressStartText.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime * 0.8f;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            PressStartText.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        PressStartText.color = new Color(color.r, color.g, color.b, targetAlpha);
    }

    private void SwitchToMainMenu()
    {
        TitleScreen.SetActive(false);
        SpinCircle.SetActive(false);
        SpriteMask.SetActive(false);  
        MainMenu.SetActive(true);
    }
}
