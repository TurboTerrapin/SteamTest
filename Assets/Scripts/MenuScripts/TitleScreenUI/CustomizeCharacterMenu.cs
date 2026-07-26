using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UIButton = UnityEngine.UI.Button;

public class CustomizeCharacterData
{
    public string CurrentHairHexValue;
    public string CurrentEyeHexValue;
    public string CurrentSkinToneHexValue;

    public string[] CustomHairSwatchHexValues;
    public string[] CustomEyeSwatchHexValues;
    public string[] CustomSkinToneSwatchHexValues;

    public string FirstName;
    public string LastName;

    public int SelectedHairOption;
    public int SelectedClothingOption;
}

public class CustomizeCharacterMenu : MonoBehaviour
{
    public GameObject MainMenu;
    public GameObject CustomizationMenu;

    public TMP_InputField HairHexInput; // HairHexInput
    public UIButton[] HairSwatchButtons; // All swatches
    public UIButton[] CustomHairSwatchButtons; // Buttons #14-22 (custom swatches).
    private int NextHairSwatchIndex = 0; // Keeps track of which swatch to change next

    public TMP_InputField EyeHexInput;
    public UIButton[] EyeSwatchButtons;
    public UIButton[] CustomEyeSwatchButtons;
    private int NextEyeSwatchIndex = 0;

    public TMP_InputField SkinToneHexInput;
    public UIButton[] SkinToneSwatchButtons;
    public UIButton[] CustomSkinToneSwatchButtons;
    private int NextSkinToneSwatchIndex = 0;

    public TMP_InputField FirstNameInput;
    public TMP_InputField LastNameInput;
    private string[] FirstNames = { "Morgan", "Charlie", "Taylor", "Cameron" };
    private string[] LastNames = { "Gregory", "Parsons", "Brannon" };

    public TMP_Text ClothingOptionText;
    public UIButton LeftClothingButton;
    public UIButton RightClothingButton;
    private int CurrentClothingOptionIndex = 0;
    private string[] ClothingOptions = { "Blue Uniform", "Purple Uniform", "Orange Uniform", "Green Uniform" };

    public TMP_Text HairOptionText;
    public UIButton LeftHairButton;
    public UIButton RightHairButton;
    private int CurrentHairOptionIndex = 0;
    private string[] HairOptions = { "Bald", "Short", "Medium", "Long" };
    [SerializeField]
    private List<GameObject> hairModels = new List<GameObject>();

    public MeshRenderer HairRenderer;
    public MeshRenderer LeftEyeRenderer;
    public MeshRenderer RightEyeRenderer;
    public SkinnedMeshRenderer DummyRenderer;


    void Start()
    {
        DeleteCharacterSaveData();
        // Input checks
        HairHexInput.characterLimit = 6;
        HairHexInput.onValidateInput = CheckHexValue;

        EyeHexInput.characterLimit = 6;
        EyeHexInput.onValidateInput = CheckHexValue;

        SkinToneHexInput.characterLimit = 6;
        SkinToneHexInput.onValidateInput = CheckHexValue;

        FirstNameInput.characterLimit = 10;
        FirstNameInput.onValidateInput = CheckName;

        LastNameInput.characterLimit = 10;
        LastNameInput.onValidateInput = CheckName;

        // Listen for when player hits enter, call OnSubmit and pass the hex value to it.
        HairHexInput.onSubmit.AddListener(OnHairHexSubmitted);
        EyeHexInput.onSubmit.AddListener(OnEyeHexSubmitted);
        SkinToneHexInput.onSubmit.AddListener(OnSkinToneHexSubmitted);

        // Listen for when player clicks off of player name inputs
        FirstNameInput.onDeselect.AddListener(OnFirstNameDeselected);
        LastNameInput.onDeselect.AddListener(OnLastNameDeselected);

        // UI to click through different clothing and hair options
        LeftClothingButton.onClick.AddListener(PreviousClothingOption);
        RightClothingButton.onClick.AddListener(NextClothingOption);
        UpdateClothingOptionText();

        LeftHairButton.onClick.AddListener(PreviousHairOption);
        RightHairButton.onClick.AddListener(NextHairOption);
        UpdateHairOptionText();

        foreach (UIButton button in HairSwatchButtons)
        {
            UIButton capturedButton = button;
            capturedButton.onClick.AddListener(() =>
            {
                Color buttonColor = capturedButton.image.color;
                ApplyHairColor(buttonColor);
            });
        }

        foreach (UIButton button in EyeSwatchButtons)
        {
            UIButton capturedButton = button;
            capturedButton.onClick.AddListener(() =>
            {
                Color buttonColor = capturedButton.image.color;
                ApplyEyeColor(buttonColor);
            });
        }

        foreach (UIButton button in SkinToneSwatchButtons)
        {
            UIButton capturedButton = button;
            capturedButton.onClick.AddListener(() =>
            {
                Color buttonColor = capturedButton.image.color;
                ApplySkinTone(buttonColor);
            });
        }

        LoadCharacterData();
    }

    // ------ HANDLE HEX VALUE INPUTS ------

    private void OnHairHexSubmitted(string HairHexValue)
    {
        // allows players to type # if they want
        if (!string.IsNullOrEmpty(HairHexValue))
        {
            if (!HairHexValue.StartsWith("#"))
            {
                HairHexValue = "#" + HairHexValue;
            }
        }
        // If the player enters a valid hex #, convert it to a unity color
        if (ColorUtility.TryParseHtmlString(HairHexValue, out Color newColor))
        {
            // Get the image component of the current swatch (button)
            Image img = CustomHairSwatchButtons[NextHairSwatchIndex].GetComponent<Image>();

            img.color = newColor;

            // Set alpha to 255
            newColor.a = 1f;

            NextHairSwatchIndex++;

            if (NextHairSwatchIndex >= CustomHairSwatchButtons.Length)
            {
                NextHairSwatchIndex = 0;
            }
        }

        HairHexInput.text = "";
    }

    private void OnEyeHexSubmitted(string EyeHexValue)
    {
        // allows players to type # if they want
        if (!string.IsNullOrEmpty(EyeHexValue))
        {
            if (!EyeHexValue.StartsWith("#"))
            {
                EyeHexValue = "#" + EyeHexValue;
            }
        }

        // If the player enters a valid hex #, convert it to a unity color
        if (ColorUtility.TryParseHtmlString(EyeHexValue, out Color newColor))
        {
            // Get the image component of the current swatch (button)
            Image img = CustomEyeSwatchButtons[NextEyeSwatchIndex].GetComponent<Image>();

            img.color = newColor;

            // Set alpha to 255
            newColor.a = 1f;

            NextEyeSwatchIndex++;

            if (NextEyeSwatchIndex >= CustomEyeSwatchButtons.Length)
            {
                NextEyeSwatchIndex = 0;
            }
        }

        EyeHexInput.text = "";
    }

    private void OnSkinToneHexSubmitted(string SkinToneHexValue)
    {

        if (!string.IsNullOrEmpty(SkinToneHexValue))
        {
            if (!SkinToneHexValue.StartsWith("#"))
            {
                SkinToneHexValue = "#" + SkinToneHexValue;
            }
        }

        // If the player enters a valid hex #, convert it to a unity color
        if (ColorUtility.TryParseHtmlString(SkinToneHexValue, out Color newColor))
        {
            // Get the image component of the current swatch (button)
            Image img = CustomSkinToneSwatchButtons[NextSkinToneSwatchIndex].GetComponent<Image>();

            img.color = newColor;

            // Set alpha to 255
            newColor.a = 1f;

            NextSkinToneSwatchIndex++;

            if (NextSkinToneSwatchIndex >= CustomSkinToneSwatchButtons.Length)
            {
                NextSkinToneSwatchIndex = 0;
            }
        }

        SkinToneHexInput.text = "";
    }

    // ------ CHARACTER FIRST AND LAST NAME ------

    private void OnFirstNameDeselected(string FirstName)
    {
        Debug.Log("Player First Name: " + FirstName);
    }

    private void OnLastNameDeselected(string LastName)
    {
        Debug.Log("Player Last Name: " + LastName);
    }

    private void SetDefaultName()
    {
        FirstNameInput.text = FirstNames[Random.Range(0, FirstNames.Length)];
        LastNameInput.text = LastNames[Random.Range(0, LastNames.Length)];
    }

    // ------ CHANGE HAIR/CLOTHING OPTIONS ------

    private void PreviousClothingOption()
    {
        CurrentClothingOptionIndex--;
        if (CurrentClothingOptionIndex < 0)
        {
            // wrap around
            CurrentClothingOptionIndex = ClothingOptions.Length - 1;
        }
        UpdateClothingOptionText();
    }

    private void NextClothingOption()
    {
        CurrentClothingOptionIndex++;
        if (CurrentClothingOptionIndex >= ClothingOptions.Length)
        {
            // wrap around
            CurrentClothingOptionIndex = 0;
        }
        UpdateClothingOptionText();
    }

    private void UpdateClothingOptionText()
    {
        ClothingOptionText.text = ClothingOptions[CurrentClothingOptionIndex];
    }


    private void PreviousHairOption()
    {
        CurrentHairOptionIndex--;
        if (CurrentHairOptionIndex < 0)
        {
            // wrap around
            CurrentHairOptionIndex = HairOptions.Length - 1;
        }
        UpdateHairOptionText();
    }

    private void NextHairOption()
    {
        CurrentHairOptionIndex++;
        if (CurrentHairOptionIndex >= HairOptions.Length)
        {
            // wrap around
            CurrentHairOptionIndex = 0;

        }
        UpdateHairOptionText();
    }

    private void UpdateHairOptionText()
    {
        HairOptionText.text = HairOptions[CurrentHairOptionIndex];
        if (CurrentHairOptionIndex == 0)
        {
            HairRenderer.gameObject.GetComponent<MeshFilter>().mesh = null;
        }
        else
        {
            HairRenderer.gameObject.GetComponent<MeshFilter>().mesh = hairModels[CurrentHairOptionIndex - 1].GetComponent<MeshFilter>().sharedMesh;
        }
    }

    // ------ TEXT INPUT LIMITATIONS/CHECKS ------

    private char CheckName(string text, int charIndex, char addedChar)
    {
        if (char.IsLetter(addedChar))
        {
            // allow
            return addedChar;
        }
        else
        {
            // '\0' tells unity to reject that char
            return '\0';
        }
    }

    private char CheckHexValue(string text, int charIndex, char addedChar)
    {
        // Allow number inputs
        if (char.IsDigit(addedChar))
        {
            // allow
            return addedChar;
        }

        // Convert char to uppercase
        char upperChar = char.ToUpper(addedChar);

        if (upperChar >= 'A' && upperChar <= 'F')
        {
            // allow
            return addedChar;
        }

        // disallow
        return '\0';
    }

    // ------ APPLY COLORS TO CHARACTER ------
    public void ApplyHairColor(Color newColor)
    {
        Material mat = HairRenderer.material;
        mat.SetColor("_BaseColor", newColor);
    }

    public void ApplyEyeColor(Color newColor)
    {
        Material matLeft = LeftEyeRenderer.material;
        Material matRight = RightEyeRenderer.material;
        matLeft.SetColor("_BaseColor", newColor);
        matRight.SetColor("_BaseColor", newColor);
    }

    public void ApplySkinTone(Color newColor)
    {
        Material mat = DummyRenderer.material;
        mat.SetColor("_BaseColor", newColor);
    }

    // ------ SAVE/LOAD CHARACTER DATA ------
    // How this works:
    // 1. We collect the data we want to save into a class to act as a container (CustomizeCharacterData)
    // 2. Then we turn the data into JSON by "string json = JsonUtitlity.ToJson(data);"
    //    Data is the CustomizeCharacterData object that has all the hex values, names, swatches, etc.
    //    Then "JsonUtility.ToJson()" basically coverts that object into a single JSON string.
    // 3. Then we store the JSON string in PlayerPrefs by "PlayerPrefs.SetString("CharacterData", json)"
    //    "CustomizeCharacterData" acts as a key and json is a string we just created.
    // 4. When we load everything, by retrieving the key "CustomizeCharacterData", it gives us the JSON string we made previously.
    //    Then we convert it back into a CustomizeCharacterData object we can use.


    public void SaveCharacterData()
    {
        // New data object
        CustomizeCharacterData data = new CustomizeCharacterData();

        // Save current player data (hair, eyes, skin tone)
        Color HairColor = HairRenderer.material.GetColor("_BaseColor");
        data.CurrentHairHexValue = ColorUtility.ToHtmlStringRGBA(HairColor);

        Color LeftEyeColor = LeftEyeRenderer.material.GetColor("_BaseColor");
        data.CurrentEyeHexValue = ColorUtility.ToHtmlStringRGBA(LeftEyeColor);

        Color SkinTone = DummyRenderer.material.GetColor("_BaseColor");
        data.CurrentSkinToneHexValue = ColorUtility.ToHtmlStringRGBA(SkinTone);

        // Save custom swatches
        data.CustomHairSwatchHexValues = new string[CustomHairSwatchButtons.Length];
        for (int i = 0; i < CustomHairSwatchButtons.Length; i++)
        {
            data.CustomHairSwatchHexValues[i] = ColorUtility.ToHtmlStringRGBA(CustomHairSwatchButtons[i].image.color);
        }

        data.CustomEyeSwatchHexValues = new string[CustomEyeSwatchButtons.Length];
        for (int i = 0; i < CustomEyeSwatchButtons.Length; i++)
        {
            data.CustomEyeSwatchHexValues[i] = ColorUtility.ToHtmlStringRGBA(CustomEyeSwatchButtons[i].image.color);
        }

        data.CustomSkinToneSwatchHexValues = new string[CustomSkinToneSwatchButtons.Length];
        for (int i = 0; i < CustomHairSwatchButtons.Length; i++)
        {
            data.CustomSkinToneSwatchHexValues[i] = ColorUtility.ToHtmlStringRGBA(CustomSkinToneSwatchButtons[i].image.color);
        }

        if (string.IsNullOrEmpty(FirstNameInput.text) || string.IsNullOrEmpty(LastNameInput.text))
        {
            SetDefaultName();
        }

        // Save player name
        data.FirstName = FirstNameInput.text;
        data.LastName = LastNameInput.text;

        // Save selected hair and clothing options
        data.SelectedHairOption = CurrentHairOptionIndex;
        data.SelectedClothingOption = CurrentClothingOptionIndex;

        // Convert data into a single JSON string
        string json = JsonUtility.ToJson(data);

        // Store the string in PlayerPrefs using CustomizeCharacterData as a key
        PlayerPrefs.SetString("CustomizeCharacterData", json);

        // Save
        PlayerPrefs.Save();
    }

    public void LoadCharacterData()
    {

        Debug.Log("Is there a save? " + PlayerPrefs.HasKey("CustomizeCharacterData"));

        if (PlayerPrefs.HasKey("CustomizeCharacterData"))
        {
            // Get the JSON string we stored in PlayerPrefs
            string json = PlayerPrefs.GetString("CustomizeCharacterData");
            // Convert the string back to a CustomizeCharacterData object
            CustomizeCharacterData data = JsonUtility.FromJson<CustomizeCharacterData>(json);

            // Load current hair, eyes, skin tone
            if (ColorUtility.TryParseHtmlString("#" + data.CurrentHairHexValue, out Color HairColor))
            {
                ApplyHairColor(HairColor);
            }

            if (ColorUtility.TryParseHtmlString("#" + data.CurrentEyeHexValue, out Color EyeColor))
            {
                ApplyEyeColor(EyeColor);
            }

            if (ColorUtility.TryParseHtmlString("#" + data.CurrentSkinToneHexValue, out Color SkinTone))
            {
                ApplySkinTone(SkinTone);
            }

            // Load swatches if theres any
            if (data.CustomHairSwatchHexValues != null)
            {
                for (int i = 0; i < data.CustomHairSwatchHexValues.Length; i++)
                {
                    if (ColorUtility.TryParseHtmlString("#" + data.CustomHairSwatchHexValues[i], out Color c))
                    {
                        CustomHairSwatchButtons[i].image.color = c;
                    }
                }

                NextHairSwatchIndex = 0;
                bool foundEmptyHairSwatch = false;

                for (int i = 0; i < CustomHairSwatchButtons.Length; i++)
                {
                    Color c = CustomHairSwatchButtons[i].image.color;

                    if (c.a <= 0.01f)
                    {
                        NextHairSwatchIndex = i;
                        foundEmptyHairSwatch = true;
                        break;
                    }
                }

                if (!foundEmptyHairSwatch)
                {
                    NextHairSwatchIndex = 0;
                }

            }

            if (data.CustomEyeSwatchHexValues != null)
            {
                for (int i = 0; i < data.CustomEyeSwatchHexValues.Length; i++)
                {
                    if (ColorUtility.TryParseHtmlString("#" + data.CustomEyeSwatchHexValues[i], out Color c))
                    {
                        CustomEyeSwatchButtons[i].image.color = c;
                    }
                }

                NextEyeSwatchIndex = 0;
                bool foundEmptyEyeSwatch = false;

                for (int i = 0; i < CustomEyeSwatchButtons.Length; i++)
                {
                    Color c = CustomEyeSwatchButtons[i].image.color;

                    if (c.a <= 0.01f)
                    {
                        NextEyeSwatchIndex = i;
                        foundEmptyEyeSwatch = true;
                        break;
                    }
                }

                if (!foundEmptyEyeSwatch)
                {
                    NextEyeSwatchIndex = 0;
                }
            }

            if (data.CustomSkinToneSwatchHexValues != null)
            {
                for (int i = 0; i < data.CustomSkinToneSwatchHexValues.Length; i++)
                {
                    if (ColorUtility.TryParseHtmlString("#" + data.CustomSkinToneSwatchHexValues[i], out Color c))
                    {
                        CustomSkinToneSwatchButtons[i].image.color = c;
                    }
                }

                NextSkinToneSwatchIndex = 0;
                bool foundEmptySkinSwatch = false;

                for (int i = 0; i < CustomSkinToneSwatchButtons.Length; i++)
                {
                    Color c = CustomSkinToneSwatchButtons[i].image.color;

                    if (c.a <= 0.01f)
                    {
                        NextSkinToneSwatchIndex = i;
                        foundEmptySkinSwatch = true;
                        break;
                    }
                }

                if (!foundEmptySkinSwatch)
                {
                    NextSkinToneSwatchIndex = 0;
                }
            }

            // Load player name
            FirstNameInput.text = data.FirstName;
            LastNameInput.text = data.LastName;

            // Load options selected
            CurrentHairOptionIndex = data.SelectedHairOption;
            CurrentClothingOptionIndex = data.SelectedClothingOption;
            UpdateHairOptionText();
            UpdateClothingOptionText();
        }
        else
        {
            SetDefaultName();
        }
    }

    public void HandleXButtonClick()
    {
        // Closes settings menu
        CustomizationMenu.SetActive(false);
        MainMenu.SetActive(true);
    }

    // TEST METHOD TO ERASE CHARACTER SAVE DATA
    private void DeleteCharacterSaveData()
    {
        PlayerPrefs.DeleteKey("CustomizeCharacterData");
        PlayerPrefs.Save();

        Debug.Log("Character Save Data Deleted");
    }

}