/*
    BackgroundAnimator.cs
    - Handles screen animations in the background of the ship
    Contributor(s): Jake Schott
    Last Updated: 10/24/2025
*/

using System.Collections.Generic;
using UnityEngine;

public class BackgroundAnimator : MonoBehaviour
{
    public GameObject background_screens;
    public List<GameObject> alternate_screens = null;

    private List<IAnimable> animable_components = new List<IAnimable>();

    private void checkScreen(Transform screen)
    {
        foreach (Transform group in screen.transform.GetChild(0))
        {
            foreach (Component c in group.GetComponents<Component>())
            {
                IAnimable anim_component = c as IAnimable;
                if (anim_component != null)
                {
                    animable_components.Add(anim_component);
                }
            }
        }
    }

    //collect all animable components
    private void Start()
    {
        foreach (Transform screen in background_screens.transform)
        {
            if (screen.transform.GetChild(0).childCount > 1)
            {
                checkScreen(screen);
            }
        }

        foreach (GameObject screen in alternate_screens)
        {
            checkScreen(screen.transform);
        }
    }

    //animate components
    private void Update()
    {
        foreach (IAnimable animation in animable_components)
        {
            animation.animate(Time.deltaTime);
        }
    }
}