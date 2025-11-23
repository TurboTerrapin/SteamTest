//using Unity.Netcode;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [SerializeField]
    private Animator myAnimator = null;
    [SerializeField]
    private AnimatorHandler myAnimatorHandler = null;

    [SerializeField]
    private float x, y, z, w;

    //private void Update()
    //{
    //    x = transform.localRotation.x; y = transform.localRotation.y; z = transform.localRotation.z; w = transform.localRotation.w;
    //}

    public void setIKActive(bool value)
    {
        myAnimatorHandler.setIKActive(value);
    }
    public void setIKHead(bool value)
    {
        myAnimatorHandler.setIKHead(value);
    }
    public void setIKRightArm(bool value)
    {
        myAnimatorHandler.setIKRightArm(value);
    }
    public void setIKLeftArm(bool value)
    {
        myAnimatorHandler.setIKLeftArm(value);
    }
    public void setRightArmIKPosition(Vector3 pos)
    {
        myAnimatorHandler.setRightArmIKPosition(pos);
    }
    public void setLeftArmIKPosition(Vector3 pos)
    {
        myAnimatorHandler.setLeftArmIKPosition(pos);
    }
    public void setHeadIKPosition(Vector3 pos)
    {
        myAnimatorHandler.setHeadIKPosition(pos);
    }

    public void setCharacterPosition()
    {
        myAnimator.transform.localPosition = Vector3.zero;
    }
    public void setCharacterPosition(Vector3 pos)
    {
        myAnimator.transform.localPosition = pos;
    }
    public void setCharacterPositionXY()
    {
        myAnimator.transform.localPosition = new Vector3(0, -0.3f, 0);
    }
    public void setCharacterPositionXY(Vector3 pos)
    {
        pos = new Vector3(pos.x, myAnimator.transform.position.y, pos.z);
        myAnimator.transform.localPosition = pos;
    }

    public void setPlayerRotationLock(bool value)
    {
        GetComponent<CameraMove>().parentRotationLock = value;
    }

    public void setPlayerForwardRotation()
    {
        transform.localRotation = Quaternion.identity;
    }
    public void setPlayerForwardRotation(Quaternion direction)
    {
        transform.localRotation = direction;
    }

    /*
    [Rpc(SendTo.Server)]
    public void SendPlayerData()
    {

    }
    */
}
