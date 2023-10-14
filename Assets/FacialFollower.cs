using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacialFollower : MonoBehaviour
{
    public SkinnedMeshRenderer source;
    public SkinnedMeshRenderer target;
    public int count;


    // Update is called once per frame
    void LateUpdate()
    {
        if (source == null || target == null)
            return;

        for(int i =0; i <count; ++i)
        {
            target.SetBlendShapeWeight(i, source.GetBlendShapeWeight(i));
        }
    }
}
