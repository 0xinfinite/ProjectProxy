using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FlexibleTransform
{
    public Transform transform;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public FlexibleTransform(Transform tf, Transform parent)
    {
        transform = tf;
        Vector3 p;
        Quaternion r;
        scale = GetWorldToLocalTransform(parent, tf, out p, out r);
        position = p;
        rotation = r;
    }


    public FlexibleTransform(Transform tf, Matrix4x4 mat)
    {
        transform = tf;
        //Vector3 p;
        //Quaternion r;
        scale = MatrixConversion.ScaleFromMatrix(mat);//GetWorldToLocalTransform(parent, tf, out p, out r);
        position = MatrixConversion.PositionFromMatrix(mat);
        rotation = MatrixConversion.RotationFromMatrix(mat);
    }

    public FlexibleTransform(Matrix4x4 parentMat, Transform tf)
    {
        Matrix4x4 m = MatrixConversion.GetLocalMatrix(parentMat, tf.position, tf.rotation, tf.lossyScale);
        
        transform = tf;
        position = MatrixConversion.PositionFromMatrix(m);
        rotation = MatrixConversion.RotationFromMatrix(m);
        scale = MatrixConversion.ScaleFromMatrix(m);
    }

    public Vector3 GetWorldToLocalTransform(Transform p_parent, Transform childTransform, out Vector3 position, out Quaternion rotation)
    {
        Matrix4x4 parentMatrix = Matrix4x4.TRS(p_parent.position, p_parent.rotation, p_parent.lossyScale);

        Matrix4x4 m = MatrixConversion.GetLocalMatrix(parentMatrix, childTransform.position, childTransform.rotation, childTransform.lossyScale);

        position = MatrixConversion.PositionFromMatrix(m);
        rotation = MatrixConversion.RotationFromMatrix(m);
        return MatrixConversion.ScaleFromMatrix(m);
    }

    

    public void SyncTransform(Matrix4x4 parentMat)
    {
        Matrix4x4 m = MatrixConversion.GetWorldMatrix(parentMat, position, rotation, scale);

        transform.position = MatrixConversion.PositionFromMatrix(m);
        transform.rotation = MatrixConversion.RotationFromMatrix(m);
        //transform.localScale = MatrixConversion.PositionFromMatrix(m);
    }
}

public static class MatrixConversion
{
    public static Matrix4x4 GetProjectionMatrix(Camera cam)
    {
        return (!cam.orthographic? 
            cam.projectionMatrix :
            GL.GetGPUProjectionMatrix(cam.projectionMatrix,false))* cam.worldToCameraMatrix;
    }

    public static Matrix4x4 GetVP(Camera cam)
    {
        bool d3d = SystemInfo.graphicsDeviceVersion.IndexOf("Direct3D") > -1;
        //Matrix4x4 M = _target.localToWorldMatrix;
        Matrix4x4 V = cam.worldToCameraMatrix;
        Matrix4x4 P = cam.projectionMatrix;
        if (d3d) {
            // Invert Y for rendering to a render texture
            for (int i = 0; i < 4; i++) {
                P[1,i] = -P[1,i];
            }
            // Scale and bias from OpenGL -> D3D depth range
            for (int i = 0; i < 4; i++) {
                P[2,i] = P[2,i]*0.5f + P[3,i]*0.5f;
            }
        }
        //Matrix4x4 MVP = P*V*M;

        //Matrix4x4 matrix_MVP = P * V * M;//_camera.worldToCameraMatrix;
        return P * V;
    }
    
    public static Vector3 PositionFromMatrix(Matrix4x4 m) { return m.GetColumn(3); }
    public static void SetPositionFromMatrix(ref Matrix4x4 m, Vector3 pos) {
        // m.SetColumn(3, new Vector4(pos.x,pos.y,pos.z, 0)); 
        //m.m03 = pos.x;
        //m.m13 = pos.y;
        //m.m23 = pos.z;
        m.SetTRS(pos, MatrixConversion.RotationFromMatrix(m), MatrixConversion.ScaleFromMatrix(m));
    }

    // Extract new local rotation
    public static Quaternion RotationFromMatrix(Matrix4x4 m)
    {
        return Quaternion.LookRotation(
            m.GetColumn(2),
            m.GetColumn(1)
            );
    }

    public static void SetRotationToMatrix(ref Matrix4x4 m, Quaternion rot)
    {
        m.SetColumn(2, rot * Vector3.forward);
        m.SetColumn(1, rot * Vector3.up);

        //return m;
    }

    // Extract new local scale
    public static Vector3 ScaleFromMatrix(Matrix4x4 m)
    {
        return new Vector3(
            m.GetColumn(0).magnitude,
            m.GetColumn(1).magnitude,
            m.GetColumn(2).magnitude);
    }

    public static Matrix4x4 GetLocalMatrix(Matrix4x4 parentMatrix, Vector3 worldPosition, Quaternion worldRotation, Vector3 lossyScale)
    {
        Matrix4x4 childMatrix = Matrix4x4.TRS(worldPosition, worldRotation, lossyScale);
        return parentMatrix.inverse * childMatrix;
    }

    public static Matrix4x4 GetLocalMatrixAndConvert(Matrix4x4 parentMatrix, Matrix4x4 childMatrix)
    {
        return parentMatrix.inverse * childMatrix;
    }

    public static Matrix4x4 GetWorldMatrix(Matrix4x4 parentMatrix, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        Matrix4x4 childMatrix = Matrix4x4.TRS(localPosition, localRotation, localScale);
        return parentMatrix * childMatrix;
    }

    public static Matrix4x4 GetWorldMatrixAndConvert(Matrix4x4 parentMatrix, Matrix4x4 childMatrix)
    {
        return parentMatrix * childMatrix;
    }

    public static Vector3 TransformVector(Matrix4x4 parentMatrix, Vector3 localVector)
    {
        return parentMatrix.MultiplyVector(localVector);
    }

    public static Vector3 InverseTransformVector(Matrix4x4 parentMatrix, Vector3 worldVector)
    {
        return parentMatrix.inverse.MultiplyVector(worldVector);
    }
    public static Vector3 TransformPoint(Matrix4x4 parentMatrix, Vector3 localPoint)
    {
        return parentMatrix.MultiplyPoint(localPoint);
    }

    public static Vector3 InverseTransformPoint(Matrix4x4 parentMatrix, Vector3 worldPoint)
    {
        return parentMatrix.inverse.MultiplyPoint(worldPoint);
    }
}
public class FlexibleTransformController : MonoBehaviour
{
    public Vector3 virtualAxisPosition;
    public Vector3 virtualAxisEularAngle;
    Quaternion virtualAxisRotation;
    public Vector3 virtualAxisScale = Vector3.one;
    public Matrix4x4 virtualAxisMatrix;

    public Vector3 virtualLocalPosition;
    public Vector3 virtualLocalEularAngle;
    Quaternion virtualLocalRotation;
    public Vector3 virtualLocalScale = Vector3.one;
    //Matrix4x4 virtualLocalMatrix;

    public Transform target;

    public Transform tf;

    public Vector4 output;

    public Camera cam;

    public MeshRenderer render;
    public MeshFilter filter;

    private void Start()
    {
        if (filter)
        {
            vertices = filter.sharedMesh.vertices; 
        }

        virtualAxisRotation = Quaternion.Euler(virtualAxisEularAngle);
        virtualAxisMatrix = Matrix4x4.TRS(virtualAxisPosition, virtualAxisRotation, virtualAxisScale);
    }


    private void Update()
    {
        //Matrix4x4 mat = cam.cameraToWorldMatrix;
        //transform.position = mat.MultiplyPoint3x4(Vector3.down);//mat.GetColumn(3);
        //transform.rotation = Quaternion.LookRotation(-mat.GetColumn(2),
        //                                   mat.GetColumn(1));
        //transform.localScale = new Vector3(mat.GetColumn(0).magnitude, mat.GetColumn(1).magnitude, mat.GetColumn(2).magnitude);

        //render.


        if (Input.GetKeyDown(KeyCode.P))
        {
            virtualAxisMatrix.SetColumn(3, new Vector4(virtualAxisPosition.x, virtualAxisPosition.y,
                virtualAxisPosition.z, 1));
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            virtualAxisRotation = Quaternion.Euler(virtualAxisEularAngle);
            virtualAxisMatrix.SetColumn(0, virtualAxisRotation * Vector3.right * virtualAxisScale.x);
            virtualAxisMatrix.SetColumn(1, virtualAxisRotation * Vector3.up * virtualAxisScale.y);
            virtualAxisMatrix.SetColumn(2, virtualAxisRotation * Vector3.forward * virtualAxisScale.z);
        }
        //if (Input.GetKeyDown(KeyCode.P))
        //{

        //}
        //output = virtualAxisMatrix.GetColumn(3);

        //virtualAxisMatrix = Matrix4x4.TRS(virtualAxisPosition, virtualAxisRotation, virtualAxisScale);
        //virtualAxisMatrix = new Matrix4x4();
        //virtualAxisMatrix.Set
        virtualLocalRotation = Quaternion.Euler(virtualLocalEularAngle);
        //virtualLocalMatrix = Matrix4x4.TRS(virtualLocalPosition, virtualLocalRotation, virtualLocalScale);

        //Gizmos.DrawSphere(virtualAxisPosition, 0.5f);

        //Matrix4x4 matrix = virtualAxisMatrix * virtualLocalMatrix;
        if (target != null)
        {
            target.position = virtualAxisMatrix.MultiplyPoint3x4(virtualLocalPosition); //MatrixConversion.PositionFromMatrix(matrix);
            target.rotation = virtualAxisMatrix.rotation * virtualLocalRotation;//MatrixConversion.RotationFromMatrix(matrix);
            target.localScale = new Vector3(virtualAxisMatrix.lossyScale.x * virtualLocalScale.x,
                virtualAxisMatrix.lossyScale.y * virtualLocalScale.y,
                virtualAxisMatrix.lossyScale.z * virtualLocalScale.z);//matrix.lossyScale;//MatrixConversion.ScaleFromMatrix(matrix);
        }
    }

    Vector3[] vertices; //= new Vector3[8];

    private void OnDrawGizmos()
    {
        if (virtualAxisMatrix != null)
        {
            Gizmos.DrawSphere(virtualAxisPosition, 0.5f);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(virtualAxisPosition, virtualAxisMatrix.MultiplyVector(Vector3.up));
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(virtualAxisPosition, virtualAxisMatrix.MultiplyVector(Vector3.forward));
            Gizmos.color = Color.red;
            Gizmos.DrawRay(virtualAxisPosition, virtualAxisMatrix.MultiplyVector(Vector3.left));
        }
        if (vertices != null)
        {
            for (int i = 0; i < vertices.Length; ++i)
            {
                var vertex = vertices[i];
                Matrix4x4 modelToWorld = render.localToWorldMatrix;
                var viewToProjection = cam.projectionMatrix;
                var worldToView = cam.worldToCameraMatrix;
                //mat.SetColumn(0, new Vector4( mat.GetColumn(0).x * output.x,
                //    mat.GetColumn(0).y * output.y,
                //    mat.GetColumn(0).z * output.z,
                //    mat.GetColumn(0).w * output.w));
                var worldVertex = (viewToProjection * worldToView * modelToWorld).MultiplyPoint(vertex); //mat.MultiplyPoint3x4(vertex);

                Gizmos.DrawWireSphere(worldVertex, 0.1f);
            }
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(Vector3.forward, new Vector3(2 , 2 ,0.0001f));
        }
    }

    //public FlexibleTransformParent[] parents;

    //public int targetIndexToSync;

    //// Start is called before the first frame update
    ////void Start()
    ////{
    ////    parent.InputTransform();
    ////}

    ////// Update is called once per frame
    //void Update()
    //{
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        Camera cam = Camera.main;
    //        RaycastHit hit;
    //        if(Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit))
    //        {
    //            AddFlexibleTransform(hit.transform);
    //        }
    //    }

    //   // if (Input.GetMouseButtonDown(1))
    //    {
    //        if(targetIndexToSync<parents.Length)
    //        {
    //            parents[targetIndexToSync].SyncTransform();
    //        }
    //    }
    //}

    //public void AddFlexibleTransform(Transform flexibleToAdd)
    //{
    //    for (int i = 0; i < parents.Length; i++)
    //    {
    //        if (parents[i].parent == flexibleToAdd)
    //            return;
    //    }

    //    for (int i = 0; i < parents.Length; i++)
    //    {
    //        var singleParent = parents[i];
    //        var childList = singleParent.child.ToList();
    //        var newChild = new FlexibleTransform(flexibleToAdd, singleParent.parent);
    //        childList.Add(newChild);
    //        singleParent.child = childList.ToArray();
    //    }

    //    var parentList = parents.ToList();
    //    var newParent = new FlexibleTransformParent(flexibleToAdd, parents);
    //    parentList.Add(newParent);
    //    parents = parentList.ToArray();
    //}


}
//[System.Serializable]
//public class VirtualTransform
//{
//    public Vector3 virtualAxisPosition;
//    public Quaternion virtualAxisRotation;
//    public Vector3 virtualAxisScale;


//}


[System.Serializable]
public class VirtualFlexibleTransformParent
{
    public Matrix4x4 parentMat;
    public FlexibleTransform[] child;

    public FlexibleTransform GetChild(int index)
    {
        return child[index];
    }

    public void InputTransform()
    {
        Matrix4x4 parentMatrix = parentMat;//Matrix4x4.TRS(parentMat.position, parentMat.rotation, parentMat.lossyScale);
        int childCount = child.Length;
        for (int i = 0; i < childCount; ++i)
        {
            FlexibleTransform childFlexible = GetChild(i);
            Transform childTransform = childFlexible.transform;

            Matrix4x4 m = MatrixConversion.GetLocalMatrix(parentMatrix, childTransform.position, childTransform.rotation, childTransform.lossyScale);

            childFlexible.position = MatrixConversion.PositionFromMatrix(m);
            childFlexible.rotation = MatrixConversion.RotationFromMatrix(m);
            childFlexible.scale = MatrixConversion.ScaleFromMatrix(m);
        }
    }

    public void SyncTransform()
    {
        int childCount = child.Length;
        Matrix4x4 parentMatrix = parentMat;//Matrix4x4.TRS(parentMat.position, parentMat.rotation, parentMat.lossyScale);
        for (int i = 0; i < childCount; ++i)
        {
            FlexibleTransform childFlexible = GetChild(i);
            Transform childTransform = childFlexible.transform;

            Matrix4x4 wm = MatrixConversion.GetWorldMatrix(parentMatrix, childFlexible.position, childFlexible.rotation, childFlexible.scale);

            childTransform.position = MatrixConversion.PositionFromMatrix(wm);
            childTransform.rotation = MatrixConversion.RotationFromMatrix(wm);
            Vector3 scale = MatrixConversion.ScaleFromMatrix(wm);
            childTransform.localScale = childTransform.parent == null ? scale : new Vector3(scale.x * childTransform.localScale.x, scale.y * childTransform.localScale.y, scale.z * childTransform.localScale.z);

        }
    }

    public VirtualFlexibleTransformParent(Matrix4x4 mat, Transform[] others)
    {
        parentMat = mat;

        child = new FlexibleTransform[others.Length];

        for (int i = 0; i < child.Length; i++)
        {
            child[i] = new FlexibleTransform(others[i], parentMat);
        }

        InputTransform();
    }
}


[System.Serializable]
public class FlexibleTransformParent
{
    public Transform parent;
    public FlexibleTransform[] child;

    public FlexibleTransform GetChild(int index)
    {
        return child[index];
    }

    public void InputTransform()
    {
        Matrix4x4 parentMatrix = Matrix4x4.TRS(parent.position, parent.rotation, parent.lossyScale);
        int childCount = child.Length;
        for (int i = 0; i < childCount; ++i)
        {
            FlexibleTransform childFlexible = GetChild(i);
            Transform childTransform = childFlexible.transform;

            Matrix4x4 m = MatrixConversion.GetLocalMatrix(parentMatrix, childTransform.position, childTransform.rotation, childTransform.lossyScale);

            childFlexible.position = MatrixConversion.PositionFromMatrix(m);
            childFlexible.rotation = MatrixConversion.RotationFromMatrix(m);
            childFlexible.scale = MatrixConversion.ScaleFromMatrix(m);
        }
    }

    public void SyncTransform()
    {
        int childCount = child.Length;
        Matrix4x4 parentMatrix = Matrix4x4.TRS(parent.position, parent.rotation, parent.lossyScale);
        for (int i = 0; i < childCount; ++i)
        {
            FlexibleTransform childFlexible = GetChild(i);
            Transform childTransform = childFlexible.transform;

            Matrix4x4 m = MatrixConversion.GetWorldMatrix(parentMatrix, childFlexible.position, childFlexible.rotation, childFlexible.scale);

            childTransform.position = MatrixConversion.PositionFromMatrix(m);
            childTransform.rotation = MatrixConversion.RotationFromMatrix(m);
            Vector3 scale = MatrixConversion.ScaleFromMatrix(m);
            childTransform.localScale = childTransform.parent==null ?scale:new Vector3(scale.x* childTransform.localScale.x,scale.y* childTransform.localScale.y,scale.z* childTransform.localScale.z);

        }
    }

    //public FlexibleTransformParent(Transform tf, FlexibleTransformParent[] otherParents)
    //{
    //    parent = tf;

    //    child = new FlexibleTransform[otherParents.Length];

    //    for (int i = 0; i < child.Length; i++)
    //    {
    //        child[i] = new FlexibleTransform(otherParents[i].parent, tf);
    //    }
    //}
    public FlexibleTransformParent(Transform tf, Transform[] others)
    {
        parent = tf;

        child = new FlexibleTransform[others.Length];

        for (int i = 0; i < child.Length; i++)
        {
            child[i] = new FlexibleTransform(others[i], tf);
        }

        InputTransform();
    }
}
