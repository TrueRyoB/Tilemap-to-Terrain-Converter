using UnityEngine;
using System;
using System.Linq;
using Fujin.TerrainGenerator.Utility;

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
        
        private SplitMesh _pair;
        private Vector2 _connectPoint;
        private bool _isFirstIndexConnect;
        public readonly Mesh Mesh;

        public SplitMesh(Mesh mesh)
        {
            Mesh = mesh;
        }

        private void Reset()
        {
            _pair = null;
            _connectPoint = Vector2.zero;
        }

        private void SetPair(SplitMesh pair, Vector2 connectPoint, bool isFirstIndexConnect)
        {
            _pair = pair;
            _connectPoint = connectPoint;
            _isFirstIndexConnect = isFirstIndexConnect;
        }

        public bool TryMerge(SplitMesh a, SplitMesh b, out Mesh mergedMesh)
        {
            mergedMesh = null;
            
            if (a._pair != b || b._pair != a)
            {
                return false;
            }

            Mesh firstConnectMesh = a._isFirstIndexConnect ? a.Mesh : b.Mesh;
            Mesh nextConnectMesh = a._isFirstIndexConnect ? b.Mesh : a.Mesh;
            
            Vector3[] vertices = new Vector3[firstConnectMesh.vertices.Length + nextConnectMesh.vertices.Length - 2];

            for (int i = 0; i < firstConnectMesh.vertices.Length; i++)
            {
                vertices[i] = firstConnectMesh.vertices[i];
            }
            for (int i = 0; i < nextConnectMesh.vertices.Length - 2; i++)
            {
                vertices[i + firstConnectMesh.vertices.Length] = nextConnectMesh.vertices[i+1];
            }

            mergedMesh = new Mesh()
            {
                vertices = vertices,
                triangles = firstConnectMesh.triangles.Concat(nextConnectMesh.triangles).ToArray(),
                uv = new Vector2[vertices.Length]
            };
            
            mergedMesh.RecalculateNormals();
            mergedMesh.RecalculateBounds();

            a.Reset();
            b.Reset();
            return true;
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

                if (Calc.IsPointOnLine(pointA, pointB, splitLine.PointA, out float r))
                {
                    splitLine.Indices.x = (i + r) % n;
                    isValidSplitPointA = true;
                }

                if (Calc.IsPointOnLine(pointA, pointB, splitLine.PointB, out r))
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
                int borderS = Calc.FloorF(splitLine.Indices.x);
                int borderE = Calc.FloorF(splitLine.Indices.y);
            
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
                
                splitMeshA.SetPair(splitMeshB, splitLine.PointA, false);
                splitMeshB.SetPair(splitMeshA, splitLine.PointA, true);
                
                return SplitResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Split failed: {ex.Message}");
                return SplitResult.Failure;
            }
        }
    }
}