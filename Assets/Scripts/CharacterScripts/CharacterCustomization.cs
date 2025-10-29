using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCustomization : MonoBehaviour
{



    [SerializeField]
    private List<GameObject> hairModels = new List<GameObject>();
    [SerializeField]
    private GameObject hairObject = null;
    [SerializeField]
    private int hair = 0;



    float timer = 0;










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
