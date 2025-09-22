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

        transform.position = new Vector3(headBone.transform.position.x, headBone.transform.position.y - 0.1f, headBone.transform.position.z);
        transform.rotation = headBone.transform.rotation;



    }
}
