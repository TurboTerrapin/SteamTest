/*
    LoadHandler.cs
    - Handles loading into BridgeEnvironment, scenarios, TitleScreen (after quitting)
    Contributor(s): Jake Schott
    Last Updated: 3/14/2026
*/

using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadHandler : MonoBehaviour
{
    //LOAD CIRCLE SETTINGS
    private static float[] SPIN_SPEEDS = new float[3] { 50.0f, 200.0f, 70.0f };

    private GameObject load_screen;
    private GameObject load_ring;
    private GameObject dummy_camera;
    private Coroutine fade_black_coroutine = null;
    private List<Coroutine> load_coroutines = new List<Coroutine>();

    private void Start()
    {
        //used to ensure there is ever only one LoadHandler
        transform.name = "TempLoadHandler";
        if (GameObject.Find("LoadHandler") != null)
        {
            GameObject.Destroy(gameObject);
        }
        transform.name = "LoadHandler";

        DontDestroyOnLoad(gameObject);

        load_screen = transform.GetChild(0).gameObject;
        load_ring = load_screen.transform.GetChild(2).gameObject;
        dummy_camera = transform.GetChild(1).gameObject;
    }

    //called by CampaignLobbyController and FriendJoinWithButton to ensure that handleSceneLoad() gets connected to the right NetworkManager
    public void connectNetworkManager()
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
        foreach (Coroutine c in load_coroutines)
        {
            StopCoroutine(c);
        }
        load_coroutines.Clear();
    }

    //only called when NetworkManager.Singleton.SceneManager changes the scene
    private void handleSceneLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        resetAllCoroutines();
        if (sceneName == "BridgeEnvironment") //BridgeEnvironment load-in
        {
            load_coroutines.Add(StartCoroutine(loadBridgeEnvironment(asyncOperation)));
        }
        else //scenario transition
        {
            load_coroutines.Add(StartCoroutine(loadScenarioTransition(asyncOperation)));
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

    IEnumerator loadBridgeEnvironment(AsyncOperation load_operation)
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
        GameObject.FindGameObjectWithTag("PlayerManager").GetComponent<PlayerManager>().addPlayer(player_prefab, this);

        //wait until PlayerManager interrupts load screen using endLoad()
        while (true)
        {
            spinRings();
            yield return null;
        }
    }

    //called whenever the client loads into a scenario 
    IEnumerator loadScenarioTransition(AsyncOperation load_operation)
    {
        PlayerManager player_manager = GameObject.FindGameObjectWithTag("PlayerManager").GetComponent<PlayerManager>();
        GameObject transition_canvas = GameObject.Find("ScenarioTransitioner").GetComponent<TransitionHandler>().TransitionCanvas;
        bool scenario_loaded = false;
        bool switched_to_load_screen = false;
        while (true)
        {
            if (scenario_loaded == false)
            {
                if (load_operation.isDone == true)
                {
                    //tell PlayerManager that the new scenario is loaded
                    player_manager.signifyScenarioLoaded();
                    scenario_loaded = true;
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