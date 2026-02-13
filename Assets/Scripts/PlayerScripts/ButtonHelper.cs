/*
    ButtonHelper.cs
    - Assists button animations when necessary
    - Basically only used for IEnumerator
    - Attached to UIPlayerCanvas
    Contributor(s): Jake Schott
    Last Updated: 9/1/2025
*/

using System.Collections;
using UnityEngine;

public class ButtonHelper : MonoBehaviour
{
    IEnumerator toggleWaiter(Button b, float toggle_length)
    {
        while (toggle_length > 0)
        {
            toggle_length -= Time.deltaTime;
            yield return null;
        }
        b.untoggle();
    }

    public void toggleHelper(Button b, float toggle_length)
    {
        StartCoroutine(toggleWaiter(b, toggle_length));
    }
}
