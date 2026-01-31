/*
    TractorBeamOptions.cs
    - Handles items in tractor beam storage position
    Contributor(s): Jake Schott
    Last Updated: 1/29/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class TractorBeamOptions : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float TURN_TIME = 0.5f;
    private static float RETICLE_SPIN_TIME = 50.0f;

    private List<string> CONTROL_NAMES = new List<string>() { "ITEM DESTROYER", "ITEM COLLECTOR" };
    private List<string> INFO_MESSAGES = new List<string>() { "Destroys the item held in the tractor beam item holding position.", "Collects and stores the item held in the tractor beam item holding position for later use." };
    private List<string> CONTROL_DESCS = new List<string>() {"DESTROY", "COLLECT"};
    private List<int> CONTROL_INDEXES = new List<int>() {6};
    private List<Button>[] BUTTON_LISTS = new List<Button>[2] { new List<Button>(), new List<Button>() };

    public GameObject item_display;
    public GameObject serial_display;
    public List<GameObject> option_displays;
    public List<GameObject> option_dials;
    private TractorBeam tractor_beam;
    private ShipInventory ship_inventory;

    private bool is_powered = false;
    private bool[] is_active = { false, false };
    private float[] dial_turn_percentages = new float[] { 0.0f, 0.0f };
    private Coroutine reticle_spin_coroutine = null;
    private Coroutine dial_turn_coroutine = null;

    private List<string> ray_targets = new List<string> { "tractor_beam_destroy", "tractor_beam_collect" };
    private int ray_target_index = -1;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;

    private void Start()
    {
        tractor_beam = GetComponent<TractorBeam>();
        ship_inventory = GameObject.FindGameObjectWithTag("Spaceship").GetComponent<ShipInventory>();

        hud_info = new HUDInfo(CONTROL_NAMES[0]);
        BUTTON_LISTS[0].Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, false));
        BUTTON_LISTS[1].Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[0], false, false));
        hud_info.setButtons(BUTTON_LISTS[0]);
        hud_info.setInfo(INFO_MESSAGES[0]);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        int index = ray_targets.IndexOf(current_target.name);
        hud_info.setTitle(CONTROL_NAMES[index]);
        hud_info.setButtons(BUTTON_LISTS[index]);
        hud_info.setInfo(INFO_MESSAGES[index]);

        return hud_info;
    }

    //used on scenario transition to automatically collect whatever is inside storage
    public void resetToDefault()
    {
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (tractor_beam.GetCapturedItem() != null)
            {
                transmitItemAdjustmentRPC(1);
            }
        }
    }

    public Color getCapturedItemColor()
    {
        if (tractor_beam.GetCapturedItem() == null)
        {
            return Color.black;
        }
        return tractor_beam.GetCapturedItem().GetComponent<ITractorBeamable>().getItemColor();
    }

    public Texture getCapturedItemTexture()
    {
        if (tractor_beam.GetCapturedItem() == null)
        {
            return null;
        }
        return tractor_beam.GetCapturedItem().GetComponent<ITractorBeamable>().getItemTexture();
    }

    private void displayTransparencyUpdate(float a)
    {
        Color c = item_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color;
        c.a = a;
        item_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = c;
        item_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = c;
        foreach (Transform line in serial_display.transform.GetChild(1))
        {
            line.GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, a);
        }
        foreach (GameObject display in option_displays)
        {
            c = display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color;
            c.a = a;
            display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = c;
            display.transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.Image>().color = c;
        }
    }

    //turns corresponding dial based on dial_turn_percentage
    private void displayDialAdjustment(int index)
    {
        //turn corresponding dial
        option_dials[index].transform.localRotation =
            Quaternion.Euler(option_dials[index].transform.localEulerAngles.x,
                             option_dials[index].transform.localEulerAngles.y,
                             Mathf.Lerp(90.0f, 180.0f, dial_turn_percentages[index]));

        //update fill circle
        option_displays[index].transform.GetChild(0).GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount = Mathf.Max(0.05f, dial_turn_percentages[index]);
    }

    private bool checkNeutralState()
    {
        for (int i = 0; i < 2; i++)
        {
            if (dial_turn_percentages[i] > 0.0f && dial_turn_percentages[i] < 1.0f)
            {
                return false;
            }
        }
        return true;
    }

    IEnumerator reticleSpinner()
    {
        float rot_z = item_display.transform.GetChild(1).localRotation.eulerAngles.z;
        while (true)
        {
            rot_z += (Time.deltaTime * RETICLE_SPIN_TIME) % 360.0f;
            item_display.transform.GetChild(1).localRotation = Quaternion.Euler(0.0f, 0.0f, rot_z);

            yield return null;
        }
    }

    IEnumerator dialActivation()
    {
        do
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            for (int i = 0; i < 2; i++)
            {
                bool able_to_turn = (is_powered == true && ray_target_index == i && is_active[i]);

                if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], keys_down) && able_to_turn) //check if turning
                {
                    dial_turn_percentages[i] = Mathf.Min(1.0f, dial_turn_percentages[i] + (dt / TURN_TIME));
                }
                else
                {
                    dial_turn_percentages[i] = Mathf.Max(0.0f, dial_turn_percentages[i] - (dt / TURN_TIME));
                }
            }

            transmitDialTurnAdjustmentRPC(dial_turn_percentages[0], dial_turn_percentages[1]);

            keys_down.Clear();
            ray_target_index = -1;

            int iterator = 0; //counts frames
            while (keys_down.Count == 0 && iterator < 2)
            {
                yield return null;
                iterator++;
            }
        } while (checkNeutralState() == false);

        if (dial_turn_percentages[0] == 1.0f) //destroy item
        {
            transmitItemAdjustmentRPC(0);
        }
        else if (dial_turn_percentages[1] == 1.0f) //collect item
        {
            transmitItemAdjustmentRPC(1);
        }

        dial_turn_coroutine = null;
    }

    IEnumerator dialReturn()
    {
        float anim_time = TURN_TIME;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);
            for (int i = 0; i < 2; i++)
            {
                dial_turn_percentages[i] = Mathf.Max(0.0f, dial_turn_percentages[i] - (Time.deltaTime / TURN_TIME));
                displayDialAdjustment(i);
            }

            yield return null;
        }

        dial_turn_coroutine = null;
    }

    public void activate(GameObject item, string serial_number)
    {
        Color item_color = getCapturedItemColor();
        item_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = item_color;
        item_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().texture = getCapturedItemTexture();
        item_display.transform.GetChild(0).gameObject.SetActive(true);
        item_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = item_color;
        displayTransparencyUpdate(1.0f);
        if (reticle_spin_coroutine == null)
        {
            reticle_spin_coroutine = StartCoroutine(reticleSpinner());
        }
        for (int i = 0; i < 2; i++)
        {
            is_active[i] = true;
            BUTTON_LISTS[i][0].updateInteractable(true);
        }

        serial_display.transform.GetChild(0).GetComponent<TMP_Text>().text = serial_number;
    }

    public void deactivate()
    {
        if (reticle_spin_coroutine != null)
        {
            StopCoroutine(reticle_spin_coroutine);
            reticle_spin_coroutine = null;
        }
        item_display.transform.GetChild(0).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f);
        item_display.transform.GetChild(0).gameObject.SetActive(false);
        serial_display.transform.GetChild(0).GetComponent<TMP_Text>().text = "";
        displayTransparencyUpdate(0.2f);
        for (int i = 0; i < 2; i++)
        {
            is_active[i] = false;
            BUTTON_LISTS[i][0].updateInteractable(false);
        }
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false)
        {
            return;
        }

        ray_target_index = ray_targets.IndexOf(current_target.name);
        keys_down = inputs;

        if (dial_turn_coroutine == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                dial_turn_coroutine = StartCoroutine(dialActivation());
            }
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        if (tractor_beam.GetCapturedItem() != null)
        {
            activate(tractor_beam.GetCapturedItem(), tractor_beam.GetCapturedItemSerialNumber());
        }
        item_display.SetActive(true);
        serial_display.SetActive(true);
        for (int i = 0; i < 2; i++)
        {
            option_displays[i].SetActive(true);
        }
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        if (is_active[0] == true)
        {
            deactivate();
        }
        item_display.SetActive(false);
        serial_display.SetActive(false);
        for (int i = 0; i < 2; i++)
        {
            option_displays[i].SetActive(false);
        }
    }

    private void transmitDialTurnAdjustmentRPC(float dp_destroy, float dp_collect)
    {
        dial_turn_percentages[0] = dp_destroy;
        dial_turn_percentages[1] = dp_collect;
        for (int i = 0; i < 2; i++)
        {
            displayDialAdjustment(i);
        }
    }

    private void transmitItemAdjustmentRPC(int index)
    {
        if (dial_turn_coroutine != null)
        {
            StopCoroutine(dial_turn_coroutine);
        } 

        if (NetworkManager.Singleton.IsHost == true)
        {
            GameObject item = tractor_beam.GetCapturedItem();
            if (item != null) 
            {
                if (index == 1) //collect
                {
                    CollectibleItem ci = item.GetComponent<CollectibleItem>();
                    ship_inventory.addItem(ci.getItemCategory(), ci.getItemIndex(), ci.getSerialNumber());
                }
                tractor_beam.GetCapturedItem().GetComponent<NetworkObject>().Despawn(true);
            }
            tractor_beam.ClearCapturedItem();
        }

        deactivate();
        dial_turn_coroutine = StartCoroutine(dialReturn());
    }
}