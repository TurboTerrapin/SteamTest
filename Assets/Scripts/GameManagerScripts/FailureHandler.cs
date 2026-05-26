/*
    FailureHandler.cs
    - Handles the post-mission failure screen
    - Handles the animations of the ship exploding or getting captured
    - Handles restart voting
    Contributor(s): Beata Musial, Jake Schott
    Last Updated: 5/25/2026
*/

using System.Collections;
using System.Collections.Generic;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class FailureHandler : NetworkBehaviour
{
    private static Vector3[] FINAL_SHIP_CHUNK_POSITIONS = new Vector3[] {new Vector3(1.3f, -1.9f, -0.5f), new Vector3(-5.1f, 5.1f, -5.6f), new Vector3(6.9f, 3.8f, -3.5f)};
    private static Quaternion[] FINAL_SHIP_CHUNK_ROTATIONS = new Quaternion[] {new Quaternion(-0.032f, -0.028f, -0.0103f, 0.999f), new Quaternion(-0.031f, 0.033f, 0.034f, 0.998f), new Quaternion(-0.003f, -0.027f, -0.024f, 0.999f)};
    private static int MINIMUM_PLAYERS_TO_RESTART = 1;

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
    public GameObject failureShip;
    private Material[][] enabledShipMaterials = new Material[4][];
    private Material[][] disabledShipMaterials = new Material[4][];
    private LobbyHandler lobbyHandler;

    public TMP_Text[] playerNames;
    public TMP_Text[] playerVotes;
    public TMP_Text notEnoughPlayersText;
    private List<ulong> playerSteamIDs = new List<ulong>();
    private List<int> playerStates = new List<int>();
    private bool lobbyDisconnected = false;

    // lobbyNames is a string list that could have 1-4 entries
    public void displayDeathScreen(List<string> lobbyNames, List<ulong> lobbySteamIDs, int scenario, string msg, bool caught)
    {
        GameObject localPlayer = ReferenceAssistor.Instance.player_manager.getLocalPlayer();
        GameObject lh = GameObject.Find("LobbyHandler");
        if (lh != null) 
        { 
            lobbyHandler = lh.GetComponent<LobbyHandler>();
        }

        // display fail ship
        bridge.SetActive(false);
        exteriorPoints.SetActive(false);
        normalShip.SetActive(false);
        failureShip.SetActive(true);

        // switch cameras
        localPlayer.GetComponent<CameraMove>().DeactivateCamera();
        failureCamera.SetActive(true);

        // freeze players
        ReferenceAssistor.Instance.player_manager.freezeAllPlayers();
        // reset/freeze camera
        localPlayer.GetComponent<CameraMove>().ResetCamera();
        // reset/freeze player
        localPlayer.GetComponent<PlayerMove>().ResetPlayerMove();

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
        StartCoroutine(printReport(lobbyNames, lobbySteamIDs, scenario, msg, caught));
    }

    // play animation then print star date and message (2-3 sentences)
    IEnumerator printReport(List<string> lobbyNames, List<ulong> lobbySteamIDs, int scenario, string msg, bool caught)
    {
        // give a one-second delay
        yield return new WaitForSeconds(1.0f);

        // play explosion animation if not caught, otherwise play caught animation
        if (caught == false)
        {
            yield return StartCoroutine(explosionAnimation());
        }
        else
        {
            yield return StartCoroutine(caughtAnimation());
        }

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
        yield return StartCoroutine(fadeGroup(fadeInGroup, 1f, 2f));

        // check if there are enough players for restart
        checkForRestart();
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

        // store new state for player
        playerStates[playerSteamIDs.IndexOf(plrSteamID)] = state;

        // update text for specific player based on their state
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
                break;
        }

        // check for restart
        checkForRestart();
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
        PlayerManager.leaveGame();
    }

    public void handleRestartButtonClick()
    {
        // to avoid error
        if (GameNetworkManager.Instance.currentLobby != null && NetworkManager.Singleton != null)
        {
            // change player state to 1 - "Ready"
            playerVoteRestartRPC(SteamClient.SteamId);
        }
    }

    private void checkForRestart()
    {
        int restartVotes = 0;
        for (int i = 0; i < playerStates.Count; i++)
        {
            if (playerStates[i] == 1) // if player is ready
            {
                restartVotes++;
            }
        }

        if (lobbyDisconnected == false && lobbyHandler != null && lobbyHandler.getNumberOfPlayersInNetworkManagerLobby() < MINIMUM_PLAYERS_TO_RESTART)
        {
            disableRestart();
            notEnoughPlayersText.color = Color.red;
            notEnoughPlayersText.SetText("NOT ENOUGH PLAYERS");
        }

        // check if enough votes for restart
        if (NetworkManager.Singleton.IsHost == true)
        {
            // if everyone in lobby says yes, restart game
            if (lobbyHandler != null && restartVotes >= lobbyHandler.getNumberOfPlayersInNetworkManagerLobby() && restartVotes >= MINIMUM_PLAYERS_TO_RESTART)
            {
                restartGameRPC();
            }
        }
    }

    private void disableRestart()
    {
        if (restartGroup.interactable == false)
        {
            return;
        }
        StartCoroutine(FadeText(notEnoughPlayersText, 1f, 0.5f));
        StartCoroutine(fadeGroup(restartGroup, 0.3f, 0.5f));
        restartGroup.interactable = false;
        restartGroup.blocksRaycasts = false;
    }

    private void displayLobbyDisconnected()
    {
        lobbyDisconnected = true;

        // overlay lobby disconnected
        notEnoughPlayersText.color = Color.red;
        notEnoughPlayersText.SetText("LOBBY DISCONNECTED");

        // disable ability to restart
        disableRestart();

        // show unknown for every player except us, which is left lobby
        for (int i = 0; i < playerSteamIDs.Count; i++)
        {
            handlePlayerStateChange(playerSteamIDs[i], 2);
            if (playerSteamIDs[i] != SteamClient.SteamId)
            {
                playerVotes[i].SetText("Unknown");
            }
        }
    }

    // called when there is a change to the lobby
    public void handleLobbyChange(bool deadLobby)
    {
        if (lobbyDisconnected == true)
        {
            return;
        }

        if (deadLobby == true)
        {
            displayLobbyDisconnected();
            return;
        }

        if (lobbyHandler == null)
        {
            return;
        }

        List<ulong> steamIDsConnected = lobbyHandler.getPlayerSteamIDsInLobby();
        for (int i = 0; i < playerSteamIDs.Count; i++)
        {
            if (steamIDsConnected.Contains(playerSteamIDs[i]) == false && playerStates[i] != 2)
            {
                if (i == 0)
                {
                    displayLobbyDisconnected();
                    return;
                }
                handlePlayerStateChange(playerSteamIDs[i], 2);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void restartGameRPC()
    {
        // destroys everything except NetworkManager
        PlayerManager.clearDontDestroyOnLoads();
        // begin loading animation
        GameObject.Find("LoadHandler").GetComponent<LoadHandler>().startLoad();
        // if host, finish reset of BridgeEnvironment to start the loop from the start
        if (NetworkManager.Singleton.IsHost == true)
        {
            SceneSwapper.Instance.ChangeScene("BridgeEnvironment", 0);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void playerVoteRestartRPC(ulong plrSteamID)
    {
        handlePlayerStateChange(plrSteamID, 1);
    }

    private void enablefailureShipLitElements(GameObject shipWhiteLights, GameObject shipRadar)
    {
        shipWhiteLights.GetComponent<MeshRenderer>().material = ReferenceAssistor.Instance.lit_white;
        shipRadar.GetComponent<MeshRenderer>().materials = enabledShipMaterials[3];
        for (int i = 0; i < 3; i++)
        {
            failureShip.transform.GetChild(i).GetComponent<MeshRenderer>().materials = enabledShipMaterials[i];
        }
    }

    private void disablefailureShipLitElements(GameObject shipWhiteLights, GameObject shipRadar)
    {
        shipWhiteLights.GetComponent<MeshRenderer>().material = ReferenceAssistor.Instance.pure_black;
        shipRadar.GetComponent<MeshRenderer>().materials = disabledShipMaterials[3];
        for (int i = 0; i < 3; i++)
        {
            failureShip.transform.GetChild(i).GetComponent<MeshRenderer>().materials = disabledShipMaterials[i];
        }
    }

    // handles animation for when ship gets caught
    IEnumerator caughtAnimation()
    {
        yield return StartCoroutine(disableLights(2));
        for (int i = 0; i < 3; i++)
        {
            failureShip.transform.GetChild(i).GetComponent<Renderer>().renderingLayerMask = (uint)LayerMask.GetMask("Default");
        }
        failureShip.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().renderingLayerMask = (uint)LayerMask.GetMask("Default");
        foreach (Transform light in blueLight.transform)
        {
            light.GetComponent<Light>().intensity = 0.2f;
        }
        StartCoroutine(shuttleSwarm());
    }

    // handles animation for when ship explodes on failure
    IEnumerator explosionAnimation()
    {
        StartCoroutine(disableLights(5));
        StartCoroutine(chunkSeperation());
        List<Transform> explosionsToTrigger = new List<Transform>();
        foreach (Transform explosion in failureShip.transform.GetChild(3))
        {
            explosionsToTrigger.Add(explosion);
        }
        for (int i = 0; i < explosionsToTrigger.Count; i++)
        {
            int nextExplosion = UnityEngine.Random.Range(0, explosionsToTrigger.Count);
            explosionsToTrigger[nextExplosion].GetComponent<Explosion>().explode(UnityEngine.Random.Range(4.0f, 8.0f));
            explosionsToTrigger.RemoveAt(nextExplosion);
            yield return new WaitForSeconds(0.15f);
        }
        yield return new WaitForSeconds(1.0f);
    }

    // flickers lights then turns them off for good on failure ship model
    IEnumerator disableLights(int flickers)
    {
        // disable ship features
        failureShip.GetComponent<ShipExteriorFeatures>().ship_engine_circles.gameObject.SetActive(false);
        GameObject shipWhiteLights = failureShip.GetComponent<ShipExteriorFeatures>().ship_white_lights;
        GameObject shipRadar = failureShip.GetComponent<ShipExteriorFeatures>().ship_radar_dish;
        Component.Destroy(failureShip.GetComponent<ShipExteriorFeatures>());

        // cache materials for light changes
        int[][] litElementIndices = new int[3][] { new int[] { 0, 6, 8, 9, 10, 13 }, new int[] { 0, 5, 8 }, new int[] { 2, 4, 7, 9 } };
        for (int i = 0; i < 3; i++)
        {
            enabledShipMaterials[i] = failureShip.transform.GetChild(i).GetComponent<MeshRenderer>().materials;
            disabledShipMaterials[i] = failureShip.transform.GetChild(i).GetComponent<MeshRenderer>().materials;
            for (int x = 0; x < litElementIndices[i].Length; x++)
            {
                disabledShipMaterials[i][litElementIndices[i][x]] = ReferenceAssistor.Instance.pure_black;
            }
        }
        enabledShipMaterials[3] = shipRadar.GetComponent<MeshRenderer>().materials;
        disabledShipMaterials[3] = shipRadar.GetComponent<MeshRenderer>().materials;
        disabledShipMaterials[3][2] = ReferenceAssistor.Instance.pure_black;
        disabledShipMaterials[3][3] = ReferenceAssistor.Instance.pure_black;

        // flicker lights
        for (int i = 0; i < flickers; i++)
        {
            enablefailureShipLitElements(shipWhiteLights, shipRadar);
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
            disablefailureShipLitElements(shipWhiteLights, shipRadar);
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

        // dim engine lights and overall lighting
        float dimTime = 2.0f;
        while (dimTime > 0.0f)
        {
            dimTime = Mathf.Max(0.0f, dimTime - Time.deltaTime);

            foreach (Transform light in blueLight.transform)
            {
                light.GetComponent<Light>().intensity = Mathf.Lerp(0.0f, 0.2f, dimTime / 2.0f);
            }

            Color c = new Color(0.0f, Mathf.Lerp(0.0f, 0.84f, dimTime / 2.0f), dimTime / 2.0f);
            foreach (Light light in engineLights)
            {
                light.color = c;
            }

            yield return null;
        }
    }

    // separates chip shunks of failure ship model as part of explosion animation
    IEnumerator chunkSeperation()
    {
        float animTime = 8.0f;
        AnimationCurve ac = AnimationCurve.Linear(0.0f, 0.0f, 8.0f, 1.0f);

        // animation the ship initially separating
        while (animTime > 0.0f)
        {
            animTime = Mathf.Max(0.0f, animTime - Time.deltaTime);

            float animationPercentage = ac.Evaluate(animTime);
            for (int i = 0; i < 3; i++)
            {
                failureShip.transform.GetChild(i).transform.localPosition = Vector3.Lerp(FINAL_SHIP_CHUNK_POSITIONS[i], Vector3.zero, animationPercentage);
                failureShip.transform.GetChild(i).transform.localRotation = Quaternion.Lerp(FINAL_SHIP_CHUNK_ROTATIONS[i], new Quaternion(0.0f, 0.0f, 0.0f, 1.0f), animationPercentage);
            }

            yield return null;
        }

        // push the ship parts away in perpetuity
        float[] rotationSpeeds = new float[] { -0.2f, -0.3f, -0.35f };
        Vector3[] transformAdjustments = new Vector3[] { new Vector3(0.1f, 0.1f, 0.05f), new Vector3(-0.8f, -0.1f, -0.3f), new Vector3(0.8f, -0.1f, -0.3f) };
        while (true)
        {
            for (int i = 0; i < 3; i++)
            {
                failureShip.transform.GetChild(i).Rotate(rotationSpeeds[i] * 0.5f * Time.deltaTime, rotationSpeeds[i] * Time.deltaTime, 0.0f);
                failureShip.transform.GetChild(i).localPosition += (transformAdjustments[i] * Time.deltaTime);
            }
            yield return null;
        }
    }

    // flies in the four shuttles after capture
    IEnumerator shuttleSwarm()
    {
        // initialize shuttle transform adjustment information
        Vector3[] startingShuttlePositions = new Vector3[4];
        Quaternion[] startingShuttleRotations = new Quaternion[4];
        Vector3[] finalShuttlePositions = new Vector3[4] { new Vector3(5.8f, -88.9f, 26.7f), new Vector3(76.1f, -87.4f, -7.3f), new Vector3(58.5f, 38.8f, 36.8f), new Vector3(91.4f, 3.4f, -20.3f) };
        Quaternion[] finalShuttleRotations = new Quaternion[4] { new Quaternion(0.08159f, 0.071f, 0.9215f, 0.3728f), new Quaternion(-0.0308f, 0.12f, 0.9009f, -0.4158f), new Quaternion(0.1093f, 0.1159f, -0.0321f, 0.9866f), new Quaternion(-0.1001f, -0.0365f, -0.3044f, 0.9465f) };

        failureShip.transform.GetChild(4).gameObject.SetActive(true);
        for (int i = 0; i < 4; i++)
        {
            startingShuttlePositions[i] = failureShip.transform.GetChild(4).GetChild(i).localPosition;
            startingShuttleRotations[i] = failureShip.transform.GetChild(4).GetChild(i).localRotation;
            failureShip.transform.GetChild(4).GetChild(i).GetComponent<ShuttleExteriorFeatures>().activateSpotlight(2.0f);
        }

        // randomize the shuttle arrival times
        List<float> possibleLengths = new List<float>() { 10.0f, 8.0f, 6.0f, 5.0f };
        float maxTime = possibleLengths[0];
        float[] animationLengths = new float[4];
        AnimationCurve[] animationCurves = new AnimationCurve[4];
        for (int i = 0; i < 4; i++)
        {
            animationLengths[i] = possibleLengths[Random.Range(0, possibleLengths.Count)];
            possibleLengths.Remove(animationLengths[i]);
            animationCurves[i] = AnimationCurve.EaseInOut(0.0f, 0.0f, animationLengths[i], 1.0f);
        }

        // animate the shuttles
        float animTime = 0.0f;
        while (animTime < maxTime)
        {
            animTime = Mathf.Min(maxTime, animTime + Time.deltaTime);

            for (int i = 0; i < 4; i++)
            {
                float animationPercentage = animationCurves[i].Evaluate(Mathf.Min(animationLengths[i], animTime));
                failureShip.transform.GetChild(4).GetChild(i).localPosition = Vector3.Lerp(startingShuttlePositions[i], finalShuttlePositions[i], animationPercentage);
                failureShip.transform.GetChild(4).GetChild(i).localRotation = Quaternion.Lerp(startingShuttleRotations[i], finalShuttleRotations[i], animationPercentage);
            }

            yield return null;
        }
    }
}