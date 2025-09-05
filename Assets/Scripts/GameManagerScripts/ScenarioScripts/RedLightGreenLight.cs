/*
    RedLightGreenLight.cs
    Contributor: Beata Musial

    1. 60 second delay before RedLightGreenLight commences.
    2. While ship health is not 0, the red light state begins until a friendly transmission is recieved.
    3. Once the friendly transmission is recieved, it will remain in the green light state for 15-30 seconds.
    4. Steps 2 and 3 will loop until the end point of the scenario is reached or the ship health is 0.

    Red Light Phase: Camera shake with damage taken to ship only while impulse > 0;
    Green Light Phase: Enemy does nothing.

*/

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Steamworks;

public class RedLightGreenLight : NetworkBehaviour, IUniversalCommunicable
{
    //CLASS CONSTANTS
    private static Color[] COLOR_OPTIONS = new Color[4] { new Color(0f, 0.84f, 1f), new Color(0.129f, 1f, 0.04f), new Color(0.69f, 0f, 0.69f), new Color(0.84f, 0.62f, 0f) };
    private static float ENDPOINT_RANGE = 25.0f;

    private GameObject PlayerPrefab;
    Vector3 OriginalCameraPosition;
    bool ScenarioEndpointReached = false;
    private ScenarioManager scenarioManager;
    private ImpulseThrottle impulse;
    private ScanWaveManager scanWaveManager;
    private ShipHealth shipHealth;
    private Coroutine redLightCoroutine = null;
    private Coroutine greenLightCoroutine = null;
    private Coroutine cameraShakeCoroutine = null;

    private GameObject spaceship;

    //--SCAN WAVE INFORMATION--//
    //CENTER OF WAVE
    public Texture center_texture;
    public float center_speed = 50.0f;

    //WAVE RINGS
    public List<Texture> texture_options = null;
    public List<float> ring_speeds = null;

    private int[] curr_colors = new int[5] { 0, 0, 0, 0, 0 }; //0 is blue, 1 is green, 2 is pink, 3 is orange
    private int num_pink = 0;
    private int num_green = 0;
    private int num_dotted = 0;
    //-------------------------//

    private void randomizeColors()
    {
        for (int i = 0; i < 4; i++)
        {
            int new_color = Random.Range(0, 4);
            curr_colors[i] = new_color;
        }
        //lastly define the center's color, which has no bearing on anything
        curr_colors[4] = Random.Range(0, 4);
    }

    private void setColorInfo()
    {
        num_pink = 0;
        num_green = 0;
        for (int i = 0; i < 4; i++)
        {
            if (curr_colors[i] == 1)
            {
                num_green++;
            }
            else if (curr_colors[i] == 2)
            {
                num_pink++;
            }
        }
    }

    private List<Color> getColorsAsColor()
    {
        List<Color> to_return = new List<Color>();
        for (int i = 0; i < 4; i++)
        {
            to_return.Add(COLOR_OPTIONS[curr_colors[i]]);
        }
        return to_return;
    }

    private void Start()
    {
        scenarioManager = GameObject.FindWithTag("ScenarioManager").GetComponent<ScenarioManager>();

        GameObject controlHandler = GameObject.FindWithTag("ControlHandler");
        impulse = controlHandler.GetComponent<ImpulseThrottle>();

        GameObject sensorHandler = GameObject.FindWithTag("SensorHandler");
        scanWaveManager = sensorHandler.GetComponent<ScanWaveManager>();

        spaceship = GameObject.FindWithTag("Spaceship");
        shipHealth = spaceship.GetComponent<ShipHealth>();

        string playerPrefabName = SteamClient.Name + "_" + SteamClient.SteamId.ToString();
        PlayerPrefab = GameObject.Find(playerPrefabName);
        OriginalCameraPosition = PlayerPrefab.transform.GetChild(0).transform.localPosition;

        //if host, begin scenario stuff
        if (NetworkManager.Singleton.IsHost)
        {
            //initialize wave, randomize initial colors and textures
            randomizeColors();

            int[] ring_textures = new int[4];
            for (int i = 0; i < 4; i++)
            {
                int random_texture = Random.Range(1, texture_options.Count);
                //50-50 chance it's dotted
                if (Random.Range(0, 2) == 0)
                {
                    random_texture = 0;
                }
                ring_textures[i] = random_texture;
            }

            string cc = "";
            for (int i = 0; i < 5; i++)
            {
                cc += curr_colors[i].ToString();
            }

            string rt = "";
            for (int i = 0; i < 4; i++)
            {
                rt += ring_textures[i].ToString();
            }

            waveInitializationRPC(cc, rt);
            enterRedLightStateRPC();
        }
    }
    IEnumerator GreenLightState()
    {
        //contract energy wave
        scanWaveManager.resizeWave(0, true, 0.5f);
        if (NetworkManager.Singleton.IsHost)
        {
            yield return new WaitForSeconds(Random.Range(15.0f, 30.0f));
            endGreenLightStateRPC(Random.Range(3.0f, 6.0f));
        }
    }

    IEnumerator EndGreenLight(float end_time)
    {
        //expand energy wave
        scanWaveManager.resizeWave(0, false, end_time);
        yield return new WaitForSeconds(end_time);
        if (NetworkManager.Singleton.IsHost && ScenarioEndpointReached == false)
        {
            enterRedLightStateRPC();
        }
    }

    IEnumerator RedLightState()
    {
        if (cameraShakeCoroutine == null)
        {
            cameraShakeCoroutine = StartCoroutine(CameraShakeState());
        }

        if (NetworkManager.Singleton.IsHost)
        {
            while (shipHealth.getHullIntegrity() > 0.0f && scenarioManager.getDistanceToEndpoint() > ENDPOINT_RANGE)
            {
                // if the ship is moving
                if (impulse.getCurrentImpulse() > 0.0f)
                {
                    float time_before_damage_is_inflicted = 1.0f;
                    while (time_before_damage_is_inflicted > 0.0f && impulse.getCurrentImpulse() > 0.0f)
                    {
                        time_before_damage_is_inflicted -= Time.deltaTime;
                        yield return null;
                    }
                    if (impulse.getCurrentImpulse() > 0.0f)
                    {
                        float[] damages = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f };
                        float damage = 10.0f * impulse.getCurrentImpulse();
                        for (int i = 0; i < 4; i++)
                        {
                            if (damages[i] == 0.0f || damages[i] != damage)
                            {
                                damages[i] = Random.Range(0.0f, damage);
                            }
                        }
                        shipHealth.damageMultipleSections(damages);
                    }
                }
                else
                {
                    yield return null;
                }
            }
            if (shipHealth.getHullIntegrity() <= 0.0f)
            {
                scenarioManager.endScenario(false);
            }
            else
            {
                scenarioManager.endScenario(true);
            }
        }
    }

    IEnumerator CameraShakeState()
    {
        //only shakes when impulse is > 0, gets worse as impulse goes up
        while (true)
        {
            if (PlayerPrefab != null)
            {
                float intensity = impulse.getCurrentImpulse() * 0.025f;
                Vector3 Shake = Random.insideUnitSphere * intensity;
                PlayerPrefab.transform.GetChild(0).transform.localPosition = OriginalCameraPosition + Shake;
            }
            yield return null;
        }
    }
    
    public bool checkTransmission(int frequency, List<int> code_indexes, List<int> code_colors, List<int> code_is_numeric)
    {
        return isFriendlyMessage(code_indexes, code_colors, code_is_numeric);
    }

    public void handleTransmission(int frequency, List<int> code_indexes, List<int> code_colors, List<int> code_is_numeric)
    {
        if (NetworkManager.Singleton.IsHost && greenLightCoroutine == null)
        {
            bool successful_transmission = isFriendlyMessage(code_indexes, code_colors, code_is_numeric);
            if (successful_transmission)
            {
                randomizeColors();
                string cc = "";
                for (int i = 0; i < 5; i++)
                {
                    cc += curr_colors[i].ToString();
                }
                enterGreenLightStateRPC(cc);
            }
            else
            {
                //possibility to damage the ship if wrong message is sent, but does nothing for now
            }
        }
    }
    private bool isFriendlyMessage(List<int> ci, List<int> cc, List<int> cin)
    {
        if (ci.Count != 8)
        {
            return false;
        }

        int[] friendlyMessageIndexes = { 5, 7, 11, 5, 3, 8, 10, 4 };
        
        //if 2+ green, all triangles become circles
        if (num_green >= 2)
        {
            friendlyMessageIndexes[2] = 1;
            friendlyMessageIndexes[6] = 1;
        }

        //if 1+ pink, flip the order
        if (num_pink >= 1)
        {
            int[] reversedIndexes = new int[8];
            for (int i = 0; i < 8; i++)
            {
                reversedIndexes[i] = friendlyMessageIndexes[7 - i];
            }
            friendlyMessageIndexes = reversedIndexes;
        }

        for (int i = 0; i < 8; i++)
        {
            //if 2+ dotted rings, make sure is orange
            if (num_dotted >= 2)
            {
                if (cc[i] != 3)
                {
                    return false;
                }
            }
            //make sure is symbol
            if (cin[i] != 0)
            {
                return false;
            }
            //make sure is right message
            if (ci[i] != friendlyMessageIndexes[i])
            {
                return false;
            }
        }
        return true;
    }

    private void resetCoroutines()
    {
        if (redLightCoroutine != null)
        {
            StopCoroutine(redLightCoroutine);
        }
        if (greenLightCoroutine != null)
        {
            StopCoroutine(greenLightCoroutine);
        }
        if (cameraShakeCoroutine != null)
        {
            StopCoroutine(cameraShakeCoroutine);
        }
        redLightCoroutine = null;
        greenLightCoroutine = null;
        cameraShakeCoroutine = null;
    }

    [Rpc(SendTo.Everyone)]
    private void waveInitializationRPC(string s_ring_colors, string s_ring_textures)
    {
        int[] temp_curr_colors = s_ring_colors.ToIntArray();
        int[] temp_textures = s_ring_textures.ToIntArray();

        for (int i = 0; i < 5; i++)
        {
            curr_colors[i] = temp_curr_colors[i] - 48;
        }
        setColorInfo();

        List<bool> ring_is_solid = new List<bool>();
        List<Texture> ring_textures = new List<Texture>();
        for (int i = 0; i < 4; i++)
        {
            temp_textures[i] = temp_textures[i] - 48;
            ring_is_solid.Add(temp_textures[i] != 0);
            if (temp_textures[i] == 0)
            {
                num_dotted++;
            }
            ring_textures.Add(texture_options[temp_textures[i]]);
        }

        WaveInfo RLGLwave = new WaveInfo();
        RLGLwave.setCenter(center_texture, COLOR_OPTIONS[curr_colors[4]], center_speed);
        RLGLwave.setRings(4, getColorsAsColor(), ring_textures, ring_is_solid, ring_speeds);

        scanWaveManager.initializeWave(0, RLGLwave);
    }

    [Rpc(SendTo.Everyone)]
    private void enterRedLightStateRPC()
    {
        resetCoroutines();
        redLightCoroutine = StartCoroutine(RedLightState());
    }

    [Rpc(SendTo.Everyone)]
    private void enterGreenLightStateRPC(string s_new_colors)
    {
        int[] temp_curr_colors = s_new_colors.ToIntArray();

        for (int i = 0; i < 5; i++)
        {
            curr_colors[i] = temp_curr_colors[i] - 48;
        }
        setColorInfo();

        scanWaveManager.updateColors(0, getColorsAsColor(), 1.0f);

        resetCoroutines();
        greenLightCoroutine = StartCoroutine(GreenLightState());
    }

    [Rpc(SendTo.Everyone)]
    private void endGreenLightStateRPC(float contraction_time)
    {
        resetCoroutines();
        greenLightCoroutine = StartCoroutine(EndGreenLight(contraction_time));
    }
}
