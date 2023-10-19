using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Jobs;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine.Rendering;

[BurstCompile]
struct BoneMatriceJob : IJobParallelForTransform
{
    public NativeArray<float4x4> localToWorldMatrices;

    public void Execute(int index, TransformAccess transform)
    {
        localToWorldMatrices[index] = transform.localToWorldMatrix;
    }
}

[BurstCompile]
struct MeshInitJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float3> origVertices;
    [ReadOnly]
    public NativeArray<float3> origNormals;
    public NativeArray<float3> vertices;
    public NativeArray<float3> normals;

    public void Execute(int index)
    {
        vertices[index] = origVertices[index];
        normals[index] = origNormals[index];
    }
}

[BurstCompile]
struct MeshBlendshapeJob : IJobParallelFor
{
    public NativeArray<float3> vertices;
    public NativeArray<float3> normals;
    [ReadOnly]
    public NativeArray<float3> deltaVertices;
    [ReadOnly]
    public NativeArray<float3> deltaNormals;
    //public NativeArray<float> blendshapeWeights;
    [ReadOnly]
    public float weight;

    public void Execute(int index)
    {
            vertices[index] += deltaVertices[index] * weight;        
    }
}

[BurstCompile]
struct MeshDeformJob : IJobParallelFor
{
    public NativeArray<float3> vertices;
    public NativeArray<float3> normals;
    [ReadOnly]
    public NativeArray<BoneWeight1> boneWeights;
    [ReadOnly]
    public NativeArray<float4x4> bindposes;
    [ReadOnly]
    public NativeArray<float4x4> bones;
    [ReadOnly]
    public NativeArray<byte> bonesPerVertex;
    [ReadOnly]
    public float4x4 transformMat;
    [ReadOnly]
    public NativeArray<int>
        boneWeightIndexStartArray;
    //public int boneWeightIndex;

    public void Execute(int index)
    {
        if (index >= vertices.Length)
            return;

        float3 deformedVertex = Vector3.zero;
        float3 deformedNormal = Vector3.zero;
        var numberOfBonesForThisVertex = bonesPerVertex[index];

        int boneWeightIndex = boneWeightIndexStartArray[index];
        
        for (int j = 0; j < numberOfBonesForThisVertex; j++)
        {
            int boneIndex = boneWeights[boneWeightIndex+j].boneIndex;
            float4x4 boneMat = bones[boneIndex];
            boneMat = math.mul(boneMat, bindposes[boneIndex]);

            float weight = boneWeights[boneWeightIndex+j].weight;
            deformedVertex += DeformVertex(vertices[index], weight, boneMat);
            deformedNormal += DeformNormal(normals[index]/* + deltaNormals[i]*/, weight, boneMat);

        }

            vertices[index] = transformMat.InverseTransformPoint(deformedVertex);
            normals[index] = transformMat.InverseTransformDirection(math.normalizesafe(deformedNormal));
        
    }

    public float3 DeformVertex(float3 vertex, float weight, float4x4 boneMatrix)
    {
        if (weight <= 0) return float3.zero;
        float3 boneVertex = math.mul(boneMatrix, new float4(vertex, 1)).xyz;
        return boneVertex * weight;
    }
    public float3 DeformNormal(float3 normal, float weight, float4x4 boneMatrix)
    {
        if (weight <= 0) return float3.zero;

        float3 boneNormal = math.mul(boneMatrix, new float4(normal,0)).xyz;
        return boneNormal * weight;
    }
}

//[GenerateTestsForBurstCompatibility]
//public struct SpawnJob : IJobParallelFor
//{
//    public Entity Prototype;
//    public int EntityCount;
//    public EntityCommandBuffer.ParallelWriter Ecb;

//    public void Execute(int index)
//    {
//        // Clone the Prototype entity to create a new entity.
//        var e = Ecb.Instantiate(index, Prototype);
//        // Prototype has all correct components up front, can use SetComponent to
//        // set values unique to the newly created entity, such as the transform.
//        Ecb.SetComponent(index, e, new LocalToWorld { Value = ComputeTransform(index) });
//    }

//    public float4x4 ComputeTransform(int index)
//    {
//        return float4x4.Translate(new float3(index, 0, 0));
//    }
//}

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter)/*, typeof(SkinnedMeshRenderer)*/)]
//[CanEditMultipleObjects]
public class CustomSkinnedMeshRenderer : MonoBehaviour
{
    public enum ThreadType { SingleThread = 0, Job, JobAndEntity}   

    [Tooltip("JobAndEntity is not working currently!")][SerializeField] ThreadType threadType;

    public Mesh mesh;
    [SerializeField]
    private Material[] sharedMaterials;
    [SerializeField]
    private Material[] sharedManualMaterials;
    [SerializeField]
    private bool renderManually;
    public Transform rootBone;
    public Transform[] bones;
    public BoneWeight[] weights;
    
    [Range(0,1)]
    public float[] blendshapeWeights;
    public bool syncShapeValueWithSkinned = true;

    private bool shaderDesignated;
    private Shader prevShader;

    private MeshFilter meshFilter;
    private Mesh deformedMesh;
    private Matrix4x4[] bindposes;
    private Vector3[] tangents3;

    [SerializeField]
    private SkinnedMeshRenderer origSkinned;
     NativeArray<float4x4> bonesForJob;
    TransformAccessArray bonesAccess;
    NativeArray<float4x4> bindPosesForJob;
    NativeArray<float3> origVerticesForJob;
    NativeArray<float3> origNormalsForJob;
    NativeArray<float3> verticesForJob;
    NativeArray<float3> normalsForJob;
    NativeArray<int> boneWeightIndexStartArray;
    NativeArray<float3> deltaVerticesForJob;
    NativeArray<float3> deltaNormalsForJob;
    NativeArray<byte> bonesPerVertex;
    NativeArray<BoneWeight1> boneWeights;

    GraphicsBuffer meshTriangles;
    GraphicsBuffer meshPositions;
    GraphicsBuffer meshNormals;
    GraphicsBuffer meshUVs;
    GraphicsBuffer meshTangents;
    GraphicsBuffer meshColors;
    //GraphicsBuffer meshIndices;

    GraphicsBuffer commandBuffer;
    GraphicsBuffer.IndirectDrawArgs[] commandData;

    private Entity entity;
    private EntityManager entityManager;
    private RenderMeshArray renderMeshArray;

    const int commandCount = 2;

    void Awake()
    {
        if (this.enabled)
        {
            meshFilter = GetComponent<MeshFilter>();

            if (origSkinned != null || TryGetComponent<SkinnedMeshRenderer>(out origSkinned))
            {
                mesh = origSkinned.sharedMesh;
                bones = origSkinned.bones;
                weights = mesh.boneWeights;
                blendshapeWeights = new float[origSkinned.sharedMesh.blendShapeCount];
                rootBone = origSkinned.rootBone;
                bindposes = mesh.bindposes;

                // sharedMaterials = origSkinned.sharedMaterials;

                origSkinned.enabled = false;

            }

            if (mesh != null)
            {
                deformedMesh = DuplicateMesh(mesh);
                meshFilter.mesh = deformedMesh;
                tangents3 = GetTangentsToVector3Array(mesh.tangents);
            }

            if (TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
            {
                renderer.sharedMaterials = /*origSkinned.*/sharedMaterials;
                //if(designatedShader != null)
                renderer.enabled = threadType == ThreadType.JobAndEntity ? false : true;//!multiThread ? true:false;
                //else
                //    renderer.enabled = true;
            }


            bonesPerVertex = mesh.GetBonesPerVertex();
            boneWeights = mesh.GetAllBoneWeights();

            if (threadType != ThreadType.SingleThread)
            {
                bonesForJob = new NativeArray<float4x4>(bones.Length, Allocator.Persistent);
                bonesAccess = new TransformAccessArray(bones);
                bindPosesForJob = MatrixToFloat4x4(bindposes);
                origVerticesForJob = Vector3ToFloat3(mesh.vertices, Allocator.Persistent);
                origNormalsForJob = Vector3ToFloat3(mesh.normals, Allocator.Persistent);
                verticesForJob = Vector3ToFloat3(mesh.vertices, Allocator.Persistent);
                normalsForJob = Vector3ToFloat3(mesh.normals, Allocator.Persistent);
                deltaVerticesForJob = new NativeArray<float3>(mesh.vertices.Length, Allocator.Persistent);
                deltaNormalsForJob = new NativeArray<float3>(mesh.normals.Length, Allocator.Persistent);

                NativeArray<byte> bonesPerVertex = mesh.GetBonesPerVertex();
                boneWeightIndexStartArray = new NativeArray<int>(mesh.vertices.Length, Allocator.Persistent);
                int currentIndex = 0;
                for (int i = 0; i < mesh.vertices.Length; ++i)
                {
                    boneWeightIndexStartArray[i] = currentIndex;
                    currentIndex += bonesPerVertex[i];
                }
            }
        }
    }

    bool renderedManually;

    void Start()
    {
        //{
        //    { 
                //shaderDesignated = designatedShader == null ? false : true;

                //if (renderManually)
                //{
                //    renderedManually = true;
                //    //for (int i = 0; i < sharedMaterials.Length; ++i)
                //    //{
                //    //    prevShader = sharedMaterials[i].shader;
                //    //    sharedMaterials[i].shader = designatedShader;
                //    //}

                //    //meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, deformedMesh.triangles.Length, sizeof(int));
                //    //meshTriangles.SetData(deformedMesh.triangles);
                //    meshPositions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, deformedMesh.vertices.Length, 3 * sizeof(float));
                //    meshPositions.SetData(deformedMesh.vertices);
                //    meshNormals = new GraphicsBuffer(GraphicsBuffer.Target.Structured, deformedMesh.normals.Length, 3 * sizeof(float));
                //    meshNormals.SetData(deformedMesh.normals);
                //    meshUVs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, deformedMesh.uv.Length, 2 * sizeof(float));
                //    meshUVs.SetData(deformedMesh.uv);
                //    meshTangents = new GraphicsBuffer(GraphicsBuffer.Target.Structured, deformedMesh.tangents.Length, 4 * sizeof(float));
                //    meshTangents.SetData(deformedMesh.tangents);
                //    meshColors = new GraphicsBuffer(GraphicsBuffer.Target.Structured, deformedMesh.colors.Length, 3 * sizeof(float));
                //    meshColors.SetData(ColorsToVector3(deformedMesh.colors));
                //    //meshIndices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, deformedMesh.GetIn.Length, 3*sizeof(int));
                //    commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawArgs.size);
                //    commandData = new GraphicsBuffer.IndirectDrawArgs[commandCount];
                //}
        //    }
        //}
        if(threadType == ThreadType.JobAndEntity)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            entityManager = world.EntityManager;

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            // Create a RenderMeshDescription using the convenience constructor
            // with named parameters.
            var desc = new RenderMeshDescription(
                shadowCastingMode: ShadowCastingMode.On,
                receiveShadows: true);

            // Create an array of mesh and material required for runtime rendering.
            renderMeshArray = new RenderMeshArray(sharedMaterials, new Mesh[] { deformedMesh });
            
            // Create empty base entity
            entity = entityManager.CreateEntity();

            // Call AddComponents to populate base entity with the components required
            // by Entities Graphics
            RenderMeshUtility.AddComponents(
                entity,
                entityManager,
                desc,
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
            entityManager.AddComponentData(entity, new LocalToWorld());

            // Spawn most of the entities in a Burst job by cloning a pre-created prototype entity,
            // which can be either a Prefab or an entity created at run time like in this sample.
            // This is the fastest and most efficient way to create entities at run time.
            //var spawnJob = new SpawnJob
            //{
            //    Prototype = prototype,
            //    Ecb = ecb.AsParallelWriter(),
            //    EntityCount = 1,
            //};

            //var spawnHandle = spawnJob.Schedule(1, 1);
            //spawnHandle.Complete();

            //ecb.Playback(entityManager);
            //ecb.Dispose();
            //entityManager.DestroyEntity(prototype);
        }
    }

    private void OnValidate()
    {
            if (TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
                renderer.enabled = renderManually ? false : true;

        if (renderManually)
        {
            Start();
        }
     
    }


    private void OnDestroy()
    {
        if (threadType != ThreadType.SingleThread)
        {
            bonesForJob.Dispose();
            bindPosesForJob.Dispose();
            origVerticesForJob.Dispose();
            origNormalsForJob.Dispose();
            verticesForJob.Dispose();
            normalsForJob.Dispose();
            boneWeightIndexStartArray.Dispose();
            deltaVerticesForJob.Dispose();
            deltaNormalsForJob.Dispose();

            if (renderedManually)
            {
                //for (int i = 0; i < sharedMaterials.Length; ++i)
                //    sharedMaterials[i].shader = prevShader;

                meshTriangles?.Dispose();
                meshTriangles = null;
                meshPositions?.Dispose();
                meshPositions = null;
                meshNormals?.Dispose();
                meshNormals = null;
                meshUVs?.Dispose();
                meshUVs = null;
                meshTangents?.Dispose();
                meshTangents = null;
                meshColors?.Dispose();
                meshColors = null;
                commandBuffer?.Dispose();
                commandBuffer = null;
            }
        }
    }

    private Mesh DuplicateMesh(Mesh sourceMesh)
    {
        Mesh targetMesh = new Mesh();
        targetMesh.name = sourceMesh.name + "_Deformed";
        targetMesh.vertices = sourceMesh.vertices;
        targetMesh.normals = sourceMesh.normals;
        targetMesh.tangents = sourceMesh.tangents;
        targetMesh.triangles = sourceMesh.triangles;
        targetMesh.uv = sourceMesh.uv;
        targetMesh.colors = sourceMesh.colors;
        targetMesh.subMeshCount = sourceMesh.subMeshCount;
        targetMesh.bindposes = sourceMesh.bindposes;

        for (int i = 0; i < targetMesh.subMeshCount; ++i)
        {
            targetMesh.SetSubMesh(i, sourceMesh.GetSubMesh(i));
        }

        return targetMesh;
    }

    public Vector3[] ColorsToVector3(Color[] colors)
    {
        Vector3[] result = new Vector3[colors.Length];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new Vector3(colors[i].r, colors[i].g, colors[i].b);
        }

        return result;
    }

    public NativeArray<float3> Vector3ToFloat3(Vector3[] arr, Allocator allocator = Allocator.TempJob)
    {
        NativeArray<float3> result = new NativeArray<float3>(arr.Length, allocator);

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new float3(arr[i].x, arr[i].y, arr[i].z);
        }

        return result;
    }
    public Vector3[] Float3ToVector3(NativeArray<float3> arr)
    {
        Vector3[] result = new Vector3[arr.Length];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = arr[i]; 
        }

        return result;
    }

    public void GetAllBonesOfLocalToWorld(
        NativeArray<float4x4> arr, Transform[] bones)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = bones[i].localToWorldMatrix;
        }
    }

    public NativeArray<float4x4> MatrixToFloat4x4(Matrix4x4[] arr)
    {
        NativeArray<float4x4> result = new NativeArray<float4x4>(arr.Length, Allocator.Persistent);//new float4x4[arr.Length];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = arr[i];
        }

        return result;
    }

    public Vector3[] GetTangentsToVector3Array(Vector4[] tangents)
    {
        Vector3[] arr = new Vector3[(int)tangents.Length];

        for(int i =0; i < tangents.Length; ++i)
        {
            arr[i] = tangents[i];
        }
        return arr;
    }

    private Vector3 DeformVertex(Vector3 vertex, BoneWeight boneWeight, int weightNum)  //Deprecated : limited by 4 bones
    {

        float weight = boneWeight.weight0;

        if (weight <= 0) return Vector3.zero;

        Transform bone = bones[boneWeight.boneIndex0];
        int boneIndex = boneWeight.boneIndex0;
        switch (weightNum)
        {
            default:                
                break;
            case 1:
                weight = boneWeight.weight1;
                boneIndex = boneWeight.boneIndex1;
                bone = bones[boneWeight.boneIndex1];
                break;
            case 2:
                weight = boneWeight.weight2;
                boneIndex = boneWeight.boneIndex2;
                bone = bones[boneWeight.boneIndex2];
                break;
            case 3:
                weight = boneWeight.weight3;
                boneIndex = boneWeight.boneIndex3;
                bone = bones[boneWeight.boneIndex3];
                break;
        }

        Matrix4x4 boneMatrix = bone.localToWorldMatrix * bindposes[boneIndex]; 
        Vector3 boneVertex = boneMatrix.MultiplyPoint3x4(vertex);
        return boneVertex * weight;

    }

    private Vector3 DeformVertex(Vector3 vertex, BoneWeight1 boneWeight)
    {

        float weight = boneWeight.weight;

        if (weight <= 0) return Vector3.zero;

        Transform bone = bones[boneWeight.boneIndex];
        

        Matrix4x4 boneMatrix = bone.localToWorldMatrix * bindposes[boneWeight.boneIndex];
        Vector3 boneVertex = boneMatrix.MultiplyPoint3x4(vertex);
        return boneVertex * weight;

    }

    private Vector3 DeformNormal(Vector3 normal, BoneWeight boneWeight, int weightNum)  //Deprecated : limited by 4 bones
    {

        float weight = boneWeight.weight0;

        if (weight <= 0) return Vector3.zero;

        Transform bone = bones[boneWeight.boneIndex0];
        int boneIndex = boneWeight.boneIndex0;
        switch (weightNum)
        {
            default:
                break;
            case 1:
                weight = boneWeight.weight1;
                boneIndex = boneWeight.boneIndex1;
                bone = bones[boneWeight.boneIndex1];
                break;
            case 2:
                weight = boneWeight.weight2;
                boneIndex = boneWeight.boneIndex2;
                bone = bones[boneWeight.boneIndex2];
                break;
            case 3:
                weight = boneWeight.weight3;
                boneIndex = boneWeight.boneIndex3;
                bone = bones[boneWeight.boneIndex3];
                break;
        }

        Matrix4x4 boneMatrix = bone.localToWorldMatrix * bindposes[boneIndex];
        Vector3 boneNormal = boneMatrix.MultiplyVector(normal);
        return boneNormal * weight;

    }

    private Vector3 DeformNormal(Vector3 normal, BoneWeight1 boneWeight)
    {

        float weight = boneWeight.weight;

        if (weight <= 0) return Vector3.zero;

        Transform bone = bones[boneWeight.boneIndex];


        Matrix4x4 boneMatrix = bone.localToWorldMatrix * bindposes[boneWeight.boneIndex];
        Vector3 boneNormal = boneMatrix.MultiplyVector(normal);
        return boneNormal * weight;
    }

    private NativeArray<float> GetBlendshapeWeights(SkinnedMeshRenderer source, Allocator allocator = Allocator.TempJob)
    {
        NativeArray<float> blendshapeWeights = new NativeArray<float>(source.sharedMesh.blendShapeCount, allocator);

        for (int i = 0; i < blendshapeWeights.Length; i++)
        {
            blendshapeWeights[i] = source.GetBlendShapeWeight(i) * 0.01f;
        }

        return blendshapeWeights;
    }

    public Vector3 MeasureBoundsCenter(NativeArray<float3> positions)
    {
        float3 center = float3.zero;

        int add =(int)( positions.Length / 6);

        for (int i = 0; i < positions.Length; i+=add )
        {
            center += positions[i];
        }

        return center/6;
    }

    void Update()
    {

                if (mesh == null || bones == null || weights == null)
            return;

        
        if (threadType == ThreadType.SingleThread)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] deltaVertices = new Vector3[vertices.Length];
            Vector3[] normals = mesh.normals;

            for (int i = 0; i < blendshapeWeights.Length; i++)
            {
                int frameCount = mesh.GetBlendShapeFrameCount(i) - 1;
                mesh.GetBlendShapeFrameVertices(i, frameCount, deltaVertices, /*deltaNormals*/null, null);
                for (int j = 0; j < vertices.Length; ++j)
                {
                    if (syncShapeValueWithSkinned && origSkinned != null)
                    {
                        blendshapeWeights[i] = origSkinned.GetBlendShapeWeight(i) * 0.01f;
                    }

                    float weight = (blendshapeWeights[i] + (i == blendshapeWeights.Length - 1 ? -1 : 0));
                    vertices[j] += deltaVertices[j] * weight;
                }
            }
            int boneWeightIndex = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 deformedVertex = Vector3.zero;
                Vector3 deformedNormal = Vector3.zero;

                var numberOfBonesForThisVertex = bonesPerVertex[i];

                for (int j = 0; j < numberOfBonesForThisVertex; j++)
                {
                    deformedVertex += DeformVertex(vertices[i] + deltaVertices[i], boneWeights[boneWeightIndex]);
                    deformedNormal += DeformNormal(normals[i]/* + deltaNormals[i]*/, boneWeights[boneWeightIndex]);

                    boneWeightIndex++;
                }


                //deformedVertex += DeformVertex(vertices[i] + deltaVertices[i], weights[i], 0);
                //deformedVertex += DeformVertex(vertices[i] + deltaVertices[i], weights[i], 1);
                //deformedVertex += DeformVertex(vertices[i] + deltaVertices[i], weights[i], 2);
                //deformedVertex += DeformVertex(vertices[i] + deltaVertices[i], weights[i], 3);

                //deformedNormal += DeformNormal(normals[i]/* + deltaNormals[i]*/, weights[i], 0);
                //deformedNormal += DeformNormal(normals[i]/* + deltaNormals[i]*/, weights[i], 1);
                //deformedNormal += DeformNormal(normals[i]/* + deltaNormals[i]*/, weights[i], 2);
                //deformedNormal += DeformNormal(normals[i]/* + deltaNormals[i]*/, weights[i], 3);  //deprecated deform

                vertices[i] = transform.InverseTransformPoint(deformedVertex);
                normals[i] = //deformedNormal.normalized;//
                             transform.InverseTransformDirection(deformedNormal.normalized);
            }

            deformedMesh.vertices = vertices;
            deformedMesh.normals = normals;

            deformedMesh.RecalculateBounds();
        }
        else
        {
            JobHandle handle;

            MeshInitJob initJob = new MeshInitJob()
            {
                origVertices = origVerticesForJob
                ,
                origNormals = origNormalsForJob
                ,
                vertices = verticesForJob
                ,
                normals = normalsForJob
            };

            handle = initJob.Schedule(verticesForJob.Length, 1);

            handle.Complete();



            if (syncShapeValueWithSkinned && origSkinned != null)
            {
                Vector3[] _deltaVertices = new Vector3[mesh.vertices.Length];
                NativeArray<float> blendshapeWeightsForJob = GetBlendshapeWeights(origSkinned);

                for (int i = 0; i < blendshapeWeights.Length; i++)
                {
                    float weight = blendshapeWeightsForJob[i];
                    if (weight > 0)
                    {
                        int frameCount = mesh.GetBlendShapeFrameCount(i) - 1;
                        mesh.GetBlendShapeFrameVertices(i, frameCount, _deltaVertices, /*deltaNormals*/null, null);

                        NativeArray<float3> deltaVerticesForJob = Vector3ToFloat3(_deltaVertices);
                        MeshBlendshapeJob shapeJob = new MeshBlendshapeJob()
                        {
                            vertices = verticesForJob
                        ,
                            normals = normalsForJob
                        ,
                            deltaVertices = deltaVerticesForJob
                        ,
                            deltaNormals = deltaNormalsForJob
                        ,
                            weight = weight
                        };

                        handle = shapeJob.Schedule(verticesForJob.Length, 1);

                        handle.Complete();

                        deltaVerticesForJob.Dispose();
                    }
                }
                blendshapeWeightsForJob.Dispose();
            }

            BoneMatriceJob matricesJob = new BoneMatriceJob()
            {
                localToWorldMatrices = bonesForJob
            };

            handle = matricesJob.Schedule(bonesAccess);

            handle.Complete();


            MeshDeformJob deformJob = new MeshDeformJob()
            {
                boneWeightIndexStartArray = boneWeightIndexStartArray
            ,
                vertices = verticesForJob
            ,
                normals = normalsForJob
            ,
                bones = bonesForJob
            ,
                bonesPerVertex = bonesPerVertex
            ,
                boneWeights = boneWeights
            ,
                bindposes = bindPosesForJob
            ,
                transformMat = transform.localToWorldMatrix
            
            };

            handle = deformJob.Schedule(verticesForJob.Length, 1);

            handle.Complete();

            switch(threadType)
            {
                case ThreadType.Job:
                case ThreadType.JobAndEntity:
                    deformedMesh.vertices = Float3ToVector3(verticesForJob);
                deformedMesh.normals = Float3ToVector3(normalsForJob);
                deformedMesh.RecalculateBounds();
            break;
               
                    
                    
                    
                ////if(renderManually)
                ////deformedMesh.vertices = Float3ToVector3(verticesForJob);
                ////deformedMesh.normals = Float3ToVector3(normalsForJob);
                ////deformedMesh.RecalculateBounds();

                ////ComputeBuffer argsBuffer;
                ////ComputeBuffer matricesBuffer;

                ////argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);

                ////for (int i = 0; i < deformedMesh.subMeshCount; ++i)
                ////{
                ////    argsBuffer.SetData(new uint[]{
                ////    (uint)deformedMesh.GetIndexCount(i),
                ////    (uint)1,
                ////    (uint)deformedMesh.GetIndexStart(i),
                ////    (uint)deformedMesh.GetBaseVertex(i)
                ////});
                ////    var bounds = new Bounds(MeasureBoundsCenter(verticesForJob), Vector3.one * 10f);

                ////    Matrix4x4[] matrices = new Matrix4x4[1] { transform.localToWorldMatrix };
                ////    matricesBuffer = new ComputeBuffer(1, sizeof(float) * 4 * 4);
                ////    matricesBuffer.SetData(matrices);
                ////    sharedMaterials[i].SetBuffer("matricesBuffer", matricesBuffer);

                ////    //Graphics.DrawMeshIns
                ////}

                ////for (int i = 0; i < deformedMesh.subMeshCount; ++i)
                ////{
                ////    RenderParams rp = new RenderParams(sharedMaterials[i]);
                ////    Graphics.RenderMesh(rp, deformedMesh, i, transform.localToWorldMatrix);
                ////}
                //meshPositions.SetData(verticesForJob);
                //meshNormals.SetData(normalsForJob);

                //////Debug.Log("deformedMesh.subMeshCount : " + deformedMesh.subMeshCount+ " / sharedMaterials[i] : "+ sharedMaterials.Length);
                ////var indexBuffer = deformedMesh.GetIndexBuffer();
                //for (int i = 0; i < deformedMesh.subMeshCount && i < sharedManualMaterials.Length; ++i)
                //{
                //    RenderParams rp = new RenderParams(sharedManualMaterials[i]);
                //    //var submesh = deformedMesh.GetSubMesh(i);
                //    int indexCount = (int)deformedMesh.GetIndexCount(i);
                //    meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indexCount, sizeof(int));
                //    meshTriangles.SetData(deformedMesh.GetTriangles(i));


                //    rp.worldBounds = new Bounds(MeasureBoundsCenter(verticesForJob), Vector3.one * 100f); //deformedMesh.bounds;
                //    rp.matProps = new MaterialPropertyBlock();
                //    rp.matProps.SetBuffer("_Triangles", meshTriangles);
                //    rp.matProps.SetBuffer("_Positions", meshPositions);
                //    rp.matProps.SetBuffer("_Normals", meshNormals);
                //    rp.matProps.SetBuffer("_UVs", meshUVs);
                //    rp.matProps.SetBuffer("_Tangents", meshTangents);
                //    rp.matProps.SetBuffer("_Colors", meshColors);
                //    //rp.matProps.SetInt("_StartIndex", (int)deformedMesh.GetIndexStart(i));
                //    rp.matProps.SetInt("_BaseVertexIndex", (int)deformedMesh.GetBaseVertex(i));


                //    //Graphics.RenderPrimitivesIndirect(rp, MeshTopology.Triangles, )
                //    //Debug.Log("submesh.indexStart : " + (int)deformedMesh.GetIndexStart(i) + " / submesh.baseVertex: " + (int)deformedMesh.GetBaseVertex(i));
                //    rp.matProps.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);


                //    commandData[0].vertexCountPerInstance = deformedMesh.GetIndexStart(i);
                //    commandData[0].instanceCount = 1;
                //    commandData[1].vertexCountPerInstance = deformedMesh.GetIndexStart(i);
                //    commandData[1].instanceCount = 1;
                //    commandBuffer.SetData(commandData);
                //    Graphics.RenderPrimitivesIndirect(rp, MeshTopology.Triangles, commandBuffer, commandCount);
                //        //Graphics.RenderMesh(rp, deformedMesh, i, transform.localToWorldMatrix);//
                //        //Graphics.RenderPrimitivesIndexed(rp, MeshTopology.Triangles, meshTriangles, indexCount, (int)deformedMesh.GetIndexStart(i));//RenderPrimitives(rp, MeshTopology.Triangles, submesh.vertexCount);
                        
                        break; 
            }
            

        }


    }
}
