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
        public Mesh MeshLeft { get; private set; }
        public Mesh MeshRight { get; private set; }

        public SplitMesh() { }

        private void AssignMesh(Mesh meshLeft, Mesh meshRight)
        {
            MeshLeft = meshLeft;
            MeshRight = meshRight;
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

        public static SplitResult TrySplit(Mesh mesh, SplitLine splitLine, out SplitMesh splitMesh)
        {
            splitMesh = null;
            
            // Return false if mesh is invalid
            if (mesh.vertices == null || mesh.vertices.Length < 3 || mesh.triangles == null || mesh.triangles.Length % 3 != 0)
            {
                return SplitResult.InvalidMesh;
            }
            
            // Return false if split points are invalid
            if (!splitLine.IsValid())
            {
                return SplitResult.InvalidPoint;
            }
            
            int n = mesh.vertices.Length;
            bool isValidSplitBottom = false;
            bool isValidSplitTop = false;

            Vector2 indices = Vector2.zero;
            for (int i = 0; i < mesh.vertices.Length; ++i)
            {
                Vector2 pointA = mesh.vertices[i];
                Vector2 pointB = mesh.vertices[(i + 1) % n];

                if (Calc.IsPointOnLine(pointA, pointB, splitLine.LeftLine[0], out float r))
                {
                    indices.x = (i + r) % n;
                    isValidSplitBottom = true;
                }

                if (Calc.IsPointOnLine(pointA, pointB, splitLine.LeftLine[^1], out r))
                {
                    indices.y = (i + r) % n;
                    isValidSplitTop = true;
                }

                if (isValidSplitBottom && isValidSplitTop) break;
            }
            
            if (!isValidSplitBottom || !isValidSplitTop)
            {
                return SplitResult.InvalidPoint;
            }

            try
            {
                // Swap elements within SplitLine to avoid confusion
                Vector3[] verticesOriginal = mesh.vertices;
                splitMesh = new SplitMesh();
            
                // Try splitting the mesh into two
                int floorBottom = Calc.FloorF(indices.x);
                int floorTop = Calc.FloorF(indices.y);
                
                int lengthLeft = floorTop - floorBottom + 1 + splitLine.LeftLine.Count;
                int lengthRight = floorBottom - floorTop + 1 + splitLine.RightLine.Count;
                int l = splitLine.LeftLine.Count, r = splitLine.RightLine.Count;
                
                if (floorBottom > floorTop) lengthLeft += n;
                else lengthRight += n;
                
                Vector3[] verticesLeft = new Vector3[lengthLeft];
                for (int i=0; i < l; ++i) verticesLeft[i] = splitLine.LeftLine[i];
                for (int i = floorBottom; i < floorTop + n; ++i) verticesLeft[i + l - floorBottom] = verticesOriginal[i % n];
                
                Vector3[] verticesRight = new Vector3[lengthRight];
                for (int i=0; i < r; ++i) verticesRight[i] = splitLine.RightLine[i];
                for (int i = floorTop; i < floorBottom; ++i) verticesRight[i + r - floorTop] = verticesOriginal[i%n];

                splitMesh.AssignMesh(MeshGenerator.CreateMeshFromVertices(verticesLeft), MeshGenerator.CreateMeshFromVertices(verticesRight));
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
