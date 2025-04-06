using UnityEngine;
using System;
using System.Linq;
using Fujin.TerrainGenerator.Utility;
using Fujin.TerrainGenerator.System;

namespace Fujin.TerrainGenerator.Model
{
    public class SplitMesh
    {
        public enum SplitResult
        {
            Success,
            InvalidPoint,
            InvalidMesh,
            InvalidSplitMesh,
            Failure,
        }

        public enum MergeResult
        {
            UnassignedMesh,
            Success,
            Failure,
        }
        
        public Mesh MeshLeft { get; private set; }
        public Mesh MeshRight { get; private set; }
        

        private void AssignMesh(Mesh meshLeft, Mesh meshRight)
        {
            MeshLeft = meshLeft;
            MeshRight = meshRight;
        }

        private void Reset()
        {
            MeshLeft = null;
            MeshRight = null;
        }

        public MergeResult TryMerge(out Mesh mergedMesh)
        {
            mergedMesh = null;

            if (MeshLeft == null || MeshRight == null)
            {
                return MergeResult.UnassignedMesh;
            }

            int l = MeshLeft.triangles.Length, r = MeshRight.triangles.Length, offset = MeshLeft.vertices.Length;
            
            int[] triangleMerged = new int[l + r];
            
            Array.Copy(MeshLeft.triangles, triangleMerged, l);
            for (int i = 0; i < r; ++i)
            {
                triangleMerged[i + l] = offset + MeshRight.triangles[i];
            }

            try
            {
                mergedMesh = new Mesh()
                {
                    vertices = MeshLeft.vertices.Concat(MeshRight.vertices).ToArray(),
                    triangles = triangleMerged,
                    uv = MeshLeft.uv.Concat(MeshRight.uv).ToArray()
                };
            
                mergedMesh.RecalculateNormals();
                mergedMesh.RecalculateBounds();

                return MergeResult.Success;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return MergeResult.Failure;
            }
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
            
            Vector3[] verticesOriginal = mesh.vertices;
            int n = verticesOriginal.Length;
            bool isValidSplitBottom = false;
            bool isValidSplitTop = false;
            int floorBottom = 0, floorTop = 0;
            
            for (int i = 0; i < mesh.vertices.Length; ++i)
            {
                Vector2 pointA = mesh.vertices[i];
                Vector2 pointB = mesh.vertices[(i + 1) % n];

                if (Calc.IsPointOnLine(pointA, pointB, splitLine.LeftLine[^1], out float r))
                {
                    floorBottom = Calc.FloorF((i + r) % n);
                    isValidSplitBottom = true;
                }

                if (Calc.IsPointOnLine(pointA, pointB, splitLine.LeftLine[0], out r))
                {
                    floorTop = Calc.FloorF((i + r) % n);
                    isValidSplitTop = true;
                }

                if (isValidSplitBottom && isValidSplitTop) break;
            }

            int ceilBottom = (floorBottom + 1) % n;
            int ceilTop = (floorTop + 1) % n;
            
            if (!isValidSplitBottom || !isValidSplitTop)
            {
                foreach (var v in mesh.vertices) Debug.LogWarning(v);
                return SplitResult.InvalidPoint;
            }

            try
            {
                // Swap elements within SplitLine to avoid confusion
                splitMesh = new SplitMesh();
            
                // Try splitting the mesh into two
                int l = splitLine.LeftLine.Length, r = splitLine.RightLine.Length;
                int lengthLeft = floorTop - ceilBottom + 1 + l;
                int lengthRight = floorBottom - ceilTop + 1 + r;
                
                if (ceilBottom > floorTop) lengthLeft += n;
                if (ceilTop > floorBottom) lengthRight += n;
                
                Vector3[] verticesLeft = new Vector3[lengthLeft];
                Array.Copy(splitLine.LeftLine, 0, verticesLeft, 0, l);
                if (ceilBottom > floorTop)
                {
                    for (int i = ceilBottom; i <= floorTop + n; ++i) verticesLeft[i + l - ceilBottom] = verticesOriginal[i % n];
                }
                else
                {
                    for (int i = ceilBottom; i <= floorTop; ++i) verticesLeft[i + l - ceilBottom] = verticesOriginal[i % n];
                }
                
                Vector3[] verticesRight = new Vector3[lengthRight];
                Array.Copy(splitLine.RightLine, 0, verticesRight, 0, r);
                if (ceilTop > floorBottom)
                {
                    for (int i = ceilTop; i <= floorBottom + n; ++i) verticesRight[i + r - ceilTop] = verticesOriginal[i % n];
                }
                else
                {
                    for (int i = ceilTop; i <= floorBottom; ++i) verticesRight[i + r - ceilTop] = verticesOriginal[i];
                }

                if (!Calc.IsClockwise(verticesLeft) || !Calc.IsClockwise(verticesRight))
                {
                    return SplitResult.InvalidPoint;
                }
                
                Mesh leftMesh = MeshGenerator.CreateMeshFromVertices(verticesLeft);
                Mesh rightMesh = MeshGenerator.CreateMeshFromVertices(verticesRight);

                if (leftMesh == null || rightMesh == null) return SplitResult.InvalidSplitMesh;

                splitMesh.AssignMesh(leftMesh, rightMesh);
                Debug.Log("Final Phase passed");
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