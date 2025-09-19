using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UIButton = UnityEngine.UI.Button;

public class CustomizeCharacterMenu : MonoBehaviour
{
    public TMP_InputField HairHexInput; // HairHexInput
    public UIButton[] HairSwatchButtons; // All swatches
    public UIButton[] CustomHairSwatchButtons; // buttons #14-22 (custom swatches).
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

    public TMP_Text ClothingOptionText;
    public UIButton LeftClothingButton;
    public UIButton RightClothingButton;
    private int CurrentClothingOptionIndex = 0;
    private string[] ClothingOptions = { "Option 1", "Option 2", "Option 3" };

    public TMP_Text HairOptionText;
    public UIButton LeftHairButton;
    public UIButton RightHairButton;
    private int CurrentHairOptionIndex = 0;
    private string[] HairOptions = { "Short", "Medium", "Long" };

    public MeshRenderer hairRenderer;
    public MeshRenderer leftEyeRenderer;
    public MeshRenderer rightEyeRenderer;
    public MeshRenderer dummyRenderer;
    

    void Start()
    {
        // Input checks
        HairHexInput.characterLimit = 6;
        HairHexInput.onValidateInput = CheckHexValue;

        EyeHexInput.characterLimit = 6;
        EyeHexInput.onValidateInput = CheckHexValue;

        SkinToneHexInput.characterLimit = 6;
        SkinToneHexInput.onValidateInput = CheckHexValue;

        FirstNameInput.characterLimit = 8;
        FirstNameInput.onValidateInput = CheckName;

        LastNameInput.characterLimit = 8;
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
    }

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

    private void OnFirstNameDeselected(string FirstName)
    {
        Debug.Log("Player First Name: " + FirstName);
    }

    private void OnLastNameDeselected(string LastName)
    {
        Debug.Log("Player Last Name: " + LastName);
    }

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
    }

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

    public void ApplyHairColor(Color newColor)
    {
        Material mat = hairRenderer.material;
        mat.SetColor("_BaseColor", newColor);
    }

    public void ApplyEyeColor(Color newColor)
    {
        Material matLeft = leftEyeRenderer.material;
        Material matRight = rightEyeRenderer.material;
        matLeft.SetColor("_BaseColor", newColor);
        matRight.SetColor("_BaseColor", newColor);
    }

    public void ApplySkinTone(Color newColor)
    {
        Material mat = dummyRenderer.material;
        mat.SetColor("_BaseColor", newColor);
    }
}
