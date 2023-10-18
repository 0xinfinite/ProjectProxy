using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Jobs;

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

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(SkinnedMeshRenderer))]
//[CanEditMultipleObjects]
public class CustomSkinnedMeshRenderer : MonoBehaviour
{
    [SerializeField] bool multiThread = true;

    public Mesh mesh;
    public Material[] sharedMaterials;
    public Transform rootBone;
    public Transform[] bones;
    public BoneWeight[] weights;
    
    [Range(0,1)]
    public float[] blendshapeWeights;
    public bool syncShapeValueWithSkinned = true;

    private MeshFilter meshFilter;
    private Mesh deformedMesh;
    private Matrix4x4[] bindposes;
    private Vector3[] tangents3;

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

    void Awake()
    {
        if (this.enabled)
        {
            meshFilter = GetComponent<MeshFilter>();

            if (TryGetComponent<SkinnedMeshRenderer>(out origSkinned))
            {
                mesh = origSkinned.sharedMesh;
                bones = origSkinned.bones;
                weights = mesh.boneWeights;
                blendshapeWeights = new float[origSkinned.sharedMesh.blendShapeCount];
                rootBone = origSkinned.rootBone;
                bindposes = mesh.bindposes;

                sharedMaterials = origSkinned.sharedMaterials;

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
                renderer.sharedMaterials = origSkinned.sharedMaterials;
                renderer.enabled = true;    // !multiThread ? true:false;
            }

            bonesPerVertex = mesh.GetBonesPerVertex();
            boneWeights = mesh.GetAllBoneWeights();

            if (multiThread)
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


    private void OnDestroy()
    {
        if (multiThread)
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

    void LateUpdate()
    {

                if (mesh == null || bones == null || weights == null)
            return;

        
        if (!multiThread)
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


            deformedMesh.vertices = Float3ToVector3(verticesForJob);
            deformedMesh.normals = Float3ToVector3(normalsForJob);

                deformedMesh.RecalculateBounds();

        }


    }
}
