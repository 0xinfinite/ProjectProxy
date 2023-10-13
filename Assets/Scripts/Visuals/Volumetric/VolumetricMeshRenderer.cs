using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

namespace UnityTemplateProjects.Visuals
{
//[ExecuteInEditMode]
    public class VolumetricMeshRenderer : MonoBehaviour
    {
        [SerializeField]
        private Camera _camera1;
        [SerializeField]
        private Camera _camera2;
        [SerializeField]
        private Camera _camera3;
        [SerializeField]
        private Camera _camera4;

        [SerializeField]
        private RenderTexture _depthTexture1;
        [SerializeField]
        private RenderTexture _depthTexture2;
        [SerializeField]
        private RenderTexture _depthTexture3;
        [SerializeField]
        private RenderTexture _depthTexture4;

        public void SetCamera(int index, Camera cam)
        {
            switch (index)
            {
                case 1:
                    _camera1.enabled = false;
                    _camera1 = cam;
                    _camera1.targetTexture = _depthTexture1;
                    _camera1.enabled = true;
                    break;
                case 2:
                    _camera2.enabled = false;
                    _camera2 = cam;
                    _camera2.targetTexture = _depthTexture2;
                    _camera2.enabled = true;
                    break;
                case 3:
                    _camera3.enabled = false;
                    _camera3 = cam;
                    _camera3.targetTexture = _depthTexture3;
                    _camera3.enabled = true;
                    break;
                case 4:
                    _camera4.enabled = false;
                    _camera4 = cam;
                    _camera4.targetTexture = _depthTexture4;
                    _camera4.enabled = true;
                    break;
            }
        }
        
        //private Camera mainCamera;
        
        //[SerializeField] private Transform _target;
        [SerializeField] private Material _material;

        private bool cam1On;
        private bool cam2On;
        private bool cam3On;
        private bool cam4On;
        
        private void OnEnable()
        {
            cam1On = _camera1 != null && _depthTexture1 != null ? true : false;
            cam2On = _camera2 != null && _depthTexture2 != null ? true : false;
            cam3On = _camera3 != null && _depthTexture3 != null ? true : false;
            cam4On = _camera4 != null && _depthTexture4 != null ? true : false;
            //mainCamera = Camera.main;
        }

        void LateUpdate()
        {
            //if (_camera == null || _material == null || _target == null) return;
            
            //Camera currentCamera = Camera.main;
           // Matrix4x4 matrixCameraToWorld = _camera.cameraToWorldMatrix;
            //Matrix4x4 matrixProjectionInverse = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false).inverse;
            
            //Matrix4x4 matrixMainCameraToWorld = mainCamera.cameraToWorldMatrix;
            //Matrix4x4 matrixMainProjectionInverse = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false).inverse;

            
            //Matrix4x4 matrixHClipToWorld = matrixMainCameraToWorld * matrixMainProjectionInverse;//matrixMainCameraToWorld * matrixMainProjectionInverse;
            
          //Matrix4x4 matrix_MVP = P * V * M;//_camera.worldToCameraMatrix;
          int count = 0;
          if (cam1On)
          {
              Matrix4x4 matrix_VP1 = MatrixConversion.GetVP(_camera1);
              Matrix4x4 matrix1 = MatrixConversion.GetProjectionMatrix(_camera1);
              _material.SetMatrix("_Projector1", matrix1);
              _material.SetMatrix("_Camera1MatrixVP", matrix_VP1);
              count = 1;
          }

          if (cam2On)
          {
              Matrix4x4 matrix_VP2 = MatrixConversion.GetVP(_camera2);
              Matrix4x4 matrix2 = MatrixConversion.GetProjectionMatrix(_camera2);
              _material.SetMatrix("_Projector2", matrix2);
              _material.SetMatrix("_Camera2MatrixVP", matrix_VP2);
              count = 2;
          }

          if (cam3On)
          {
              Matrix4x4 matrix_VP3 = MatrixConversion.GetVP(_camera3);
              Matrix4x4 matrix3 = MatrixConversion.GetProjectionMatrix(_camera3);
              _material.SetMatrix("_Projector3", matrix3);
              _material.SetMatrix("_Camera3MatrixVP", matrix_VP3);
              count = 3;
          }

          if (cam4On)
          {
              Matrix4x4 matrix_VP4 = MatrixConversion.GetVP(_camera4);
              Matrix4x4 matrix4 = MatrixConversion.GetProjectionMatrix(_camera4);
              _material.SetMatrix("_Projector4", matrix4);
              _material.SetMatrix("_Camera4MatrixVP", matrix_VP4);
              count = 4;
          }
          _material.SetInt("_ProjectorCount", count);
        }

      
    
        // struct UpdateVolumetricMeshJob : IJobFor
        // {
        //     
        //  //public NativeArray<>
        //
        //  // [ReadOnly]
        //  // public NativeArray<float3> edgeVertex1;
        //  // [ReadOnly]
        //  // public NativeArray<float3> edgeVertex2;
        //  // [WriteOnly]
        //  // public NativeArray<float3> stretchedVertex1;
        //  // [WriteOnly]
        //  // public NativeArray<float3> stretchedVertex2;
        //  public NativeArray<EdgeForNative> edgesNative;
        //  [WriteOnly]
        //  public NativeArray</*StretchedVertices*/float3x2> stretchedVertices;
        //  [ReadOnly]
        //  public LightSourceNative lightSource;
        //  //[WriteOnly]
        //  //public NativeArray<float3x4> matrices;
        //
        //  //[ReadOnly] public NativeArray<PropertyForMatrix> matrixProperty;//MatrixProprety matrixProperty;
        //
        //     public void Execute(int index)
        //     {
        //         StretchedVertices newVertices = new StretchedVertices()
        //         {
        //             stretchedVertex1 = math.normalizesafe( edgesNative[index].edgeVertex1 - lightSource.lightSourcePos)*100,
        //             stretchedVertex2 = math.normalizesafe( edgesNative[index].edgeVertex2 - lightSource.lightSourcePos)*100
        //         };
        //         stretchedVertices[index] = float3x2(newVertices.stretchedVertex1.x,newVertices.stretchedVertex1.y,newVertices.stretchedVertex1.z,
        //             newVertices.stretchedVertex2.x,newVertices.stretchedVertex2.y,newVertices.stretchedVertex2.z) ;
        //         // float3x3 r = matrixProperty[index].worldRotation;
        //         // matrices[index] = float3x4(r.c0, r.c1, r.c2, matrixProperty[index].worldPosition);
        //     }
        // }
        //
        // struct EdgeForNative
        // {
        //     public float3 edgeVertex1;
        //     public float3 edgeVertex2;
        // }
        //
        // struct StretchedVertices
        // {
        //     public float3 stretchedVertex1;
        //     public float3 stretchedVertex2;
        // }
        //
        // // struct MatrixProprety
        // // {
        // //     public float m00;
        // //     public float m01;
        // //     public float m02;
        // //     public float m03;
        // //     public float m10;
        // //     public float m11;
        // //     public float m12;
        // //     public float m13;
        // //     public float m20;
        // //     public float m21;
        // //     public float m22;
        // //     public float m23;
        // //     public float m30;
        // //     public float m31;
        // //     public float m32;
        // //     public float m33;
        // // }
        // // struct PropertyForMatrix
        // // {
        // //     public float3x3 worldRotation;
        // //     public float3 worldPosition;
        // // }
        //
        // struct LightSourceNative
        // {
        //     public float3 lightSourcePos;
        //     // public int lightType;
        //     // public float lightFov;
        //     // public float lightAspect;
        // }
        //
        // // private Mesh EdgeToQuad(Edge edge, Vector3 offset1, Vector3 offset2)
        // // {
        // //     Matrix4x4 mat = _archivedEdges.GetMatrixOnMatSets(edge.parentMatId);
        // //     Vector3 vertex1 = edge.GetFirstVertexOnScene(mat);
        // //     Vector3 vertex2 = edge.GetSecondVertexOnScene(mat);
        // //     return CreateQuadMesh(); //(new Vector3[4] { vertex1, vertex2, vertex1 + offset1, vertex2 + offset2 });
        // // }
        //
        // private Mesh CreateQuadMesh()
        // {
        //     Mesh quadMesh = new Mesh();
        //
        //     int[] triangles = new int[]
        //     {
        //         0, 1, 2,
        //         2, 3, 0
        //     };
        //
        //     quadMesh.vertices = new Vector3[4]
        //     {
        //         Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero
        //     };
        //     quadMesh.triangles = triangles;
        //     quadMesh.uv = new Vector2[4]
        //     {
        //         Vector2.zero, new Vector2(0, 1), 
        //         new Vector2(1, 0), Vector2.one
        //     };
        //     quadMesh.SetColors(new Color[4]
        //     {
        //         new Color(1, 0, 0, 0), new Color(0, 1, 0, 0),
        //         new Color(0, 0, 1, 0), new Color(0, 0, 0, 1)
        //     });
        //
        //     return quadMesh;
        // }
        //
        // private Mesh _mesh;
        //
        // [SerializeField] private ArchivedEdges _archivedEdges;
        //
        // [SerializeField] private Camera myCamera;
        //
        // [SerializeField] private Transform lightSource;
        // [SerializeField] private LightType lightSourceType;
        // [SerializeField, Range(0,180)] private float lightSourceFov = 30;
        // [SerializeField, Min(0.00001f)] private float lightSourceAspect = 1;
        //
        // [SerializeField] private Material _material;
        //
        // private static MaterialPropertyBlock block;
        //
        // private void OnDrawGizmosSelected()
        // {
        //     if (lightSource == null)
        //         return;
        //     
        //     Gizmos.matrix = lightSource.localToWorldMatrix;
        //     switch (lightSourceType)
        //     {
        //         case LightType.Spot:
        //             Gizmos.DrawFrustum(Vector3.zero, lightSourceFov, 100, 0, 1);
        //             break;
        //         case LightType.Point:
        //             Gizmos.DrawWireSphere(Vector3.zero, 0.25f);
        //             break;
        //     }
        //     
        // }
        //
        // //private NativeArray<float3>[] verticePos1;
        // //private NativeArray<float3>[] verticePos2;
        // //private NativeArray<float3x4> matrices;
        //
        // //private static readonly int matricesId = Shader.PropertyToID("_Matrices");
        // //private static readonly int stretched1Id = Shader.PropertyToID("_Stretched1");
        // //private static readonly int stretched2Id = Shader.PropertyToID("_Stretched2");
        // private static readonly int stretchedId = Shader.PropertyToID("_Stretched");
        //
        // private NativeArray<EdgeForNative> edgesNative;
        // private NativeArray<float3x2> stretchedVertices;//<StretchedVertices> stretchedVertices;
        // ComputeBuffer[] meshBuffers;
        //
        //
        // // Start is called before the first frame update
        // void Start()
        // {
        //     if (myCamera == null)
        //         myCamera = Camera.main?Camera.main:Camera.allCameras[0];
        //
        //     _mesh = CreateQuadMesh();//(new Vector3[4] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
        //
        //     edgesNative = EdgesToNative(_archivedEdges);
        // }
        //
        // private void OnDestroy()
        // {
        //     edgesNative.Dispose();
        // }
        //
        // private void OnEnable()
        // {
        //     int length = 1;
        //     int stride = 12 * 4;
        //     int count = _archivedEdges.edges.Count;
        //     meshBuffers = new ComputeBuffer[count];
        //     //matrices = new NativeArray<float3x4>(count, Allocator.Persistent);//new NativeArray<float3x4>[count];
        //     stretchedVertices = new NativeArray</*StretchedVertices*/float3x2>(count, Allocator.Persistent);
        //     for (int i = 0; i < count; i++)
        //     {
        //         meshBuffers[i] = new ComputeBuffer(length, stride);
        //         //stretchedVertices[i] = new NativeArray<float3x2>(count, Allocator.Persistent);
        //         //matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
        //         // Matrix4x4 mat = _archivedEdges.GetMatrixOnMatSets(_archivedEdges.edges[i].parentMatId);
        //         // matrices[i] = new float3x4(mat.m00, mat.m01, mat.m02, mat.m03,
        //         //     mat.m10, mat.m11, mat.m12, mat.m13,
        //         //     mat.m20, mat.m21, mat.m22, mat.m23);
        //     }
        //
        //     block = new MaterialPropertyBlock();
        // }
        //
        // private void OnDisable()
        // {
        //     //stretchedVertices.Dispose();
        //      for (int i = 0; i < meshBuffers.Length; i++)
        //      {
        //          meshBuffers[i].Release();
        //          //stretchedVertices[i].Dispose();
        //     //     //matrices[i].Dispose();
        //      }
        //     //
        //     //
        //      meshBuffers = null;
        //      stretchedVertices.Dispose(); //= null;
        //     // matrices.Dispose();//matrices = null; 
        //    
        //    block.Clear();
        //    block = null;
        // }
        //
        // // Update is called once per frame
        // void Update()
        // {
        //     JobHandle jobHandle = default;
        //
        //
        //     for (int i = 0; i < _archivedEdges.edges.Count; i++)
        //     {
        //         var position = lightSource.position;
        //         jobHandle = new UpdateVolumetricMeshJob
        //         {
        //             edgesNative = edgesNative,
        //             lightSource = new LightSourceNative()
        //             {
        //                 lightSourcePos = new float3(position.x, position.y,
        //                     position.z),
        //                 // lightType = lightSourceType == LightType.Spot?1:0 ,
        //                 // lightFov = lightSourceFov,
        //                 // lightAspect = lightSourceAspect
        //             },
        //             stretchedVertices = stretchedVertices,
        //             //matrices = matrices,
        //
        //             //matrixProperty = 
        //             //matrices = matrices
        //         }.ScheduleParallel(_archivedEdges.edges.Count, 1, jobHandle);
        //     }
        //
        //     jobHandle.Complete();
        //
        //     var cameraTransform = myCamera.transform;
        //     var bounds = new Bounds(cameraTransform.position + cameraTransform.forward, Vector3.one);
        //
        //     for (int i = 0; i < _archivedEdges.edges.Count; i++)
        //     {
        //         ComputeBuffer buffer = meshBuffers[i];
        //         // buffer.SetData(matrices);
        //         //buffer.SetData(stretchedVertices[i]);
        //          block.SetBuffer(stretchedId, buffer);
        //         
        //         Graphics.DrawMeshInstancedProcedural(_mesh, 0, _material,
        //             bounds, i, block);
        //     }
        // }
        //
        // private NativeArray<EdgeForNative> EdgesToNative(ArchivedEdges ae)
        // {
        //     List<Edge> edges = ae.edges;
        //     NativeArray<EdgeForNative> newArr = new NativeArray<EdgeForNative>(edges.Count, Allocator.Persistent);
        //
        //     for (int i = 0; i < edges.Count; i++)
        //     {
        //         Matrix4x4 mat = ae.GetMatrixOnMatSets(edges[i].parentMatId);
        //         newArr[i] = new EdgeForNative() { edgeVertex1 = edges[i].GetFirstVertexOnScene(mat), 
        //             edgeVertex2 = edges[i].GetSecondVertexOnScene(mat) }; 
        //     }
        //
        //     return newArr;
        // }
    }
}