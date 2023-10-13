using System.Collections.Generic;
using UnityEngine;

namespace UnityTemplateProjects.Visuals
{
    public class EdgeArchiveManager : MonoBehaviour
    {
        [SerializeField] private ArchivedEdges _archivedEdges;
        public ArchivedEdges ArchivedEdges
        {
            get { return _archivedEdges; }
            set { _archivedEdges = value; }
        }
        
        public void GetAllEdgesOnScene()
        {
            _archivedEdges.ClearAll();

            hashset_edge = new HashSet<string>();
            
            MeshFilter[] meshFilters = GameObject.FindObjectsOfType<MeshFilter>();

            foreach (var meshFilter in meshFilters)
            {
                Mesh sharedMesh = meshFilter.sharedMesh;

                int count = sharedMesh.subMeshCount;
                for (int i = 0; i < count; i++)
                {
                    var subMesh = sharedMesh.GetSubMesh(i);
                    for (int j = 0; j < subMesh.indexCount-1; j++)
                    {
                        GetEdge(meshFilter, sharedMesh.triangles[subMesh.indexStart+j], sharedMesh.triangles[subMesh.indexStart+j+1]);
                    }
                }
                
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_archivedEdges == null) return;

            //transform.position = _archivedEdges.edges[0].GetFirstVertexOnScene();
            
            foreach (var edge in _archivedEdges.edges)
            {
                Matrix4x4 mat = _archivedEdges.GetMatrixOnMatSets(edge.parentMatId);
                Gizmos.DrawLine(edge.GetFirstVertexOnScene(mat), edge.GetSecondVertexOnScene(mat));
            }
        }

        private HashSet<string> hashset_edge;
        
        private void GetEdge(MeshFilter meshFilter, int index1, int index2)
        {
            Transform parent = meshFilter.transform;
            Matrix4x4 mat = parent.localToWorldMatrix;
            
            if (!CheckOverlappedEdge(meshFilter.sharedMesh.vertices[index1], meshFilter.sharedMesh.vertices[index2], mat))
            {
                Edge newEdge = new Edge(meshFilter.sharedMesh.vertices[index1], meshFilter.sharedMesh.vertices[index2],_archivedEdges,
                    meshFilter.GetInstanceID(),mat);
                _archivedEdges.edges.Add(newEdge);
                hashset_edge.Add(meshFilter.sharedMesh.vertices[index1].ToString()+"/"+meshFilter.sharedMesh.vertices[index2].ToString());
                hashset_edge.Add(meshFilter.sharedMesh.vertices[index2].ToString()+"/"+meshFilter.sharedMesh.vertices[index1].ToString());
            }

            //transform.position = MatrixConversion.PositionFromMatrix(mat);
            //transform.rotation = Quaternion.LookRotation( mat * Vector3.forward, mat*Vector3.up);
        }

        
        private bool CheckOverlappedEdge(Vector3 vertex1, Vector3 vertex2, Matrix4x4 matrix)
        {
            // foreach (var edge in _archivedEdges.edges)
            // {
            //     Vector3 firstEdgeVertex = edge.GetFirstVertex();
            //     Vector3 secondEdgeVertex = edge.GetSecondVertex();
            //
            //     if (((VectorApproximately(firstEdgeVertex, vertex1) && VectorApproximately(secondEdgeVertex, vertex2)) ||
            //         (VectorApproximately(firstEdgeVertex, vertex2) && VectorApproximately(secondEdgeVertex, vertex1))))
            //     {
            //         if (matrix == edge.GetTRS())
            //         {
            //             return true;
            //         }
            //     }
            // }
            //
            // return false;
            return hashset_edge.Contains(vertex1.ToString()+"/" +vertex2.ToString()) ||
                   hashset_edge.Contains(vertex2.ToString()+"/" +vertex1.ToString());
        }

        private bool VectorApproximately(Vector3 one, Vector3 compare)
        {
            return Mathf.Approximately(one.x, compare.x) &&  Mathf.Approximately(one.y, compare.y) && Mathf
                .Approximately(one.z, compare.z);
        }
    }
}