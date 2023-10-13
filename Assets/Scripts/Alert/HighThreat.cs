using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HighThreat : MonoBehaviour
{
    private Camera _camera;

    public Camera GetCamera()
    {
        return _camera;
    }
    
    public float threatRate;
    
    public void EvaluateThreat(Transform target)
    {
        Vector3 localTargetDir =
            transform.InverseTransformDirection(Vector3.Normalize(target.position - transform.position));
        threatRate = _camera.farClipPlane>localTargetDir.z ?
            Vector3.Dot(localTargetDir,Vector3.forward) : -1; 
    }

    IEnumerator Start()
    {
        _camera = GetComponent<Camera>();
        while (ThreatSortManager.manager == null)
        {
            yield return null;
        }
        ThreatSortManager.manager.AddThreat(this);
        yield return null;
    }
    
    private void OnEnable()
    {
        ThreatSortManager.manager?.AddThreat(this);
    }

    private void OnDisable()
    {
        ThreatSortManager.manager?.RemoveThreat(this);
    }


}
