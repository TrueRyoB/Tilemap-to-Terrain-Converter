using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Fujin.TerrainGenerator
{
    public class MeshGenerator : MonoBehaviour
    {
        public static Mesh CreateMeshFromVertices(List<Vector2> vertices)
        {
            if (vertices == null || vertices.Count < 3)
            {
                Debug.LogError("The number of vertices must be more than or equal to 3");
                return null;
            }

            Mesh mesh = new Mesh();
            
            // Copy the vertices while converting them to Vector3 from Vector2
            Vector3[] meshVertices = new Vector3[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                meshVertices[i] = new Vector3(vertices[i].x, vertices[i].y, 0);
            }

            
            List<int> triangles = Triangulate(vertices);

            if (triangles == null || triangles.Count < 3)
            {
                Debug.LogError("Failed to generate triangles");
                return null;
            }

            mesh.vertices = meshVertices;
            mesh.triangles = triangles.ToArray();

            // 法線とUV計算
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.uv = vertices.Select(v => Vector2.zero).ToArray(); //TODO: 面倒なので省略
            
            return mesh;
        }

        // 三角形分割アルゴリズム
        private static List<int> Triangulate(List<Vector2> vertices)
        {
            List<int> indices = new List<int>();
            List<int> remainingIndices = Enumerable.Range(0, vertices.Count).ToList();

            while (remainingIndices.Count > 3)
            {
                bool earFound = false;

                for (int i = 0; i < remainingIndices.Count; i++)
                {
                    int prev = remainingIndices[(i - 1 + remainingIndices.Count) % remainingIndices.Count];
                    int curr = remainingIndices[i];
                    int next = remainingIndices[(i + 1) % remainingIndices.Count];

                    if (IsEar(vertices, prev, curr, next, remainingIndices))
                    {
                        indices.Add(prev);
                        indices.Add(curr);
                        indices.Add(next);
                        remainingIndices.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }

                if (!earFound)
                {
                    Debug.LogError("耳切り法で三角形分割できませんでした！");
                    return null;
                }
            }

            // 残り3頂点で最終三角形
            indices.Add(remainingIndices[0]);
            indices.Add(remainingIndices[1]);
            indices.Add(remainingIndices[2]);

            return indices;
        }


        // ポリゴンの面積計算
        private static float Area(List<Vector2> vertices)
        {
            int n = vertices.Count;
            float area = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 v0 = vertices[p];
                Vector2 v1 = vertices[q];
                area += v0.x * v1.y - v1.x * v0.y;
            }

            return (area * 0.5f);
        }

        private static bool IsEar(List<Vector2> vertices, int prev, int curr, int next, List<int> remainingIndices)
        {
            Vector2 a = vertices[prev];
            Vector2 b = vertices[curr];
            Vector2 c = vertices[next];

            // 凹角であれば耳ではない
            if (Vector3.Cross(b - a, c - a).z <= 0)
            {
                return false;
            }

            // 三角形内に他の頂点があるかチェック
            for (int i = 0; i < remainingIndices.Count; i++)
            {
                int index = remainingIndices[i];
                if (index == prev || index == curr || index == next)
                {
                    continue;
                }

                if (IsPointInTriangle(vertices[index], a, b, c))
                {
                    return false;
                }
            }

            return true;
        }


        private static float TriangleArea(Vector2 a, Vector2 b, Vector2 c)
            => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        
        private static bool IsPointInTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float area = Mathf.Abs(TriangleArea(A, B, C));
            float area1 = Mathf.Abs(TriangleArea(P, B, C));
            float area2 = Mathf.Abs(TriangleArea(A, P, C));
            float area3 = Mathf.Abs(TriangleArea(A, B, P));

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
