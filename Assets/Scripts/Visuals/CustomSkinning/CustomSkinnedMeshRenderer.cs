using UnityEngine;
using UnityEngine.Rendering;

public class CustomSkinnedMeshRenderer : MonoBehaviour
{
    public Mesh mesh;
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

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();

        if(TryGetComponent<SkinnedMeshRenderer>(out origSkinned))
        {
            mesh = origSkinned.sharedMesh;
            bones = origSkinned.bones;
            weights = mesh.boneWeights;
            blendshapeWeights = new float[origSkinned.sharedMesh.blendShapeCount];
            rootBone = origSkinned.rootBone;
            bindposes = mesh.bindposes;


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
            renderer.enabled = true;
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

    void LateUpdate()
    {

                if (mesh == null || bones == null || weights == null)
            return;

        var bonesPerVertex = mesh.GetBonesPerVertex();
        var boneWeights = mesh.GetAllBoneWeights();

        Vector3[] vertices = mesh.vertices;
        Vector3[] deltaVertices = new Vector3[vertices.Length];
        Vector3[] normals = mesh.normals;
        //Vector3[] deltaNormals = new Vector3[vertices.Length];

        for (int i = 0; i < blendshapeWeights.Length; i++)
        {
            int frameCount = mesh.GetBlendShapeFrameCount(i) - 1;
            mesh.GetBlendShapeFrameVertices(i, frameCount, deltaVertices, /*deltaNormals*/null, null);
            for(int j =0; j < vertices.Length; ++j)
            {
                if (syncShapeValueWithSkinned && origSkinned != null)
                {
                    blendshapeWeights[i] = origSkinned.GetBlendShapeWeight(i)*0.01f;
                }
                    
                float weight = (blendshapeWeights[i] + (i == blendshapeWeights.Length - 1 ? -1 : 0));
                vertices[j] += deltaVertices[j] * weight; 
                //normals[j] += deltaNormals[j] * weight;
            }
        }
        int boneWeightIndex = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 deformedVertex = Vector3.zero;
            Vector3 deformedNormal = Vector3.zero;

            var numberOfBonesForThisVertex = bonesPerVertex[i];

            for(int j = 0; j < numberOfBonesForThisVertex; j++)
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
                         transform.InverseTransformDirection( deformedNormal.normalized);
        }

        deformedMesh.vertices = vertices;
        deformedMesh.normals = normals;

        deformedMesh.RecalculateBounds();

    }
}
