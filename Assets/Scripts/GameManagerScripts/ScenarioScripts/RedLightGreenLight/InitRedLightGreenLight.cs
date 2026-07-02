/*
    InitRedLightGreenLight
    - Used for initializing permanent information on RedLightGreenLight (code options)
    Contributor(s): Jake Schott
    Last Updated: 2/24/2026
*/

using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InitRedLightGreenLight : NetworkBehaviour, IScenarioInitialization
{
    public static int SCENARIO_DATABASE_INDEX = 0;
    private static int[] POSSIBLE_CODE_CHARACTERS = new int[] { 4, 8, 10, 12 }; //corresponds to easy, medium, hard, expert difficulty

    private GameObject scenarioDatabaseRLGL;
    private int[] centerColors = new int[8];

    public List<UnityEngine.UI.Image> manual_options;
    public TMP_Text manual_result_text;

    private void Awake()
    {
        scenarioDatabaseRLGL = transform.GetChild(0).GetChild(SCENARIO_DATABASE_INDEX).gameObject;
    }

    private void Start()
    {
        manual_result_text.SetText(manual_result_text.text + RedLightGreenLight.GREEN_LIGHT_PERIOD_TIMES[GetComponent<ScenarioManager>().getDifficulty()] + " SECONDS");
    }

    public void initializeDatabaseInformation()
    {
        int num_possible_characters = POSSIBLE_CODE_CHARACTERS[GetComponent<ScenarioManager>().getDifficulty()];
        for (int i = 0; i < 8; i++)
        {
            int[] indices = new int[8];
            for (int x = 0; x < 8; x++)
            {
                List<int> possibleSymbols = new List<int>();
                for (int o = 0; o < num_possible_characters; o++)
                {
                    if (x < 2)
                    {
                        possibleSymbols.Add(o);
                    }
                    else
                    {
                        if (o != indices[x - 1] || (indices[x - 1] != indices[x - 2]))
                        {
                            possibleSymbols.Add(o);
                        }
                    }
                }
                indices[x] = possibleSymbols[UnityEngine.Random.Range(0, possibleSymbols.Count)];
            }

            int centerColor = UnityEngine.Random.Range(0, 4);

            transmitCodeInitializationRPC(i, centerColor, DataConverter.arrayToString(indices));
        }
    }

    public int getCenterColor(int index)
    {
        return centerColors[index];
    }

    [Rpc(SendTo.Everyone)]
    private void transmitCodeInitializationRPC(int index, int centerColor, string stringCodeIndices)
    {
        centerColors[index] = centerColor;
        Color c = ReferenceAssistor.Instance.module_handlers[2].GetComponent<EnergyPattern>().color_options[centerColor];
        manual_options[index].GetComponent<ManualEnergyPatternCenterLinker>().setColor(c);
        c.a = 0.2f;
        manual_options[index].color = c;
        manual_options[index].transform.GetChild(0).GetComponent<UnityEngine.UI.Image>().color = c;
        manual_options[index].transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().color = c;
        manual_options[index].transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = c;

        int[] ci = DataConverter.stringToArray(stringCodeIndices);
        bool[] cin = new bool[8];
        int[] cc = new int[8];
        for (int i = 0; i < 8; i++)
        {
            cin[i] = false;
            cc[i] = 0;
        }
        scenarioDatabaseRLGL.transform.GetChild(index).GetComponent<UniversalCommunicatorCodeData>().setCodeIndexes(ci);
        scenarioDatabaseRLGL.transform.GetChild(index).GetComponent<UniversalCommunicatorCodeData>().setCodeIsNumeric(cin);
        scenarioDatabaseRLGL.transform.GetChild(index).GetComponent<UniversalCommunicatorCodeData>().setCodeColors(cc);
    }
}