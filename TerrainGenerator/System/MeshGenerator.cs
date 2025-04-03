using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Fujin.TerrainGenerator.Model;
using Fujin.TerrainGenerator.Utility;

namespace Fujin.TerrainGenerator.System
{
    public class MeshGenerator
    {
        private MeshGenerator()
        {
        }

        public static Mesh CreateMeshFromVertices(List<Vector2> vertices, List<List<Vector2>> holes)
        {
            if (vertices == null || vertices.Count < 3)
            {
                Debug.LogError("The number of vertices must be more than or equal to 3");
                return null;
            }

            if (holes == null)
            {
                return CreateMeshFromVertices(vertices);
            }
            
            Mesh baseMesh = CreateMeshFromVertices(vertices);
            SplitLine splitLine = new SplitLine(SplitLine.Param.Hole);
            
            Vector2 minMax = Vector2.zero;
            {
                List<Vector2> v = Calc.FilterByMostTwo(vertices);
                minMax.x = v[0].y;
                minMax.y = v[1].y;
            }

            foreach (List<Vector2> hole in holes)
            {
                SplitLine.SetResult r = splitLine.TrySetLine(hole, minMax);
                if (r != SplitLine.SetResult.Success)
                {
                    Debug.LogError($"Error occured at TrySetLine: {r}");
                    return null;
                }

                SplitMesh.SplitResult re = SplitMesh.TrySplit(baseMesh, splitLine, out SplitMesh splitMesh);
                if (re != SplitMesh.SplitResult.Success)
                {
                    Debug.LogError($"Error occured at TrySplit: {re}");
                    return null;
                }

                SplitMesh.MergeResult res = splitMesh.TryMerge(out baseMesh);
                if (res != SplitMesh.MergeResult.Success)
                {
                    Debug.LogError($"Error occured at TryMerge: {res}");
                    return null;
                }
            }
            
            return baseMesh;
        }
        
        public static Mesh CreateMeshFromVertices(List<Vector2> vertices)
        {
            if (vertices == null || vertices.Count < 3)
            {
                Debug.LogError("The number of vertices must be more than or equal to 3");
                return null;
            }
            
            // Copy the vertices while converting them to Vector3 from Vector2
            Vector3[] meshVertices = new Vector3[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                meshVertices[i] = new Vector3(vertices[i].x, vertices[i].y, 0);
            }
            
            // Get a list of sets of three indices of vertices
            if (!TryTriangulate(vertices, out List<int> triangles))
            {
                Debug.LogError("Failed to generate triangles");
                return null;
            }

            // Mesh with them
            Mesh mesh = new Mesh()
            {
                vertices = meshVertices,
                triangles = triangles.ToArray(),
                uv = new Vector2[vertices.Count] //TODO: 面倒なのでVector2で省略
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }

        public static Mesh CreateMeshFromVertices(Vector3[] vertices)
        {
            if (vertices == null || vertices.Length < 3)
            {
                Debug.LogError("The number of vertices must be more than or equal to 3");
                return null;
            }
            
            List<Vector2> vertices2D = new List<Vector2>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices2D.Add(new Vector2(vertices[i].x, vertices[i].y));
            }
            
            if (!TryTriangulate(vertices2D, out List<int> triangles))
            {
                Debug.LogError("Failed to generate triangles");
                return null;
            }
            
            Mesh mesh = new Mesh()
            {
                vertices = vertices,
                triangles = triangles.ToArray(),
                uv = new Vector2[vertices.Length] //TODO: 面倒なのでVector2で省略
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }

        private static bool TryTriangulate(List<Vector2> vertices, out List<int> indices)
        {
            indices = new List<int>();

            if (vertices.Count < 3) return false;
            List<int> remaining = Enumerable.Range(0, vertices.Count).ToList();

            while (remaining.Count > 3)
            {
                bool found = false;

                for (int i = 0; i < remaining.Count; ++i)
                {
                    int prev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                    int curr = remaining[i];
                    int next = remaining[(i + 1) % remaining.Count];

                    if (IsEar(vertices, prev, curr, next, remaining))
                    {
                        indices.Add(prev);
                        indices.Add(curr);
                        indices.Add(next);
                        remaining.RemoveAt(i);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    string s = "";
                    foreach (int i in remaining)
                    {
                        s += $"{i}:{vertices[i]} ";
                    }
                    Debug.Log("FALSE Remaining indices: " + s);
                    return false;
                }
            }
            
            indices.Add(remaining[0]);
            indices.Add(remaining[1]);
            indices.Add(remaining[2]);

            return true; 
        }
        

        private static bool IsEar(List<Vector2> vertices, int prev, int curr, int next, List<int> remainingIndices)
        {
            Vector2 a = vertices[prev];
            Vector2 b = vertices[curr];
            Vector2 c = vertices[next];

            // False if the vector is counter-clockwise
            if ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) > 0)
            {
                // Debug.LogWarning($"Invalid ear: prev={prev}, curr={curr}, next={next} (counter clockwise)");
                return false;
            }
            
            // False if a triangle contains another index
            foreach (int index in remainingIndices)
            {
                if (index == prev || index == curr || index == next) continue;

                if (IsPointInTriangle(a, b, c, vertices[index]))
                {
                    // Debug.LogWarning($"Invalid ear: prev={prev}, curr={curr}, next={next} (containing a point {index})");
                    return false;
                }
            }

            return true;
        }

        private static float TriangleArea(Vector2 a, Vector2 b, Vector2 c)
            => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        
        private static bool IsPointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            float area = Mathf.Abs(TriangleArea(a, b, c));
            float area1 = Mathf.Abs(TriangleArea(p, c, c));
            float area2 = Mathf.Abs(TriangleArea(a, p, c));
            float area3 = Mathf.Abs(TriangleArea(a, b, p));

            return Mathf.Approximately(area, (area1 + area2 + area3));
        }
        
        public static Mesh Create3DMeshFrom2DMesh(Mesh mesh2D, float depth)
        {
            if (mesh2D == null || mesh2D.vertexCount == 0)
            {
                Debug.LogError("Error: Passed mesh is invalid!");
                return null;
            }

            Vector3[] vertices2D = mesh2D.vertices;
            int[] triangles2D = mesh2D.triangles;

            int vertexCount = vertices2D.Length;
            Vector3[] vertices3D = new Vector3[vertexCount * 2];
            int[] triangles3D = new int[triangles2D.Length * 2 + vertexCount * 6]; // (front + back) + (a pair of triangles for side)
            
            for (int i = 0; i < vertexCount; i++)
            {
                vertices3D[i] = vertices2D[i]; // Front
                vertices3D[i + vertexCount] = vertices2D[i] + new Vector3(0, 0, -depth); // Back
            }

            for (int i = 0; i < triangles2D.Length; i += 3)
            {
                // Front
                triangles3D[i] = triangles2D[i];
                triangles3D[i + 1] = triangles2D[i + 1];
                triangles3D[i + 2] = triangles2D[i + 2];

                // Back (reversed)
                triangles3D[triangles2D.Length + i] = triangles2D[i] + vertexCount;
                triangles3D[triangles2D.Length + i + 1] = triangles2D[i + 2] + vertexCount;
                triangles3D[triangles2D.Length + i + 2] = triangles2D[i + 1] + vertexCount;
            }

            // Side
            int sideIndex = triangles2D.Length * 2;
            for (int i = 0; i < vertexCount; i++)
            {
                int next = (i + 1) % vertexCount;

                // Side 1
                triangles3D[sideIndex++] = i;
                triangles3D[sideIndex++] = i + vertexCount;
                triangles3D[sideIndex++] = next;

                // Side 2
                triangles3D[sideIndex++] = next;
                triangles3D[sideIndex++] = i + vertexCount;
                triangles3D[sideIndex++] = next + vertexCount;
            }

            Mesh mesh3D = new Mesh
            {
                vertices = vertices3D,
                triangles = triangles3D
            };
            mesh3D.RecalculateNormals();
            mesh3D.RecalculateBounds();
            mesh3D.Optimize();

            return mesh3D;
        }
    }
}
