using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;



[System.Serializable]
public struct SecondaryBones
{
    public enum Axis { Forward = 1, Up = 2, Right = 3, Back = -1, Down = -2, Left = -3 , None = 0 }

    public enum SecondaryBone { Shoulder = 0, Armpit, Elbow, Hip, Knee}

    public Transform shoulder_l;
    public Transform shoulder_r;
    public Transform armpit_l;
    public Transform armpit_r;
    public Transform elbow_l;
    public Transform elbow_r;
    public Transform hip_l;
    public Transform hip_r;
    public Transform knee_l;
    public Transform knee_r;

    private VirtualAxisTransform vat_shoulder_l;
    private VirtualAxisTransform vat_shoulder_r;
    private VirtualAxisTransform vat_armpit_l;
    private VirtualAxisTransform vat_armpit_r;
    private VirtualAxisTransform vat_elbow_l;
    private VirtualAxisTransform vat_elbow_r;
    private VirtualAxisTransform vat_hip_l;
    private VirtualAxisTransform vat_hip_r;
    private VirtualAxisTransform vat_knee_l;
    private VirtualAxisTransform vat_knee_r;

    private Vector3 shoulder_l_localPos;
    private Vector3 shoulder_r_localPos;
    private Vector3 armpit_l_localPos;
    private Vector3 armpit_r_localPos;
    private Vector3 elbow_l_localPos;
    private Vector3 elbow_r_localPos;
    private Vector3 hip_l_localPos;
    private Vector3 hip_r_localPos;
    private Vector3 knee_l_localPos;
    private Vector3 knee_r_localPos;

    public Transform upperBody;
    public Vector3 upperBodyForwardAxis;
    public Vector3 upperBodyUpAxis;
    public Transform upperArm_l;
    public Vector3 upperArm_l_axis;
    public Vector3 upperArm_l_bendAxis;
    public Transform upperArm_r;
    public Vector3 upperArm_r_axis;
    public Vector3 upperArm_r_bendAxis;
    public Transform forearm_l;
    //public Vector3 forearm_l_axis;
    public Transform forearm_r;
    //public Vector3 forearm_r_axis;
    public Transform pelvis;
    public Vector3 pelvisForwardAxis;
    public Vector3 pelvisUpAxis;
    public Transform thigh_l;
    public Vector3 thigh_l_axis;
    public Vector3 thigh_l_bendAxis;
    public Transform thigh_r;
    public Vector3 thigh_r_axis;
    public Vector3 thigh_r_bendAxis;
    public Transform calf_l;
    //public Vector3 calf_l_axis;
    public Transform calf_r;
    //public Vector3 calf_r_axis;

    private Matrix4x4 upperBodyWorldAxis;
    private Matrix4x4 pelvisWorldAxis;

    //private float upperArmLength;
   // private float thighLength;

    private bool[] boneExists;
    public static Vector3 GetVectorFromAxis( Axis axis)
    {
        switch (axis)
        {
            case Axis.Forward:
                return Vector3.forward;
            case Axis.Up:
                return Vector3.up;
            case Axis.Right:
                return Vector3.right;
            case Axis.Back:
                return Vector3.forward * -1;
            case Axis.Down:
                return Vector3.up * -1;
            case Axis.Left:
                return Vector3.right * -1;
        }
        return Vector3.zero;
    }
    public static Vector3 GetVectorFromAxis(Transform tf, Axis axis)
    {
        switch (axis)
        {
            case Axis.Forward:
                return tf.forward;
            case Axis.Up:
                return tf.up;
            case Axis.Right:
                return tf.right;
            case Axis.Back:
                return tf.forward * -1;
            case Axis.Down:
                return tf.up * -1;
            case Axis.Left:
                return tf.right * -1;
        }
        return Vector3.zero;
    }
    public static Axis ReversedAxis(Axis a)
    {
        return (Axis)((int)a * -1);
    }

    public void InitBones(Transform transform)
    {
        boneExists = new bool[(int)SecondaryBone.Knee + 1];
        //upperArmLength = Vector3.Distance(upperArm_l.position, forearm_l.position);
        //thighLength = Vector3.Distance(thigh_l.position, calf_l.position);

        Vector3 worldUpperBodyUpAxis = upperBody.TransformVector(upperBodyUpAxis);//GetVectorFromAxis(upperBody, upperBodyUpAxis);
        Vector3 worldUpperBodyForwardAxis = upperBody.TransformVector(upperBodyForwardAxis);// GetVectorFromAxis(upperBody, upperBodyForwardAxis);
        Vector3 worldPelvisUpAxis = pelvis.TransformVector(pelvisUpAxis);//GetVectorFromAxis(pelvis, pelvisUpAxis);
        Vector3 worldPelvisForwardAxis = pelvis.TransformVector(pelvisForwardAxis);// GetVectorFromAxis(pelvis, pelvisForwardAxis);
        Quaternion upperBodyRotation = Quaternion.LookRotation(worldUpperBodyForwardAxis, worldUpperBodyUpAxis);
        Quaternion pelvisRotation = Quaternion.LookRotation(worldPelvisForwardAxis, worldPelvisUpAxis);

        upperBodyWorldAxis = GetCenterAxis(upperBody, upperBodyForwardAxis, upperBodyUpAxis);
        pelvisWorldAxis = GetCenterAxis(pelvis, pelvisForwardAxis, pelvisUpAxis);

        if (shoulder_l != null)
        {
            boneExists[(int)SecondaryBone.Shoulder] = true;
            
            vat_shoulder_l = new VirtualAxisTransform(shoulder_l, GetShoulderRotation( upperBody, upperBodyWorldAxis, upperArm_l, upperArm_l_axis, Axis.Left, upperBodyForwardAxis, upperBodyUpAxis));
            vat_shoulder_r = new VirtualAxisTransform(shoulder_r, GetShoulderRotation(upperBody, upperBodyWorldAxis, upperArm_r, upperArm_r_axis, Axis.Right, upperBodyForwardAxis, upperBodyUpAxis));
            shoulder_l_localPos = MatrixConversion.PositionFromMatrix(MatrixConversion.GetLocalMatrix(upperBody.localToWorldMatrix, shoulder_l.position, shoulder_l.rotation, shoulder_l.lossyScale));//shoulder_l.localPosition;
            shoulder_r_localPos = MatrixConversion.PositionFromMatrix(MatrixConversion.GetLocalMatrix(upperBody.localToWorldMatrix, shoulder_r.position, shoulder_r.rotation, shoulder_r.lossyScale));
        }
        if (armpit_l != null)
        {
            boneExists[(int)SecondaryBone.Armpit] = true;
            vat_armpit_l = new VirtualAxisTransform(armpit_l, GetArmpitRotation(upperBody, upperBodyWorldAxis, upperArm_l, upperArm_l_axis, Axis.Left, upperBodyForwardAxis, upperBodyUpAxis));
            vat_armpit_r = new VirtualAxisTransform(armpit_r, GetArmpitRotation(upperBody, upperBodyWorldAxis, upperArm_r, upperArm_r_axis, Axis.Right, upperBodyForwardAxis, upperBodyUpAxis));
            armpit_l_localPos = MatrixConversion.PositionFromMatrix(MatrixConversion.GetLocalMatrix(upperBody.localToWorldMatrix, armpit_l.position, armpit_l.rotation, armpit_l.lossyScale));
            armpit_r_localPos = MatrixConversion.PositionFromMatrix(MatrixConversion.GetLocalMatrix(upperBody.localToWorldMatrix, armpit_r.position, armpit_r.rotation, armpit_r.lossyScale));
        }
        if (elbow_l != null)
        {
            boneExists[(int)SecondaryBone.Elbow] = true;
            vat_elbow_l = new VirtualAxisTransform(elbow_l, GetElbowRotation(upperArm_l, forearm_l,upperArm_l_axis, upperArm_l_bendAxis));//Quaternion.LookRotation(transform.root.forward, Vector3.Cross(transform.root.forward, GetVectorFromAxis(upperArm_l, upperArm_l_axis))));
            vat_elbow_r = new VirtualAxisTransform(elbow_r, GetElbowRotation(upperArm_r, forearm_r, upperArm_r_axis, upperArm_r_bendAxis));//Quaternion.LookRotation(transform.root.forward, Vector3.Cross(transform.root.forward, GetVectorFromAxis(upperArm_r, ReversedAxis(upperArm_r_axis)))));
            elbow_l_localPos = elbow_l.localPosition;
            elbow_r_localPos = elbow_r.localPosition;
        }
        if (hip_l != null)
        {
            boneExists[(int)SecondaryBone.Hip] = true;
            vat_hip_l = new VirtualAxisTransform(hip_l, GetHipRotation(pelvis, pelvisWorldAxis, thigh_l, thigh_l_axis, Axis.Left, pelvisForwardAxis, pelvisUpAxis));
            vat_hip_r = new VirtualAxisTransform(hip_r, GetHipRotation(pelvis, pelvisWorldAxis, thigh_r, thigh_r_axis, Axis.Right, pelvisForwardAxis, pelvisUpAxis));
            hip_l_localPos = hip_l.localPosition;
            hip_r_localPos = hip_r.localPosition;
        }
        if (knee_l != null)
        {
            boneExists[(int)SecondaryBone.Knee] = true;
            vat_knee_l = new VirtualAxisTransform(knee_l, GetElbowRotation(thigh_l, calf_l, thigh_l_axis, thigh_l_bendAxis));//Quaternion.LookRotation(transform.root.forward, GetVectorFromAxis(thigh_l, ReversedAxis(thigh_l_axis))));
            vat_knee_r = new VirtualAxisTransform(knee_r, GetElbowRotation(thigh_r, calf_r, thigh_r_axis, thigh_r_bendAxis));//Quaternion.LookRotation(transform.root.forward, GetVectorFromAxis(thigh_r, ReversedAxis(thigh_r_axis))));
            knee_l_localPos = knee_l.localPosition;
            knee_r_localPos = knee_r.localPosition;
        }
    }

    public void SyncBones()
    {
        upperBodyWorldAxis = GetCenterAxis(upperBody, upperBodyForwardAxis, upperBodyUpAxis);
        pelvisWorldAxis = GetCenterAxis(pelvis, pelvisForwardAxis, pelvisUpAxis);

        if (boneExists[(int)SecondaryBone.Shoulder] == true)
        {
            SyncShoulder(vat_shoulder_l, upperBodyWorldAxis, upperBody, upperArm_l, upperArm_l_axis, Axis.Left, upperBodyForwardAxis, upperBodyUpAxis, shoulder_l_localPos);
            SyncShoulder(vat_shoulder_r, upperBodyWorldAxis,  upperBody, upperArm_r, upperArm_r_axis, Axis.Right, upperBodyForwardAxis, upperBodyUpAxis, shoulder_r_localPos);
        }
        if (boneExists[(int)SecondaryBone.Armpit] == true)
        {
            SyncArmpit(vat_armpit_l, upperBodyWorldAxis,  upperBody, upperArm_l, upperArm_l_axis, Axis.Left, upperBodyForwardAxis, upperBodyUpAxis, armpit_l_localPos);
            SyncArmpit(vat_armpit_r, upperBodyWorldAxis, upperBody, upperArm_r, upperArm_r_axis, Axis.Right, upperBodyForwardAxis, upperBodyUpAxis, armpit_r_localPos);
        }
        if (boneExists[(int)SecondaryBone.Elbow] == true)
        {
            SyncElbow(vat_elbow_l, elbow_l, elbow_l_localPos, upperArm_l, forearm_l, upperArm_l_axis, upperArm_l_bendAxis);//, upperArm_l_axis, upperArm_l_forwardAxis, forearm_l_upAxis, upperArmLength);
            SyncElbow(vat_elbow_r, elbow_r, elbow_r_localPos, upperArm_r, forearm_r, upperArm_r_axis, upperArm_r_bendAxis);//, upperArm_r_axis, upperArm_r_forwardAxis, forearm_r_upAxis, upperArmLength);
        }
        if (boneExists[(int)SecondaryBone.Hip] == true)
        {
            SyncHip(vat_hip_l, pelvisWorldAxis, pelvis, thigh_l, thigh_l_axis, Axis.Left, pelvisForwardAxis, pelvisUpAxis, hip_l_localPos); //(vat_hip_l, pelvisWorldAxis, hip_l, pelvis, thigh_l, thigh_l_axis, thigh_l_bendAxis, hip_l_localPos);
            SyncHip(vat_hip_r, pelvisWorldAxis, pelvis, thigh_r, thigh_r_axis, Axis.Right, pelvisForwardAxis, pelvisUpAxis, hip_r_localPos);
        }
        if (boneExists[(int)SecondaryBone.Knee] == true)
        {
            SyncElbow(vat_knee_l, knee_l, knee_l_localPos, thigh_l, calf_l, thigh_l_axis, thigh_l_bendAxis);//, thigh_l_axis, thigh_l_forwardAxis, calf_l_rightAxis, upperArmLength);
            SyncElbow(vat_knee_r, knee_r, knee_r_localPos, thigh_r, calf_r, thigh_r_axis, thigh_r_bendAxis);//, thigh_r_axis, thigh_r_forwardAxis, calf_r_rightAxis, thighLength);
        }
    }

    public Matrix4x4 GetCenterAxis(Transform center, Vector3 centerForwardAxis, Vector3 centerUpAxis)
    {
        //Vector3 limbVector = limb.TransformDirection(limbAxis);//MatrixConversion.InverseTransformVector(vat.virtualParent, /*GetVectorFromAxis(upperArm, upperArmAxis)*/upperArm.TransformVector(upperArmAxis));
        //bool isSpreadToRight = limbSpreadAxis == Axis.Right;
        Vector3 upAxis = center.TransformVector(centerUpAxis);//GetVectorFromAxis(upperBody, upperBodyUpAxis);
        Vector3 forwardAxis = center.TransformVector(centerForwardAxis);// GetVectorFromAxis(upperBody, upperBodyForwardAxis);
        Vector3 spreadAxis = center.TransformDirection(Vector3.Cross(forwardAxis, upAxis)); //* (isSpreadToRight ? -1f : 1f));

        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = spreadAxis.x;
        m.m10 = spreadAxis.y;
        m.m20 = spreadAxis.z;   //GetColumn(0)
        m.m01 = upAxis.x;
        m.m11 = upAxis.y;
        m.m21 = upAxis.z;      //GetColumn(1)
        m.m02 = forwardAxis.x;
        m.m12 = forwardAxis.y;
        m.m22 = forwardAxis.z;   //GetColumn(2)
        //m.m30 = limbVector.x;
        //m.m31 = limbVector.y;
        //m.m32 = limbVector.z;  

        return m;
    }

    public Vector3 GetLimbsDots(Transform center, Matrix4x4 centerAxis, Transform limb, Vector3 limbAxis, Axis limbSpreadAxis, Vector3 centerForwardAxis, Vector3 centerUpAxis)
    {
        Vector3 limbVector = limb.TransformDirection(limbAxis);

        float upDot = Vector3.Dot(center.TransformDirection(/*upAxis*/centerAxis.GetColumn(1)), /*upperArmVector*/limbVector);
        float forwardDot = Vector3.Dot(center.TransformDirection(/*forwardAxis*/centerAxis.GetColumn(2)), /*upperArmVector*/limbVector);
        float spreadDot = Vector3.Dot(/*spreadAxis*/centerAxis.GetColumn(0) * (limbSpreadAxis == Axis.Right?-1:1f), /*upperArmVector*/limbVector);

        //Debug.Log(upDot+"/"+forwardDot+"/"+spreadDot);

        //ExtendedMathmatics.Remap(rightDot, -1, 1, 0, 1);

        return new Vector3(spreadDot, upDot, forwardDot);
    }

    public Quaternion GetShoulderRotation(Transform upperBody, Matrix4x4 bodyWorldAxis, Transform upperArm, Vector3 upperArmAxis, Axis upperArmSpreadAxis, Vector3 upperBodyForwardAxis, Vector3 upperBodyUpAxis)
    {
        Vector3 limbDot = GetLimbsDots(upperBody, bodyWorldAxis, upperArm, upperArmAxis, upperArmSpreadAxis, upperBodyForwardAxis, upperBodyUpAxis);

        limbDot.x = Mathf.Clamp01(limbDot.x);
        limbDot.z = ExtendedMathmatics.Remap(limbDot.z, -1, 1, 0, 1);

        Axis oppositeAxis = (Axis)((int)upperArmSpreadAxis * -1);

        Quaternion forwardRot = Quaternion.LookRotation(GetVectorFromAxis(upperBody, oppositeAxis), /*upAxis*/bodyWorldAxis.GetColumn(1));
        Quaternion upRot = Quaternion.LookRotation(/*upAxis*/bodyWorldAxis.GetColumn(1), /*GetVectorFromAxis(upperBody, (Axis)((int)upperBodyForwardAxis * -1))*/upperBody.TransformVector(-upperBodyForwardAxis));
        Quaternion spreadRot = Quaternion.LookRotation(/*forwardAxis*/bodyWorldAxis.GetColumn(2), /*upAxis*/bodyWorldAxis.GetColumn(1));
        Quaternion downRot = Quaternion.LookRotation(/*forwardAxis*/bodyWorldAxis.GetColumn(2), (/*upAxis+spreadAxis*/bodyWorldAxis.GetColumn(1)+ bodyWorldAxis.GetColumn(0).normalized));

        Quaternion q = Quaternion.Lerp( //Quaternion.Lerp(Quaternion.LookRotation(forwardAxis, upAxis), Quaternion.LookRotation(new Vector3(isSpreadToRight ? 1 : -1, 0, forwardDot).normalized, GetVectorFromAxis(upperBody, upperBodyUpAxis)), forwardDot),
                /*upDot*/limbDot.y>0?
                Quaternion.Lerp(forwardRot,upRot,/*upDot*/limbDot.y)
                :
                Quaternion.Lerp(forwardRot,downRot, Mathf.Clamp01(/*upDot*/limbDot.y*-1))
                ,
            spreadRot, /*spreadDot*/limbDot.x);

        //if (upDot > 0)
        //{
        //    q = Quaternion.Lerp(q, Quaternion.LookRotation(upperBody.TransformDirection( new Vector3(0, 1, 1).normalized), GetVectorFromAxis(upperBody, (Axis)((int)upperBodyForwardAxis*-1))), upDot);
        //}
        //else
        //{
        //    q = Quaternion.Lerp(q, Quaternion.LookRotation(GetVectorFromAxis(upperBody, upperBodyForwardAxis), new Vector3(isSpreadToRight ? 1 : -1, 1, 0).normalized), upDot * -1);
        //}

        return q;
    }

    public Quaternion GetArmpitRotation(Transform upperBody, Matrix4x4 bodyWorldAxis, Transform upperArm, Vector3 upperArmAxis, Axis upperArmSpreadAxis, Vector3 upperBodyForwardAxis, Vector3 upperBodyUpAxis)
    {
        Vector3 limbDot = GetLimbsDots(upperBody, bodyWorldAxis, upperArm, upperArmAxis, upperArmSpreadAxis, upperBodyForwardAxis, upperBodyUpAxis);

        /*spreadDot*/limbDot.x = Mathf.Clamp01(/*spreadDot*/limbDot.x);
        //forwardDot = ExtendedMathmatics.Remap(forwardDot, -1, 1, 0, 1);

        Axis oppositeAxis = (Axis)((int)upperArmSpreadAxis * -1);

        Quaternion spreadRot = Quaternion.LookRotation(/*forwardAxis*/bodyWorldAxis.GetColumn(2), /*upAxis*/bodyWorldAxis.GetColumn(1));
        Quaternion forwardRot = Quaternion.Lerp(Quaternion.LookRotation(GetVectorFromAxis(upperBody, oppositeAxis), /*upAxis*/bodyWorldAxis.GetColumn(1)), spreadRot, Mathf.Clamp01(/*upDot*/limbDot.y * -1));


        Quaternion q = Quaternion.Lerp(Quaternion.Lerp(forwardRot, spreadRot, /*spreadDot*/limbDot.x), Quaternion.Lerp(spreadRot, forwardRot, /*upDot*/limbDot.y), Mathf.Clamp01(/*forwardDot*/limbDot.z * -1));

        return q;
    }

    public Quaternion GetHipRotation(Transform pelvis, Matrix4x4 pelvisWorldAxis, Transform thigh, Vector3 thighAxis, Axis thighSpreadAxis, Vector3 pelvisForwardAxis, Vector3 pelvisUpAxis)
    {
        //Vector3 thighVector = thigh.TransformDirection(thighAxis);
        Vector3 limbDot = GetLimbsDots(pelvis, pelvisWorldAxis, thigh, thighAxis, thighSpreadAxis, pelvisForwardAxis, pelvisUpAxis);

        Axis oppositeAxis = (Axis)((int)thighSpreadAxis * -1);

        Quaternion forwardRot = Quaternion.LookRotation(pelvisWorldAxis.GetColumn(1), pelvis.TransformVector(-pelvisForwardAxis));
        Quaternion upRot = Quaternion.LookRotation(pelvis.TransformVector(-pelvisForwardAxis), pelvis.TransformVector(-pelvisUpAxis));
        Quaternion downRot = Quaternion.LookRotation(pelvis.TransformVector(pelvisForwardAxis), pelvis.TransformVector(pelvisUpAxis));
        Quaternion spreadRot = Quaternion.LookRotation(pelvis.TransformVector(pelvisForwardAxis), pelvis.TransformVector(Vector3.Cross(pelvisForwardAxis,pelvisUpAxis)*(thighSpreadAxis == Axis.Right ?1:-1)));
        spreadRot = Quaternion.Lerp(downRot, spreadRot, 0.5f);
        Quaternion backRot = Quaternion.LookRotation(pelvis.TransformVector(-pelvisUpAxis), pelvis.TransformVector(pelvisForwardAxis));
        backRot = Quaternion.Lerp(downRot, backRot, 0.5f);

        Debug.Log(limbDot);

        Quaternion q =
            Quaternion.Lerp( 
                Quaternion.Lerp(
                    Quaternion.Lerp( 
                        Quaternion.Lerp(upRot,forwardRot, Mathf.Tan(Mathf.Clamp01(limbDot.z*-1) * 0.7854f))
                    ,downRot, Mathf.Tan(Mathf.Clamp01(limbDot.y) * 0.7854f))
                ,backRot, Mathf.Tan(Mathf.Clamp01(limbDot.z) * 0.7854f))
            ,spreadRot, Mathf.Tan( Mathf.Clamp01(limbDot.x * -1) * 0.7854f));

        return q;
    }

    public void SyncShoulder(VirtualAxisTransform vat, Matrix4x4 upperBodyWorldAxis, Transform upperBody, Transform upperArm, Vector3 upperArmAxis, Axis upperArmSpreadAxis, Vector3 upperBodyForwardAxis, Vector3 upperBodyUpAxis,  Vector3 shoulderLocalPos)
    {
        Quaternion q = GetShoulderRotation(upperBody, upperBodyWorldAxis, upperArm, upperArmAxis, upperArmSpreadAxis, upperBodyForwardAxis, upperBodyUpAxis);

        vat.SetPositionAndRotationParentMatrix(upperBody.TransformPoint(shoulderLocalPos), q);//parentMat.SetTRS(shoulder.parent.TransformPoint(shoulderLocalPos), q, shoulder.localScale);
        vat.SyncTransform();

    }
    public void SyncArmpit(VirtualAxisTransform vat, Matrix4x4 upperBodyWorldAxis,  Transform upperBody, Transform upperArm, Vector3 upperArmAxis, Axis upperArmSpreadAxis, Vector3 upperBodyForwardAxis, Vector3 upperBodyUpAxis, Vector3 armpitLocalPos)
    {
        
        Quaternion q = GetArmpitRotation(upperBody, upperBodyWorldAxis, upperArm, upperArmAxis, upperArmSpreadAxis, upperBodyForwardAxis, upperBodyUpAxis);

        vat.SetPositionAndRotationParentMatrix(upperBody.TransformPoint(armpitLocalPos), q);//parentMat.SetTRS(armpit.parent.TransformPoint(armpitLocalPos), q, armpit.localScale);
        vat.SyncTransform();
    }

    public Quaternion GetElbowRotation(Transform upper, Transform fore, Vector3 axis, Vector3 bendAxis)
    {
        Quaternion upperRot = Quaternion.LookRotation(upper.TransformVector(axis), fore.TransformVector(bendAxis));
        Quaternion foreRot = Quaternion.LookRotation(fore.TransformVector(axis), fore.TransformVector(bendAxis));

        return Quaternion.Lerp(upperRot, foreRot, 0.5f);
    }

    public void SyncElbow(VirtualAxisTransform vat, Transform elbow, Vector3 elbowLocalPos, Transform upper, Transform fore, Vector3 axis, Vector3 bendAxis /*, Axis boneAxis, Axis upperBoneForwardAxis, Axis foreBoneUpAxis, float boneLength*/)
    {
        
        vat.SetPositionAndRotationParentMatrix(upper.TransformPoint(elbowLocalPos), GetElbowRotation(upper, fore, axis, bendAxis));
        vat.SyncTransform();
    }

    public void SyncHip(VirtualAxisTransform vat, Matrix4x4 pelvisWorldAxis, Transform pelvis, Transform thigh, Vector3 thighAxis, Axis thighSpreadAxis, Vector3 pelvisForwardAxis, Vector3 pelvisUpAxis, Vector3 hipLocalPos)
    {
        
        Quaternion q = GetHipRotation(pelvis, pelvisWorldAxis, thigh, thighAxis, thighSpreadAxis, pelvisForwardAxis, pelvisUpAxis);
        vat.SetPositionAndRotationParentMatrix(pelvis.TransformPoint(hipLocalPos), q);
        vat.SyncTransform();
    }
}

public class SecondaryBoneController : MonoBehaviour
{
    public SecondaryBones bones;

    public string shoulderName = "shoulder_secondary";//"Shoulder";
    public string armpitName = "Armpit";
    public string elbowName = "Elbow";
    public string hipName =  "Hip";
    public string kneeName = "Knee";

    public string upperBodyName = "spine_02";//"Spine_3";
    public string upperArmName = "arm_stretch";//"UpperArm";
    public string forearmName = "forearm_stretch";//"Forearm";
    public string pelvisName = "root.x";//"Pelvis";
    public string thighName = "thigh_stretch";//"Thigh";
    public string calfName = "leg_stretch";//"Calf";

    public string leftIdentifier = ".l";//"_L";
    public string rightIdentifier = ".r";// "_R";

    public SecondaryBones.Axis armBendDirection = SecondaryBones.Axis.Forward;
    public SecondaryBones.Axis legBendDirection = SecondaryBones.Axis.Back;

    private void Start()
    {
        bones.InitBones(transform);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        bones.SyncBones();
    }

   public void FindBones()
    {
        FindBone(transform, ref bones);
    }

    private Vector3 FindForwardVector(Transform bone)
    {
        if (bone.forward.z > 0.7071069f)
        {
            return bone.forward;
        }
        if (bone.up.z > 0.7071069f)
        {
            return bone.up;
        }
        if (bone.right.z > 0.7071069f)
        {
            return bone.right;
        }
        if (bone.forward.z < -0.7071069f)
        {
            return -bone.forward;
        }
        if (bone.up.z < -0.7071069f)
        {
            return -bone.up;
        }
        if (bone.right.z < -0.7071069f)
        {
            return -bone.right;
        }
        return Vector3.zero;
    }
    private Vector3 FindUpVector(Transform bone)
    {
        if (bone.forward.y > 0.7071069f)
        {
            return bone.forward;
        }
        if (bone.up.y > 0.7071069f)
        {
            return bone.up;
        }
        if (bone.right.y > 0.7071069f)
        {
            return bone.right;
        }
        if (bone.forward.y < -0.7071069f)
        {
            return -bone.forward;
        }
        if (bone.up.y < -0.7071069f)
        {
            return -bone.up;
        }
        if (bone.right.y < -0.7071069f)
        {
            return -bone.right;
        }
        return Vector3.zero;
    }
    private Vector3 FindVectorByWorld(Transform bone, Vector3 desireVector)
    {

        if (Vector3.Project(bone.forward, desireVector).magnitude > 0.7071069f)
        {
            return bone.forward;
        }
        if (Vector3.Project(bone.up, desireVector).magnitude > 0.7071069f)
        {
            return bone.up;
        }
        if (Vector3.Project(bone.right, desireVector).magnitude > 0.7071069f)
        {
            return bone.right;
        }
        if (Vector3.Project(bone.forward, desireVector).magnitude < -0.7071069f)
        {
            return -bone.forward;
        }
        if (Vector3.Project(bone.up, desireVector).magnitude < -0.7071069f)
        {
            return -bone.up;
        }
        if (Vector3.Project(bone.right, desireVector).magnitude < -0.7071069f)
        {
            return -bone.right;
        }
        return Vector3.zero;
    }

    private Vector3 FindVector(Transform bone, Vector3 desireVector)
    {

        if (Vector3.Project( bone.forward, bone.InverseTransformVector( desireVector)).magnitude > 0.7071069f)
        {
            return bone.forward;
        }
        if (Vector3.Project(bone.up, bone.InverseTransformVector(desireVector)).magnitude > 0.7071069f)
        {
            return bone.up;
        }
        if (Vector3.Project(bone.right, bone.InverseTransformVector(desireVector)).magnitude > 0.7071069f)
        {
            return bone.right;
        }
        if (Vector3.Project(bone.forward, bone.InverseTransformVector(desireVector)).magnitude < -0.7071069f)
        {
            return -bone.forward;
        }
        if (Vector3.Project(bone.up, bone.InverseTransformVector(desireVector)).magnitude < -0.7071069f)
        {
            return -bone.up;
        }
        if (Vector3.Project(bone.right, bone.InverseTransformVector(desireVector)).magnitude < -0.7071069f)
        {
            return -bone.right;
        }
        return Vector3.zero;
    }

    private SecondaryBones.Axis FindForwardAxis(Transform bone)
    {
        if (bone.forward.z > 0.7071069f)
        {
            return SecondaryBones.Axis.Forward;
        }
        if (bone.up.z > 0.7071069f)
        {
            return SecondaryBones.Axis.Up;
        }
        if (bone.right.z > 0.7071069f)
        {
            return SecondaryBones.Axis.Right;
        }
        if (bone.forward.z < -0.7071069f)
        {
            return SecondaryBones.Axis.Back;
        }
        if (bone.up.z < -0.7071069f)
        {
            return SecondaryBones.Axis.Down;
        }
        if (bone.right.z < -0.7071069f)
        {
            return SecondaryBones.Axis.Left;
        }
        return SecondaryBones.Axis.None;
    }
    private SecondaryBones.Axis FindUpAxis(Transform bone)
    {
        if (bone.forward.y > 0.7071069f)
        {
            return SecondaryBones.Axis.Forward;
        }
        if (bone.up.y > 0.7071069f)
        {
            return SecondaryBones.Axis.Up;
        }
        if (bone.right.y > 0.7071069f)
        {
            return SecondaryBones.Axis.Right;
        }
        if (bone.forward.y < -0.7071069f)
        {
            return SecondaryBones.Axis.Back;
        }
        if (bone.up.y < -0.7071069f)
        {
            return SecondaryBones.Axis.Down;
        }
        if (bone.right.y < -0.7071069f)
        {
            return SecondaryBones.Axis.Left;
        }
        return SecondaryBones.Axis.None;
    }
    private Vector3 FindForwardAxisVector(Transform bone)
    {
        if (bone.forward.z > 0.7071069f)
        {
            return Vector3.forward;
        }
        if (bone.up.z > 0.7071069f)
        {
            return Vector3.up;
        }
        if (bone.right.z > 0.7071069f)
        {
            return Vector3.right;
        }
        if (bone.forward.z < -0.7071069f)
        {
            return Vector3.back;
        }
        if (bone.up.z < -0.7071069f)
        {
            return Vector3.down;
        }
        if (bone.right.z < -0.7071069f)
        {
            return Vector3.left;
        }
        return Vector3.zero;
    }
    private Vector3 FindUpAxisVector(Transform bone)
    {
        if (bone.forward.y > 0.7071069f)
        {
            return Vector3.forward;
        }
        if (bone.up.y > 0.7071069f)
        {
            return Vector3.up;
        }
        if (bone.right.y > 0.7071069f)
        {
            return Vector3.right;
        }
        if (bone.forward.y < -0.7071069f)
        {
            return Vector3.back;
        }
        if (bone.up.y < -0.7071069f)
        {
            return Vector3.down;
        }
        if (bone.right.y < -0.7071069f)
        {
            return Vector3.left;
        }
        return Vector3.zero;
    }

    public void FindBone(Transform bone, ref SecondaryBones bones)
    {
        string boneName = bone.name;

        if(boneName.Contains(shoulderName) && boneName.Contains(leftIdentifier) && bones.shoulder_l == null)
        {
            bones.shoulder_l = bone;
        }
        if (boneName.Contains(shoulderName) && boneName.Contains(rightIdentifier) && bones.shoulder_r == null)
        {
            bones.shoulder_r = bone;
        }
        if (boneName.Contains(armpitName) && boneName.Contains(leftIdentifier) && bones.armpit_l == null)
        {
            bones.armpit_l = bone;
        }
        if (boneName.Contains(armpitName) && boneName.Contains(rightIdentifier) && bones.armpit_r == null)
        {
            bones.armpit_r = bone;
        }
        if (boneName.Contains(elbowName) && boneName.Contains(leftIdentifier) && bones.elbow_l == null)
        {
            bones.elbow_l = bone;
        }
        if (boneName.Contains(elbowName) && boneName.Contains(rightIdentifier) && bones.elbow_r == null)
        {
            bones.elbow_r = bone;
        }
        if (boneName.Contains(hipName) && boneName.Contains(leftIdentifier) && bones.hip_l == null)
        {
            bones.hip_l = bone;
        }
        if (boneName.Contains(hipName) && boneName.Contains(rightIdentifier) && bones.hip_r == null)
        {
            bones.hip_r = bone;
        }
        if (boneName.Contains(kneeName) && boneName.Contains(leftIdentifier) && bones.knee_l == null)
        {
            bones.knee_l = bone;
        }
        if (boneName.Contains(kneeName) && boneName.Contains(rightIdentifier) && bones.knee_r == null)
        {
            bones.knee_r = bone;
        }
        if (boneName.Contains(forearmName) && boneName.Contains(leftIdentifier) && bones.forearm_l == null)
        {
            bones.forearm_l = bone;
        }
        if (boneName.Contains(forearmName) && boneName.Contains(rightIdentifier) && bones.forearm_r == null)
        {
            bones.forearm_r = bone;
        }
        if (boneName.Contains(upperArmName) && !boneName.Contains(forearmName) && boneName.Contains(leftIdentifier) && bones.upperArm_l == null && bones.forearm_l != null)
        {
            bones.upperArm_l = bone;
            bones.upperArm_l_axis = FindVector(bones.upperArm_l, Vector3.Normalize(bones.forearm_l.position-bones.upperArm_l.position));
            bones.upperArm_l_bendAxis = FindVector(bones.upperArm_l, SecondaryBones.GetVectorFromAxis(armBendDirection));
        }
        if (boneName.Contains(upperArmName) && !boneName.Contains(forearmName) && boneName.Contains(rightIdentifier) && bones.upperArm_r == null && bones.forearm_r != null)
        {
            bones.upperArm_r = bone;
            bones.upperArm_r_axis = FindVector(bones.upperArm_r, Vector3.Normalize(bones.forearm_r.position - bones.upperArm_r.position));
            bones.upperArm_r_bendAxis = FindVector(bones.upperArm_r, SecondaryBones.GetVectorFromAxis(armBendDirection));
        }
        if (boneName.Contains(calfName) && boneName.Contains(leftIdentifier) && bones.calf_l == null)
        {
            bones.calf_l = bone;
        }
        if (boneName.Contains(calfName) && boneName.Contains(rightIdentifier) && bones.calf_r == null)
        {
            bones.calf_r = bone;
        }
        if (boneName.Contains(thighName) && boneName.Contains(leftIdentifier) && bones.thigh_l == null && bones.calf_l)
        {
            bones.thigh_l = bone;
            bones.thigh_l_axis = FindVector(bones.thigh_l, Vector3.Normalize(bones.calf_l.position - bones.thigh_l.position));
            bones.thigh_l_bendAxis = FindVector(bones.thigh_l, SecondaryBones.GetVectorFromAxis(legBendDirection));
        }
        if (boneName.Contains(thighName) && boneName.Contains(rightIdentifier) && bones.thigh_r == null && bones.calf_r)
        {
            bones.thigh_r = bone;
            bones.thigh_r_axis = FindVector(bones.thigh_r, Vector3.Normalize(bones.calf_r.position - bones.thigh_r.position));
            bones.thigh_r_bendAxis = FindVector(bones.thigh_r, SecondaryBones.GetVectorFromAxis(legBendDirection));
        }
        
        if (boneName.Contains(upperBodyName) && bones.upperBody == null)
        {
            bones.upperBody = bone;
            bones.upperBodyForwardAxis = bone.InverseTransformVector(Vector3.forward);// FindForwardAxisVector(bones.upperBody);
            bones.upperBodyUpAxis = bone.InverseTransformVector(Vector3.up);//FindUpAxisVector(bones.upperBody);
        }
        if (boneName.Contains(pelvisName) && bones.pelvis == null)
        {
            bones.pelvis = bone;
            bones.pelvisForwardAxis = bone.InverseTransformVector(Vector3.forward);//FindForwardAxisVector(bones.pelvis);
            bones.pelvisUpAxis = bone.InverseTransformVector(Vector3.up);//FindUpAxisVector(bones.pelvis);
        }


        for (int i = 0; i < bone.childCount; ++i)
        {
            FindBone(bone.GetChild(i), ref bones);
        }
    }

    public void ClearBones()
    {
            bones.shoulder_l = null;
                bones.shoulder_r = null;
        bones.armpit_l = null;
        bones.armpit_r = null;
        bones.elbow_l = null;
        bones.elbow_r = null;
        bones.hip_l = null;
        bones.hip_r = null;
        bones.knee_l = null;
        bones.knee_r = null;
        bones.upperArm_l = null;
        bones.upperArm_r = null;
        bones.forearm_l = null;
        bones.forearm_r = null;

        bones.thigh_l = null;
        bones.thigh_r = null;
        bones.calf_l = null;
        bones.calf_r = null;

        bones.upperBody = null;
        bones.pelvis = null;
    }

}
