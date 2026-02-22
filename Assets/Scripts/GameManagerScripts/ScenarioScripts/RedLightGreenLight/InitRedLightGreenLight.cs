/*
    InitRedLightGreenLight
    - Used for initializing permanent information on RedLightGreenLight (code options)
    Contributor(s): Jake Schott
    Last Updated: 2/22/2026
*/

using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

public class InitRedLightGreenLight : NetworkBehaviour, IScenarioInitialization
{
    public static int SCENARIO_DATABASE_INDEX = 0;

    private GameObject scenarioDatabaseRLGL;
    private int[] centerColors = new int[8];

    public List<UnityEngine.UI.RawImage> manual_options;

    private void Awake()
    {
        scenarioDatabaseRLGL = transform.GetChild(0).GetChild(SCENARIO_DATABASE_INDEX).gameObject;
    }

    public void initializeDatabaseInformation()
    {
        for (int i = 0; i < 8; i++)
        {
            int[] indices = new int[8];
            for (int x = 0; x < 8; x++)
            {
                List<int> possibleSymbols = new List<int>();
                for (int o = 0; o < 12; o++)
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
        c.a = 0.2f;
        manual_options[index].color = c;

        int[] ci = DataConverter.stringToArray(stringCodeIndices);
        bool[] cin = new bool[8];
        int[] cc = new int[8];
        for (int i = 0; i < 8; i++)
        {
            cin[i] = false;
            cc[i] = 0;
        }
        scenarioDatabaseRLGL.transform.GetChild(index).GetComponent<UniversalCommunicatorCodeData>().setCodeIndices(ci);
        scenarioDatabaseRLGL.transform.GetChild(index).GetComponent<UniversalCommunicatorCodeData>().setCodeIsNumeric(cin);
        scenarioDatabaseRLGL.transform.GetChild(index).GetComponent<UniversalCommunicatorCodeData>().setCodeColors(cc);
    }
}