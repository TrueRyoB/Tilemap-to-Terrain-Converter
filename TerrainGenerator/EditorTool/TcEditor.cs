using UnityEngine;
using UnityEditor;
using Fujin.TerrainGenerator.System;

namespace Fujin.TerrainGenerator.EditorTool
{
    [CustomEditor(typeof(TilemapConverter))]
    public class MyEditorToolEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TilemapConverter myTool = (TilemapConverter)target;

            if (GUILayout.Button("Parse tilemap and create a platform"))
            {
                myTool.ConvertTilemap();
            }

            if (GUILayout.Button("Destroy the most recent object"))
            {
                myTool.DestroyMostRecent3DMap();
            }
            
            if (GUILayout.Button("Create a cheese"))
            {
                myTool.CreateCheese();
            }

            if (GUILayout.Button("Create a cute star"))
            {
                myTool.CreateStar();
            }
            
            if (GUILayout.Button("Fact Check"))
            {
                myTool.FactCheck();
            }
        }
    }
}