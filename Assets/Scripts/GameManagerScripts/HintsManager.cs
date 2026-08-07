/*
    HintsManager.cs
    - Handles displaying helpful hints on left side of screen
    Contributor(s): Jake Schott
    Last Updated: 8/7/2026
*/

using System.Collections;
using TMPro;
using UnityEngine;

public class HintsManager : MonoBehaviour
{
    //CLASS CONSTANTS
    private static float HINT_SHOW_TIME = 1.0f;
    private static float HINT_HIDE_TIME = 0.25f;
    private static float HINT_FLASH_TIME = 0.5f;

    public GameObject hints_frame;

    private bool[] hints_occupied = new bool[5] { false, false, false, false, false };
    private Hint[] corresponding_hints = new Hint[5];
    private Coroutine[] movement_coroutines = new Coroutine[5] { null, null, null, null, null };
    private Coroutine flash_coroutine = null;

    public struct Hint
    {
        public string message;
        public Color border_color;
        public Texture hint_icon;
        public Color icon_color;
    }

    public void resetHints()
    {
        for (int i = 0; i < hints_occupied.Length; i++)
        {
            hints_occupied[i] = false;
            corresponding_hints[i] = new Hint();
            if (movement_coroutines[i] != null)
            {
                StopCoroutine(movement_coroutines[i]);
            }
            movement_coroutines[i] = null;
            float vertical_position = hints_frame.transform.GetChild(i).GetComponent<RectTransform>().anchoredPosition.y;
            hints_frame.transform.GetChild(i).GetComponent<RectTransform>().anchoredPosition = new Vector2(-2300f, vertical_position);
        }
        if (flash_coroutine != null)
        {
            StopCoroutine(flash_coroutine);
            flash_coroutine = null;
        }
    }

    private int getIndexOfHint(Hint hint_to_check)
    {
        for (int i = 0; i < hints_occupied.Length; i++)
        {
            if (hint_to_check.message.CompareTo(corresponding_hints[i].message) == 0 && hint_to_check.border_color == corresponding_hints[i].border_color && hint_to_check.hint_icon == corresponding_hints[i].hint_icon && hint_to_check.icon_color == corresponding_hints[i].icon_color)
            {
                return i; //hint already displayed
            }
        }

        return -1;
    }

    public void displayHint(Hint hint_to_add)
    {
        //check if hint already displayed
        if (getIndexOfHint(hint_to_add) != -1)
        {
            return;
        }

        //check if available hint slots
        bool free_slot = false;
        for (int i = 0; i < hints_occupied.Length; i++)
        {
            free_slot = (free_slot || hints_occupied[i] == false);
        }
        if (free_slot == false)
        {
            return;
        }

        //add hint
        for (int i = 0; i < hints_occupied.Length; i++)
        {
            if (hints_occupied[i] == false)
            {
                //record hint
                hints_occupied[i] = true;
                corresponding_hints[i] = hint_to_add;

                //adjust hint appearance
                GameObject new_hint = hints_frame.transform.GetChild(i).gameObject;
                new_hint.transform.GetChild(2).GetComponent<TMP_Text>().SetText(hint_to_add.message);
                foreach (Transform t in new_hint.transform.GetChild(1).GetChild(0))
                {
                    t.GetComponent<UnityEngine.UI.RawImage>().color = hint_to_add.border_color;
                }
                new_hint.transform.GetChild(1).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = hint_to_add.icon_color;
                new_hint.transform.GetChild(1).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().texture = hint_to_add.hint_icon;
                new_hint.GetComponent<RectTransform>().anchoredPosition = new Vector2();

                //animate in
                if (movement_coroutines[i] != null)
                {
                    StopCoroutine(movement_coroutines[i]);
                }
                movement_coroutines[i] = StartCoroutine(showHint(i));

                break;
            }
        }
    }

    public bool removeHint(Hint hint_to_remove)
    {
        //check if hint already displayed
        int hint_remove_index = getIndexOfHint(hint_to_remove);
        if (hint_remove_index == -1)
        {
            return false;
        }

        //remove hint
        if (movement_coroutines[hint_remove_index] != null)
        {
            StopCoroutine(movement_coroutines[hint_remove_index]);
        }
        movement_coroutines[hint_remove_index] = StartCoroutine(hideHint(hint_remove_index));

        return true;
    }

    IEnumerator showHint(int hint_index)
    {
        checkForFlashUpdate();
        float anim_time = HINT_SHOW_TIME;
        float vertical_position = hints_frame.transform.GetChild(hint_index).GetComponent<RectTransform>().anchoredPosition.y;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            hints_frame.transform.GetChild(hint_index).GetComponent<RectTransform>().anchoredPosition = new Vector2(Mathf.Lerp(-1600f, -2300f, anim_time / HINT_SHOW_TIME), vertical_position);

            yield return null;
        }

        movement_coroutines[hint_index] = null;
    }

    IEnumerator hideHint(int hint_index)
    {
        corresponding_hints[hint_index] = new Hint();

        float anim_time = HINT_HIDE_TIME;
        float starting_horizontal_position = hints_frame.transform.GetChild(hint_index).GetComponent<RectTransform>().anchoredPosition.x;
        float vertical_position = hints_frame.transform.GetChild(hint_index).GetComponent<RectTransform>().anchoredPosition.y;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            hints_frame.transform.GetChild(hint_index).GetComponent<RectTransform>().anchoredPosition = new Vector2(Mathf.Lerp(-2300f, starting_horizontal_position, anim_time / HINT_HIDE_TIME), vertical_position);

            yield return null;
        }

        hints_occupied[hint_index] = false;
        movement_coroutines[hint_index] = null;
        checkForFlashUpdate();
    }

    private void checkForFlashUpdate()
    {
        int number_of_active_hints = 0;
        for (int i = 0; i < hints_occupied.Length; i++)
        {
            if (hints_occupied[i] == true)
            {
                number_of_active_hints++;
            }
        }
        if (number_of_active_hints == 0 && flash_coroutine != null)
        {
            StopCoroutine(flash_coroutine);
            flash_coroutine = null;
        } 
        else if (number_of_active_hints > 0 && flash_coroutine == null) 
        {    
            flash_coroutine = StartCoroutine(hintFlasher());
        }
    }

    IEnumerator hintFlasher()
    {
        float elapsed_time = 0.0f;
        while (true)
        {
            elapsed_time += Time.deltaTime;
            float a = Mathf.Lerp(0.2f, 1.0f, Mathf.PingPong(elapsed_time, HINT_FLASH_TIME));
            foreach (Transform hint in hints_frame.transform)
            {
                hint.transform.GetChild(1).GetComponent<CanvasGroup>().alpha = a;
            }

            yield return null;
        }
    }
}