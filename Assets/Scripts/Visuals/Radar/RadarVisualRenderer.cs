using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class RadarVisualRenderer : MonoBehaviour
{
    //[SerializeField] private Camera _beamCamera;
    [SerializeField] private Camera _cashCamera;
    [SerializeField] private Material _material;
    //[SerializeField] private Material _screenMaterial;
    [SerializeField] private RenderTexture _cashRT;
    //[SerializeField] private RenderTexture _beamRT;
    private bool canGo;
    
    // Start is called before the first frame update
    void Start()
    {
        //_camera = GetComponent<Camera>();
    }

    private static int _RadarCameraMatrixVPID = Shader.PropertyToID("_RadarCameraMatrixVP");
    private static int _RadarProjectorID = Shader.PropertyToID("_RadarProjector");
    private static int _RadarCameraMatrix_I_VPID = Shader.PropertyToID("_RadarCameraMatrix_I_VP");
    private static int _RadarDepthTexID = Shader.PropertyToID("_RadarDepthTex");
    private static int _RadarCameraPosID = Shader.PropertyToID("_RadarCameraPos");
    private static int _RadarCameraToWorldID = Shader.PropertyToID("_RadarCameraToWorld");
    private static int _RadarCameraInverseProjectionID = Shader.PropertyToID("_RadarCameraInverseProjection");
    private static int _RadarCameraWorldToHClipMatrixID = Shader.PropertyToID("_RadarCameraWorldToHClipMatrix");
    
    
        
    private void OnEnable()
    {
        canGo = //_beamCamera != null && 
                _cashCamera != null && _material != null ? true : false;
    }

    // Update is called once per frame
   
    void LateUpdate()//OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!canGo) return;
        
        
        Matrix4x4 matrix_VP = MatrixConversion.GetVP(_cashCamera);
        Matrix4x4 matrix = MatrixConversion.GetProjectionMatrix(_cashCamera);
        
        Matrix4x4 matrixCameraToWorld = _cashCamera.cameraToWorldMatrix;
        Matrix4x4 matrixProjectionInverse = GL.GetGPUProjectionMatrix(_cashCamera.projectionMatrix, false).inverse;
        Matrix4x4 matrixHClipToWorld = matrixProjectionInverse*matrixCameraToWorld;


        //_beamCamera.Render();
        _cashCamera.Render();
        //Graphics.Blit(_beamRT, _cashRT);
        _material.SetTexture(_RadarDepthTexID, _cashRT);
        _material.SetMatrix(_RadarProjectorID, matrix);
        _material.SetMatrix("_RadarMatrixHClipToWorld", matrixHClipToWorld);
        _material.SetMatrix(_RadarCameraMatrixVPID, matrix_VP);
        _material.SetVector(_RadarCameraPosID, _cashCamera.transform.position);
        _material.SetMatrix(_RadarCameraToWorldID, _cashCamera.cameraToWorldMatrix);
        _material.SetMatrix(_RadarCameraWorldToHClipMatrixID, _cashCamera.projectionMatrix*_cashCamera.worldToCameraMatrix);
        _material.SetMatrix(_RadarCameraInverseProjectionID,Matrix4x4.Inverse( _cashCamera.projectionMatrix));
        _material.SetMatrix(_RadarCameraMatrix_I_VPID,  Matrix4x4.Inverse(matrix_VP));
        _material.SetMatrix("_RadarCameraMatrix_I_P",  Matrix4x4.Inverse(_cashCamera.projectionMatrix));
        _material.SetMatrix("_RadarCameraMatrix_I_V",  _cashCamera.worldToCameraMatrix.inverse);
        
        //Graphics.Blit(src, dest, cameraMaterial);
    }

    // private void OnRenderImage(RenderTexture src, RenderTexture dest)
    // {
    //     //if (!canGo)
    //         return;
    //     
    //         Matrix4x4 matrix_VP = MatrixConversion.GetVP(_cashCamera);
    //         Matrix4x4 matrix = MatrixConversion.GetProjectionMatrix(_cashCamera);
    //         _beamCamera.Render();
    //      //   _cashCamera.Render();
    //         Graphics.Blit(_beamRT, _cashRT);
    //         _screenMaterial.SetTexture(_RadarDepthTexID, _cashRT);
    //         //_material.SetMatrix("_RadarProjector", matrix);
    //         _screenMaterial.SetMatrix(_RadarCameraMatrixVPID, matrix_VP);
    //     
    //         Graphics.Blit(src, dest, _screenMaterial);
    //     
    // }
}
