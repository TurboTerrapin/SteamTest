using UnityEngine;
using TMPro;
using System.Collections;

public class TitleScreenController : MonoBehaviour
{
    //TitleScreen
    public TextMeshProUGUI PressStartText;
    public float FadeDuration = 1.5f; // Time for a full fade in/out
    public GameObject TitleScreenContents;

    //Audio
    [SerializeField] AudioSource MusicSource;
    public AudioClip TitleScreenAudio;

    // Rings
    public GameObject SpinCircle;
    public GameObject SpriteMask;
    public static float[] SPIN_SPEEDS = new float[3] { 12.5f, 50.0f, 22.5f };

    //MainMenu
    public GameObject MainMenu;

    //LoadHandler.cs handles hiding/showing TitleScreenContents
    private void Start()
    {
        PlayTitleAudio();
        StartCoroutine(FadeText());
    }

    // Call SwitchCanvas() if any key is pressed
    private void Update()
    {
        spinRings();

        if (Input.anyKeyDown && TitleScreenContents.activeSelf)
        {
            SwitchToMainMenu();
        }
    }

    public void PlayTitleAudio()
    {
        MusicSource.clip = TitleScreenAudio;
        MusicSource.Play();
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
        TitleScreenContents.SetActive(false);
        MainMenu.SetActive(true);
    }
}