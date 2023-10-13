using System.Collections.Generic;
using System.Drawing.Drawing2D;
using UnityEngine;

namespace UnityTemplateProjects.Visuals
{
    [System.Serializable]
    public struct MatrixSet
    {
        public int id;
        public Matrix4x4 matrix;

        public MatrixSet(int _id, Matrix4x4 _mat)
        {
            id = _id;
            matrix = _mat;
        }
    }
    
    
    [System.Serializable]
    public struct Edge
    {
        //public Matrix4x4 _trs;
        public int parentMatId;
        
        public Vector3[] _vertices;

        public Edge(Vector3 vertex1, Vector3 vertex2) : this()
        {
            SetVertices(vertex1,vertex2);
        }

        public Edge(Vector3 vertex1, Vector3 vertex2, ArchivedEdges edges, int parentObjId, Matrix4x4 mat) : this()
        {
            SetVertices(vertex1,vertex2);
            //SetTRS(trs);
            // if (edges.matSets == null)
            // {
            //     edges.matSets = new Dictionary<int, Matrix4x4>();
            // }
            // edges.matSets.TryAdd(parentObjId, mat);

            bool matrixExists = false;
            for (int i = 0; i < edges.matrixSets.Count; i++)
            {
                if (edges.matrixSets[i].id == parentObjId)
                    matrixExists = true;
                break;
            }

            if (!matrixExists)
            {
                edges.matrixSets.Add(new MatrixSet(parentObjId, mat));
            }
            
            parentMatId = parentObjId;
        }
        
        // public Matrix4x4 GetTRS()
        // {
        //     return _trs;
        // }
        //
        // public void SetTRS(Matrix4x4 value)
        // {
        //     _trs = value;
        // }

        public void SetVertices(Vector3 one, Vector3 two)
        {
            _vertices = new Vector3[2];
            _vertices[0] = one;
            _vertices[1] = two;
        }

        public Vector3[] GetVertices()
        {
            return _vertices;
        }

        public Vector3 GetFirstVertex()
        {
            return _vertices[0];
        }

        public Vector3 GetSecondVertex()
        {
            return _vertices[1];
        }

        public Vector3 GetFirstVertexOnScene(Matrix4x4 parentMat)
        {
            return MatrixConversion.PositionFromMatrix(parentMat) + (Vector3)(parentMat * _vertices[0]);
        } 
        public Vector3 GetSecondVertexOnScene(Matrix4x4 parentMat)
        {
            return MatrixConversion.PositionFromMatrix(parentMat) + (Vector3)(parentMat * _vertices[1]);
        }
    }
    
    [CreateAssetMenu(fileName = "FILENAME", menuName = "Archived Edge", order = 0)]
    public class ArchivedEdges : ScriptableObject
    {
        private Dictionary<int, Matrix4x4> matSets;
        
        public Matrix4x4 GetMatrixOnMatSets(int id)
        {
            if (matSets == null)
            {
                matSets = new Dictionary<int, Matrix4x4>();
            }

            if (!matSets.ContainsKey(id))
            {
                for (int i = 0; i < matrixSets.Count; i++)
                {
                    if (matrixSets[i].id == id)
                    {
                        matSets.Add(matrixSets[i].id, matrixSets[i].matrix);
                        break;
                    }
                }
            }

            return matSets[id];
        }

        public List<MatrixSet> matrixSets;
        public List<Edge> edges;

        public void ClearAll()
        {
            matSets.Clear();
            matrixSets?.Clear();
            edges?.Clear();
        }
    }
}