/*
    ScenarioManager.cs
    - Handles loading and transitioning of scenarios
    Contributor(s): John Aylward, Jake Schott
    Last Updated: 8/13/2025
*/

using Unity.Netcode;
using UnityEngine;

public class ScenarioManager : NetworkBehaviour
{
    //CLASS CONSTANTS
    public const int BOUNDARY_SIZE = 5000; //diamater of boundary circle, referenced by PilotingSystem, EngineerMap
    public const int BOUNDARY_ALTITUDE = 100; //how high/low the ship can go in either direction
    public const int START_DIST_OFFSET = 500; //how far back the ship starts in the entrance path
    public const int DIST_TO_ENDPOINT = 200; //how far into the exit path until endpoint reached
    public const float PATH_SIZE = 10.0f; //degrees of the boundary, does not reflect on EngineerMap so be careful!

    private EngineerMap engineer_map;

    //entrance/exit channel info
    private Vector2 entrance_position;
    private float entrance_rotation;
    private Vector2 exit_position;
    private float exit_rotation;

    //called by generatePathLocation() and PilotingSystem.CalculatePoint()
    public static Vector2 getBoundaryPointFromAngle(float ang)
    {
        Vector2 return_point = new Vector2(0.0f, 0.0f);
        float path_slope = Mathf.Tan(Mathf.Deg2Rad * ang);
        return_point.x = ((BOUNDARY_SIZE * 0.5f) * (BOUNDARY_SIZE * 0.5f)) / (1.0f + (path_slope * path_slope));
        return_point.x = Mathf.Sqrt(return_point.x);
        return_point.y = return_point.x * path_slope;
        return return_point;
    }

    //called by generatePaths() to generate the points for the entrance/exit channels
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

    //sets entrance/exit channel points and rotations
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
        engineer_map = GameObject.FindWithTag("SensorHandler").GetComponent<EngineerMap>();

        //host starts stuff
        if (NetworkManager.Singleton.IsHost == true)
        {
            startScenario();
        }
    }

    //only run by host
    public void startScenario()
    {
        generatePaths();
        setNewPathsRPC(entrance_position, entrance_rotation, exit_position, exit_rotation);
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

    [Rpc(SendTo.Everyone)]
    private void setNewPathsRPC(Vector2 ent_pos, float ent_rot, Vector2 exit_pos, float exit_rot)
    {
        entrance_position = ent_pos;
        entrance_rotation = ent_rot;
        exit_position = exit_pos;
        exit_rotation = exit_rot;

        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject.FindGameObjectWithTag("Spaceship").GetComponent<PilotingSystem>().SetPaths(entrance_position, entrance_rotation, exit_position, exit_rotation);
            GameObject.FindGameObjectWithTag("Spaceship").GetComponent<PilotingSystem>().PlaceShip(entrance_position, ent_rot);
        }
        engineer_map.updatePathLocations(entrance_position, entrance_rotation, exit_position, exit_rotation);
    }
}
