// Settings Controller for the BridgeEnvironment Scene

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BESettingsController : MonoBehaviour
{
    public GameObject SettingsMenu;
    public GameObject PauseMenu;
    // Fullscreen
    public Toggle FullScreenToggle;
    // Resolution
    public TMP_Dropdown ResolutionDropdown;
    // Master Volume
    public Slider MasterVolumeSlider;
    public Image MasterVolumeFillBar;
    public TMP_Text ActualVolumeLabel;
    // HUD Visibility
    public TMP_Dropdown HUDVisibilityDropdown;
    public GameObject control_script_holder;
    // Camera Sensitivity
    public Slider CameraSensitivitySlider;
    public Image CameraSensitivityFillBar;
    public TMP_Text ActualCameraSensitivityLabel;


    void Start()
    {
        // Loads player full screen preference (default is true if nothing is saved)
        bool isFullScreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        // Sets toggle to match saved state
        FullScreenToggle.isOn = isFullScreen;
        // Applies state
        Screen.fullScreen = isFullScreen;

        // Loads player resolution preference (default is 1, 1920 x 1080)
        int ResIndex = PlayerPrefs.GetInt("Resolution", 1);
        // Sets dropdown UI to display option corresponding to selected index
        ResolutionDropdown.value = ResIndex;
        // Applies option based on index
        HandleResolutionDropdownClicked(ResIndex);

        // Loads player volume preferece (default is 50%)
        float volume = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
        MasterVolumeSlider.value = volume;
        HandleMasterVolumeDragged(volume);

        // Loads player HUD visbility preference (default is 0 if nothing is saved)
        int HUDIndex = PlayerPrefs.GetInt("HUDVisibility", 0);
        // Sets dropdown UI to display option corresponding to selected index
        HUDVisibilityDropdown.value = HUDIndex;
        // Applies option based on index
        HandleHUDDropdownClicked(HUDIndex);

        float camSensitivity = PlayerPrefs.GetFloat("CameraSensitivity", 0.5f);
        CameraSensitivitySlider.value = camSensitivity;
        HandleCameraSensitivityDragged(camSensitivity);

        // Listens for changes
        FullScreenToggle.onValueChanged.AddListener(HandleFullScreenToggleClicked);
        ResolutionDropdown.onValueChanged.AddListener(HandleResolutionDropdownClicked);
        MasterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeDragged);
        HUDVisibilityDropdown.onValueChanged.AddListener(HandleHUDDropdownClicked);
        CameraSensitivitySlider.onValueChanged.AddListener(HandleCameraSensitivityDragged);
    }

    public void HandleFullScreenToggleClicked(bool isOn)
    {
        // Applies fullscreen or windowed mode
        Screen.fullScreen = isOn;

        // Saves players preferece (1 = true, 0 = false)
        PlayerPrefs.SetInt("Fullscreen", isOn ? 1 : 0);

        // Writes changes to disk
        PlayerPrefs.Save();
    }

    public void HandleResolutionDropdownClicked(int index)
    {
        int width = 1920;
        int height = 1080;

        // Resolution options
        switch (index)
        {
            case 0:
                width = 1280; height = 720;
                break;
            case 1:
                width = 1920; height = 1080;
                break;
            case 2:
                width = 2560; height = 1440;
                break;
            case 3:
                width = 3840; height = 2160;
                break;
            default:
                width = 1920; height = 1080;
                break;
        }

        // Applies resolution
        Screen.SetResolution(width, height, Screen.fullScreen);

        // Saves player preferences
        PlayerPrefs.SetInt("Resolution", index);

        // Writes changes to disk
        PlayerPrefs.Save();
    }

    public void HandleMasterVolumeDragged(float volume)
    {
        // Sets master volume
        AudioListener.volume = volume;

        // Updates volume text
        int percent = Mathf.RoundToInt(volume * 100);
        ActualVolumeLabel.text = percent.ToString();

        // Saves player preference
        PlayerPrefs.SetFloat("MasterVolume", volume);

        // Writes to disk
        PlayerPrefs.Save();
    }

    public void HandleHUDDropdownClicked(int index)
    {
        // Gets the ControlScript component
        PrimaryScript x = control_script_holder.GetComponent<PrimaryScript>();

        // Sends the index to the control script
        x.setHUD(index);

        // Saves player preferences
        PlayerPrefs.SetInt("HUDVisibility", index);

        // Writes changes to disk
        PlayerPrefs.Save();
    }

    public void HandleCameraSensitivityDragged(float mouseSensitivity)
    {
        // Converts slider value (0-1) to (100-400)
        float actualSensitivity = Mathf.Lerp(1f, 4f, mouseSensitivity);

        // Converts to % and updates sensitivity text
        int percent = Mathf.RoundToInt(mouseSensitivity * 100f);
        ActualCameraSensitivityLabel.text = percent.ToString();

        // Gets main cameras game object
        GameObject cameraObject = Camera.main.gameObject;

        // Gets the PlayerCameraMove component attached to camera
        CameraMove camMove = cameraObject.GetComponent<CameraMove>();

        // Apply sensitivity to camera controller
        camMove.SetMouseSensitvity(actualSensitivity);

        // Saves camera preference
        PlayerPrefs.SetFloat("CameraSensitivity", mouseSensitivity);

        // Writes changes to disk
        PlayerPrefs.Save();
    }

    public void HandleXButtonClick()
    {
        // Closes settings menu
        SettingsMenu.SetActive(false);

        // Opens pause menu
        PauseMenu.SetActive(true);
    }
}
