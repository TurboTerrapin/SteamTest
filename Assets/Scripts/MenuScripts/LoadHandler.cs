/*
    LoadHandler.cs
    - Handles loading into BridgeEnvironment (at this time)
    Contributor(s): Jake Schott
    Last Updated: 8/25/2025
*/

using System.Collections;
using System.Collections.Generic;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadHandler : NetworkBehaviour
{
    //LOAD CIRCLE SETTINGS
    private static Color[] LOAD_COLORS = new Color[4] { new Color(0f, 0.84f, 1f), new Color(0.129f, 1f, 0.04f), new Color(0.69f, 0f, 0.69f), new Color(0.84f, 0.62f, 0f) };
    private static float[] SPIN_SPEEDS = new float[3] { 50.0f, 150.0f, 25.0f };

    private GameObject load_screen;
    private GameObject load_ring;
    private Coroutine load_coroutine = null;

    void Start()
    {
        transform.name = "TempLoadHandler";
        if (GameObject.Find("LoadHandler") != null)
        {
            GameObject.Destroy(gameObject);
        }
        transform.name = "LoadHandler";

        DontDestroyOnLoad(gameObject);

        load_screen = transform.GetChild(0).gameObject;
        load_ring = load_screen.transform.GetChild(2).gameObject;
    }

    public void connectNetworkManager()
    {
        StartCoroutine(yieldForNetworkSceneManager());
    }

    IEnumerator yieldForNetworkSceneManager()
    {
        while (NetworkManager.Singleton.SceneManager == null)
        {
            yield return null;
        }
        NetworkManager.Singleton.SceneManager.OnLoad += handleSceneLoad;
    }

    private void handleSceneLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        //currently only does something for the initial load-in
        if (sceneName == "BridgeEnvironment")
        {
            if (load_coroutine != null)
            {
                StopCoroutine(load_coroutine);
            }
            load_coroutine = StartCoroutine(loadBridgeEnvironment(asyncOperation));
        }
    }

    //currently only called by quit button in pause menu
    public void startLoad()
    {
        if (load_coroutine != null)
        {
            StopCoroutine(load_coroutine);
            load_coroutine = null;
        }
        randomizeColors();
        load_coroutine = StartCoroutine(loadLoop());
        load_screen.SetActive(true);
    }

    //terminates the loading screen
    public void endLoad()
    {
        if (load_coroutine != null)
        {
            StopCoroutine(load_coroutine);
            load_coroutine = null;
        }
        load_screen.SetActive(false);
    }

    //randomizes the colors for the spinny load circle
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
            load_ring.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = LOAD_COLORS[possible_colors[c]];
            possible_colors.RemoveAt(c);
        }
    }

    IEnumerator loadLoop()
    {
        while (true)
        {
            for (int i = 0; i < 3; i++)
            {
                float z = load_ring.transform.GetChild(i).GetComponent<RectTransform>().rotation.eulerAngles.z + SPIN_SPEEDS[i] * Time.deltaTime;
                load_ring.transform.GetChild(i).GetComponent<RectTransform>().rotation = Quaternion.Euler(0.0f, 0.0f, z);
            }
            yield return null;
        }
    }

    IEnumerator loadBridgeEnvironment(AsyncOperation load_operation)
    {
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
        load_screen.transform.GetChild(1).GetComponent<TMP_Text>().SetText("LOADING");
        load_screen.SetActive(true);
        //switch cameras
        Camera.main.gameObject.SetActive(false);
        player_prefab.transform.GetChild(0).gameObject.SetActive(true);
        //wait for scene to load
        while (load_operation.isDone == false)
        {
            //spin circles while waiting
            for (int i = 0; i < 3; i++)
            {
                float z = load_ring.transform.GetChild(i).GetComponent<RectTransform>().rotation.eulerAngles.z + SPIN_SPEEDS[i] * Time.deltaTime;
                load_ring.transform.GetChild(i).GetComponent<RectTransform>().rotation = Quaternion.Euler(0.0f, 0.0f, z);
            }
            yield return null;
        }
        GameObject.FindGameObjectWithTag("PlayerManager").GetComponent<PlayerManager>().addPlayer(player_prefab, this);
        //wait until PlayerManager interrupts load screen using endLoad()
        load_coroutine = StartCoroutine(loadLoop());
    }

    public void startLoadForAllPlayers()
    {
        allPlayersLoadRPC();
    }

    [Rpc(SendTo.Everyone)]
    private void allPlayersLoadRPC()
    {
        startLoad();
    }
}
