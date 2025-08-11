/*
    ScenarioManager.cs
    - Handles loading and transitioning of scenarios
    Contributor(s): John Aylward
    Last Updated: 8/11/2025
*/

using Unity.Netcode;
using UnityEngine;

public class ScenarioManager : NetworkBehaviour
{
    private GameObject spaceship;
    private GameObject endpoint;

    //called by ControlScript after scene is loaded in and all player scripts (ControlScript, CameraMove, PlayerMove) are initialized
    public void initializeScenarioManager()
    {
        spaceship = GameObject.FindWithTag("Spaceship");
        endpoint = GameObject.FindWithTag("ScenarioEndPoint");
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
}
