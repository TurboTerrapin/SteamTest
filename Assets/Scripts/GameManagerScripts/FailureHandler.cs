using System.Collections;
using System.Collections.Generic;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEditor.SpeedTree.Importer;
using UnityEngine;

public class FailureHandler : NetworkBehaviour
{
    private static Vector3[] FINAL_SHIP_CHUNK_POSITIONS = new Vector3[] {new Vector3(1.3f, -1.9f, -0.5f), new Vector3(-5.1f, 5.1f, -5.6f), new Vector3(6.9f, 3.8f, -3.5f)};
    private static Quaternion[] FINAL_SHIP_CHUNK_ROTATIONS = new Quaternion[] {new Quaternion(-0.032f, -0.028f, -0.0103f, 0.999f), new Quaternion(-0.031f, 0.033f, 0.034f, 0.998f), new Quaternion(-0.003f, -0.027f, -0.024f, 0.999f)};

    public TMP_Text StarDateText;
    public TMP_Text Report;
    public GameObject FailureHandlerCanvas;
    public CanvasGroup fadeInGroup;
    public CanvasGroup restartGroup;
    public GameObject failureCamera;
    public GameObject bridge;
    public GameObject exteriorPoints;
    public List<Light> engineLights;
    public List<Light> selfLights;
    public GameObject blueLight;
    public GameObject normalShip;
    public GameObject destroyedShip;
    private Material[][] enabledShipMaterials = new Material[3][];
    private Material[][] disabledShipMaterials = new Material[3][];

    public TMP_Text[] playerNames;
    public TMP_Text[] playerVotes;
    private List<ulong> playerSteamIDs = new List<ulong>();
    private List<int> playerStates = new List<int>();
    private bool quitButtonPressed = false;
    public TMP_Text notEnoughPlayersText;

    // lobbyNames is a string table that could have 1-4 entries
    public void displayDeathScreen(List<string> lobbyNames, List<ulong> lobbySteamIDs, int scenario, string msg)
    {
        GameObject localPlayer = GameObject.Find("PlayerManager").GetComponent<PlayerManager>().getLocalPlayer();

        // display destroyed ship
        bridge.SetActive(false);
        exteriorPoints.SetActive(false);
        normalShip.SetActive(false);
        destroyedShip.SetActive(true);

        // switch cameras
        localPlayer.GetComponent<CameraMove>().DeactivateCamera();
        failureCamera.SetActive(true);

        // freeze players
        GameObject.Find("PlayerManager").GetComponent<PlayerManager>().freezeAllPlayers();
        // reset/freeze camera
        localPlayer.transform.GetComponent<CameraMove>().ResetCamera();
        // reset/freeze player
        localPlayer.transform.GetComponent<PlayerMove>().ResetPlayerMove();

        // show UI
        FailureHandlerCanvas.SetActive(true);

        // initialize states to 0
        for (int i = 0; i < 4; i++)
        {
            if (lobbySteamIDs[i] != 0)
            {
                playerSteamIDs.Add(lobbySteamIDs[i]);
                playerStates.Add(0);
            }
        }

        // print report
        StartCoroutine(printReport(lobbyNames, lobbySteamIDs, scenario, msg));
    }

    private void enableDestroyedShipLitElements(GameObject shipWhiteLights)
    {
        shipWhiteLights.GetComponent<MeshRenderer>().material = ReferenceAssistor.Instance.lit_white;
        for (int i = 0; i < 3; i++)
        {
            destroyedShip.transform.GetChild(i).GetComponent<MeshRenderer>().materials = enabledShipMaterials[i];
        }
    }

    private void disableDestroyedShipLitElements(GameObject shipWhiteLights)
    {
        shipWhiteLights.GetComponent<MeshRenderer>().material = ReferenceAssistor.Instance.pure_black;
        for (int i = 0; i < 3; i++)
        {
            destroyedShip.transform.GetChild(i).GetComponent<MeshRenderer>().materials = disabledShipMaterials[i];
        }
    }

    IEnumerator explosionAnimation()
    {
        StartCoroutine(chunkSeperation());
        StartCoroutine(disableLights());
        List<Transform> explosionsToTrigger = new List<Transform>();
        foreach (Transform explosion in destroyedShip.transform.GetChild(3))
        {
            explosionsToTrigger.Add(explosion);
        }
        for (int i = 0; i < explosionsToTrigger.Count; i++)
        {
            int nextExplosion = UnityEngine.Random.Range(0, explosionsToTrigger.Count);
            explosionsToTrigger[nextExplosion].GetComponent<Explosion>().explode(UnityEngine.Random.Range(20.0f, 25.0f));
            explosionsToTrigger.RemoveAt(nextExplosion);
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(1.0f);
    }

    IEnumerator disableLights()
    {
        // disable ship features
        destroyedShip.GetComponent<ShipExteriorFeatures>().ship_engine_circles.gameObject.SetActive(false);
        GameObject shipWhiteLights = destroyedShip.GetComponent<ShipExteriorFeatures>().ship_white_lights;
        Component.Destroy(destroyedShip.GetComponent<ShipExteriorFeatures>());

        // cache materials for light changes
        int[][] litElementIndices = new int[3][] { new int[] { 0, 6, 8, 9, 10, 13 }, new int[] { 0, 5, 8 }, new int[] { 2, 4, 7, 9 } };
        for (int i = 0; i < 3; i++)
        {
            enabledShipMaterials[i] = destroyedShip.transform.GetChild(i).GetComponent<MeshRenderer>().materials;
            disabledShipMaterials[i] = destroyedShip.transform.GetChild(i).GetComponent<MeshRenderer>().materials;
            for (int x = 0; x < litElementIndices[i].Length; x++)
            {
                disabledShipMaterials[i][litElementIndices[i][x]] = ReferenceAssistor.Instance.pure_black;
            }
        }

        for (int i = 0; i < 5; i++)
        {
            enableDestroyedShipLitElements(shipWhiteLights);
            float animTime = 0.1f;
            while (animTime > 0.0f)
            {
                animTime = Mathf.Max(0.0f, animTime - Time.deltaTime);
                float whitePercentage = 1.0f - (animTime / 0.1f);
                Color c = new Color(whitePercentage, whitePercentage, whitePercentage);
                foreach (Light l in selfLights)
                {
                    l.color = c;
                }
                yield return null;
            }
            disableDestroyedShipLitElements(shipWhiteLights);
            animTime = 0.1f;
            while (animTime > 0.0f)
            {
                animTime = Mathf.Max(0.0f, animTime - Time.deltaTime);
                float whitePercentage = animTime / 0.1f;
                Color c = new Color(whitePercentage, whitePercentage, whitePercentage);
                foreach (Light l in selfLights)
                {
                    l.color = c;
                }
                yield return null;
            }
        }

        float anim_time = 2.0f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            foreach (Transform light in blueLight.transform)
            {
                light.GetComponent<Light>().intensity = Mathf.Lerp(0.0f, 0.2f, anim_time / 2.0f);
            }

            Color c = new Color(0.0f, Mathf.Lerp(0.0f, 0.84f, anim_time / 2.0f), anim_time / 2.0f);
            foreach (Light light in engineLights)
            {
                light.color = c;
            }

            yield return null;
        }
    }

    IEnumerator chunkSeperation()
    {
        float anim_time = 5.0f;
        AnimationCurve ac = AnimationCurve.EaseInOut(0.0f, 0.0f, 5.0f, 1.0f);
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            float animation_percentage = ac.Evaluate(anim_time);
            for (int i = 0; i < 3; i++)
            {
                destroyedShip.transform.GetChild(i).transform.localPosition = Vector3.Lerp(FINAL_SHIP_CHUNK_POSITIONS[i], Vector3.zero, animation_percentage);
                destroyedShip.transform.GetChild(i).transform.localRotation = Quaternion.Lerp(FINAL_SHIP_CHUNK_ROTATIONS[i], new Quaternion(0.0f, 0.0f, 0.0f, 1.0f), animation_percentage);
            }

            yield return null;
        }

        float[] rotate_speeds = new float[] { -0.2f, -0.3f, -0.35f };
        Vector3[] transform_adjustments = new Vector3[] { new Vector3(0.1f, 0.1f, 0.05f), new Vector3(-0.8f, -0.1f, -0.3f), new Vector3(0.8f, -0.1f, -0.3f) };
        while (true)
        {
            for (int i = 0; i < 3; i++)
            {
                destroyedShip.transform.GetChild(i).Rotate(rotate_speeds[i] * 0.5f * Time.deltaTime, rotate_speeds[i] * Time.deltaTime, 0.0f);
                destroyedShip.transform.GetChild(i).localPosition += (transform_adjustments[i] * Time.deltaTime);
            }
            yield return null;
        }
    }

    // print star date and message (2-3 sentences)
    IEnumerator printReport(List<string> lobbyNames, List<ulong> lobbySteamIDs, int scenario, string msg)
    {
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(explosionAnimation());
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

        // display player names and default states
        for (int i = 0; i < 4; i++)
        {
            if (i < lobbyNames.Count && !string.IsNullOrEmpty(lobbyNames[i]))
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

    public void handlePlayerStateChange(ulong plrSteamID, int state)
    {
        int plrIndex = playerSteamIDs.IndexOf(plrSteamID);
        if (plrIndex == -1)
        {
            return;
        }

        // Store new state for player
        playerStates[playerSteamIDs.IndexOf(plrSteamID)] = state;

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
        if (NetworkManager.Singleton.IsHost == true)
        {
            int restartVotes = 0;
            for (int i = 0; i < playerStates.Count; i++)
            {
                if (playerStates[i] == 1) // if player is ready
                {
                    restartVotes++;
                }
            }

            // if enough votes, restart game
            if (restartVotes >= NetworkManager.Singleton.ConnectedClients.Count)
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
        // to avoid error
        if (GameNetworkManager.Instance.currentLobby != null && NetworkManager.Singleton != null)
        {
            // change player state to 2 - "Left Lobby"
            playerStateChangeRPC(SteamClient.SteamId, 2);
        }
        PlayerManager.leaveGame();
    }

    public void handleRestartButtonClick()
    {
        // to avoid error
        if (GameNetworkManager.Instance.currentLobby != null && NetworkManager.Singleton != null)
        {
            // change player state to 1 - "Ready"
            playerStateChangeRPC(SteamClient.SteamId, 1);
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
    private void playerStateChangeRPC(ulong plrSteamID, int newState)
    {
        handlePlayerStateChange(plrSteamID, newState);
    }
}
