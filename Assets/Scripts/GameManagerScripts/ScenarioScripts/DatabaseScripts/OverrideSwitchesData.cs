/*
    OverrideSwitchesData.cs
    - Data holder for override switch combinations
    Contributor(s): Jake Schott
    Last Updated: 6/27/2026
*/

using UnityEngine;

public class OverrideSwitchesData : MonoBehaviour
{
    private bool[] override_switch_configurations = new bool[6] { false, false, false, false, false, false };

    public void setSwitchConfigurations(bool[] new_overrides)
    {
        override_switch_configurations = new_overrides;
    }

    public void setSwitchConfigurations(int[] new_overrides)
    {
        for (int i = 0; i < 6; i++)
        {
            override_switch_configurations[i] = (new_overrides[i] != 0);
        }
    }

    public void setSwitchConfiguraiton(int index, bool enabled)
    {
        override_switch_configurations[index] = enabled;
    }

    public bool getSwitchEnabled(int index)
    {
        return override_switch_configurations[index];
    }
}