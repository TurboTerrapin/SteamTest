using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCustomization : MonoBehaviour
{

    [SerializeField]
    private GameObject leftEyeObject = null; 
    [SerializeField]
    private GameObject rightEyeObject = null;
    [SerializeField]
    private GameObject playerObject = null;



    [SerializeField]
    private List<GameObject> hairModels = new List<GameObject>();
    [SerializeField]
    private GameObject hairObject = null;
    [SerializeField]
    private int hair = 0;
    [SerializeField]
    private int clothing = 0;




    float timer = 0;

    void Start()
    {
        LoadCharacterData();
    }

    // ------ APPLY COLORS TO CHARACTER ------
    public void ApplyHairColor(Color newColor)
    {
        Material mat = hairObject.GetComponent<MeshRenderer>().material;
        mat.SetColor("_BaseColor", newColor);
    }

    public void ApplyEyeColor(Color newColor)
    {
        Material matLeft = leftEyeObject.GetComponent<MeshRenderer>().material;
        Material matRight = rightEyeObject.GetComponent<MeshRenderer>().material;
        matLeft.SetColor("_BaseColor", newColor);
        matRight.SetColor("_BaseColor", newColor);
    }

    public void ApplySkinTone(Color newColor)
    {
        Material mat = playerObject.GetComponent<SkinnedMeshRenderer>().material;
        mat.SetColor("_BaseColor", newColor);
    }


    public void LoadCharacterData()
    {
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

            // Load player name
            //FirstNameInput.text = data.FirstName;
            //LastNameInput.text = data.LastName;

            // Load options selected
            hair = data.SelectedHairOption;
            clothing = data.SelectedClothingOption;
        }
    }



    public void ChangeHairType(int newHair)
    {
        hair = newHair;

        if (hair == 0)
        {
            hairObject.GetComponent<MeshFilter>().mesh = null;
            return;
        }

        hairObject.GetComponent<MeshFilter>().mesh = hairModels[hair - 1].GetComponent<MeshFilter>().sharedMesh;
    }

    /*
    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 2)
        {
            
            timer = 0;
            ChangeHairType(Random.Range(0, hairModels.Count + 1));

        }



    }*/
}
