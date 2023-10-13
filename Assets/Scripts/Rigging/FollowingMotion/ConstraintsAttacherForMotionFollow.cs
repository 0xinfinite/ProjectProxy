using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.Entities.UniversalDelegates;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

[ExecuteInEditMode()]
public class ConstraintsAttacherForMotionFollow : MonoBehaviour
{
    public bool removeMode;

    public BodyBones sourceBones;
    public BodyBones targetBones;

    private void OnEnable()
    {
        if (removeMode)
        {
            ParentConstraint[] prt = FindObjectsOfType<ParentConstraint>();

            LookAtConstraint[] look = FindObjectsOfType<LookAtConstraint>();

            RotationConstraint[] rot = FindObjectsOfType<RotationConstraint>();

            
            for(int i = prt.Length-1; i>=0; --i)
            {
                if (prt[i].transform.root == targetBones.pelvis.root)
                    DestroyImmediate(prt[i]);
            }
            for (int i = look.Length - 1; i >= 0; --i)
            {
                if (look[i].transform.root == targetBones.pelvis.root)
                    DestroyImmediate(look[i]);
            }
            for (int i = rot.Length - 1; i >= 0; --i)
            {
                if (rot[i].transform.root == targetBones.pelvis.root)
                    DestroyImmediate(rot[i]);
            }
            this.enabled = false;
            return;
        }

        targetBones.pelvis.AddComponent<ParentConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.pelvis, weight = 1 });
        targetBones.thigh_l.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.thigh_l, weight = 1 });
        targetBones.calf_l.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform= sourceBones.calf_l, weight = 1 });
        targetBones.foot_l.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.foot_l, weight = 1 });
        targetBones.toe_l.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.toe_l, weight = 1 });
        targetBones.thigh_r.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.thigh_r, weight = 1 });
        targetBones.calf_r.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.calf_r, weight = 1 });
        targetBones.foot_r.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.foot_r, weight = 1 });
        targetBones.toe_r.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.toe_r, weight = 1 });
        targetBones.spine1.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.spine1, weight = 1 });
        targetBones.spine2.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.spine2, weight = 1 });
        if(targetBones.spine3!=null && sourceBones.spine3!=null)
        targetBones.spine3.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.spine3, weight = 1 });
        targetBones.clavicle_l.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.clavicle_l, weight = 1 });
        targetBones.upperArm_l.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.upperArm_l, weight = 1 });
        targetBones.forearm_l.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.forearm_l, weight = 1 });
        targetBones.wrist_l.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.wrist_l, weight = 1 });
        targetBones.thumb_l_1.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.thumb_l_1, weight = 1 });
        targetBones.thumb_l_2.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.thumb_l_2, weight = 1 });
        targetBones.thumb_l_3.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.thumb_l_3, weight = 1 });
        targetBones.index_l_1.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.index_l_1, weight = 1 });
        targetBones.index_l_2.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.index_l_2, weight = 1 });
        targetBones.index_l_3.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.index_l_3, weight = 1 });
        targetBones.middle_l_1.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.middle_l_1, weight = 1 });
        targetBones.middle_l_2.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.middle_l_2, weight = 1 });
        targetBones.middle_l_3.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.middle_l_3, weight = 1 });
        targetBones.ring_l_1.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.ring_l_1, weight = 1 });
        targetBones.ring_l_2.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.ring_l_2, weight = 1 });
        targetBones.ring_l_3.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.ring_l_3, weight = 1 });
        targetBones.pinky_l_1.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.pinky_l_1, weight = 1 });
        targetBones.pinky_l_2.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.pinky_l_2, weight = 1 });
        targetBones.pinky_l_3.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.pinky_l_3, weight = 1 });
        targetBones.clavicle_r.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.clavicle_r, weight = 1 });
        targetBones.upperArm_r.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.upperArm_r, weight = 1 });
        targetBones.forearm_r.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.forearm_r, weight = 1 });
        targetBones.wrist_r.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.wrist_r, weight = 1 });
        targetBones.thumb_r_1.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.thumb_r_1, weight = 1 });
        targetBones.thumb_r_2.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.thumb_r_2, weight = 1 });
        targetBones.thumb_r_3.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.thumb_r_3, weight = 1 });
        targetBones.index_r_1.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.index_r_1, weight = 1 });
        targetBones.index_r_2.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.index_r_2, weight = 1 });
        targetBones.index_r_3.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.index_r_3, weight = 1 });
        targetBones.middle_r_1.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.middle_r_1, weight = 1 });
        targetBones.middle_r_2.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.middle_r_2, weight = 1 });
        targetBones.middle_r_3.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.middle_r_3, weight = 1 });
        targetBones.ring_r_1.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.ring_r_1, weight = 1 });
        targetBones.ring_r_2.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.ring_r_2, weight = 1 });
        targetBones.ring_r_3.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.ring_r_3, weight = 1 });
        targetBones.pinky_r_1.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.pinky_r_1, weight = 1 });
        targetBones.pinky_r_2.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.pinky_r_2, weight = 1 });
        targetBones.pinky_r_3.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.pinky_r_3, weight = 1 });
        targetBones.neck.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.neck, weight = 1 });
        targetBones.head.AddComponent<RotationConstraint>().AddSource(new ConstraintSource() { sourceTransform = sourceBones.head, weight = 1 });


        //p.GetComponent<IConstraint>().;

        StartCoroutine(ActiveConstraint());

    }
    IEnumerator ActiveConstraint()
    {
        ParentConstraint[] prt = FindObjectsOfType<ParentConstraint>();

        LookAtConstraint[] look = FindObjectsOfType<LookAtConstraint>();

        RotationConstraint[] rot = FindObjectsOfType<RotationConstraint>();

        foreach(ParentConstraint p in prt)
        {
            p.constraintActive = true;
            yield return null;
            p.weight = 0;
            yield return null;
            p.locked = false;
            yield return null;
            p.weight = 1;
            yield return null;
            p.locked = true;
            yield return null;
        }

        foreach (LookAtConstraint l in look)
        {
            // l.locked = true;
            l.constraintActive = true;
            l.locked = true;
        }
        foreach (RotationConstraint r in rot)
        {
            // r.locked = true;
            r.constraintActive = true;
            yield return null;
            r.weight = 0;
            yield return null;
            r.locked = false;
            yield return null;
            r.weight = 1;
            yield return null;
            r.locked = true;
            yield return null;
        }


        this.enabled = false;
    }
}
