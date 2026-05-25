/*
    LoadHandler.cs
    - Handles loading into BridgeEnvironment, scenarios, TitleScreen (after quitting)
    - Handles displaying disconnection and connecting (...) screens
    - Handles Steam checks
    Contributor(s): Jake Schott, Beata Musial
    Last Updated: 5/24/2026
*/

using System.Collections;
using System.Collections.Generic;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadHandler : MonoBehaviour
{
    //LOAD CIRCLE SETTINGS
    private static float[] SPIN_SPEEDS = new float[3] { 50.0f, 200.0f, 70.0f };

    private GameObject load_screen;
    private GameObject load_ring;
    private GameObject connecting_box;
    private GameObject connection_lost;
    private GameObject steam_failure;
    private GameObject dummy_camera;
    private AsyncOperation load_operation = null;
    private Coroutine fade_black_coroutine = null;
    private Coroutine connecting_coroutine = null;
    private List<Coroutine> load_coroutines = new List<Coroutine>();

    private void Start()
    {
        //used to ensure there is ever only one LoadHandler
        transform.name = "TempLoadHandler";
        GameObject already_existing_load_handler = GameObject.Find("LoadHandler");
        if (already_existing_load_handler != null)
        {
            if (already_existing_load_handler.GetComponent<LoadHandler>().isLoading() == true) //end the load of the other LoadHandler if loading back into TitleScreen
            {
                already_existing_load_handler.GetComponent<LoadHandler>().endLoad(false);
                already_existing_load_handler.GetComponent<LoadHandler>().returnToMainMenu();
                already_existing_load_handler.GetComponent<LoadHandler>().titleScreenSteamCheck();
            }
            GameObject.Destroy(gameObject);
            return;
        }
        transform.name = "LoadHandler";

        load_screen = transform.GetChild(0).gameObject;
        load_ring = load_screen.transform.GetChild(2).gameObject;
        connecting_box = transform.GetChild(1).gameObject;
        connection_lost = transform.GetChild(2).gameObject;
        steam_failure = transform.GetChild(3).gameObject;
        dummy_camera = transform.GetChild(4).gameObject;

        //check for Steam at beginning, and if successful, show title screen
        GameObject.Find("TitleScreen").GetComponent<TitleScreenController>().TitleScreenContents.SetActive(titleScreenSteamCheck() == true);

        DontDestroyOnLoad(gameObject);
    }

    //checks if in TitleScreen and if so aborts everything if not connected to Steam
    public bool titleScreenSteamCheck()
    {
        if (SceneManager.GetActiveScene().name.Equals("TitleScreen") == true && connection_lost.activeSelf == false)
        {
            if (SteamClient.IsValid == false || SteamClient.IsLoggedOn == false)
            {
                steam_failure.SetActive(true);
                hideTitleAndMainMenuElements();
                return false;
            }
        }
        return true;
    }

    //called by CampaignLobbyController.cs and FriendJoinWithButton.cs to ensure that handleSceneLoad() gets linked to the right NetworkManager
    public void linkNetworkManager()
    {
        StartCoroutine(yieldForNetworkSceneManager());
    }

    //waits until NetworkManager's SceneManager is available, then links to handleSceneLoad on any scene being loaded (by NetworkManager)
    IEnumerator yieldForNetworkSceneManager()
    {
        while (NetworkManager.Singleton.SceneManager == null)
        {
            yield return null;
        }
        NetworkManager.Singleton.SceneManager.OnLoad += handleSceneLoad;
    }

    //stops all coroutines
    private void resetAllCoroutines()
    {
        if (fade_black_coroutine != null)
        {
            StopCoroutine(fade_black_coroutine);
            fade_black_coroutine = null;
        }
        if (connecting_coroutine != null)
        {
            StopCoroutine(connecting_coroutine);
            connecting_coroutine = null;
        }
        foreach (Coroutine c in load_coroutines)
        {
            StopCoroutine(c);
        }
        load_coroutines.Clear();
    }

    //only called when NetworkManager.Singleton.SceneManager changes the scene
    private void handleSceneLoad(ulong client_id, string scene_name, LoadSceneMode load_scene_mode, AsyncOperation async_operation)
    {
        resetAllCoroutines();
        load_operation = async_operation;
        if (scene_name == "BridgeEnvironment") //BridgeEnvironment load-in
        {
            load_coroutines.Add(StartCoroutine(loadBridgeEnvironment()));
        }
        else //scenario load-in
        {
            load_coroutines.Add(StartCoroutine(loadScenarioTransition()));
        }
    }

    //will begin the infinite loading screen (until terminated by endLoad() or the loading of a specific scene)
    public void startLoad()
    {
        resetAllCoroutines();
        randomizeColors();
        load_coroutines.Add(StartCoroutine(loadLoop()));
        load_screen.SetActive(true);
    }

    //returns true if loading at least one scene/scenario
    public bool isLoading()
    {
        return load_coroutines.Count > 0;
    }

    //if currently on TitleScreen scene, hide all elements
    private void hideTitleAndMainMenuElements()
    {
        if (SceneManager.GetActiveScene().name == "TitleScreen")
        {
            GameObject main_menu = GameObject.Find("MainMenuCanvas");
            for (int i = 0; i < main_menu.transform.childCount; i++)
            {
                main_menu.transform.GetChild(i).gameObject.SetActive(false);
            }
            GameObject.Find("TitleScreen").GetComponent<TitleScreenController>().TitleScreenContents.SetActive(false);
        }
    }

    //will begin the connecting screen (until terminated by endConnecting())
    public void startConnecting()
    {
        //check if already connecting
        if (connecting_coroutine != null)
        {
            return;
        }

        hideTitleAndMainMenuElements();
        resetAllCoroutines();
        connecting_coroutine = StartCoroutine(connectingLoop());
        connecting_box.SetActive(true);
    }

    //will end the connecting screen
    public void endConnecting()
    {
        resetAllCoroutines();
        connecting_box.SetActive(false);

        if (SceneManager.GetActiveScene().name == "TitleScreen")
        {
            GameObject main_menu = GameObject.Find("MainMenuCanvas");
            main_menu.transform.GetChild(2).gameObject.SetActive(true);
        }
    }

    //terminates the loading screen
    public void endLoad(bool fade)
    {
        dummy_camera.SetActive(false);
        if (load_coroutines.Count == 0)
        {
            return;
        }

        resetAllCoroutines();
        if (fade == true)
        {
            fade_black_coroutine = StartCoroutine(fadeBlackScreen(1.0f));
        }
        else
        {
            load_screen.SetActive(false);
        }
    }

    IEnumerator lostConnectionDisplayer(string message)
    {
        while (load_operation != null && load_operation.isDone == false)
        {
            yield return null;
        }

        if (load_coroutines.Count > 0) //if currently loading into BridgeEnvironment, loading a scene, or transitioning between scenes
        {
            GameObject.Destroy(NetworkManager.Singleton.gameObject);
            PlayerManager.clearDontDestroyOnLoads();
            SceneManager.LoadScene("TitleScreen", LoadSceneMode.Single);
            startLoad();
            while (SceneManager.GetActiveScene().name != "TitleScreen") //get back to TitleScreen
            {
                yield return null;
            }
            endLoad(false);
        }
        else if (SceneManager.GetActiveScene().name != "TitleScreen") //currently playing in an active session
        {
            if (ReferenceAssistor.Instance != null && ReferenceAssistor.Instance.failure_handler.failureCamera.activeSelf == true)
            {
                //if in failure state let the failure handler know then do nothing after
                ReferenceAssistor.Instance.failure_handler.handleLobbyChange(true);
                yield break;
            }
            PrimaryScript.Instance.unpause(); //forces unpause
            PrimaryScript.Instance.deactivate(false, true); //stops control interaction
            ReferenceAssistor.Instance.audio_manager.GetComponent<AudioManager>().MuteAudio(); //mute SFX
            dummy_camera.SetActive(true);
        }
        hideTitleAndMainMenuElements();
        resetAllCoroutines();

        connection_lost.transform.GetChild(3).GetComponent<TMP_Text>().SetText(message + " Please return to the main menu.");
        connecting_box.SetActive(false);
        connection_lost.SetActive(steam_failure.activeSelf == false);
    }

    public void displayLostConnection(string message)
    {
        if (connection_lost.activeSelf == true)
        {
            return;
        }

        StartCoroutine(lostConnectionDisplayer(message));
    }

    //called when clicking the main menu button on connection lost screen
    public void returnToMainMenu()
    {
        connection_lost.SetActive(false);
        if (SceneManager.GetActiveScene().name.Equals("TitleScreen") == true)
        {
            GameObject.Find("MainMenuCanvas").transform.GetChild(0).gameObject.SetActive(titleScreenSteamCheck() == true);
        }
        else
        {
            PlayerManager.leaveGame();
        }
    }

    //randomizes the colors for the load circle
    private void randomizeColors()
    {
        //only randomize colors if load screen hasn't been shown yet
        if (load_screen.activeSelf == true)
        {
            return;
        }
        List<int> possible_colors = new List<int> { 0, 1, 2, 3 };
        for (int i = 0; i < 3; i++)
        {
            int c = Random.Range(0, possible_colors.Count);
            load_ring.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = ReferenceAssistor.COLOR_OPTIONS[possible_colors[c]];
            possible_colors.RemoveAt(c);
        }
    }

    //helper method used to spin the rings on a single frame (yield return null)
    private void spinRings()
    {
        for (int i = 0; i < 3; i++)
        {
            float z = load_ring.transform.GetChild(i).GetComponent<RectTransform>().rotation.eulerAngles.z + SPIN_SPEEDS[i] * Time.deltaTime;
            load_ring.transform.GetChild(i).GetComponent<RectTransform>().rotation = Quaternion.Euler(0.0f, 0.0f, z);
        }
    }

    //links to the quit button in the Steam connection failure
    public void handleQuitButtonClick()
    {
        Application.Quit();
    }

    //handles the ... animation for connecting
    IEnumerator connectingLoop()
    {
        TMP_Text connecting_text = connecting_box.transform.GetChild(1).GetComponent<TMP_Text>();
        float elapsed_time = 0.0f;
        while (elapsed_time < GameNetworkManager.CONNECTION_TIMEOUT_PERIOD)
        {
            elapsed_time += Time.deltaTime;
            string elipse = "";
            for (int i = 0; i < 4; i++)
            {
                connecting_text.SetText("CONNECTING" + elipse);
                yield return new WaitForSeconds(0.25f);
                elapsed_time += 0.25f;
                elipse += ".";
            }
            yield return null;
        }
        GameNetworkManager.Instance.Disconnect();
        displayLostConnection("Connection timeout");
    }

    //default loading loop
    IEnumerator loadLoop()
    {
        load_screen.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.0f, 0.0f);
        load_screen.transform.GetChild(1).gameObject.SetActive(true);
        load_ring.SetActive(true);
        while (true)
        {
            spinRings();
            yield return null;
        }
    }

    IEnumerator loadBridgeEnvironment()
    {
        dummy_camera.SetActive(true);

        //find player
        string player_prefab_name = SteamClient.Name + "_" + SteamClient.SteamId.ToString();
        GameObject player_prefab = GameObject.Find(player_prefab_name);
        while (player_prefab == null)
        {
            player_prefab = GameObject.Find(player_prefab_name);
            yield return null;
        }

        //enable load screen
        randomizeColors();
        load_screen.SetActive(true);

        //switch cameras
        if (Camera.main != null)
        {
            Camera.main.gameObject.SetActive(false);
        }
        player_prefab.transform.GetChild(0).gameObject.SetActive(true);

        //wait for BridgeEnvironment to load
        while (load_operation.isDone == false)
        {
            //spin circles while waiting
            spinRings();
            yield return null;
        }
        load_operation = null;
        ReferenceAssistor.Instance.player_manager.addPlayer(player_prefab, this);

        //wait until PlayerManager interrupts load screen using endLoad()
        while (true)
        {
            spinRings();
            yield return null;
        }
    }

    //called whenever the client loads into a scenario 
    IEnumerator loadScenarioTransition()
    {
        ReferenceAssistor.Instance.player_manager.GetComponent<PlayerManager>();
        GameObject transition_canvas = GameObject.Find("ScenarioTransitioner").GetComponent<TransitionHandler>().TransitionCanvas;
        bool scenario_loaded = false;
        bool switched_to_load_screen = false;
        while (true)
        {
            if (scenario_loaded == false)
            {
                if (load_operation != null && load_operation.isDone == true)
                {
                    //tell PlayerManager that the new scenario is loaded
                    ReferenceAssistor.Instance.player_manager.signifyScenarioLoaded();
                    scenario_loaded = true;
                    load_operation = null;
                }
            }

            //if transition canvas gets deleted then stop
            if (transition_canvas == null)
            {
                break;
            }

            //if transition is over and we haven't switched to load screen yet, then switch
            if (transition_canvas.activeSelf == false && switched_to_load_screen == false)
            {
                switched_to_load_screen = true;
                randomizeColors();
                load_screen.SetActive(true);
            }
            
            //spin rings if on load screen instead of transition screen
            if (switched_to_load_screen == true)
            {
                spinRings();
            }

            yield return null;
        }
    }

    IEnumerator fadeBlackScreen(float fade_time)
    {
        float anim_time = fade_time;
        load_screen.transform.GetChild(1).gameObject.SetActive(false);
        load_ring.SetActive(false);

        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            float a = Mathf.Lerp(0.0f, 1.0f, anim_time / fade_time);

            load_screen.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.0f, 0.0f, a);

            yield return null;
        }
        load_screen.SetActive(false);

        fade_black_coroutine = null;
    }
}