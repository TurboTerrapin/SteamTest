/*
    ManualEnergyPatternCenterLinker.cs
    - Used for linking scenario database entries to a transmission wave image
    Contributor(s): Jake Schott
    Last Updated: 7/1/2026
*/

using TMPro;
using UnityEngine;

public class ManualEnergyPatternCenterLinker : MonoBehaviour, IManualLinker
{
    [SerializeField]
    private GameObject corresponding_transmission_energy_pattern_center;
    [SerializeField]
    private Color energy_pattern_center_color = Color.white;
    [SerializeField]
    private Texture energy_pattern_center_texture = null;

    public void setColor(Color c)
    {
        energy_pattern_center_color = c;
    }

    public void setTexture(Texture t)
    {
        energy_pattern_center_texture = t;
    }

    public void link()
    {
        corresponding_transmission_energy_pattern_center.GetComponent<UnityEngine.UI.RawImage>().texture = energy_pattern_center_texture;
        corresponding_transmission_energy_pattern_center.GetComponent<UnityEngine.UI.RawImage>().color = energy_pattern_center_color;
        foreach (Transform t in corresponding_transmission_energy_pattern_center.transform)
        {
            if (t.GetComponent<UnityEngine.UI.RawImage>() != null)
            {
                t.GetComponent<UnityEngine.UI.RawImage>().color = energy_pattern_center_color;
            }
            else if (t.GetComponent<UnityEngine.UI.Image>() != null)
            {
                t.GetComponent<UnityEngine.UI.Image>().color = energy_pattern_center_color;
            }
            else if (t.GetComponent<TMP_Text>() != null)
            {
                t.GetComponent<TMP_Text>().color = energy_pattern_center_color;
            }
        }
    }
}