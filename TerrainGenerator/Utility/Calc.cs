using System.Collections.Generic;
using UnityEngine;
using System;

namespace Fujin.TerrainGenerator.Utility
{
    public class Calc
    {
        public static bool IsPointOnLine(Vector2 a, Vector2 b, Vector2 p, out float ratio)
        {
            float length = Vector2.Distance(a, b);
            float lengthA = Vector2.Distance(a, p);
            float lengthB = Vector2.Distance(p, b);
            ratio = lengthA / length;

            return Mathf.Abs(lengthA + lengthB - length) < 0.1f;
        }
        
        public static List<Vector3> FilterByMostTwo(List<Vector3> points)
        {
            if (points.Count < 3)
            {
                return points;
            }
            
            Vector3 min = (points[0].y > points[1].y) ? points[1] : points[0];
            Vector3 max = (points[0].y <= points[1].y) ? points[1] : points[0];
            
            foreach (Vector3 point in points)
            {
                if (min.y < point.y) min = point;
                if (max.y > point.y) max = point;
            }
            
            return new List<Vector3> { min, max };
        }
        
        public static List<Vector2> FilterByMostTwo(List<Vector2> points)
        {
            if (points.Count < 3)
            {
                return points;
            }
            
            Vector3 min = (points[0].y > points[1].y) ? points[1] : points[0];
            Vector3 max = (points[0].y <= points[1].y) ? points[1] : points[0];
            
            foreach (Vector3 point in points)
            {
                if (min.y > point.y) min = point;
                if (max.y < point.y) max = point;
            }
            
            return new List<Vector2> { min, max };
        }

        public static Vector2[] SkipSameElement(Vector2[] vertices)
        {
            if (vertices.Length == 0) return vertices;

            int writeIndex = 1;
            for (int readIndex = 1; readIndex < vertices.Length; readIndex++)
            {
                if (vertices[readIndex] != vertices[readIndex - 1])
                {
                    vertices[writeIndex++] = vertices[readIndex];
                }
            }

            Array.Resize(ref vertices, writeIndex);
            return vertices;
        }

        public static Vector3[] Simplify(List<Vector2> vertices)
        {
            Stack<Vector4> stack = new Stack<Vector4>();
            Vector2 tempVec = Vector2.zero;

            foreach (var v in vertices)
            {
                int c = stack.Count;
                if (c == 0)
                {
                    stack.Push(new Vector4(v.x, v.y, 0, 0));
                    continue;
                }
                if (SamePosition(stack.Peek(), v))
                {
                    continue;
                }
                GetDirection(stack.Peek(), v, ref tempVec);
                while (SameDirection(stack.Peek(), tempVec))
                {
                    stack.Pop();
                    GetDirection(stack.Peek(), v, ref tempVec);
                }
                stack.Push(new Vector4(v.x, v.y, tempVec.x, tempVec.y));
            }
            Vector3[] result = new Vector3[stack.Count];
            for (int i = stack.Count - 1; i >= 0; --i) result[i] = Convert23(stack.Pop());
            return result;
        }

        public static Vector2[] Simplify(Vector2[] vertices)
        {
            Stack<Vector4> stack = new Stack<Vector4>();
            Vector2 tempVec = Vector2.zero;

            foreach (var v in vertices)
            {
                if (stack.Count == 0)
                {
                    stack.Push(new Vector4(v.x, v.y, 0, 0));
                    continue;
                }
                if (SamePosition(stack.Peek(), v))
                {
                    continue;
                }
                GetDirection(stack.Peek(), v, ref tempVec);
                while (SameDirection(stack.Peek(), tempVec))
                {
                    stack.Pop();
                    GetDirection(stack.Peek(), v, ref tempVec);
                }
                stack.Push(new Vector4(v.x, v.y, tempVec.x, tempVec.y));
            }

            int count = stack.Count;
            Vector2[] result = new Vector2[count];
            for (int i = count - 1; i >= 0; --i) result[i] = stack.Pop();
            return result;
        }
        
        private static Vector3 Convert23(Vector4 v) => new Vector3(v.x, v.y, 0);

        private static void GetDirection(Vector4 a, Vector2 b, ref Vector2 res)
        {
            if (res == null) res = Vector2.zero;
            res.x = a.x - b.x;
            res.y = a.y - b.y;
            res.Normalize();
        }
        
        private static bool SameDirection(Vector4 v4, Vector2 newDir) => OnSameLine(new Vector2(v4.z, v4.w), newDir);

        private static bool SamePosition(Vector4 v1, Vector2 v2) => SameFloat(v1.x, v2.x) && SameFloat(v1.y, v2.y);
        
        private static bool OnSameLine(Vector2 v1, Vector2 v2)
        {
            if (v1 == Vector2.zero || v2 == Vector2.zero) return false;
            if (v1.x == 0 && v2.x == 0 || v1.y == 0 && v2.y == 0) return true;
            
            float crossProduct = v1.x * v2.y - v1.y * v2.x;
            if (Mathf.Abs(crossProduct) > 1e-6f) return false;

            float kX = (v1.x != 0) ? (v2.x / v1.x) : (v2.y / v1.y);
            float kY = (v1.y != 0) ? (v2.y / v1.y) : (v2.x / v1.x);

            return Mathf.Abs(kX - kY) < 1e-6f;
        }
        
        private static bool SameVector(Vector3 v3, Vector2 v2) => SameFloat(v3.x, v2.x) && SameFloat(v3.y, v2.y);
        
        public static List<Vector2> GetSplitVertices(List<Vector2> vertices ,List<Vector3> crossedPoints, bool isLeft)
        {
            if (crossedPoints.Count > 2 || crossedPoints.Count == 0)
            {
                //Debug.LogError("This function is designed to work only for the length of 1 or 2!!");
                return vertices;
            }
            
            int n = vertices.Count;
            int startIndex = CeilF(isLeft ? crossedPoints[^1].z : crossedPoints[0].z); // max y vs min y
            int endIndex = FloorF(isLeft ? crossedPoints[0].z : crossedPoints[^1].z);
            Vector3 head = isLeft ? crossedPoints[^1] : crossedPoints[0];
            Vector3 tail = isLeft ? crossedPoints[0] : crossedPoints[^1];
            
            List<Vector2> result = SameVector(head, vertices[startIndex]) ? new List<Vector2>() : new List<Vector2>{head };
            
            // Return already if there exists only a single element
            if (isLeft && crossedPoints.Count == 1) return result;
            
            if (endIndex > startIndex) startIndex += n;

            for (int i = startIndex; i >= endIndex; --i)
            {
                result.Add(vertices[i % n]);
            }
            
            if (!SameVector(tail, vertices[endIndex%n])) result.Add(tail);
            
            return result;
        }
        
        public static List<Vector3> FindPointsOnContour(List<Vector2> vertices, float x)
        {
            int length = vertices.Count;
            List<Vector3> result = new List<Vector3>();

            for (int i = 0; i < length; ++i)
            {
                int j = (i - 1 + length) % length;
                
                // Skip the element with the same x value to prevent a division by 0
                if (Mathf.Abs(vertices[i].x - vertices[j].x) < 0.001f)
                {
                    continue;
                }

                if (IsBetween(vertices[i].x, vertices[j].x, x))
                {
                    // Find the position of y based on the slope
                    float range = vertices[i].x - vertices[j].x;
                    float slope = (vertices[i].y - vertices[j].y) / range;
                    float xDiff = x - vertices[j].x;
                    float y = xDiff * slope + vertices[j].y;
                    
                    float ratio = xDiff / range;
                    
                    result.Add(new Vector3(x, y, (j + ratio) % length));
                }
            }

            return result;
        }

        public static bool IsClockwise(Vector3[] vertices)
        {
            if (vertices == null || vertices.Length < 3)
            {
                Debug.LogWarning("A valid polygon must have at least 3 vertices.");
                return false;
            }
            
            float sum = 0f;
            int count = vertices.Length;

            for (int i = 0; i < count; i++)
            {
                Vector2 current = vertices[i];
                Vector2 next = vertices[(i + 1) % count];

                sum += (next.x - current.x) * (next.y + current.y);
            }

            return sum > 0;
        }
        
        public static bool IsClockwise(List<Vector2> vertices)
        {
            if (vertices == null || vertices.Count < 3)
            {
                Debug.LogWarning("A valid polygon must have at least 3 vertices.");
                return false;
            }

            float sum = 0f;
            int count = vertices.Count;

            for (int i = 0; i < count; i++)
            {
                Vector2 current = vertices[i];
                Vector2 next = vertices[(i + 1) % count];

                sum += (next.x - current.x) * (next.y + current.y);
            }

            return sum > 0;
        }

        private static bool SameFloat(float f, float f2) => Mathf.Abs(f - f2) < 0.001f;
        
        public static int FloorF(float f) => (int)Mathf.Floor(f);
        private static int CeilF(float f) => (int)Mathf.Ceil(f);
        private static bool IsBetween(float a, float b, float x)
            => (a >= x && b <= x) || (a <= x && b >= x);
        
        private Calc() {}
    }
}