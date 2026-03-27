/*
    AnimatorHandler.cs
    - Handles IK positioning
    - Handles animation events (sitting down, getting up)
    Contributor(s): John Aylward
    Last Updated: 11/26/2025
*/

using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorHandler : MonoBehaviour
{
    [SerializeField]
    private Animator myAnimator = null;


    public enum HandInteractionType {Grasp, Pinch, Press, SidePinch}

    [SerializeField]
    private bool ikActive = true;
    [SerializeField]
    private bool ikHead = true;
    [SerializeField]
    private bool ikRightArm = false;
    [SerializeField]
    private bool ikLeftArm = false;
    [SerializeField]
    private Transform rightHandObj = null;
    [SerializeField]
    private Transform leftHandObj = null;
    [SerializeField]
    private Transform lookObj = null;

    private bool shouldAnimLerp = true;

    void Start()
    {
        myAnimator = GetComponent<Animator>();
        myAnimator.applyRootMotion = true;
    }



    public bool getIKActiveRightArm()
    {
        return ikRightArm;
    }


    public void setIKActive(bool value)
    {
        ikActive = value;
    }

    public void setIKHead(bool value)
    {
        ikHead = value;
    }

    public void setIKRightArm(bool value)
    {
        ikRightArm = value;
    }

    public void setIKLeftArm(bool value)
    {
        ikLeftArm = value;
    }

    public void setRightArmIKPosition(Vector3 pos)
    {
        rightHandObj.position = pos;
    }
    public void setRightArmIKRotation(Quaternion rot)
    {
        rightHandObj.rotation = rot;
    }
    public void setRightArmIKTransform(Transform transform)
    {
        rightHandObj.position = transform.position;
        rightHandObj.rotation = transform.rotation;
    }
    public void flipRightArmIKRotation(bool flip)
    {
        if (!flip) return;

        rightHandObj.rotation *= Quaternion.AngleAxis(180, Vector3.forward);
    }
    //Use this when the animation is a pinch or press that uses both hands
    public void adjustRightArmIKPosition(Vector3 adjustment)
    {
        rightHandObj.position += adjustment;
    }
    public void setLeftArmIKPosition(Vector3 pos)
    {
        leftHandObj.position = pos;
    }

    public void setLeftArmIKRotation(Quaternion rot)
    {
        leftHandObj.rotation = rot;
    }
    public void setLeftArmIKTransform(Transform transform)
    {
        leftHandObj.position = transform.position;
        leftHandObj.rotation = transform.rotation;
    }

    public void setHeadIKPosition(Vector3 pos)
    {
        lookObj.position = pos;
    }

    public void setHandInteractionType(HandInteractionType handInteractionType)
    {
        myAnimator.SetInteger("HandInteractionType", (int)handInteractionType);
    }


    public void setAnimatorLayerWeight(int layer, float weight)
    {
        myAnimator.SetLayerWeight(layer, weight);
    }
    public void setAnimatorLayerWeight(string layerName, float weight)
    {
        myAnimator.SetLayerWeight(myAnimator.GetLayerIndex(layerName), weight);
    }


    public void onSitAnimationEnd()
    {
        if (transform.parent.GetComponent<NetworkObject>().IsOwner == true)
        {
            PrimaryScript.Instance.assumePosition();
        }

    }

    public void setCharacterRotationUp(float rot)
    {
        //transform.parent.localRotation *= Quaternion.AngleAxis(rot, transform.up);
        transform.localRotation *= Quaternion.AngleAxis(rot, transform.up);
        //transform.localRotation = Quaternion.Euler(0, rot, 0);
        Debug.Log("Tried Rotation of object " + transform.name);
    }

    //called on last frame of get up animations
    public void onGetUpAnimationEnd()
    {
        if (transform.parent.GetComponent<NetworkObject>().IsOwner == true)
        {
            if (myAnimator.GetInteger("Seat") == 3)
            {
                transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            }
            else
            {
                myAnimator.applyRootMotion = false;
                myAnimator.StopPlayback();

                Vector3 char_position = transform.position;
                //transform.parent.position = char_position + new Vector3(0.0f, -0.12f, 0.0f);
                transform.position = char_position;
            }

            StartCoroutine(playerReadjustment());
        }
    }

    IEnumerator playerReadjustment()
    {
        Vector3 this_pos = transform.localPosition;

        float anim_time = 0.15f;
        while (anim_time > 0.0f)
        {
            anim_time = Mathf.Max(0.0f, anim_time - Time.deltaTime);

            transform.localPosition = Vector3.Lerp(new Vector3(0.0f, 0.0f, 0.0f), this_pos, anim_time / 0.1f);
            
            yield return null;
        }

        myAnimator.SetBool("GettingUp", false);
        myAnimator.SetBool("SittingDown", false);
        PrimaryScript.Instance.relinquishPosition();
    }
    

    private Vector3 currentR;
    private Vector3 currentL;
    private Quaternion currentRotationR;
    private Quaternion currentRotationL;
    //a callback for calculating IK
    void OnAnimatorIK()
    {
        if (!myAnimator) return;

        //if the IK is active, set the position and rotation directly to the goal.
        if (!ikActive) return;

        if (ikHead)
        {
            // Set the look target position, if one has been assigned
            if (lookObj != null)
            {
                myAnimator.SetLookAtWeight(1);
                myAnimator.SetLookAtPosition(lookObj.position);
            }
        }
        else
        {
            myAnimator.SetLookAtWeight(0);
        }

        if (ikRightArm)
        {
            // Set the right hand target position and rotation, if one has been assigned
            if (rightHandObj != null)
            {
                myAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                myAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);

                if (shouldAnimLerp)
                {
                    //Testing Lerp
                    Vector3 currentPos = myAnimator.GetIKPosition(AvatarIKGoal.RightHand);
                    Vector3 targetPos = rightHandObj.position;

                    currentR = Vector3.Lerp(currentR, targetPos, Time.deltaTime * 5f);
                    myAnimator.SetIKPosition(AvatarIKGoal.RightHand, currentR);

                    currentRotationR = Quaternion.Lerp(currentRotationR, rightHandObj.rotation, Time.deltaTime * 5f);
                    myAnimator.SetIKRotation(AvatarIKGoal.RightHand, currentRotationR);
                }
                else
                {
                    myAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightHandObj.position);
                    myAnimator.SetIKRotation(AvatarIKGoal.RightHand, rightHandObj.rotation);
                }
            }
        }
        //if the IK is not active, set the position and rotation of the hand back to the original position
        else
        {
            myAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            myAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
            //currentR = Vector3.zero;
        }


        if (ikLeftArm)
        {
            if (leftHandObj != null)
            {
                myAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                myAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                
                if (shouldAnimLerp)
                {
                    //Testing Lerp
                    Vector3 currentPos = myAnimator.GetIKPosition(AvatarIKGoal.LeftHand);
                    Vector3 targetPos = leftHandObj.position;

                    currentL = Vector3.Lerp(currentL, targetPos, Time.deltaTime * 5f);
                    myAnimator.SetIKPosition(AvatarIKGoal.LeftHand, currentL);

                    currentRotationL = Quaternion.Lerp(currentRotationL, leftHandObj.rotation, Time.deltaTime * 5f);
                    myAnimator.SetIKRotation(AvatarIKGoal.LeftHand, currentRotationL);
                }
                else
                {
                    myAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandObj.position);
                    myAnimator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandObj.rotation);
                }
            }
        }
        //if the IK is not active, set the position and rotation of the hand back to the original position
        else
        {
            myAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            myAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
            //currentL = Vector3.zero;
        }
    }
}