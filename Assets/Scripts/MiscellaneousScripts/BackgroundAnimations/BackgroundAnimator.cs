/*
    BackgroundAnimator.cs
    - Handles screen animations in the background of the ship
    Contributor(s): Jake Schott
    Last Updated: 11/10/2025
*/

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class BackgroundAnimator : MonoBehaviour
{
    public GameObject background_screens;
    public List<GameObject> alternate_screens = null;

    private List<GameObject> screen_displays = new List<GameObject>();
    private List<IAnimable> animable_components = new List<IAnimable>();

    private Coroutine screen_enable_coroutine = null;

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
                screen_displays.Add(screen.transform.GetChild(0).GetChild(1).gameObject);
                checkScreen(screen);
            }
        }

        foreach (GameObject screen in alternate_screens)
        {
            checkScreen(screen.transform);
        }
    }

    public void disableAllScreens()
    {
        foreach (GameObject screen in screen_displays)
        {
            screen.SetActive(false);
        }
    }

    public void enableAllScreens()
    {
        foreach (GameObject screen in screen_displays)
        {
            screen.SetActive(true);
        }
    }

    public void enableAllScreens(float time)
    {
        if (screen_enable_coroutine != null)
        {
            StopCoroutine(screen_enable_coroutine);
        }

        screen_enable_coroutine = StartCoroutine(screenEnableSequence(time));
    }

    IEnumerator screenEnableSequence(float time)
    {
        List<GameObject> all_screens = new List<GameObject>();
        for (int i = 0; i < screen_displays.Count; i++)
        {
            all_screens.Add(screen_displays[i].gameObject);
        }

        List<GameObject> screens_to_enable = new List<GameObject>();
        for (int i = 0; i < screen_displays.Count; i++)
        {
            int index = Random.Range(0, all_screens.Count - 1);
            screens_to_enable.Add(all_screens[index]);
            all_screens.RemoveAt(index);
        }

        float anim_time = time;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            for (int x = 0; x < screens_to_enable.Count; x++)
            {
                screens_to_enable[x].gameObject.SetActive((x * 1.0f / screens_to_enable.Count) <= (1.0f - (anim_time / time)));
            }

            yield return null;
        }
        enableAllScreens();

        screen_enable_coroutine = null;
    }

    //animate components
    private void Update()
    {
        foreach (IAnimable animation in animable_components)
        {
            animation.animate(Mathf.Min(1.0f / 30.0f, Time.deltaTime));
        }
    }
}