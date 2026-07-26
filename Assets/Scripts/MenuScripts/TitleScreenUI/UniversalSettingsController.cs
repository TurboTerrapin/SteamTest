// Universal Settings Controller

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class UniversalSettingsController : MonoBehaviour
{
    public GameObject SettingsMenu;
    public GameObject MainMenu;
    public GameObject PauseMenu;

    // Tabs
    public GameObject VideoTab;
    public GameObject AudioTab;
    public GameObject GameTab;

    // Fullscreen
    public Toggle FullScreenToggle;

    // Resolution
    public TMP_Dropdown ResolutionDropdown;

    // Vsync
    public Toggle VSyncToggle;

    // Frame Rate
    public TMP_Dropdown FrameRateDropdown;
    public TMP_Text FrameRateLabel;
    public CanvasGroup FrameRateGroup;

    public AudioMixer MasterMixer;

    // Master Volume
    public Slider MasterVolumeSlider;
    public Image MasterVolumeFillBar;
    public TMP_Text ActualMasterVolumeLabel;

    // Music Volume
    public Slider MusicVolumeSlider;
    public Image MusicVolumeFillBar;
    public TMP_Text ActualMusicVolumeLabel;

    // SFX Volume
    public Slider SFXVolumeSlider;
    public Image SFXVolumeFillBar;
    public TMP_Text ActualSFXVolumeLabel;

    // Camera Sensitivity
    public Slider CameraSensitivitySlider;
    public Image CameraSensitivityFillBar;
    public TMP_Text ActualCameraSensitivityLabel;

    // HUD Visibility
    public TMP_Dropdown HUDVisibilityDropdown;

    // Info Visibility
    public Toggle InfoVisibilityToggle;
    public TMP_Text InfoVisibilityLabel;
    public CanvasGroup InfoVisibilityGroup;

    void Start()
    {
        // Loads player full screen preference (default is true if nothing is saved)
        bool isFullScreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        // Sets toggle to match saved state
        FullScreenToggle.isOn = isFullScreen;
        // Applies state
        Screen.fullScreen = isFullScreen;

        // Set dropdown UI to display possible options
        ResolutionDropdown.ClearOptions();
        // Gather possible resolution options based on the max resolution
        Resolution maxResolution = Screen.resolutions[Screen.resolutions.Length - 1];
        Vector2[] defaultResolutionOptions = new Vector2[4] { new Vector2(1280, 720), new Vector2(1920, 1080), new Vector2(2560, 1440), new Vector2(3840, 2160) };
        List<TMP_Dropdown.OptionData> possibleResolutionOptions = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < defaultResolutionOptions.Length; i++)
        {
            // Add resolution to options if default (1280 x 720) or is less than max resolution (x, y)
            if (i == 0 || (defaultResolutionOptions[i].x <= maxResolution.width && defaultResolutionOptions[i].y <= maxResolution.height))
            {
                TMP_Dropdown.OptionData res_option = new TMP_Dropdown.OptionData();
                res_option.text = defaultResolutionOptions[i].x + " x " + defaultResolutionOptions[i].y;
                possibleResolutionOptions.Add(res_option);
            }
        }
        ResolutionDropdown.AddOptions(possibleResolutionOptions);
        // Loads player resolution preference (default is highest possible resolution)
        int ResIndex = PlayerPrefs.GetInt("Resolution", possibleResolutionOptions.Count - 1);
        if (ResIndex > possibleResolutionOptions.Count - 1)
        {
            ResIndex = possibleResolutionOptions.Count - 1;
        }
        // Sets dropdown UI to display option corresponding to selected index
        ResolutionDropdown.value = ResIndex;
        // Applies option based on index
        HandleResolutionDropdownClicked(ResIndex);

        // Loads player volume preferece (default is 50%)
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
        MasterVolumeSlider.value = masterVolume;
        HandleMasterVolumeDragged(masterVolume);

        // Loads player volume preferece (default is 50%)
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        MusicVolumeSlider.value = musicVolume;
        HandleMusicVolumeDragged(musicVolume);

        // Loads player volume preferece (default is 50%)
        float SFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        SFXVolumeSlider.value = musicVolume;
        HandleSFXVolumeDragged(musicVolume);

        // Loads player VSync preference (default is true if nothing is saved)
        bool isVSyncOn = PlayerPrefs.GetInt("VSync", 0) == 1;
        VSyncToggle.isOn = isVSyncOn;
        QualitySettings.vSyncCount = isVSyncOn ? 1 : 0;
        
        // If VSync is on when you load up the game, Max Frame Rate setting is dimmed out
        if (isVSyncOn == true)
        {
            FrameRateLabel.alpha = 0.2f;
            FrameRateGroup.alpha = 0.2f;
        }

        // Loads player VSync preference (default is 60FPS)
        int FPSIndex = PlayerPrefs.GetInt("MaxFrameRate", 1);
        FrameRateDropdown.value = FPSIndex;
        HandleMaxFrameRateDropDownClicked(FPSIndex);

        // Loads player cam sensitivity preference (default is 50%)
        float camSensitivity = PlayerPrefs.GetFloat("CameraSensitivity", 0.5f);
        CameraSensitivitySlider.value = camSensitivity;
        HandleCameraSensitivityDragged(camSensitivity);

        // Loads player HUD visbility preference (default is 0 if nothing is saved)
        int HUDIndex = PlayerPrefs.GetInt("HUDVisibility", 0);
        // Sets dropdown UI to display option corresponding to selected index
        HUDVisibilityDropdown.value = HUDIndex;
        // Applies option based on index
        HandleHUDDropdownClicked(HUDIndex);

        // Loads player hints toggle preference (default is true if nothing is saved)
        bool isInfoVisibilityOn = PlayerPrefs.GetInt("InfoVisibility", 0) == 1;
        // Sets dropdown UI to display option corresponding to selected index
        InfoVisibilityToggle.isOn = isInfoVisibilityOn;
        // Applies info invisibility
        HandleInfoVisibilityToggleClicked(isInfoVisibilityOn);

        // Listens for changes
        FullScreenToggle.onValueChanged.AddListener(HandleFullScreenToggleClicked);
        ResolutionDropdown.onValueChanged.AddListener(HandleResolutionDropdownClicked);
        VSyncToggle.onValueChanged.AddListener(HandleVSyncToggleClicked);
        FrameRateDropdown.onValueChanged.AddListener(HandleMaxFrameRateDropDownClicked);
        MasterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeDragged);
        MusicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeDragged);
        SFXVolumeSlider.onValueChanged.AddListener(HandleSFXVolumeDragged);
        CameraSensitivitySlider.onValueChanged.AddListener(HandleCameraSensitivityDragged);
        HUDVisibilityDropdown.onValueChanged.AddListener(HandleHUDDropdownClicked);
        InfoVisibilityToggle.onValueChanged.AddListener(HandleInfoVisibilityToggleClicked);
    }

    // For testing FPS
    //void Update()
    //{
    //    if (Time.frameCount % 60 == 0)
    //    {
    //        //Debug.Log("Vsync on: " + (QualitySettings.vSyncCount > 0));
    //        //Debug.Log("Target frame rate: " + Application.targetFrameRate);
    //        //Debug.Log("Current fps: " + (1f / Time.deltaTime));
    //    }
    //}

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
        int width = 1280;
        int height = 720;

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
                width = 1280; height = 720;
                break;
        }

        // Applies resolution
        Screen.SetResolution(width, height, Screen.fullScreen);

        // Saves player preferences
        PlayerPrefs.SetInt("Resolution", index);

        // Writes changes to disk
        PlayerPrefs.Save();
    }

    public void HandleVSyncToggleClicked(bool isOn)
    {
        QualitySettings.vSyncCount = isOn ? 1 : 0;

        // if vsync is on, let it control the frame rate
        if (isOn)
        {
            Application.targetFrameRate = -1;
        }
        else
        {
            HandleMaxFrameRateDropDownClicked(FrameRateDropdown.value);
        }

        PlayerPrefs.SetInt("VSync", isOn ? 1 : 0);

        PlayerPrefs.Save();

        FrameRateDropdown.interactable = !isOn;

        FrameRateGroup.alpha = isOn ? 0.2f : 1f;
        FrameRateLabel.alpha = isOn ? 0.2f : 1f;
    }

    public void HandleMaxFrameRateDropDownClicked(int index)
    {
        int FPS;

        switch (index)
        {
            case 0:
                FPS = 30;
                break;
            case 1:
                FPS = 60;
                break;
            case 2:
                FPS = 120;
                break;
            case 3:
                FPS = 144;
                break;
            case 4:
                FPS = 240;
                break;
            case 5:
                FPS = -1; // unlimited
                break;
            default:
                FPS = 60;
                break;
        }

        if (QualitySettings.vSyncCount == 0)
        {
            Application.targetFrameRate = FPS;
        }

        PlayerPrefs.SetInt("MaxFrameRate", index);

        PlayerPrefs.Save();
    }

    public void HandleMasterVolumeDragged(float volume)
    {
        MasterMixer.SetFloat("Master", Mathf.Log10(volume) * 20);

        // Updates volume text
        int percent = Mathf.RoundToInt(volume * 100);
        ActualMasterVolumeLabel.text = percent.ToString();

        // Saves player preference
        PlayerPrefs.SetFloat("MasterVolume", volume);

        // Writes to disk
        PlayerPrefs.Save();
    }


    public void HandleMusicVolumeDragged(float volume)
    {
        MasterMixer.SetFloat("Music", 20 * Mathf.Log10(Mathf.Max(volume, 0.0001f)));

        // Updates volume text
        int percent = Mathf.RoundToInt(volume * 100);
        ActualMusicVolumeLabel.text = percent.ToString();

        // Saves player preference
        PlayerPrefs.SetFloat("MusicVolume", volume);

        // Writes to disk
        PlayerPrefs.Save();
    }

    public void HandleSFXVolumeDragged(float volume)
    {
        MasterMixer.SetFloat("SFX", 20 * Mathf.Log10(Mathf.Max(volume, 0.0001f)));

        // Updates volume text
        int percent = Mathf.RoundToInt(volume * 100);
        ActualSFXVolumeLabel.text = percent.ToString();

        // Saves player preference
        PlayerPrefs.SetFloat("SFXVolume", volume);

        // Writes to disk
        PlayerPrefs.Save();
    }

    public float GetCameraSensitivity()
    {
        float mouseSensitivity = PlayerPrefs.GetFloat("CameraSensitivity");

        // Converts slider value (0-1) to (.25-3)
        float actualSensitivity = Mathf.Lerp(0.25f, 3f, mouseSensitivity);

        return actualSensitivity;
    }

    public void HandleCameraSensitivityDragged(float mouseSensitivity)
    {
        // Saves camera preference
        PlayerPrefs.SetFloat("CameraSensitivity", mouseSensitivity);

        // Writes changes to disk
        PlayerPrefs.Save();

        // Converts to % and updates sensitivity text
        int percent = Mathf.RoundToInt(mouseSensitivity * 100f);
        ActualCameraSensitivityLabel.text = percent.ToString();

        if (PrimaryScript.Instance != null)
        {
            // Sends the sensitivity to PrimaryScript
            PrimaryScript.Instance.setCameraSensitivity(GetCameraSensitivity());
        }
    }

    public void HandleHUDDropdownClicked(int index)
    {
        if (PrimaryScript.Instance != null)
        {
            // Sends the index to PrimaryScript
            PrimaryScript.Instance.setHUD(index);
        }

        // Saves player preferences
        PlayerPrefs.SetInt("HUDVisibility", index);

        // Writes changes to disk
        PlayerPrefs.Save();

        InfoVisibilityToggle.interactable = index < 2;
        InfoVisibilityLabel.alpha = index < 2 ? 1f : 0.2f;
        InfoVisibilityGroup.alpha = index < 2 ? 1f : 0.2f;
    }

    public void HandleInfoVisibilityToggleClicked(bool isOn)
    {
        if (PrimaryScript.Instance != null)
        {
            // Sends the bool to PrimaryScript
            PrimaryScript.Instance.setInfoVisibilityEnabled(isOn);
        }

        // Saves players preferece (1 = true, 0 = false)
        PlayerPrefs.SetInt("InfoVisibility", isOn ? 1 : 0);

        // Writes changes to disk
        PlayerPrefs.Save();
    }

    public void HandleVideoTabClicked()
    {
        SwitchTabs(VideoTab);
    }

    public void HandleAudioTabClicked()
    {
        SwitchTabs(AudioTab);
    }

    public void HandleGameTabClicked()
    {
        SwitchTabs(GameTab);
    }

    public void HandleXButtonClick()
    {
        // Closes settings menu
        SettingsMenu.SetActive(false);

        if (MainMenu != null)
        {
            // Opens main menu
            MainMenu.SetActive(true);
        }

        if (PauseMenu != null)
        {
            // Opens main menu
            PauseMenu.SetActive(true);
        }
    }

    public void SwitchTabs(GameObject target)
    {
        VideoTab.SetActive(false);
        AudioTab.SetActive(false);
        GameTab.SetActive(false);

        target.SetActive(true);
    }
}