/*
    HintsManager.cs
    - Handles displaying helpful hints on left side of screen
    Contributor(s): Jake Schott
    Last Updated: 8/8/2026
*/

using System.Collections;
using TMPro;
using UnityEngine;
using Unity.Netcode;

public class HintsManager : NetworkBehaviour
{
    //CLASS CONSTANTS
    private static float HINT_SHOW_TIME = 1.0f;
    private static float HINT_HIDE_TIME = 0.25f;
    private static float HINT_FLASH_TIME = 0.5f;
    private static Color[] HINT_BORDER_COLOR_OPTIONS = new Color[] { ReferenceAssistor.COLOR_OPTIONS[0], ReferenceAssistor.COLOR_OPTIONS[1], ReferenceAssistor.COLOR_OPTIONS[2], ReferenceAssistor.COLOR_OPTIONS[3], Color.red };
    private static Texture[] HINT_ICON_OPTIONS = new Texture[] { null, null, null, null, null }; //pilot, tactician, engineer, captain, info
    private static Color[] HINT_ICON_COLOR_OPTIONS = new Color[] { ReferenceAssistor.COLOR_OPTIONS[0], ReferenceAssistor.COLOR_OPTIONS[1], ReferenceAssistor.COLOR_OPTIONS[2], ReferenceAssistor.COLOR_OPTIONS[3], Color.red };

    public GameObject hints_overlay;
    public Texture hint_icon;

    private bool[] hints_occupied = new bool[5] { false, false, false, false, false };
    private Hint[] corresponding_hints = new Hint[5];
    private Coroutine[] movement_coroutines = new Coroutine[5] { null, null, null, null, null };
    private Coroutine flash_coroutine = null;

    public enum HintType
    {
        Pilot,
        Tactician,
        Engineer,
        Captain,
        Danger
    }

    private void Start()
    {
        for (int i = 0; i < 4; i++)
        {
            HINT_ICON_OPTIONS[i] = ReferenceAssistor.Instance.position_icons[i];
        }
        HINT_ICON_OPTIONS[4] = hint_icon;
    }

    public struct Hint
    {
        public string message;
        public HintType hint_type;

        public Hint(string message, HintType hint_type)
        {
            this.message = message;
            this.hint_type = hint_type;
        }
    }

    public void resetHints()
    {
        //reset every hint slot
        for (int i = 0; i < hints_occupied.Length; i++)
        {
            hints_occupied[i] = false;
            corresponding_hints[i] = new Hint();
            if (movement_coroutines[i] != null)
            {
                StopCoroutine(movement_coroutines[i]);
            }
            movement_coroutines[i] = null;
            float vertical_position = hints_overlay.transform.GetChild(i).GetComponent<RectTransform>().anchoredPosition.y;
            hints_overlay.transform.GetChild(i).GetComponent<RectTransform>().anchoredPosition = new Vector2(-2300f, vertical_position);
        }

        //stop flash coroutine
        if (flash_coroutine != null)
        {
            StopCoroutine(flash_coroutine);
            flash_coroutine = null;
        }

        //stop automatic procedure manual hint delivery
        ReferenceAssistor.Instance.module_handlers[3].GetComponent<ProcedureManual>().endHintDelivery();
    }

    private int getIndexOfHint(Hint hint_to_check)
    {
        for (int i = 0; i < hints_occupied.Length; i++)
        {
            if (hint_to_check.message.CompareTo(corresponding_hints[i].message) == 0 && hint_to_check.hint_type == corresponding_hints[i].hint_type)
            {
                return i; //hint already displayed
            }
        }

        return -1;
    }

    private void displayHint(Hint hint_to_add)
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
                GameObject new_hint = hints_overlay.transform.GetChild(i).gameObject;
                new_hint.transform.GetChild(2).GetComponent<TMP_Text>().SetText(hint_to_add.message);
                foreach (Transform t in new_hint.transform.GetChild(1).GetChild(0))
                {
                    t.GetComponent<UnityEngine.UI.RawImage>().color = HINT_BORDER_COLOR_OPTIONS[(int)hint_to_add.hint_type];
                }
                new_hint.transform.GetChild(1).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().texture = HINT_ICON_OPTIONS[(int)hint_to_add.hint_type];
                new_hint.transform.GetChild(1).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = HINT_ICON_COLOR_OPTIONS[(int)hint_to_add.hint_type];

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

    private bool removeHint(Hint hint_to_remove)
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
        AnimationCurve animation_curve = AnimationCurve.EaseInOut(0.0f, 0.0f, HINT_SHOW_TIME, 1.0f);
        float vertical_position = hints_overlay.transform.GetChild(hint_index).GetComponent<RectTransform>().anchoredPosition.y;
        hints_overlay.transform.GetChild(hint_index).gameObject.SetActive(true);
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            hints_overlay.transform.GetChild(hint_index).GetComponent<RectTransform>().anchoredPosition = new Vector2(Mathf.Lerp(-1600f, -2300f, animation_curve.Evaluate(anim_time)), vertical_position);

            yield return null;
        }

        movement_coroutines[hint_index] = null;
    }

    IEnumerator hideHint(int hint_index)
    {
        corresponding_hints[hint_index] = new Hint();

        float anim_time = HINT_HIDE_TIME;
        float starting_horizontal_position = hints_overlay.transform.GetChild(hint_index).GetComponent<RectTransform>().anchoredPosition.x;
        float vertical_position = hints_overlay.transform.GetChild(hint_index).GetComponent<RectTransform>().anchoredPosition.y;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            hints_overlay.transform.GetChild(hint_index).GetComponent<RectTransform>().anchoredPosition = new Vector2(Mathf.Lerp(-2300f, starting_horizontal_position, anim_time / HINT_HIDE_TIME), vertical_position);

            yield return null;
        }

        hints_overlay.transform.GetChild(hint_index).gameObject.SetActive(false);
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
            float a = Mathf.Lerp(0.1f, 1.0f, Mathf.PingPong(elapsed_time, HINT_FLASH_TIME) / HINT_FLASH_TIME);
            foreach (Transform hint in hints_overlay.transform)
            {
                hint.transform.GetComponent<CanvasGroup>().alpha = a;
            }

            yield return null;
        }
    }

    public void addHint(string msg, int hint_type)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        Hint to_add = new Hint(msg, (HintType)hint_type);
        if (getIndexOfHint(to_add) != -1)
        {
            return;
        }

        transmitHintAdditionRPC(msg, hint_type);
    }

    public void removeHint(string msg, int hint_type)
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        transmitHintRemovalRPC(msg, hint_type);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitHintAdditionRPC(string msg, int hint_type)
    {
        Hint hint_to_add = new Hint(msg, (HintType)hint_type);
        displayHint(hint_to_add);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitHintRemovalRPC(string msg, int hint_type)
    {
        Hint hint_to_remove = new Hint(msg, (HintType)hint_type);
        removeHint(hint_to_remove);
    }
}