/*
    CargoEject.cs
    - Launches item loaded in cargo bay
    Contributor(s): Jake Schott
    Last Updated: 1/23/2026
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class CargoEject : NetworkBehaviour, IControllable, IPowerable
{
    //CLASS CONSTANTS
    private static float ARM_TIME = 0.8f;
    private static float PUSH_TIME = 0.8f;
    private static float COOLDOWN_TIME = 1.5f;
    private static float CARGO_TRANSFORM_ADJUSTMENT_TIME = 3.0f;
    private static Vector3 PUSH_DIRECTION = new Vector3(0.006f, -0.0151f, 0.0f);
    private static float[] SPAWN_X_COORDINATES = new float[] { -8.0f, 8.0f }; //cargo spawn positions so they don't bump into each other

    private string CONTROL_NAME = "CARGO EJECT";
    private static string INFO_MESSAGE = "Ejects whatever cargo is held in the cargo eject as loaded in the engineer position.";
    private List<string> CONTROL_DESCS = new List<string>() { "EJECT", "ARM" };
    private List<int> CONTROL_INDEXES = new List<int>() { 6, 11 };
    private List<Button> BUTTONS = new List<Button>(0);

    public Material lit_red;
    public Material lit_green;
    public Material unlit_red;
    public Material unlit_green;

    public GameObject dial;
    public GameObject cargo_jettison_display;
    public GameObject active_indicator;
    public GameObject inactive_indicator;
    public List<GameObject> ejectable_items;

    private CargoEjectLoader cargo_eject_loader;

    private bool is_powered = false;
    private bool is_active = false;
    private int spawn_index = 0;
    private float dial_turn_percentage = 0.0f;
    private Vector3 initial_pos;
    private Coroutine dial_turn_coroutine = null;
    private Coroutine cargo_eject_coroutine = null;

    private List<KeyCode> keys_down = new List<KeyCode>();

    private static HUDInfo hud_info = null;

    private void Start()
    {
        cargo_eject_loader = transform.GetComponent<CargoEjectLoader>();

        hud_info = new HUDInfo(CONTROL_NAME);

        BUTTONS.Add(new Button(CONTROL_DESCS[0], CONTROL_INDEXES[0], false, true));
        BUTTONS.Add(new Button(CONTROL_DESCS[1], CONTROL_INDEXES[1], false, false));
        initial_pos = dial.transform.localPosition;

        hud_info.setButtons(BUTTONS);
        hud_info.setInfo(INFO_MESSAGE);
    }

    public HUDInfo getHUDinfo(GameObject current_target)
    {
        return hud_info;
    }

    private void setDisplayColor(Color c)
    {
        cargo_jettison_display.transform.GetChild(0).GetComponent<TMP_Text>().color = c;
        cargo_jettison_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = c;
        foreach (Transform image in cargo_jettison_display.transform.GetChild(3))
        {
            image.GetComponent<UnityEngine.UI.RawImage>().color = c;
        }
    }

    public void activate()
    {
        is_active = true;
        cargo_jettison_display.transform.GetChild(0).GetComponent<TMP_Text>().SetText("READY");
        cargo_jettison_display.transform.GetChild(1).GetComponent<UnityEngine.UI.RawImage>().texture = cargo_eject_loader.getCurrentItemImage();
        cargo_jettison_display.transform.GetChild(1).gameObject.SetActive(true);
        cargo_jettison_display.transform.GetChild(2).gameObject.SetActive(false);

        Color icon_color = cargo_eject_loader.getCurrentItemColor();
        icon_color.a = 1.0f;
        setDisplayColor(icon_color);

        if (is_powered == true)
        {
            active_indicator.GetComponent<Renderer>().material = lit_green;
            inactive_indicator.GetComponent<Renderer>().material = unlit_red;
        }

        BUTTONS[1].updateInteractable(is_powered && cargo_eject_coroutine == null);
    }

    public void deactivate()
    {
        is_active = false;
        cargo_jettison_display.transform.GetChild(0).GetComponent<TMP_Text>().SetText("EMPTY");
        cargo_jettison_display.transform.GetChild(1).gameObject.SetActive(false);
        cargo_jettison_display.transform.GetChild(2).gameObject.SetActive(true);
        setDisplayColor(new Color(0.0f, 0.84f, 1.0f, 0.2f));

        if (is_powered == true)
        {
            active_indicator.GetComponent<Renderer>().material = unlit_green;
            inactive_indicator.GetComponent<Renderer>().material = lit_red;
        }

        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(false);
    }

    private void displayDialTurn()
    {
        dial.transform.localRotation =
            Quaternion.Euler(dial.transform.localEulerAngles.x,
                             dial.transform.localEulerAngles.y,
                             Mathf.Lerp(-90.0f, -180.0f, dial_turn_percentage));
    }

    private bool checkNeutralState()
    {
        if (dial_turn_percentage > 0.0f && cargo_eject_coroutine == null)
        {
            return false;
        }
        return true;
    }

    IEnumerator dialTurn()
    {
        while (keys_down.Count > 0 || checkNeutralState() == false)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);

            if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], keys_down) && is_powered == true && is_active == true)
            {
                dial_turn_percentage = Mathf.Min(1.0f, dial_turn_percentage + (dt / ARM_TIME));
            }
            else
            {
                dial_turn_percentage = Mathf.Max(0.0f, dial_turn_percentage - (dt / ARM_TIME));
            }
            BUTTONS[0].updateInteractable(dial_turn_percentage >= 1.0f && is_powered);

            transmitDialArmRPC(dial_turn_percentage);

            keys_down.Clear();
            yield return null;
        }

        dial_turn_coroutine = null;
    }

    //run by the host to push the launched cargo item away from the ship
    IEnumerator cargoTransformAdjustment(GameObject ejected_item)
    {
        float anim_time = CARGO_TRANSFORM_ADJUSTMENT_TIME;

        while (ejected_item != null && anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            ejected_item.transform.localPosition = new Vector3(ejected_item.transform.localPosition.x, ejected_item.transform.localPosition.y, Mathf.Lerp(40.0f, 0.0f, anim_time / CARGO_TRANSFORM_ADJUSTMENT_TIME));

            yield return null;
        }
        if (ejected_item != null)
        {
            Transform world_root = GameObject.FindGameObjectWithTag("WorldRoot").transform;
            ejected_item.GetComponent<NetworkObject>().TrySetParent(world_root);
            Collider c = ejected_item.GetComponent<Collider>();
            c.excludeLayers = LayerMask.GetMask("None");
        }
    }

    IEnumerator ejectCargo()
    {
        //spawn item as a NetworkObject if host
        if (NetworkManager.Singleton.IsHost == true)
        {
            Transform spaceship = GameObject.FindGameObjectWithTag("Spaceship").transform;
            int eject_index = cargo_eject_loader.getEjectItemIndex();
            GameObject ejected_item = GameObject.Instantiate(ejectable_items[eject_index], spaceship);
            spawn_index += 1;
            if (spawn_index > SPAWN_X_COORDINATES.Length - 1)
            {
                spawn_index = 0;
            }
            ejected_item.transform.position = new Vector3(spaceship.position.x + SPAWN_X_COORDINATES[spawn_index], spaceship.position.y - 7.0f, spaceship.position.z);
            ejected_item.transform.rotation = spaceship.rotation;
            Vector3 curr_rotation = ejected_item.transform.rotation.eulerAngles;
            ejected_item.transform.rotation = Quaternion.Euler(curr_rotation.x + Random.Range(-15.0f, 15.0f), curr_rotation.y + Random.Range(-15.0f, 15.0f), curr_rotation.z + Random.Range(-15.0f, 15.0f));
            ejected_item.GetComponent<NetworkObject>().SpawnWithOwnership(0, true);
            ejected_item.GetComponent<NetworkObject>().TrySetParent(spaceship);
            ejected_item.GetComponent<CollectibleItem>().setSerialNumber(cargo_eject_loader.getCurrentItemSerialNumber());
            StartCoroutine(cargoTransformAdjustment(ejected_item));
        }
        deactivate();
        cargo_eject_loader.onCargoEject();

        dial.transform.localPosition = initial_pos;
        dial.transform.localRotation =
            Quaternion.Euler(dial.transform.localEulerAngles.x,
                             dial.transform.localEulerAngles.y,
                             -180.0f);

        Vector3 final_pos = initial_pos + PUSH_DIRECTION;

        //push the dial in
        float push_time = PUSH_TIME;
        while (push_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            push_time = Mathf.Max(0.0f, push_time - dt);

            dial.transform.localPosition = Vector3.Lerp(initial_pos, final_pos, 1.0f - (push_time / PUSH_TIME));

            yield return null;
        }

        //bring the dial back and unrotate
        float cooldown_time = COOLDOWN_TIME;
        while (cooldown_time > 0.0f)
        {
            float dt = Mathf.Min(Time.deltaTime, 1.0f / 30.0f);
            cooldown_time = Mathf.Max(0.0f, cooldown_time - dt);

            dial.transform.localPosition = Vector3.Lerp(initial_pos, final_pos, cooldown_time / COOLDOWN_TIME);

            dial.transform.localRotation =
                Quaternion.Euler(dial.transform.localEulerAngles.x,
                                 dial.transform.localEulerAngles.y,
                                 Mathf.Lerp(-90.0f, -180.0f, cooldown_time / COOLDOWN_TIME));

            yield return null;
        }

        BUTTONS[1].updateInteractable(is_powered && is_active);
        dial_turn_percentage = 0.0f;

        dial_turn_coroutine = null;
        cargo_eject_coroutine = null;
    }

    public void handleInputs(List<KeyCode> inputs, GameObject current_target, float dt, int position)
    {
        if (is_powered == false || is_active == false)
        {
            return;
        }

        keys_down = inputs;

        //check for eject
        if (dial_turn_percentage >= 1.0f && cargo_eject_coroutine == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[0], inputs))
            {
                BUTTONS[0].toggle(0.2f);
                BUTTONS[0].updateInteractable(false);
                BUTTONS[1].updateInteractable(false);
                transmitEjectRPC();
                return;
            }
        }

        //check for dial turn
        if (dial_turn_percentage == 0.0f && cargo_eject_coroutine == null)
        {
            if (ControlScript.checkInputIndex(CONTROL_INDEXES[1], inputs))
            {
                if (dial_turn_coroutine == null)
                {
                    dial_turn_coroutine = StartCoroutine(dialTurn());
                }
            }
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        if (is_active == true)
        {
            activate();
        }
        cargo_jettison_display.SetActive(true);
        BUTTONS[0].updateInteractable(false);
        BUTTONS[1].updateInteractable(cargo_eject_coroutine == null && is_active);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        cargo_jettison_display.SetActive(false);
        active_indicator.GetComponent<Renderer>().material = unlit_green;
        inactive_indicator.GetComponent<Renderer>().material = unlit_red;
        BUTTONS[0].updateInteractable(false);
        BUTTONS[0].untoggle();
        BUTTONS[1].updateInteractable(false);
    }

    [Rpc(SendTo.Everyone)]
    private void transmitDialArmRPC(float dp)
    {
        dial_turn_percentage = dp;
        if (cargo_eject_coroutine == null)
        {
            displayDialTurn();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void transmitEjectRPC()
    {
        if (cargo_eject_coroutine != null)
        {
            StopCoroutine(cargo_eject_coroutine);
        }
        if (dial_turn_coroutine != null)
        {
            StopCoroutine(dial_turn_coroutine);
        }

        cargo_eject_coroutine = StartCoroutine(ejectCargo());
    }
}