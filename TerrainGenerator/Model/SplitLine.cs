using System.Collections.Generic;
using UnityEngine;
using System;
using Fujin.TerrainGenerator.Utility;

namespace Fujin.TerrainGenerator.Model
{
    public class SplitLine
    {
        public Vector3[] LeftLine { get; private set; }
        public Vector3[] RightLine { get; private set; }
        private readonly Param _param;
            
        public enum Param
        {
            Hole,
        }

        public enum SetResult
        {
            InvalidParam,
            InvalidVertices,
            InvalidHeightRange,
            InvalidMethod,
            Success,
            Failure,
        }

        public bool IsValid() => LeftLine != null && RightLine != null && LeftLine.Length>= 2 && RightLine.Length >= 2 && LeftLine[0] == RightLine[^1] && LeftLine[^1] == RightLine[0];
        
        public SetResult TrySetLine(List<Vector2> vertices, Vector2 heightRange)
        {
            if (_param != Param.Hole)
            {
                Debug.LogError($"Error: Method SetLine cannot be computed with {_param} set!");
                return SetResult.InvalidParam;
            }

            if (vertices.Count < 3)
            {
                Debug.LogError($"Error: the number of vertices must be more or equal to 3!");
                return SetResult.InvalidVertices;
            }

            if (!Calc.IsClockwise(vertices))
            {
                Debug.LogWarning("Please ensure to sort vertices so that they are ordered clockwise");
                vertices.Reverse();
            }
            
            // Ensure that x is min and y is max
            if (heightRange.x > heightRange.y)
            {
                (heightRange.x, heightRange.y) = (heightRange.y, heightRange.x);
            }

            {
                List<Vector2> maxAndMin = Calc.FilterByMostTwo(vertices);
                if (maxAndMin[0].y < heightRange.x || maxAndMin[1].y > heightRange.y)
                {
                    return SetResult.InvalidHeightRange;
                }
            }

            try
            {
                List<Vector3> crossedPoints = Calc.FilterByMostTwo(Calc.FindPointsOnContour(vertices, vertices[0].x));

                if (crossedPoints.Count == 0) return SetResult.InvalidMethod;
                
                Vector2 bottomEnd = new Vector2(vertices[0].x, heightRange.x);
                Vector2 topEnd = new Vector2(vertices[0].x, heightRange.y);

                List<Vector2> tempLeft = new List<Vector2> { topEnd };
                tempLeft.AddRange(Calc.GetSplitVertices(vertices, crossedPoints, true));
                tempLeft.Add(bottomEnd);
                LeftLine = Calc.Simplify(tempLeft);
                
                List<Vector2> tempRight = new List<Vector2> { bottomEnd };
                tempRight.AddRange(Calc.GetSplitVertices(vertices, crossedPoints, false));
                tempRight.Add(topEnd);
                RightLine = Calc.Simplify(tempRight);
            
                return SetResult.Success;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return SetResult.Failure;
            }
        }
        
        
        public SplitLine(Param p)
        {
            if (p != Param.Hole)
            {
                Debug.LogError("Error: SplitLine cannot parse inputs other than vertices representing a hole.");
                return;
            }

            _param = p;
        }
    }
}