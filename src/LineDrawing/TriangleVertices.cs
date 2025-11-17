// TriangleVertices.cs
using UnityEngine;

namespace CinematicUnityExplorer.Inspectors
{
    public struct TriangleVertices
    {
        public Vector3 VertexA;
        public Vector3 VertexB;
        public Vector3 VertexC;

        public TriangleVertices(Vector3 a, Vector3 b, Vector3 c)
        {
            VertexA = a;
            VertexB = b;
            VertexC = c;
        }

        // 添加 Deconstruct 方法，使得可以進行解構賦值
        public void Deconstruct(out Vector3 a, out Vector3 b, out Vector3 c)
        {
            a = VertexA;
            b = VertexB;
            c = VertexC;
        }
    }
}