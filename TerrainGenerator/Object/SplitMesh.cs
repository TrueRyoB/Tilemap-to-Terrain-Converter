using UnityEngine;
using System;

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
            public Vector2 Indices;
        }
        
        private SplitMesh _pair;
        private Vector2 _connectPoint;
        public readonly Mesh Mesh;

        public SplitMesh(Mesh mesh)
        {
            Mesh = mesh;
        }

        public void SetPair(SplitMesh pair, Vector2 connectPoint)
        {
            pair = _pair;
            _connectPoint = connectPoint;
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
            int n = mesh.vertices.Length;
            bool isValidSplitPointA = false;
            bool isValidSplitPointB = false;
            for (int i = 0; i < mesh.vertices.Length; ++i)
            {
                Vector2 pointA = mesh.vertices[i];
                Vector2 pointB = mesh.vertices[(i + 1) % n];

                if (IsPointOnLine(pointA, pointB, splitLine.PointA, out float r))
                {
                    splitLine.Indices.x = (i + r) % n;
                    isValidSplitPointA = true;
                }

                if (IsPointOnLine(pointA, pointB, splitLine.PointB, out r))
                {
                    splitLine.Indices.y = (i + r) % n;
                    isValidSplitPointB = true;
                }

                if (isValidSplitPointA && isValidSplitPointB) break;
            }
            
            if (!isValidSplitPointA || !isValidSplitPointB)
            {
                return SplitResult.InvalidPoint;
            }

            try
            {
                // Swap elements within SplitLine to avoid confusion
                Vector3[] verticesOriginal = mesh.vertices;
                if (splitLine.Indices.x > splitLine.Indices.y)
                {
                    (splitLine.PointA, splitLine.PointB) = (splitLine.PointB, splitLine.PointA);
                    (splitLine.Indices.x, splitLine.Indices.y) = (splitLine.Indices.y, splitLine.Indices.x);
                }
            
                // Try splitting the mesh into two
                int borderS = FloorF(splitLine.Indices.x);
                int borderE = FloorF(splitLine.Indices.y);
            
                int lengthA = borderS + (n - (borderE + 1)) + 2;
                Vector3[] verticesA = new Vector3[lengthA];
                Array.Copy(verticesOriginal, 0, verticesA, 0, borderS);
                verticesA[borderS] = splitLine.PointA;
                verticesA[borderS + 1] = splitLine.PointB;
                Array.Copy(verticesOriginal, borderE + 1, verticesA, borderS + 2, n - (borderE + 1));
                splitMeshA = new SplitMesh(MeshGenerator.CreateMeshFromVertices(verticesA));
                
                int lengthB = borderE - (borderS + 1) + 2;
                Vector3[] verticesB = new Vector3[lengthB];
                verticesB[0] = splitLine.PointA;
                Array.Copy(verticesOriginal, borderS + 1, verticesB, 1, lengthB - 2);
                verticesB[^1] = splitLine.PointB;
                splitMeshB = new SplitMesh(MeshGenerator.CreateMeshFromVertices(verticesB));
                
                splitMeshA.SetPair(splitMeshB, splitLine.PointA);
                splitMeshB.SetPair(splitMeshA, splitLine.PointA);
                
                return SplitResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Split failed: {ex.Message}");
                return SplitResult.Failure;
            }
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