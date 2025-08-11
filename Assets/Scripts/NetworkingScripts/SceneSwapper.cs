
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Netcode.Transports.Facepunch;
using Unity.Netcode;

public class SceneSwapper : MonoBehaviour
{

    public static SceneSwapper Instance { get; private set; } = null;

    [SerializeField]
    private List<string> sceneNames = null;
    [SerializeField]
    private List<string> easyScenarios = null;
    [SerializeField]
    private List<string> mediumScenarios = null;
    [SerializeField]
    private List<string> hardScenarios = null;

    private int currentScene = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }


    [ClientRpc]
    public void ChangeSceneClientRPC(string sceneName)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void ChangeSceneRandom()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        int scene = Random.Range(0, sceneNames.Count);
        while (scene == currentScene)
        {
            scene = Random.Range(0, sceneNames.Count);
        }
        ChangeSceneClientRPC(sceneNames[scene]);
    }

    public void ChangeScenarioEasy()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        int scene = Random.Range(0, easyScenarios.Count);
        while (scene == currentScene)
        {
            scene = Random.Range(0, easyScenarios.Count);
        }
        ChangeSceneClientRPC(easyScenarios[scene]);
    }


    public void ChangeScenarioMedium()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        int scene = Random.Range(0, mediumScenarios.Count);
        while (scene == currentScene)
        {
            scene = Random.Range(0, mediumScenarios.Count);
        }
        ChangeSceneClientRPC(mediumScenarios[scene]);
    }


    public void ChangeScenarioHard()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        int scene = Random.Range(0, hardScenarios.Count);
        while (scene == currentScene)
        {
            scene = Random.Range(0, hardScenarios.Count);
        }
        ChangeSceneClientRPC(hardScenarios[scene]);
    }
}