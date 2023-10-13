using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum StepStatus { AllAir, OneStep, TwoStep }

public class FootTurning : MonoBehaviour {

   // public RootMotion.FinalIK.LegIK[] legIK;
   // public RootMotion.FinalIK.GrounderIK grounder;
    public Transform[] feet;   // 0:Right, 1:Left
    private Transform turningFoot;
    public Transform TurningFoot { get { return turningFoot; } }
    private StepStatus stepStatus;
    public LayerMask stepLayers;
    private Animator anim;
    //[MinMax(-2,2)]
    public Vector2 stepHeightAllow = new Vector2(-0.01f, 0.01f);

    public bool simpleCalculation;

//    public enum AnimatorCurrentState { Pure = 0,/* Blending = 1,*/ MovingViaRootmotion}
 //   private AnimatorCurrentState currentAnimState;

    public AnimationCurve toRunCurve = AnimationCurve.EaseInOut(0,0,1,1);
    public AnimationCurve toWalkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

 //   public bool footIKOn;

    public CharacterController cc;

    //public AnimationCurve leftFootWalkTransferCurve;
    //public AnimationCurve rightFootWalkTransferCurve;

    private void Start()
    {
        anim = GetComponent<Animator>();
    //    currentAnimState = AnimatorCurrentState.Pure;

        preVRootPos = transform.position;
    }

    //public void ChangeCurrentAnimatorState(AnimatorCurrentState value)
    //{
    //    currentAnimState = value;
    //}

    //public void ChangeCurrentAnimatorStateFromMoving()
    //{

    //    if (anim.pivotWeight < 1f)
    //    {
    //        currentAnimState = AnimatorCurrentState.Blending;
    //    }
    //    else
    //    {
    //        currentAnimState = AnimatorCurrentState.Pure;
    //    }
    //}

    public void Jumping()
    {

    }

    public void JumpingEnd()
    {

    }

    public bool StepCheck(Transform foot)
    {
        float checkDistance = stepHeightAllow.y - stepHeightAllow.x;

        if (Physics.Raycast(new Vector3(foot.position.x, Mathf.Max(foot.position.y,transform.position.y)+stepHeightAllow.y,foot.position.z),//foot.position + new Vector3(0, stepHeightAllow.y, 0),
            Vector3.down,/* out hit,*/ checkDistance, stepLayers))//grounder.solver.layers))
        {
            return true;
        }
        return false;
    }

    public void Move(Vector3 motion)
    {
        if (cc != null)
        {
            cc.Move(motion);
        }
        else
        {
            transform.Translate(motion, Space.World);
        }
    }

    Vector3 preVRootPos = Vector3.zero;
    Vector3 airMove = Vector3.zero;


    // Update is called once per frame
    void FixedUpdate () {

        int stepOnCount = 0;
        for (int i = 0; i < feet.Length; i++)
        {
            if (StepCheck(feet[i]))
            {
                if (stepOnCount==0&&stepStatus == StepStatus.AllAir)
                {
                    turningFoot = feet[i];
                }
                stepOnCount++;
                if (stepOnCount == 2 && stepStatus == StepStatus.OneStep)
                {
                    turningFoot = feet[i];
                }
            }

        }
        switch (stepStatus)
        {
            case StepStatus.TwoStep:
                if(latestFootTf==turningFoot)
                Move(latestFootPos- turningFoot.position);
                if (stepOnCount == 1)
                {
                    stepStatus = StepStatus.OneStep;
                }
                else if(stepOnCount == 0)
                {
                    airMove = transform.position - preVRootPos; //new Vector3(transform.position.x - preVRootPos.x, 0, transform.position.z - preVRootPos.z);//transform.position - preVRootPos;

                    stepStatus = StepStatus.AllAir;
                    turningFoot = null;
                }
                break;
            case StepStatus.OneStep:
                if (latestFootTf == turningFoot)
                    Move(latestFootPos - turningFoot.position);
                if (stepOnCount == 2)
                {
                    stepStatus = StepStatus.TwoStep;
                }
                else if (stepOnCount == 0)
                {
                    airMove = transform.position - preVRootPos;// new Vector3(transform.position.x - preVRootPos.x, 0, transform.position.z - preVRootPos.z);//transform.position - preVRootPos;

                    stepStatus = StepStatus.AllAir;
                    turningFoot = null;
                }
                break;
            case StepStatus.AllAir:
                Move(airMove);
                if (stepOnCount == 1)
                {
                    stepStatus = StepStatus.OneStep;
                }
                else if (stepOnCount == 2)
                {
                    stepStatus = StepStatus.TwoStep;
                }
                break;
        }
        

		//for(int i=0; i < feet.Length; i++)
  //      {
  //          Vector3 footCheckPos = feet[i].position + new Vector3(0, stepHeightAllow.y, 0);
  //          float checkDistance = stepHeightAllow.y - stepHeightAllow.x;

  //        //  RaycastHit hit;
  //         // Debug.DrawLine(footCheckPos, footCheckPos - new Vector3(0, checkDistance, 0), Color.red, 0.5f);
  //          if(StepCheck(feet[i]))//grounder.solver.layers))
  //          {
  //       //       Debug.DrawLine(footCheckPos, hit.point, Color.red, 0.5f);
  //              turningFoot = feet[i];
  //              //return;
  //          }
  //          else
  //          {
  //              if (!simpleCalculation)
  //              {
  //                  Array.Sort(feet, delegate (Transform t1, Transform t2) {
  //                      return t1.position.y.CompareTo(t2.position.y);
  //                  });
  //              }

  //              turningFoot = null;
  //              latestFootTf = null;
  //          }
  //          /*if (feet[i].position.y > stepHeightAllow.x && feet[i].position.y < stepHeightAllow.y)
  //          {
  //              turningFoot = feet[i];
  //              return;
  //          }*/

  //          if (turningFoot != null)
  //          {
  //              if (turningFoot == latestFootTf)
  //              {
  //                  Move((turningFoot.position - latestFootPos) + new Vector3(anim.deltaPosition.x, 0, anim.deltaPosition.z));
  //                  //if (cc != null)
  //                  //{ cc.Move((turningFoot.position - latestFootPos)+new Vector3(anim.deltaPosition.x, 0, anim.deltaPosition.z)); }
  //                  //else
  //                  //{
  //                  //    transform.Translate((turningFoot.position - latestFootPos) + new Vector3(anim.deltaPosition.x, 0, anim.deltaPosition.z), Space.World);
  //                  //}
  //              }
  //              //else
  //              //{
  //              //}

  //              latestFootTf = turningFoot;
  //              latestFootPos = turningFoot.position;
  //          }
  //      }
        //turningFoot = null;
        //latestFootTf = null;
        //return;

        preVRootPos = transform.position;
        if (turningFoot != null)
        {
            latestFootPos = turningFoot.position;
            latestFootTf = turningFoot;
        }

	}

    static Vector3 RotateAbout(Vector3 position ,Vector3 rotatePoint,Vector3 axis, float angle)     {
           return (Quaternion.AngleAxis(angle, axis) * (position - rotatePoint)) + rotatePoint;
    }

public void TurnAround(Vector3 axis, float angle)
    {
        if(turningFoot!=null)
        transform.RotateAround(turningFoot.position, axis, angle);
    }

    public void TurnAroundOut(Vector3 axis, float angle, out Vector3 outPos)
    {
        if (turningFoot == null)
        {
            outPos = transform.position;
            return;
        }


        outPos = RotateAbout(transform.position, turningFoot.position, axis, angle);
        
        transform.Rotate(axis, angle);
    }

    public void TurnToDirection(Vector3 targetDir)
    {
        float angle = Vector3.Angle(transform.forward, targetDir);
        Vector3 axis = Vector3.Cross(transform.forward, targetDir);

        TurnAround(axis, angle);
    }

    public void TurnToDirection(Vector3 targetDir,float maxDegreesDelta)
    {
        float angle = Mathf.Min(maxDegreesDelta, Vector3.Angle(transform.forward, targetDir));
        Vector3 axis = Vector3.Cross(transform.forward, targetDir);

        TurnAround(axis, angle);
    }

    public Vector3 TurnToDirectionOut(Vector3 targetDir)
    {
        float angle = Vector3.Angle(transform.forward, targetDir);
        Vector3 axis = Vector3.Cross(transform.forward, targetDir);

        Vector3 outPos = Vector3.zero;

         TurnAroundOut(axis, angle,out outPos);

        return outPos;
    }

    public Vector3 TurnToDirectionOut(Vector3 targetDir, float maxDegreesDelta)
    {
        float angle = Mathf.Min(maxDegreesDelta, Vector3.Angle(transform.forward, targetDir));
        Vector3 axis = Vector3.Cross(transform.forward, targetDir);

        Vector3 outPos = Vector3.zero;

        TurnAroundOut(axis, angle, out outPos);

        return outPos;
    }

    bool isFirstSteppingFrame =true;
    Vector3 latestFootPos;
    Transform latestFootTf;

    public void Stepping()
    {
        if (turningFoot == null)
        {
            return;
        }

        if (latestFootTf != turningFoot)
        {
            latestFootPos = turningFoot.position;
            return;
        }

        if (cc == null)
        {
            transform.Translate(latestFootPos - turningFoot.position, Space.World);
        }
        else
        {
            /*cc.*/Move(latestFootPos - turningFoot.position);
        }
    }

    //public void Stepping()
    //{
    //    //Debug.Log("Stepping");
    //    walkTime = 0;
    //    //  anim.SetLayerWeight((int)AnimationLayer.LeftFoot, 0f);
    //    //  anim.SetLayerWeight((int)AnimationLayer.RightFoot, 0f);
    //    if (turningFoot != null)
    //    {
    //        return;
    //    }

    //    if (latestFootTf != turningFoot)
    //    {
    //        latestFootPos = turningFoot.position;

    //        latestFootTf = turningFoot;
    //        return;
    //    }

    //    if (turningFoot == null)
    //        return;

    //    if (isFirstSteppingFrame)
    //    {
    //        isFirstSteppingFrame = false;
    //        latestFootPos = turningFoot.position;
    //        return;
    //    }

    //    Vector3 calibration = latestFootPos - turningFoot.position;
    //    calibration = new Vector3(calibration.x, 0, calibration.z);
    //    transform.Translate(calibration*2f,Space.World);
    //    latestFootPos = turningFoot.position;

    //}

    private float walkTime;

    public void Moving()
    {
        walkTime += Time.deltaTime;

      //  anim.SetLayerWeight((int)AnimationLayer.LeftFoot, leftFootWalkTransferCurve.Evaluate(Mathf.Min(
     //       walkTime / (anim.GetNextAnimatorStateInfo(0).length * anim.GetNextAnimatorStateInfo(0).speed), 1f)));

      //  anim.SetLayerWeight((int)AnimationLayer.RightFoot, rightFootWalkTransferCurve.Evaluate(Mathf.Min(
     //       walkTime / (anim.GetNextAnimatorStateInfo(0).length * anim.GetNextAnimatorStateInfo(0).speed),1f)));

        isFirstSteppingFrame = true;
      //  latestFootPos = Vector3.zero;
    }

    private RaycastHit leftHit;
    private RaycastHit rightHit;

    //private void OnAnimatorIK(int layerIndex)
    //{
    //    if (!footIKOn) { return; }

      

    //    if(Physics.Raycast(new Vector3(feet[1].position.x, anim.bodyPosition.y, feet[1].position.z),Vector3.down,out leftHit, 2f, LayerMask.NameToLayer("Character")))
    //    {
    //        Debug.Log(leftHit.transform.name);
    //        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
    //        anim.SetIKPosition(AvatarIKGoal.LeftFoot, leftHit.point);
    //    }
    //    else
    //    {
    //        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
    //    }
    //    if (Physics.Raycast(new Vector3(feet[0].position.x, anim.bodyPosition.y, feet[0].position.z), Vector3.down, out rightHit, 2f, LayerMask.NameToLayer("Character")))
    //    {
    //        Debug.Log(rightHit.transform.name);
    //        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
    //        anim.SetIKPosition(AvatarIKGoal.RightFoot, rightHit.point);
    //    }
    //    else
    //    {
    //        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
    //    }
    //}

}
