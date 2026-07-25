using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class TitleScreenController : MonoBehaviour
{
    //TitleScreen
    public TMP_Text PressAnyText;
    public Image JourneyToText;
    public Image DeepSpaceFiveText;
    public Image Border;
    public TMP_Text VersionLabel;
    public float FadeDuration = 1.5f; // Time for a full fade in/out
    public GameObject TitleScreenContents;
    private bool PressAnyTextAppears = false;

    //Audio
    [SerializeField] AudioSource MusicSource;
    public AudioClip TitleScreenAudio;
    public AudioClip SFXTest;

    // Rings
    public GameObject SpinCircle;
    public GameObject SpriteMask;
    public static float[] SPIN_SPEEDS = new float[3] { 12.5f, 50.0f, 22.5f };
    public SpriteRenderer[] RingSprites;

    //MainMenu
    public GameObject MainMenu;

    //LoadHandler.cs handles hiding/showing TitleScreenContents
    private void Start()
    {
        // Set alpha of everything to 0
        SetRingAlpha(0f);
        JourneyToText.color = new Color(1, 1, 1, 0); // transparent
        DeepSpaceFiveText.color = new Color(1, 1, 1, 0);
        Border.color = new Color(1, 1, 1, 0);
        PressAnyText.alpha = 0f;
        VersionLabel.alpha = 0f;

        StartCoroutine(IntroSequence());
    }

    // Call SwitchCanvas() if any key is pressed
    private void Update()
    {
        spinRings();

        if (Input.anyKeyDown && PressAnyTextAppears && TitleScreenContents.activeSelf)
        {
            SwitchToMainMenu();
            VersionLabel.alpha = 0f;
        }
    }

    IEnumerator IntroSequence()
    {
        yield return StartCoroutine(FadeTo(JourneyToText, 1f, 2f));

        StartCoroutine(FadeTo(DeepSpaceFiveText, 1f, 2f));

        yield return StartCoroutine(FadeRings(1f, 2f));

        StartCoroutine(FadeTo(Border, 1f, 2f));

        StartCoroutine(FadeTo(VersionLabel, 1f, 2f));

        StartCoroutine(FadePressAnyButtonText(PressAnyText));

        PressAnyTextAppears = true;

    }

    IEnumerator FadePressAnyButtonText(TMP_Text text)
    {
        while (true)
        {
            yield return StartCoroutine(FadeTo(text, 1f, FadeDuration)); // Fade in
            yield return StartCoroutine(FadeTo(text, 0f, FadeDuration)); // Fade out
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

    IEnumerator FadeRings(float targetAlpha, float duration)
    {
        float startAlpha = RingSprites[0].color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            
            SetRingAlpha(alpha);

            yield return null;
        }

        SetRingAlpha(targetAlpha);

    }

    public void SetRingAlpha(float alpha)
    {
        foreach (SpriteRenderer ring in RingSprites)
        {
            Color color = ring.color;
            color.a = alpha;
            ring.color = color;
        }

    }


    // Both Image and TMP_Text inherit from Graphic
    IEnumerator FadeTo(Graphic graphic, float targetAlpha, float duration)
    {
        Color color = graphic.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime * 0.8f;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            graphic.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        graphic.color = new Color(color.r, color.g, color.b, targetAlpha);
    }

    private void SwitchToMainMenu()
    {
        TitleScreenContents.SetActive(false);
        MainMenu.SetActive(true);
    }
}