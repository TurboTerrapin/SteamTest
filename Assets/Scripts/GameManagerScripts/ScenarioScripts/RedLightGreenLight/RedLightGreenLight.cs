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
    public static int[] GREEN_LIGHT_PERIOD_TIMES = new int[] { 40, 35, 30, 20 }; //easy, medium, hard, expert
    private static float CENTER_SPEED = 50.0f;
    private static float[] RING_SPEEDS = new float[] { 25.0f, 60.0f, 40.0f, 75.0f };

    private GameObject playerPrefab;
    private bool scenarioEndpointReached = false;
    private ScenarioManager scenarioManager;
    private ImpulseThrottle impulse;
    private EnergyPattern energyPattern;
    private ShipHealth shipHealth;
    private Coroutine redLightCoroutine = null;
    private Coroutine greenLightCoroutine = null;
    public VisualSpectacleLighting visualSpectacleLighting;
    public AudioSource RLGLsound;

    private GameObject spaceship;
    private Transform scenarioDatabaseRLGL;

    //--ENERGY PATTERN INFORMATION--//
    private int centerIndex = -1; //corresponds to InitRedLightGreenLight/scenarioDatabaseRLGL
    private int[] currColors = new int[4] { 0, 0, 0, 0 }; //0 is blue, 1 is purple, 2 is orange, 3 is green
    private bool[] currIsDotted = new bool[4] { false, false, false, false };
    private int numPurple = 0;
    private int numOrange = 0;
    private int numDotted = 0;
    //-------------------------//

    //randomizes ring color
    private void randomizeColors()
    {
        for (int i = 0; i < 4; i++)
        {
            int newColor = Random.Range(0, 4);
            currColors[i] = newColor;
        }
    }

    private void updateRelevantPatternDescriptors()
    {
        numPurple = 0;
        numOrange = 0;
        numDotted = 0;
        for (int i = 0; i < 4; i++)
        {
            //check if dotted
            if (currIsDotted[i] == true)
            {
                numDotted++;
            }

            //check colors
            if (currColors[i] == 1)
            {
                numPurple++;
            }
            else if (currColors[i] == 2)
            {
                numOrange++;
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
    }

    private void Update()
    {
        RLGLsound.volume = impulse.getCurrentImpulse() * 0.5f;
    }

    //only run by host
    public void initiateScenario()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        //visual spectacle
        scenarioManager.forceSpawnLocation(new Vector3(0.0f, 0.0f, ScenarioManager.BOUNDARY_SIZE * 0.5f), 650.0f, true);
        //asteroid field
        GetComponent<AsteroidField>().spawnField(100);

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
        if (NetworkManager.Singleton.IsHost == true)
        {
            float greenLightMinimumTime = GREEN_LIGHT_PERIOD_TIMES[scenarioManager.GetComponent<ScenarioManager>().getDifficulty()];
            yield return new WaitForSeconds(Random.Range(greenLightMinimumTime, greenLightMinimumTime + 5.0f));
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
        if (NetworkManager.Singleton.IsHost == true)
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
                        shipHealth.damageAllSections(10.0f * impulse.getCurrentImpulse());
                    }
                }
                else
                {
                    yield return null;
                }
            }
        }
    }

    public bool checkTransmission(int frequency, List<int> code_indexes, List<int> code_colors, List<int> code_is_numeric)
    {
        return isFriendlyMessage(code_indexes, code_colors, code_is_numeric);
    }

    public void handleTransmission(int frequency, List<int> codeIndexes, List<int> codeColors, List<int> codeIsNumeric)
    {
        if (NetworkManager.Singleton.IsHost == true && greenLightCoroutine == null)
        {
            bool successfulTransmission = isFriendlyMessage(codeIndexes, codeColors, codeIsNumeric);
            if (successfulTransmission == true)
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

        int[] friendlyMessageIndexes = new int[8];
        int[] correspondingCode = scenarioDatabaseRLGL.transform.GetChild(centerIndex).GetComponent<UniversalCommunicatorCodeData>().getCodeIndices();
        for (int i = 0; i < 8; i++)
        {
            friendlyMessageIndexes[i] = correspondingCode[i];
        }

        //if 1+ purple, swap first and last
        if (numPurple >= 1)
        {
            int last = friendlyMessageIndexes[7];
            friendlyMessageIndexes[7] = friendlyMessageIndexes[0];
            friendlyMessageIndexes[0] = last;
        }

        //if 2+ dotted, replace triangles with circle
        if (numDotted >= 2)
        {
            for (int i = 0; i < 8; i++)
            {
                if (friendlyMessageIndexes[i] == 0)
                {
                    friendlyMessageIndexes[i] = 1;
                }
            }
        }

        bool toReturn = true;

        for (int i = 0; i < 8; i++)
        {
            //if 2+ orange rings, make sure is orange
            if (numOrange >= 2)
            {
                if (cc[i] != 2)
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
        redLightCoroutine = null;
        greenLightCoroutine = null;
    }

    public string getDeathMessage()
    {
        return DEATH_MESSAGE;
    }

    [Rpc(SendTo.Everyone)]
    private void patternInitializationRPC(int centerOption, string stringRingColors, string stringRingTextures, string stringRingIsDotted)
    {
        centerIndex = centerOption;
        currColors = DataConverter.stringToArray(stringRingColors);
        int[] tempTextures = DataConverter.stringToArray(stringRingTextures);
        int[] tempIsDotted = DataConverter.stringToArray(stringRingIsDotted);
        for (int i = 0; i < 4; i++)
        {
            currIsDotted[i] = (tempIsDotted[i] == 1);
        }
        updateRelevantPatternDescriptors();

        PatternData RLGLpattern = new PatternData();
        RLGLpattern.setCenter(centerOption, scenarioManager.GetComponent<InitRedLightGreenLight>().getCenterColor(centerOption), CENTER_SPEED);
        RLGLpattern.setRings(4, currColors, tempTextures, currIsDotted, RING_SPEEDS);

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
        currColors = DataConverter.stringToArray(stringNewColors);

        updateRelevantPatternDescriptors();

        energyPattern.updateColors(currColors, scenarioManager.GetComponent<InitRedLightGreenLight>().getCenterColor(centerIndex), 1.0f);

        resetCoroutines();

        visualSpectacleLighting.SetGreenLight();

        greenLightCoroutine = StartCoroutine(GreenLightState());
    }

    [Rpc(SendTo.Everyone)]
    private void endGreenLightStateRPC(float contractionTime)
    {
        resetCoroutines();
        greenLightCoroutine = StartCoroutine(EndGreenLight(contractionTime));
    }
}