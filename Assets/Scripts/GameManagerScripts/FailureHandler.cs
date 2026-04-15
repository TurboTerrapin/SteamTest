using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class FailureHandler : NetworkBehaviour
{
    public TMP_Text StarDateText;
    public TMP_Text Report;
    public GameObject FailureHandlerCanvas;
    public CanvasGroup fadeInGroup;
    public CanvasGroup restartGroup;

    public TMP_Text[] playerNames;
    public TMP_Text[] playerVotes;
    private int[] playerStates = new int[4] { 0, 0, 0, 0 };
    private bool quitButtonPressed = false;
    public TMP_Text notEnoughPlayersText;

    // lobbyNames is a string table that could have 1-4 entries
    public void displayDeathScreen(string[] lobbyNames, int scenario, string msg)
    {
        GameObject localPlayer = GameObject.Find("PlayerManager").GetComponent<PlayerManager>().getLocalPlayer();

        GameObject failureCamera = transform.GetChild(0).gameObject;

        // switch cameras
        localPlayer.GetComponent<CameraMove>().DeactivateCamera();
        failureCamera.SetActive(true);

        // freeze players
        GameObject.Find("PlayerManager").GetComponent<PlayerManager>().freezeAllPlayers();
        // reset/freeze camera
        localPlayer.transform.GetComponent<CameraMove>().ResetCamera();
        // reset/freeze player
        localPlayer.transform.GetComponent<PlayerMove>().resetPlayerMove();

        // show UI
        FailureHandlerCanvas.SetActive(true);

        // print report
        StartCoroutine(printReport(lobbyNames, scenario, msg));
    }

    // print star date and message (2-3 sentences)
    IEnumerator printReport(string[] lobbyNames, int scenario, string msg)
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
        yield return StartCoroutine(printTextCharbyChar(Report, msg));

        //set player names and default states
        for (int i = 0; i < playerNames.Length; i++)
        {

            if (i < lobbyNames.Length && !string.IsNullOrEmpty(lobbyNames[i]))
            {
                playerNames[i].text = lobbyNames[i];

                playerVotes[i].text = "Not Ready";
                playerVotes[i].color = Color.white;


            }
            else
            {
                playerNames[i].text = "";
                playerVotes[i].text = "";
            }
        }
        // fade in restart button, quit button, player names, and their votes
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(fadeGroup(fadeInGroup, 1f, 2f));

        ////set player names and default states
        //for (int i = 0; i < playerNames.Length; i++)
        //{
        //    string name = lobbyNames[i];

        //    if (!string.IsNullOrEmpty(name))
        //    {
        //        playerNames[i].text = name;
        //        playerVotes[i].text = "Not Ready";
        //        playerVotes[i].color = Color.white;
        //    }
        //}
        //// fade in restart button, quit button, player names, and their votes
        //yield return new WaitForSeconds(0.5f);
        //StartCoroutine(fadeGroup(fadeInGroup, 1f, 2f));
    }

    IEnumerator printTextCharbyChar(TMP_Text targetText, string fullText)
    {
        targetText.text = "";
        targetText.color = Color.cyan;
        foreach (char c in fullText)
        {
            targetText.text += c;
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
    
    public void handlePlayerStateChange(int plrIndex, int state)
    {
        // Store new state for player
        playerStates[plrIndex] = state;

        // Update text for specific player based on their state
        switch (state)
        {
            case 0:
                playerVotes[plrIndex].text = "Not Ready";
                playerVotes[plrIndex].color = Color.white;
                break;
            case 1:
                playerVotes[plrIndex].text = "Ready";
                playerVotes[plrIndex].color = Color.cyan;
                break;
            case 2:
                playerVotes[plrIndex].text = "Left Lobby";
                playerVotes[plrIndex].color = Color.red;

                // on first player who quits, display "not enough players" and fade restart button
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

        // check if enough votes for restart
        if (NetworkManager.Singleton.IsHost)
        {
            int restartVotes = 0;
            for (int i = 0; i < 4; i++)
            {
                if (playerStates[i] == 1) // if player is ready
                {
                    restartVotes++;
                }
            }

            // if enough votes, restart game
            if (restartVotes >= GameObject.Find("PlayerManager").GetComponent<PlayerManager>().getNumStartingPlayers())
            {
                restartGameRPC();
            }
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
        int plrIndex = GameObject.Find("PlayerManager").GetComponent<PlayerManager>().getPlayerIndex();
        if (plrIndex >= 0 && NetworkManager.Singleton != null) // to avoid error
        {
            playerStateChangeRPC(plrIndex, 2);
        }
        PlayerManager.leaveGame();
    }

    public void handleRestartButtonClick()
    {
        // change player state to 1 - "Ready"
        int plrIndex = GameObject.Find("PlayerManager").GetComponent<PlayerManager>().getPlayerIndex();
        if (plrIndex >= 0 && NetworkManager.Singleton != null) // to avoid error
        {
            playerStateChangeRPC(plrIndex, 1);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void restartGameRPC()
    {
        // destroys everything except NetworkManager
        PlayerManager.clearDontDestroyOnLoads();
        // begin loading
        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().startLoad();
        // if host, finish reset of BridgeEnvironment to start the loop from the start
        if (NetworkManager.Singleton.IsHost)
        {
            SceneSwapper.Instance.ChangeScene("BridgeEnvironment", 0);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void playerStateChangeRPC(int plrIndex, int newState)
    {
        handlePlayerStateChange(plrIndex, newState);
    }
}
