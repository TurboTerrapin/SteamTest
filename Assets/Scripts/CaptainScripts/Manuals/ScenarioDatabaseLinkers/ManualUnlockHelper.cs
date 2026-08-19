/*
    ManualUnlockHelper.cs
    - Used for unlocking a hidden screen in the manual
    Contributor(s): Jake Schott
    Last Updated: 8/19/2026
*/

using UnityEngine;
using System.Collections.Generic;

public class ManualUnlockHelper : MonoBehaviour, IManualLinker
{
    [SerializeField]
    private List<CanvasGroup> fade_groups;
    [SerializeField]
    private List<GameObject> locked_messages;

    public void link()
    {
        foreach (CanvasGroup group in fade_groups)
        {
            group.alpha = 1.0f;
        }
        foreach (GameObject message in locked_messages)
        {
            message.SetActive(false);
        }
    }
}