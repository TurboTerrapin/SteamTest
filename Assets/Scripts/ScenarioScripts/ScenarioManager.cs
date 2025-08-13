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
    //CLASS CONSTANTS
    public static int BOUNDARY_SIZE = 5000; //referenced by ShipController, EngineerMap
    public static float PATH_SIZE = 10.0f; //degrees of the boundary, does not reflect on EngineerMap so be careful!

    private GameObject spaceship;
    private GameObject endpoint;
    private EngineerMap engineer_map;

    private Vector2 entrance_position;
    private float entrance_rotation;
    private Vector2 exit_position;
    private float exit_rotation;

    public static Vector2 getBoundaryPointFromAngle(float ang)
    {
        Vector2 return_point = new Vector2(0.0f, 0.0f);
        float path_slope = Mathf.Tan(Mathf.Deg2Rad * ang);
        return_point.x = ((BOUNDARY_SIZE * 0.5f) * (BOUNDARY_SIZE * 0.5f)) / (1.0f + (path_slope * path_slope));
        return_point.x = Mathf.Sqrt(return_point.x);
        return_point.y = return_point.x * path_slope;
        return return_point;
    }

    private Vector2 generatePathLocation()
    {
        float path_angle = Random.Range(0.0f, 15.0f);
        Vector2 path_point = getBoundaryPointFromAngle(path_angle);
        //determine if the path point will be above or below the midline of the circle/boundary
        if (Random.Range(0,2) == 0)
        {
            path_point.y *= -1;
        }
        return path_point;
    }

    private void generatePaths()
    {
        entrance_position = generatePathLocation();
        entrance_position.x *= -1.0f;
        entrance_rotation = Random.Range(-10.0f, 10.0f);
        exit_position = generatePathLocation();
        exit_rotation = Random.Range(-10.0f, 10.0f);
    }

    //called by PlayerManager after scene is loaded in and all player scripts (ControlScript, CameraMove, PlayerMove) are initialized
    public void initializeScenarioManager()
    {
        spaceship = GameObject.FindWithTag("Spaceship");
        endpoint = GameObject.FindWithTag("ScenarioEndPoint");
        engineer_map = GameObject.FindWithTag("SensorHandler").GetComponent<EngineerMap>();

        startScenario();
    }

    public void startScenario()
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            generatePaths();
            setNewPathsRPC(entrance_position, entrance_rotation, exit_position, exit_rotation);
        }
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
    private void setNewPathsRPC(Vector2 ent_pos, float ent_rot, Vector2 exit_pos, float exit_rot)
    {
        entrance_position = ent_pos;
        entrance_rotation = ent_rot;
        exit_position = exit_pos;
        exit_rotation = exit_rot;

        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject.FindGameObjectWithTag("Spaceship").GetComponent<PilotingSystem>().PlaceShip(entrance_position, ent_rot);
            GameObject.FindGameObjectWithTag("Spaceship").GetComponent<PilotingSystem>().SetPaths(entrance_position, entrance_rotation, exit_position, exit_rotation);
        }
        engineer_map.updatePathLocations(entrance_position, entrance_rotation, exit_position, exit_rotation);
    }
}
