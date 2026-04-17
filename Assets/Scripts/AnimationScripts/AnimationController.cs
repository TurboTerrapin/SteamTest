//using Unity.Netcode;
using UnityEngine;
using static AnimatorHandler;
using static UnityEngine.Rendering.DebugUI.Table;

public class AnimationController : MonoBehaviour
{
    [SerializeField]
    private Animator myAnimator = null;
    [SerializeField]
    private AnimatorHandler myAnimatorHandler = null;

    /*
    [SerializeField]
    private float x, y, z, w;

    [SerializeField]
    private float Movement, Forward, HandPose;

    [SerializeField]
    private bool SittingDown, GettingUp, IsLeft;

    [SerializeField]
    private int Seat;

    private void Update()
    {
        //x = transform.localRotation.x; y = transform.localRotation.y; z = transform.localRotation.z; w = transform.localRotation.w;
        SittingDown = myAnimator.GetBool("SittingDown");
        GettingUp = myAnimator.GetBool("GettingUp");
        Movement = myAnimator.GetFloat("Movement");
        Forward = myAnimator.GetFloat("Forward");
        Seat = myAnimator.GetInteger("Seat");
        IsLeft = myAnimator.GetBool("IsLeft");
        HandPose = myAnimator.GetFloat("HandPose");
    }
    */
    public bool getIKActiveRightArm()
    {
        return myAnimatorHandler.getIKActiveRightArm();
    }

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
    public void setRightArmIKRotation(Quaternion rot)
    {
        myAnimatorHandler.setRightArmIKRotation(rot);
    }
    public void setRightArmIKTransform(Transform transform)
    {
        myAnimatorHandler.setRightArmIKTransform(transform);
    }
    public void flipRightArmIKRotation(bool flip)
    {
        myAnimatorHandler.flipRightArmIKRotation(flip);
    }
    public void adjustRightArmIKPosition(Vector3 adjustment)
    {
        myAnimatorHandler.adjustRightArmIKPosition(adjustment);
    }
    public void setLeftArmIKPosition(Vector3 pos)
    {
        myAnimatorHandler.setLeftArmIKPosition(pos);
    }
    public void setLeftArmIKRotation(Quaternion rot)
    {
        myAnimatorHandler.setLeftArmIKRotation(rot);
    }
    public void setLeftArmIKTransform(Transform transform)
    {
        myAnimatorHandler.setLeftArmIKTransform(transform);
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
    public void setCharacterPositionXZ()
    {
        myAnimator.transform.localPosition = new Vector3(0, -0.3f, 0);
    }
    public void setCharacterPositionX(float pos)
    {
        myAnimator.transform.localPosition = new Vector3(pos, 0, 0);
    }
    public void setCharacterPositionZ(float pos)
    {
        myAnimator.transform.localPosition = new Vector3(0, 0, pos);
    }
    public void setCharacterPositionXZ(Vector3 pos)
    {
        pos = new Vector3(pos.x, myAnimator.transform.position.y, pos.z);
        myAnimator.transform.localPosition = pos;
    }
    public void setCharacterRotationUp(float rot)
    {
        myAnimatorHandler.setCharacterRotationUp(rot);
    }

    public void setAnimatorLayerWeight(int layer, float weight)
    {
        myAnimator.SetLayerWeight(layer, weight);
    }
    public void setAnimatorLayerWeight(string layerName, float weight)
    {
        myAnimator.SetLayerWeight(myAnimator.GetLayerIndex(layerName), weight);
    }
    public void setHandPose(float pose)
    {
        //myAnimator[layerName].speed = 0;
        myAnimator.SetFloat("HandAnimSpeed", 0);
        myAnimator.SetFloat("HandPose", pose);
    }
    public void setLerpSpeed(float speed)
    {
        myAnimatorHandler.setLerpSpeed(speed);
    }
    public void resetLerpSpeed()
    {
        myAnimatorHandler.resetLerpSpeed();
    }
    public void setAnimatorBool(string name, bool value)
    {
        myAnimator.SetBool(name, value);
    }
    public void setAnimatorFloat(string name, float value)
    {
        myAnimator.SetFloat(name, value);
    }
    public void setAnimatorInteger(string name, int value)
    {
        myAnimator.SetInteger(name, value);
    }
    public void setHandInteractionType(HandInteractionType handInteractionType)
    {
        myAnimator.SetInteger("HandInteractionType", (int)handInteractionType);
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
