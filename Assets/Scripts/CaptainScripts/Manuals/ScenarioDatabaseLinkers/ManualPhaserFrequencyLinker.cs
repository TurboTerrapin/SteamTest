/*
    ManualPhaserFrequencyLinker.cs
    - Used for linking scenario database entries to a manual phaser frequencies display
    Contributor(s): Jake Schott
    Last Updated: 6/28/2026
*/

using TMPro;
using UnityEngine;

public class ManualPhaserFrequencyLinker : MonoBehaviour, IManualLinker
{
    [SerializeField]
    private GameObject corresponding_database_entry;
    [SerializeField]
    private GameObject corresponding_phaser_frequency_display;

    public void link()
    {
        PhaserFrequencyData data = corresponding_database_entry.GetComponent<PhaserFrequencyData>();

        for (int i = 0; i < 2; i++)
        {
            corresponding_phaser_frequency_display.transform.GetChild(i).GetComponent<TMP_Text>().SetText(data.getPhaserFrequency(i) + ".0GH");
        }
    }
}