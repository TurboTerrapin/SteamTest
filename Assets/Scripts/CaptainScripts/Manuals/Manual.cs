/*
    Manual.cs
    - Parent class for ProcedureManual and OperatingManual
    Contributor(s): Jake Schott
    Last Updated: 8/24/2026
*/

using TMPro;
using UnityEngine;

public class Manual : MonoBehaviour, IPowerable
{
    public GameObject welcome_screen;
    public GameObject home_screen;
    public GameObject curr_screen;
    public GameObject reusable_elements;
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
        else if (dir == 2) //left
        {
            new_button = curr_button.GetComponent<ManualButton>().left;
        }
        else if (dir == 3) //right
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
        bool[] selector_options = new bool[6] { false, false, false, false, false, false };

        selector_options[1] = (curr_screen.GetComponent<PanelInfo>().back_panel != null);
        if (curr_button != null)
        {
            selector_options[0] = (curr_button.GetComponent<ManualButton>().select_panel != null);
            selector_options[2] = (curr_button.GetComponent<ManualButton>().up != null);
            selector_options[3] = (curr_button.GetComponent<ManualButton>().down != null);
            selector_options[4] = (curr_button.GetComponent<ManualButton>().left != null);
            selector_options[5] = (curr_button.GetComponent<ManualButton>().right != null);
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
            updateReusableElements();
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

    protected void hideReusableElements()
    {
        foreach (Transform t in reusable_elements.transform)
        {
            t.gameObject.SetActive(false);
        }
    }

    private void updateReusableElements()
    {
        //hide all elements to start
        hideReusableElements();

        //set page number
        PanelInfo panel_info = curr_screen.GetComponent<PanelInfo>();
        if (panel_info.page_number.Length > 0)
        {
            reusable_elements.transform.GetChild(0).gameObject.SetActive(true);
            reusable_elements.transform.GetChild(0).GetComponent<TMP_Text>().SetText("PAGE " + panel_info.page_number);
        }

        //recolor footer
        for (int i = 0; i < 5; i++)
        {
            ManualColorSwitcher.changeColor(reusable_elements.transform.GetChild(i).gameObject, panel_info.footer_color);
        }

        //update page buttons
        bool[] page_buttons = new bool[2] { (panel_info.prev_button_placeholder != null) , (panel_info.next_button_placeholder != null) };
        GameObject[] placeholder_info = new GameObject[2] { panel_info.prev_button_placeholder, panel_info.next_button_placeholder };
        for (int i = 0; i < 2; i++)
        {
            reusable_elements.transform.GetChild(1 + i).gameObject.SetActive(page_buttons[i]);
            reusable_elements.transform.GetChild(3 + i).gameObject.SetActive(page_buttons[0] || page_buttons[1]);

            float width = 0.16f;
            float y_pos = 0.11f;
            if (page_buttons[i] == true)
            {
                width = 0.105f;
                y_pos = 0.1375f;
                ManualButtonOptions mbo = placeholder_info[i].GetComponent<ManualButtonOptions>();
                PageButton pb = reusable_elements.transform.GetChild(1 + i).GetComponent<PageButton>();
                pb.up = mbo.button_info[0];
                pb.down = mbo.button_info[1];
                pb.left = mbo.button_info[2];
                pb.right = mbo.button_info[3];
                pb.select_panel = mbo.button_info[4];
            }
            if (i == 0)
            {
                y_pos *= -1.0f;
            }
            reusable_elements.transform.GetChild(3 + i).GetComponent<RectTransform>().sizeDelta = new Vector2(width, 0.002f);
            reusable_elements.transform.GetChild(3 + i).GetComponent<RectTransform>().anchoredPosition = new Vector2(-0.09f, y_pos);
        }

        AnomalyPanelInfo api = curr_screen.GetComponent<AnomalyPanelInfo>();
        if (api != null) //anomaly entry
        {
            //update first step if necessary
            if (api.first_step_destination != null)
            {
                //recolor first step elements
                for (int i = 5; i < 7; i++)
                {
                    ManualColorSwitcher.changeColor(reusable_elements.transform.GetChild(i).gameObject, panel_info.footer_color);
                }

                reusable_elements.transform.GetChild(3).gameObject.SetActive(true);
                reusable_elements.transform.GetChild(3).GetComponent<RectTransform>().sizeDelta = new Vector2(0.18f, 0.002f);
                reusable_elements.transform.GetChild(3).GetComponent<RectTransform>().anchoredPosition = new Vector2(-0.09f, -0.1f);
                reusable_elements.transform.GetChild(5).gameObject.SetActive(true);
                reusable_elements.transform.GetChild(6).gameObject.SetActive(true);
                reusable_elements.transform.GetChild(6).GetComponent<PageButton>().select_panel = api.first_step_destination;
            }

            //update anomaly icon
            if (api.anomaly_icon != null)
            {
                reusable_elements.transform.GetChild(7).gameObject.SetActive(true);
                reusable_elements.transform.GetChild(7).GetComponent<UnityEngine.UI.RawImage>().texture = api.anomaly_icon;
            }
            
            //update anomaly id
            if (api.anomaly_id.Length > 0)
            {
                reusable_elements.transform.GetChild(8).gameObject.SetActive(true);
                reusable_elements.transform.GetChild(8).GetComponent<TMP_Text>().SetText("ANOMALY ID#" + api.anomaly_id);
            }

            //update corresponding info
            if (api.anomaly_observation_info.Length > 0)
            {
                reusable_elements.transform.GetChild(9).gameObject.SetActive(true);
                reusable_elements.transform.GetChild(9).GetComponent<TMP_Text>().SetText(api.anomaly_observation_info);
            }

            //update step info
            if (api.step_number.Length > 0 && api.step_title.Length > 0)
            {
                reusable_elements.transform.GetChild(10).gameObject.SetActive(true);
                reusable_elements.transform.GetChild(11).gameObject.SetActive(true);
                reusable_elements.transform.GetChild(10).GetChild(1).GetComponent<TMP_Text>().SetText("STEP " + api.step_number);
                reusable_elements.transform.GetChild(11).GetComponent<TMP_Text>().SetText(api.step_title);
            }
        }
        else
        {
            OperatingPanelInfo opi = curr_screen.GetComponent<OperatingPanelInfo>();
            if (opi != null) //operating manual entry
            {
                //recolor header elements
                for (int i = 5; i < 8; i++)
                {
                    ManualColorSwitcher.changeColor(reusable_elements.transform.GetChild(i).gameObject, opi.header_color);
                }

                //check for general overview exception
                if (opi.general_overview == true)
                {
                    reusable_elements.transform.GetChild(6).gameObject.SetActive(true);
                    reusable_elements.transform.GetChild(6).GetChild(0).GetComponent<TMP_Text>().SetText(opi.page_name.ToUpper());
                }
                else
                {
                    //update page icon and title
                    if (opi.page_icon != null)
                    {
                        reusable_elements.transform.GetChild(5).gameObject.SetActive(true);
                        reusable_elements.transform.GetChild(5).GetComponent<UnityEngine.UI.RawImage>().texture = opi.page_icon;
                        reusable_elements.transform.GetChild(5).GetChild(0).GetComponent<TMP_Text>().SetText(opi.page_name.ToUpper());
                    }
                }

                //show/hide header line
                reusable_elements.transform.GetChild(7).gameObject.SetActive(opi.header_line == true);

                //update max power usage
                reusable_elements.transform.GetChild(8).gameObject.SetActive(opi.max_power_usage >= 0);
                reusable_elements.transform.GetChild(8).GetChild(0).gameObject.SetActive(opi.max_power_usage > 0);
                if (opi.max_power_usage == 0)
                {
                    reusable_elements.transform.GetChild(8).GetComponent<TMP_Text>().SetText("NO POWER USAGE");
                }
                else if (opi.max_power_usage > 0)
                {
                    reusable_elements.transform.GetChild(8).GetComponent<TMP_Text>().SetText("MAX POWER USAGE:  " + opi.max_power_usage);
                    float x_pos = 0.04f;
                    if (opi.max_power_usage > 9)
                    {
                        x_pos = 0.035f;
                    }
                    reusable_elements.transform.GetChild(8).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(x_pos, 0.0f);
                }
            }
        }
    }

    public void forward()
    {
        if (curr_button.GetComponent<ManualButton>().select_panel != null)
        {
            foreach (IManualLinker ml in curr_button.GetComponents<IManualLinker>())
            {
                ml.link();
            }
            if (curr_button.GetComponent<ManualButton>().select_panel != curr_screen)
            {
                GameObject prev_screen = curr_screen;
                curr_button.GetComponent<IManualButton>().deselect();
                curr_screen.SetActive(false);
                curr_screen = curr_button.GetComponent<ManualButton>().select_panel;
                updateReusableElements();
                foreach (IManualLinker ml in curr_screen.GetComponents<IManualLinker>())
                {
                    ml.link();
                }
                curr_screen.SetActive(true);
                if (curr_screen.GetComponent<PanelInfo>().default_button != null)
                {
                    curr_button = curr_screen.GetComponent<PanelInfo>().default_button;

                    //page button quirk (if previously hit back, default button should be back, not next)
                    if (curr_button.GetComponent<PageButton>() != null)
                    {
                        if (curr_button.GetComponent<PageButton>().left != null && curr_button.GetComponent<PageButton>().left.GetComponent<PageButton>() != null)
                        {
                            if (curr_button.GetComponent<PageButton>().left.name.Contains("Next") == true && curr_button.GetComponent<PageButton>().select_panel == prev_screen)
                            {
                                curr_button = curr_button.GetComponent<PageButton>().left;
                            }
                        }
                    }

                    curr_button.GetComponent<IManualButton>().select();
                    curr_screen.GetComponent<PanelInfo>().last_pressed_button = curr_button;
                }
                else
                {
                    curr_button = null;
                }
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
        hideReusableElements();
        GetComponent<ManualOnOff>().disableManual(manual_index, time);
        cancelActivation();
    }
}