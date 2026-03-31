/*
    Manual.cs
    - Parent class for ShipManual and CommunicationsManual
    Contributor(s): Jake Schott
    Last Updated: 1/31/2026
*/

using UnityEngine;

public class Manual : MonoBehaviour, IPowerable
{
    public GameObject welcome_screen;
    public GameObject home_screen;
    public GameObject curr_screen;
    public GameObject manual_logo;

    protected bool is_powered = false;
    protected GameObject curr_button;
    protected int manual_index;
    protected bool[] interactable_options = new bool[6];
    protected bool currently_enabled = false;
    protected Coroutine power_on_coroutine = null;

    public bool getIsPowered()
    {
        return is_powered;
    }

    public bool getCurrentlyEnabled()
    {
        return currently_enabled;
    }

    public bool getCurrentlyAnimating()
    {
        return (power_on_coroutine != null);
    }

    public void switchButtons(int dir)
    {
        GameObject new_button = null;
        if (dir == 0) //up
        {
            new_button = curr_button.GetComponent<ManualButton>().up;
        }
        else if (dir == 1) //down
        {
            new_button = curr_button.GetComponent<ManualButton>().down;
        }
        else if (dir == 2)
        {
            new_button = curr_button.GetComponent<ManualButton>().left;
        }
        else if (dir == 3)
        {
            new_button = curr_button.GetComponent<ManualButton>().right;
        }
        if (new_button != null)
        {
            curr_button.GetComponent<IManualButton>().deselect();
            curr_button = new_button;
            curr_button.GetComponent<IManualButton>().select();
            curr_screen.GetComponent<PanelInfo>().last_pressed_button = new_button;
        }
        updateInteractableButtons();
    }

    public void updateInteractableButtons()
    {
        bool[] selector_options = new bool[6];
        if (curr_screen.GetComponent<PanelInfo>().back_panel != null)
        {
            selector_options[1] = true;
        }
        if (curr_button != null)
        {
            if (curr_button.GetComponent<ManualButton>().select_panel != null)
            {
                selector_options[0] = true;
            }
            if (curr_button.GetComponent<ManualButton>().up != null)
            {
                selector_options[2] = true;
            }
            if (curr_button.GetComponent<ManualButton>().down != null)
            {
                selector_options[3] = true;
            }
            if (curr_button.GetComponent<ManualButton>().left != null)
            {
                selector_options[4] = true;
            }
            if (curr_button.GetComponent<ManualButton>().right != null)
            {
                selector_options[5] = true;
            }
        }
        else
        {
            selector_options[2] = false;
            selector_options[3] = false;
            selector_options[4] = false;
            selector_options[5] = false;
        }
        interactable_options = selector_options;
    }

    public bool isValidInput(int input_index)
    {
        return (interactable_options[input_index]);
    }

    public bool[] getInteractableOptions()
    {
        return interactable_options;
    }

    public void back()
    {
        if (curr_screen.GetComponent<PanelInfo>().back_panel != null)
        {
            if (curr_button != null)
            {
                curr_button.GetComponent<IManualButton>().deselect();
            }
            curr_screen.SetActive(false);
            curr_screen = curr_screen.GetComponent<PanelInfo>().back_panel;
            curr_screen.SetActive(true);
            if (curr_screen.GetComponent<PanelInfo>().last_pressed_button != null)
            {
                curr_button = curr_screen.GetComponent<PanelInfo>().last_pressed_button;
                curr_button.GetComponent<IManualButton>().select();
            }
            else if (curr_screen.GetComponent<PanelInfo>().default_button != null)
            {
                curr_button = curr_screen.GetComponent<PanelInfo>().default_button;
                curr_button.GetComponent<IManualButton>().select();
            }
            else
            {
                curr_button = null;
            }
        }
        updateInteractableButtons();
    }

    public void forward()
    {
        if (curr_button.GetComponent<ManualButton>().select_panel != null)
        {
            curr_button.GetComponent<IManualButton>().deselect();
            curr_screen.SetActive(false);
            curr_screen = curr_button.GetComponent<ManualButton>().select_panel;
            curr_screen.SetActive(true);
            if (curr_button.GetComponent<ManualCodeLinker>() != null)
            {
                curr_button.GetComponent<ManualCodeLinker>().link();
            }
            if (curr_screen.GetComponent<PanelInfo>().default_button != null)
            {
                curr_button = curr_screen.GetComponent<PanelInfo>().default_button;
                curr_button.GetComponent<IManualButton>().select();
                curr_screen.GetComponent<PanelInfo>().last_pressed_button = curr_button;
            }
            else
            {
                curr_button = null;
            }
        }
        updateInteractableButtons();
    }

    protected void cancelActivation()
    {
        if (power_on_coroutine != null)
        {
            StopCoroutine(power_on_coroutine);
            power_on_coroutine = null;
            welcome_screen.SetActive(false);
        }
    }

    public void powerOn(int position)
    {
        is_powered = true;
        currently_enabled = false;
        manual_logo.SetActive(true);
        GetComponent<ManualOnOff>().reactivate(manual_index);
    }

    public void powerOff(int position, float time)
    {
        is_powered = false;
        currently_enabled = false;
        manual_logo.SetActive(false);
        GetComponent<ManualOnOff>().disableManual(manual_index, time);
        cancelActivation();
    }
}