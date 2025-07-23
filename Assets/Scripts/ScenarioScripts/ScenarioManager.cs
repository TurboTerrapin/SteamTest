/*
    ScenarioManager.cs
    - Handles loading and transitioning of scenarios
    Contributor(s): John Aylward
    Last Updated: 7/23/2025
*/

using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ScenarioManager : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static int MINIMUM_PLAYERS = 4;

    private GameObject spaceship;
    private GameObject endpoint;

    private int players_ready = 0;

    //called by ControlScript after scene is loaded in and all player scripts (ControlScript, CameraMove, PlayerMove) are initialized
    public void initializeScenarioManager()
    {
        spaceship = GameObject.FindWithTag("Spaceship");
        endpoint = GameObject.FindWithTag("ScenarioEndPoint");

        readyToPlayRPC();

        if (NetworkManager.Singleton.IsHost)
        {
            StartCoroutine(waitForOthers());
        }
    }

    //only run by the host
    IEnumerator waitForOthers()
    {
        //wait until MINIMUM_PLAYERS have loaded in
        while (players_ready < MINIMUM_PLAYERS)
        {
            yield return null;
        }

        //at this point, load the first scenario and stuff
        //------------------------------------//
    }

    //called by whatever scenario is in the scene upon ending (ex. ship destruction, endpoint reached)
    public void endScenario(bool success)
    {
        if (success)
        {
            //transition and load next scenario
        }
        else
        {
            //reload scenario? end game? who knows!
        }
    }

    public float getDistanceToEndpoint()
    {
        float dist = 9999.9f;
        if (endpoint != null && spaceship != null)
        {
            dist = Vector3.Distance(endpoint.transform.position, spaceship.transform.position);
        }
        return dist;
    }

    [Rpc(SendTo.Everyone)]
    private void endScenarioRPC(bool success)
    {
        //temporary for testing purposes
        GameObject plr_canvas = GameObject.Find("Canvas");
        if (plr_canvas != null)
        {
            if (success == true)
            {
                plr_canvas.GetComponent<EndScenario>().displayEndScenario("SCENARIO COMPLETE", new Color(0.0f, 1.0f, 0.0f, 0.0f));
            }
            else
            {
                plr_canvas.GetComponent<EndScenario>().displayEndScenario("SCENARIO FAILED", new Color(1.0f, 0.0f, 0.0f, 0.0f));
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void readyToPlayRPC()
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            players_ready++;
        }
    }
}
