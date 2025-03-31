using System;
using UnityEngine;

namespace Fujin.TerrainGenerator.Object
{
    public class SplitMesh
    {
        public enum SplitResult
        {
            Success,
            InvalidPoint,
            InvalidMesh,
            Failure,
        }
        public struct SplitLine
        {
            public Vector2 PointA;
            public Vector2 PointB;
            public Vector2 FrontIndices;
        }
        
        private SplitMesh _pair;
        private Vector2 _connectPoint;
        public Mesh Mesh { get; private set; }
        private Vector3[] _vertices;

        public SplitMesh(Vector3[] vertices)
        {
            _vertices = vertices;
            Mesh = new Mesh();
        }

        public static SplitResult TrySplit(Mesh mesh, SplitLine splitLine, out SplitMesh splitMeshA,
            out SplitMesh splitMeshB)
        {
            splitMeshA = null;
            splitMeshB = null;
            
            // Return false if mesh is invalid
            if (mesh.vertices == null || mesh.vertices.Length < 3 || mesh.triangles == null || mesh.triangles.Length % 3 != 0)
            {
                return SplitResult.InvalidMesh;
            }
            
            // Return false if split points are invalid
            if (Vector2.Distance(splitLine.PointA, splitLine.PointB) < 0.001f)
            {
                return SplitResult.InvalidPoint;
            }
            bool isValidSplitPointA = false;
            bool isValidSplitPointB = false;
            for (int i = 0; i < mesh.vertices.Length; ++i)
            {
                Vector2 pointA = mesh.vertices[i];
                Vector2 pointB = mesh.vertices[(i + 1) % mesh.vertices.Length];

                if (IsPointOnLine(pointA, pointB, splitLine.PointA, out float r))
                {
                    splitLine.FrontIndices[0] = i+r;
                    isValidSplitPointA = true;
                }

                if (IsPointOnLine(pointA, pointB, splitLine.PointB, out r))
                {
                    splitLine.FrontIndices[1] = i+r;
                    isValidSplitPointB = true;
                }

                if (isValidSplitPointA && isValidSplitPointB) break;
            }
            if (!isValidSplitPointA || !isValidSplitPointB)
            {
                return SplitResult.InvalidPoint;
            }
            
            // Swap elements within SplitLine to avoid confusion
            if (splitLine.FrontIndices[0] > splitLine.FrontIndices[1])
            {
                (splitLine.PointA, splitLine.PointB) = (splitLine.PointB, splitLine.PointA);
                (splitLine.FrontIndices[0], splitLine.FrontIndices[1]) = (splitLine.FrontIndices[1], splitLine.FrontIndices[0]);
            }
            
            // Try splitting the mesh into two
            Vector3[] verticesOriginal = mesh.vertices;
            int verticesCountA = GetNumberVertices(FloorF(splitLine.FrontIndices.x), verticesOriginal, ref splitLine);
            
            Vector3[] verticesA = new Vector3[verticesCountA];
            Array.Copy(verticesOriginal, verticesA, verticesCountA);
            
            

            return SplitResult.Success; //TODO: remove this
        }

        private static int GetNumberVertices(int n, Vector3[] verticesOriginal, ref SplitLine splitLine)
        {
            return n + (Same(splitLine.FrontIndices.x, FloorF(splitLine.FrontIndices.x)) ? 1 : 0)
                     + (Same(splitLine.FrontIndices.y, FloorF(splitLine.FrontIndices.y)+1) ? 0 : 1);
        }
        
        private static int FloorF(float f) => (int)Mathf.Floor(f);
        private static bool Same(float f, int i) => Mathf.Abs(f - i) < 0.001f;


        
        private static bool IsPointOnLine(Vector2 a, Vector2 b, Vector2 p, out float ratio)
        {
            float length = Vector2.Distance(a, b);
            float lengthA = Vector2.Distance(a, p);
            float lengthB = Vector2.Distance(p, b);
            ratio = lengthA / length;

            return Mathf.Abs(lengthA + lengthB - length) < 0.1f;
        }
    }
}