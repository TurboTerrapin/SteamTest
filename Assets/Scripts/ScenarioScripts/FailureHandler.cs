using UnityEngine;
using System.Collections;
using TMPro;

public class FailureHandler : MonoBehaviour
{
    public TMP_Text StarDateText;
    public TMP_Text Report;
    public CanvasGroup fadeInGroup;
    public CanvasGroup restartGroup;

    public TMP_Text[] playerNames;
    public TMP_Text[] playerVotes;
    private int[] playerStates = new int[4];
    private string[] testNames = new string[4];
    private bool quitButtonPressed = false;
    public TMP_Text notEnoughPlayersText;

    // for testing
    void Start()
    {
        testNames = new string[] {"Beata", "Henryk", "Jake", "John" };
        StartCoroutine(printReport(7));
    }

    // player_names is a string table that could have 1-4 entries.
    public void displayDeathScreen(string[] player_names, int scenario)
    {
        // print report
        //StartCoroutine(printReport(1));
        //StartCoroutine(printReport(scenario));
    }

    // print star date and message (2-3 sentences).
    IEnumerator printReport(int scenario)
    {
        // clear text before printing new text
        StarDateText.text = "";
        Report.text = "";

        // print stardate based on scenario #
        switch (scenario)
        {
            case 1:
                yield return StartCoroutine(printTextCharbyChar(StarDateText, "STAR DATE: 5199.509"));
                break;
            case 2:
                yield return StartCoroutine(printTextCharbyChar(StarDateText, "STAR DATE: 5199.762"));
                break;
            case 3:
                yield return StartCoroutine(printTextCharbyChar(StarDateText, "STAR DATE: 5199.931"));
                break;
            case 4:
                yield return StartCoroutine(printTextCharbyChar(StarDateText, "STAR DATE: 5200.227"));
                break;
            case 5:
                yield return StartCoroutine(printTextCharbyChar(StarDateText, "STAR DATE: 5200.501"));
                break;
            case 6:
                yield return StartCoroutine(printTextCharbyChar(StarDateText, "STAR DATE: 5200.691"));
                break;
            case 7:
                yield return StartCoroutine(printTextCharbyChar(StarDateText, "STAR DATE: 5200.987"));
                break;
            case 8:
                yield return StartCoroutine(printTextCharbyChar(StarDateText, "STAR DATE: 5201.219"));
                break;
            case 9:
                yield return StartCoroutine(printTextCharbyChar(StarDateText, "STAR DATE: 5201.515"));
                break;
            case 10:
                yield return StartCoroutine(printTextCharbyChar(StarDateText, "STAR DATE: 5201.599"));
                break;
        }

        // print report message
        yield return StartCoroutine(printTextCharbyChar(Report, "Stolen ship designated NCC_3002 was discovered adrift in space with severe hull damage. No survivors found and ship has been deemed unsalvageable due to irreparable damage."));

        //set player names and default states
        for (int i = 0; i < testNames.Length; i++)
        {
            playerNames[i].text = testNames[i];
            playerVotes[i].text = "Not Ready";
            playerVotes[i].color = Color.white;
        }
        // fade in restart button, quit button, player names, and their votes
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(fadeGroup(fadeInGroup, 1f, 2f));
    }

    IEnumerator printTextCharbyChar(TMP_Text TargetText, string FullText)
    {
        TargetText.text = "";
        TargetText.color = Color.cyan;
        foreach (char c in FullText)
        {
            TargetText.text += c;
            yield return new WaitForSeconds(0.03f);
        }
    }

    // fade in restart button, quit button, player names, and their votes
    IEnumerator fadeGroup(CanvasGroup group, float targetAlpha, float duration)
    {
        float startAlpha = group.alpha;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            group.alpha = alpha;
            yield return null;
        }

        group.alpha = targetAlpha;
    }
    
    public void playerStateChange(int plr_index, int state)
    {
        // Store new state for player
        playerStates[plr_index] = state;

        // Update text for specific player based on their state
        switch (state)
        {
            case 0:
                playerVotes[plr_index].text = "Not Ready";
                playerVotes[plr_index].color = Color.white;
                break;
            case 1:
                playerVotes[plr_index].text = "Ready";
                playerVotes[plr_index].color = Color.cyan;
                break;
            case 2:
                playerVotes[plr_index].text = "Left Lobby";
                playerVotes[plr_index].color = Color.red;

                // on first player who quits, display "not enough players" and fade restart button.
                if (quitButtonPressed == false)
                {
                    quitButtonPressed = true;
                    StartCoroutine(FadeText(notEnoughPlayersText, 1f, 0.5f));
                    StartCoroutine(fadeGroup(restartGroup, 0.3f, 0.5f));
                    restartGroup.interactable = false;
                    restartGroup.blocksRaycasts = false;
                }
                break;
        }
    }

    // Fade in "not enough players"
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

    public void handleQuitButtonClick()
    {
        // change player state to 2 - "Left Lobby"
        //playerStateChange(plrIndex, 2)
    }

    public void handleRestartButtonClick()
    {
        // change player state to 1 - "Ready"
        //playerStateChange(plrIndex, 1)
    }
}
