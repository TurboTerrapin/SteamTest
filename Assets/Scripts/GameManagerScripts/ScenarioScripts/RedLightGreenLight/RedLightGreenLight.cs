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
using Unity.Netcode;
using UnityEngine;

public class RedLightGreenLight : NetworkBehaviour, IScenario, IUniversalCommunicable
{
    //CLASS CONSTANTS
    private static string DEATH_MESSAGE = "Stolen ship SEACC-3002 was discovered with critical damage to all areas of the ship after being exposed to an unexplainable anomaly of unknown origin that targets ships with impulse engines.";
    private static float CENTER_SPEED = 50.0f;
    private static List<float> RING_SPEEDS = new List<float>() { 25.0f, 60.0f, 40.0f, 35.0f };

    private GameObject playerPrefab;
    private Vector3 originalCameraPosition;
    private bool scenarioEndpointReached = false;
    private ScenarioManager scenarioManager;
    private ImpulseThrottle impulse;
    private EnergyPattern energyPattern;
    private ShipHealth shipHealth;
    private Coroutine redLightCoroutine = null;
    private Coroutine greenLightCoroutine = null;
    private Coroutine cameraShakeCoroutine = null;
    public VisualSpectacleLighting visualSpectacleLighting;

    private GameObject spaceship;
    private Transform scenarioDatabaseRLGL;

    private int centerIndex = -1; //corresponds to InitRedLightGreenLight

    //--ENERGY PATTERN INFORMATION--//
    private int[] currColors = new int[4] { 0, 0, 0, 0 }; //0 is blue, 1 is purple, 2 is orange, 3 is green
    private int numPurple = 0;
    private int numGreen = 0;
    private int numDotted = 0;
    //-------------------------//

    private void randomizeColors()
    {
        currColors[0] = scenarioManager.GetComponent<InitRedLightGreenLight>().getCenterColor(Random.Range(0, 7));
        for (int i = 0; i < 4; i++)
        {
            int newColor = Random.Range(0, 4);
            currColors[i] = newColor;
        }
    }

    private void updateColorInfo()
    {
        numPurple = 0;
        numGreen = 0;
        for (int i = 0; i < 4; i++)
        {
            if (currColors[i] == 1)
            {
                numPurple++;
            }
            else if (currColors[i] == 3)
            {
                numGreen++;
            }
        }
    }

    private void Start()
    {
        scenarioManager = GameObject.FindWithTag("ScenarioManager").GetComponent<ScenarioManager>();
        scenarioDatabaseRLGL = scenarioManager.transform.GetChild(0).GetChild(InitRedLightGreenLight.SCENARIO_DATABASE_INDEX);

        impulse = ReferenceAssistor.Instance.module_handlers[0].GetComponent<ImpulseThrottle>();

        energyPattern = ReferenceAssistor.Instance.module_handlers[2].GetComponent<EnergyPattern>();

        spaceship = GameObject.FindWithTag("Spaceship");
        shipHealth = spaceship.GetComponent<ShipHealth>();

        playerPrefab = GameObject.FindGameObjectWithTag("PlayerManager").GetComponent<PlayerManager>().getLocalPlayer();
        originalCameraPosition = playerPrefab.transform.GetChild(0).transform.localPosition;
    }

    //only run by host
    public void initiateScenario()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        GetComponent<AsteroidField>().initiateScenario();

        //initialize pattern, randomize initial colors and textures
        randomizeColors();

        int[] ringTextures = new int[4];
        int[] ringIsDotted = new int[4];
        for (int i = 0; i < 4; i++)
        {
            //50-50 chance it's dotted
            if (Random.Range(0, 2) == 0)
            {
                ringIsDotted[i] = 1;
                ringTextures[i] = 0;
            }
            else
            {
                ringIsDotted[i] = 0;
                ringTextures[i] = UnityEngine.Random.Range(0, 4);
            }
        }

        centerIndex = UnityEngine.Random.Range(0, 8);
        string cc = DataConverter.arrayToString(currColors);
        string rt = DataConverter.arrayToString(ringTextures);
        string rid = DataConverter.arrayToString(ringIsDotted);

        patternInitializationRPC(centerIndex, cc, rt, rid);
        enterRedLightStateRPC();
    }
    IEnumerator GreenLightState()
    {
        //contract energy pattern
        energyPattern.resizePattern(true, 0.5f);
        if (NetworkManager.Singleton.IsHost)
        {
            yield return new WaitForSeconds(Random.Range(15.0f, 30.0f));
            endGreenLightStateRPC(Random.Range(3.0f, 6.0f));
        }
    }

    IEnumerator EndGreenLight(float end_time)
    {
        //expand energy pattern
        energyPattern.resizePattern(false, end_time);
        yield return new WaitForSeconds(end_time);
        if (NetworkManager.Singleton.IsHost == true && scenarioEndpointReached == false)
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
            while (true)
            {
                //if the ship is moving
                if (impulse.getCurrentImpulse() > 0.0f)
                {
                    float timeBeforeDamageIsInflicted = 1.0f;
                    while (timeBeforeDamageIsInflicted > 0.0f && impulse.getCurrentImpulse() > 0.0f)
                    {
                        timeBeforeDamageIsInflicted -= Time.deltaTime;
                        yield return null;
                    }
                    if (impulse.getCurrentImpulse() > 0.0f)
                    {
                        //shipHealth.damageAllSections(10.0f * impulse.getCurrentImpulse());
                    }
                }
                else
                {
                    yield return null;
                }
            }
        }
    }

    IEnumerator CameraShakeState()
    {
        //only shakes when impulse is > 0, gets worse as impulse goes up
        while (true)
        {
            if (playerPrefab != null)
            {
                float intensity = impulse.getCurrentImpulse() * 0.025f;
                Vector3 shake = Random.insideUnitSphere * intensity;
                // PlayerPrefab.transform.GetChild(0).transform.localPosition = OriginalCameraPosition + shake;
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    public bool checkTransmission(int frequency, List<int> code_indexes, List<int> code_colors, List<int> code_is_numeric)
    {
        return isFriendlyMessage(code_indexes, code_colors, code_is_numeric);
    }

    public void handleTransmission(int frequency, List<int> codeIndexes, List<int> codeColors, List<int> codeIsNumeric)
    {
        if (NetworkManager.Singleton.IsHost && greenLightCoroutine == null)
        {
            bool successfulTransmission = isFriendlyMessage(codeIndexes, codeColors, codeIsNumeric);
            if (successfulTransmission)
            {
                randomizeColors();
                string cc = DataConverter.arrayToString(currColors);
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
        if (numGreen >= 2)
        {
            friendlyMessageIndexes[2] = 1;
            friendlyMessageIndexes[6] = 1;
        }

        //if 1+ purple, flip the order
        if (numPurple >= 1)
        {
            int[] reversedIndexes = new int[8];
            for (int i = 0; i < 8; i++)
            {
                reversedIndexes[i] = friendlyMessageIndexes[7 - i];
            }
            friendlyMessageIndexes = reversedIndexes;
        }

        bool toReturn = true;

        for (int i = 0; i < 8; i++)
        {
            //if 2+ dotted rings, make sure is orange
            if (numDotted >= 2)
            {
                if (cc[i] != 3)
                {
                    toReturn = false;
                }
            }
            //make sure is symbol
            if (cin[i] != 0)
            {
                toReturn = false;
            }
            //make sure is right message
            if (ci[i] != friendlyMessageIndexes[i])
            {
                toReturn = false;
            }
        }
        return toReturn;
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

    public string getDeathMessage()
    {
        return DEATH_MESSAGE;
    }

    [Rpc(SendTo.Everyone)]
    private void patternInitializationRPC(int centerOption, string stringRingColors, string stringRingTextures, string stringRingIsDotted)
    {
        centerIndex = centerOption;
        int[] currColors = DataConverter.stringToArray(stringRingColors);
        int[] currTextures = DataConverter.stringToArray(stringRingTextures);
        int[] tempIsDotted = DataConverter.stringToArray(stringRingIsDotted);

        List<int> tempColors = new List<int>();
        List<int> tempTextures = new List<int>();
        List<bool> ringIsDotted= new List<bool>();
        numDotted = 0;
        for (int i = 0; i < 4; i++)
        {
            tempColors.Add(currColors[i]);
            tempTextures.Add(currTextures[i]);
            ringIsDotted.Add(tempIsDotted[i] == 1);
            if (ringIsDotted[i] == true)
            {
                Debug.Log(tempTextures[i]);
                numDotted++;
            }
        }

        updateColorInfo();

        PatternData RLGLpattern = new PatternData();
        RLGLpattern.setCenter(centerOption, scenarioManager.GetComponent<InitRedLightGreenLight>().getCenterColor(centerOption), CENTER_SPEED);
        RLGLpattern.setRings(4, tempColors, tempTextures, ringIsDotted, RING_SPEEDS);

        energyPattern.setPattern(RLGLpattern);
    }

    [Rpc(SendTo.Everyone)]
    private void enterRedLightStateRPC()
    {
        resetCoroutines();

        visualSpectacleLighting.SetRedLight();

        redLightCoroutine = StartCoroutine(RedLightState());
    }

    [Rpc(SendTo.Everyone)]
    private void enterGreenLightStateRPC(string stringNewColors)
    {
        int[] currColors = DataConverter.stringToArray(stringNewColors);
        List<int> tempColors = new List<int>();
        for (int i = 0; i < currColors.Length; i++)
        {
            tempColors.Add(currColors[i]);
        }

        updateColorInfo();

        energyPattern.updateColors(tempColors, scenarioManager.GetComponent<InitRedLightGreenLight>().getCenterColor(centerIndex), 1.0f);

        resetCoroutines();

        visualSpectacleLighting.SetGreenLight();

        greenLightCoroutine = StartCoroutine(GreenLightState());
    }

    [Rpc(SendTo.Everyone)]
    private void endGreenLightStateRPC(float contraction_time)
    {
        resetCoroutines();
        greenLightCoroutine = StartCoroutine(EndGreenLight(contraction_time));
    }
}