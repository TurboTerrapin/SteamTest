/*
    ManualSelectionHelper.cs
    - Used for color selection in a list of buttons
    Contributor(s): Jake Schott
    Last Updated: 8/19/2026
*/

using UnityEngine;

public class ManualSelectionHelper : MonoBehaviour, IManualLinker
{
    [SerializeField]
    private Color select_color;
    [SerializeField]
    private Color default_color;

    public void link()
    {
        foreach (Transform t in transform.parent)
        {
            if (t != transform)
            {
                ManualColorSwitcher.changeColor(t.gameObject, default_color);
            }
            else
            {
                ManualColorSwitcher.changeColor(t.gameObject, select_color);
            }
        }
    }
}