
using Netcode.Transports.Facepunch;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField]
    private List<string> completedScenarios = null;

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

    public void ChangeScene(string sceneName, int newScene)
    {
        currentScene = newScene;
        Debug.Log("Scene num for " + sceneName + " is " + currentScene + " in sceneNames list");
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void ChangeSceneRandom()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        

        if (currentScene != 0)
        {
            completedScenarios.Add(sceneNames[currentScene]);
        }

        int scene = FindSceneByList(sceneNames);
        
        ChangeScene(sceneNames[scene], scene);
    }

    public void ChangeScenarioEasy()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        Debug.Log("Starting to find new scenario");
        
        if (currentScene != 0)
        {
            completedScenarios.Add(sceneNames[currentScene]);
            Debug.Log("Added scene to completed list");
        }
        
        //Finds the scene
        int scene = FindSceneByList(easyScenarios);


        if (scene == -1)
        {
            ChangeScene(sceneNames[0], 0);
            return;
        }



        Debug.Log("Found scene " + scene);


        string sceneName = easyScenarios[scene];

        //Finds the value of the picked scene from the total sceneNames list
        for (int i = 0; i < sceneNames.Count; i++) 
        {
            if (sceneNames[i] == sceneName)
            {
                currentScene = i;
            }
        }

        ChangeScene(sceneName, currentScene);
    }


    public void ChangeScenarioMedium()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        Debug.Log("Starting to find new scenario");

        if (currentScene != 0)
        {
            completedScenarios.Add(sceneNames[currentScene]);
            Debug.Log("Added scene to completed list");
        }

        //Finds the scene
        int scene = FindSceneByList(mediumScenarios);
        Debug.Log("Found scene " + scene);


        string sceneName = mediumScenarios[scene];

        //Finds the value of the picked scene from the total sceneNames list
        for (int i = 0; i < sceneNames.Count; i++)
        {
            if (sceneNames[i] == sceneName)
            {
                currentScene = i;
            }
        }

        ChangeScene(sceneName, currentScene);
    }


    public void ChangeScenarioHard()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        Debug.Log("Starting to find new scenario");

        if (currentScene != 0)
        {
            completedScenarios.Add(sceneNames[currentScene]);
            Debug.Log("Added scene to completed list");
        }

        //Finds the scene
        int scene = FindSceneByList(hardScenarios);
        Debug.Log("Found scene " + scene);


        string sceneName = hardScenarios[scene];

        //Finds the value of the picked scene from the total sceneNames list
        for (int i = 0; i < sceneNames.Count; i++)
        {
            if (sceneNames[i] == sceneName)
            {
                currentScene = i;
            }
        }

        ChangeScene(sceneName, currentScene);
    }


    private int FindSceneByList(List<string> list)
    {
        int scene;
        bool sceneFound = false;

        if(completedScenarios.Count >= sceneNames.Count - 1)
        {
            return -1;
        }

        //Check if the scene is in list of completed scenarios
        do
        {
            //Generate random scene number (in the list that you choose as an argument)
            scene = Random.Range(0, list.Count);
            
            string name = list[scene];
            //Debug.Log("Picked scene " + scene + " with name " + name);

            if (completedScenarios.Count == 0)
            {
                return scene;
            }
            //Checks each of the completed scenarios
            for (int i = 0; i < completedScenarios.Count; i++)
            {
                if (completedScenarios[i] == name)
                {
                    Debug.Log("Scene " + name + " has already been completed");
                    break;
                }
                else if (i == completedScenarios.Count - 1 && completedScenarios[i] != name)
                {
                    sceneFound = true;
                }
            }

        } while (!sceneFound);

        return scene;
    }
}