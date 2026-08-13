/*
    ManualUnlockHelper.cs
    - Used for unlocking a hidden screen in the manual
    Contributor(s): Jake Schott
    Last Updated: 8/13/2026
*/

using UnityEngine;

public class ManualUnlockHelper : MonoBehaviour, IManualLinker
{
    [SerializeField]
    private CanvasGroup fade_group;
    [SerializeField]
    private GameObject locked_message;

    public void link()
    {
        if (fade_group != null)
        {
            fade_group.alpha = 1.0f;
        }
        if (locked_message != null)
        {
            locked_message.SetActive(false);
        }
    }
}