using UnityEngine;
using UnityEditor;

namespace Fujin.TerrainGenerator
{
    [CustomEditor(typeof(TilemapConverter))]
    public class MyEditorToolEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TilemapConverter myTool = (TilemapConverter)target;

            if (GUILayout.Button("Convert into 3D map"))
            {
                myTool.ConvertTilemap();
            }

            if (GUILayout.Button("Destroy the most recent 3D map !!"))
            {
                myTool.DestroyMostRecent3DMap();
            }

            if (GUILayout.Button("Create a cute star"))
            {
                myTool.CreateStar();
            }

            if (GUILayout.Button("Create a box"))
            {
                myTool.CreateBox();
            }
            
            if (GUILayout.Button("Fact Check"))
            {
                myTool.FactCheck();
            }
        }
    }
}
