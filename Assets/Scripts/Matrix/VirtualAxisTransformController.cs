using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode()]
public class VirtualAxisTransformController : MonoBehaviour
{
    [SerializeField] private Vector3 desireToForward = new Vector3(0,0,1);
    [SerializeField] private Vector3 desireToUp = new Vector3(0, 1, 0);

    private VirtualAxisTransform vatf;
    public Transform tempParent;



    private void OnEnable()
    {
        if (!Application.isPlaying) 
        desireToForward = desireToForward.normalized;
        desireToUp = desireToUp.normalized;

        return;


    }

    // Start is called before the first frame update
    void Start()
    {
        if (!Application.isPlaying) return;

        vatf = new VirtualAxisTransform(this.transform, tempParent.position, tempParent.rotation);

        //if (tempParent)
        //    tempParent.rotation = MatrixConversion.RotationFromMatrix(vatf.virtualParent);
    }

    // Update is called once per frame
    void Update()
    {
        if (!Application.isPlaying) return;

        if (tempParent)
        {
            vatf.SetRotationParentMatrix(tempParent.rotation); //MatrixConversion.SetRotationToMatrix(ref vatf.virtualParent, tempParent.rotation);
        }

        vatf.SyncTransform();
    }
}

public class VirtualAxisTransform
{
    public FlexibleTransform flexibleTransform;
    public Matrix4x4 virtualParent;

    public VirtualAxisTransform(Transform transform, Vector3 desirePosition, Vector3 desireToForward, Vector3 desireToUp)
    {

        virtualParent = Matrix4x4.TRS(desirePosition, Quaternion.LookRotation(transform.TransformDirection(desireToForward), transform.TransformDirection(desireToUp)), Vector3.one);
        flexibleTransform = new FlexibleTransform(virtualParent, transform);
    }

    public VirtualAxisTransform(Transform transform, Vector3 desireToForward, Vector3 desireToUp)
    {

        virtualParent = Matrix4x4.TRS(transform.position, Quaternion.LookRotation(transform.TransformDirection(desireToForward), transform.TransformDirection(desireToUp)), Vector3.one);
        flexibleTransform = new FlexibleTransform(virtualParent, transform);
    }
    public VirtualAxisTransform(Transform transform, Quaternion desireRotation)
    {

        virtualParent = Matrix4x4.TRS(transform.position, desireRotation, Vector3.one);
        flexibleTransform = new FlexibleTransform(virtualParent, transform);
    }

    public VirtualAxisTransform(Transform transform, Vector3 desirePosition, Quaternion desireRotation)
    {

        virtualParent = Matrix4x4.TRS(desirePosition, desireRotation, Vector3.one);
        flexibleTransform = new FlexibleTransform(virtualParent, transform);
    }

    public void SetPositionAndRotationParentMatrix(Vector3 position, Quaternion rotation)
    {
        MatrixConversion.SetPositionFromMatrix(ref virtualParent, position);
        MatrixConversion.SetRotationToMatrix(ref virtualParent, rotation);
    }

    public void SetPositionParentMatrix(Vector3 position)
    {
        MatrixConversion.SetPositionFromMatrix(ref virtualParent, position);
    }

    public void SetRotationParentMatrix(Quaternion rotation)
    {
        MatrixConversion.SetRotationToMatrix(ref virtualParent, rotation);
    }

    public void InputParentMatrix(Matrix4x4 parentMat)
    {
        virtualParent = parentMat;
    }

    public void SyncTransform()
    {

        flexibleTransform.SyncTransform(virtualParent);

    }
}