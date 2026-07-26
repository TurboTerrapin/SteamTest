/*
    ManualCodeLinker.cs
    - Used for linking scenario database entries to a manual code display
    Contributor(s): Jake Schott
    Last Updated: 6/29/2026
*/

using TMPro;
using UnityEngine;

public class ManualCodeLinker : MonoBehaviour, IManualLinker
{
    [SerializeField]
    private GameObject corresponding_database_entry;
    [SerializeField]
    private GameObject corresponding_code_display;

    public void link()
    {
        UniversalCommunicatorCodeData data = corresponding_database_entry.GetComponent<UniversalCommunicatorCodeData>();
        UniversalCommunicator uc = ReferenceAssistor.Instance.module_handlers[1].GetComponent<UniversalCommunicator>();

        int[] ci = data.getCodeIndexes();
        bool[] cin = data.getCodeIsNumeric();
        int[] cc = data.getCodeColors();

        for (int i = 0; i < 8; i++)
        {
            GameObject cd = uc.getCharacterDisplay(ci[i]);

            corresponding_code_display.transform.GetChild(i).GetChild(0).gameObject.SetActive(cin[i]);
            corresponding_code_display.transform.GetChild(i).GetChild(1).gameObject.SetActive(!cin[i]);
            if (cin[i] == true)
            {
                corresponding_code_display.transform.GetChild(i).GetChild(0).GetComponent<TMP_Text>().SetText(cd.transform.GetChild(0).GetComponent<TMP_Text>().text);
                corresponding_code_display.transform.GetChild(i).GetChild(0).GetComponent<TMP_Text>().color = ReferenceAssistor.COLOR_OPTIONS[cc[i]];
            }
            else
            {
                corresponding_code_display.transform.GetChild(i).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().texture = cd.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().texture;
                corresponding_code_display.transform.GetChild(i).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = ReferenceAssistor.COLOR_OPTIONS[cc[i]];
            }
        }
    }
}