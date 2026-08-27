/*
    ManualColorSwitcher.cs
    - Static class used to help switch colors on UI elements in the captain manuals
    Contributor(s): Jake Schott
    Last Updated: 8/26/2026
*/

using TMPro;
using UnityEngine;

public class ManualColorSwitcher
{
    private static void changeColorHelper(Transform t, Color c)
    {
        //only update color if not a cover up UI element
        if (t.gameObject.name.CompareTo("CoverUp") == 0)
        {
            return;
        }

        //check for text or image to recolor
        if (t.GetComponent<TMP_Text>() != null)
        {
            c.a = t.GetComponent<TMP_Text>().color.a;
            t.GetComponent<TMP_Text>().color = c;
        }
        else if (t.GetComponent<UnityEngine.UI.RawImage>() != null)
        {
            c.a = t.GetComponent<UnityEngine.UI.RawImage>().color.a;
            t.GetComponent<UnityEngine.UI.RawImage>().color = c;
        }
        else if (t.GetComponent<UnityEngine.UI.Image>() != null)
        {
            c.a = t.GetComponent<UnityEngine.UI.Image>().color.a;
            t.GetComponent<UnityEngine.UI.Image>().color = c;
        }
    }

    public static void changeColor(GameObject element, Color c)
    {
        changeColorHelper(element.transform, c);
        foreach (Transform t in element.transform)
        {
            changeColorHelper(t, c);
            changeColor(t.gameObject, c);
        }
    }
}
