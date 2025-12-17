using System.Collections.Generic;
using UnityEngine;


//Copied from user TwoTen at https://discussions.unity.com/t/tutorial-how-to-make-clothes-animate-along-with-character/667297
//Sets the position of each bone in the current skinned mesh renderer to the position of the bones in the target skinned mesh renderer

public class CopyBonePosition : MonoBehaviour
{
    public SkinnedMeshRenderer TargetMeshRenderer;

    void Start()
    {
        Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();
        foreach (Transform bone in TargetMeshRenderer.bones)
            boneMap[bone.gameObject.name] = bone;


        SkinnedMeshRenderer myRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();

        Transform[] newBones = new Transform[myRenderer.bones.Length];
        for (int i = 0; i < myRenderer.bones.Length; ++i)
        {
            GameObject bone = myRenderer.bones[i].gameObject;
            if (!boneMap.TryGetValue(bone.name, out newBones[i]))
            {
                Debug.Log("Unable to map bone \"" + bone.name + "\" to target skeleton.");
                break;
            }
        }
        myRenderer.bones = newBones;

    }
}