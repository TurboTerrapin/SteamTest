/*
    ManualOverrideSwitchesLinker.cs
    - Used for linking scenario database entries to a manual override switches display
    Contributor(s): Jake Schott
    Last Updated: 6/28/2026
*/

using UnityEngine;

public class ManualOverrideSwitchesLinker : MonoBehaviour, IManualLinker
{
    [SerializeField]
    private GameObject corresponding_database_entry;
    [SerializeField]
    private GameObject corresponding_override_switches_display;

    public void link()
    {
        OverrideSwitchesData data = corresponding_database_entry.GetComponent<OverrideSwitchesData>();

        for (int i = 0; i < 6; i++)
        {
            Color c = corresponding_override_switches_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color;
            if (data.getSwitchEnabled(i) == false)
            {
                c.a = 0.1f;
            }
            else
            {
                c.a = 1.0f;
            }
            corresponding_override_switches_display.transform.GetChild(i).GetComponent<UnityEngine.UI.RawImage>().color = c;
            corresponding_override_switches_display.transform.GetChild(i).GetChild(1).gameObject.SetActive(data.getSwitchEnabled(i) == true);
        }
    }
}