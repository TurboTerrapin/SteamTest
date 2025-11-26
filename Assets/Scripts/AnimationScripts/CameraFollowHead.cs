using UnityEngine;

public class CameraFollowHead : MonoBehaviour
{


    public bool active = false;

    [SerializeField]
    private GameObject headBone = null;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!active) return;

        transform.position = headBone.transform.position;
        transform.rotation = headBone.transform.rotation;

        Debug.DrawRay(transform.position, transform.forward, Color.green);
        //Debug.DrawRay(headBone.transform.position, headBone.transform.forward, Color.red);

    }

    public void SetActive(bool state)
    {
        active = state;
    }




}
