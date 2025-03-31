using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace Fujin.TerrainGenerator
{
    /// <summary>
    /// Converts the registered tilemap to the 3d object
    /// </summary>
    public class TilemapConverter : MonoBehaviour
    {
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private Material mapMaterial;
        [SerializeField] private Vector3 mapCreatePosition;

        public void ConvertTilemap()
        {
            if (tilemap == null)
            {
                Debug.LogError("Error: tilemap is not assigned!");
                return;
            }
            
            Vector2 bottomLeft = Vector2.zero;
            Vector2 topRight = Vector2.zero;
            
            Debug.Log("Test!");
            BoundsInt bounds = tilemap.cellBounds;
            TileBase[] allTiles = tilemap.GetTilesBlock(bounds); //tile.nameで識別可能

            for (int x = 0; x < bounds.size.x; x++)
            {
                for (int y = 0; y < bounds.size.y; y++)
                {
                    TileBase tile = allTiles[x + y * bounds.size.x];
                    if (tile != null)
                    {
                        Debug.Log($"x: {x}, y: {y}");
                        bottomLeft.x = Mathf.Min(bottomLeft.x, x);
                        bottomLeft.y = Mathf.Min(bottomLeft.y, y);
                        topRight.x = Mathf.Max(topRight.x, x);
                        topRight.y = Mathf.Max(topRight.y, y);
                    }
                }
            }
            
            Debug.Log("Not creating anything!");
            
            
            //rangeで取得するのはいいとして...
            //1) 取りこぼしを防ぐ：　fill pool → 素材ごとに分ける → 繋がっている同素材ごとにchunkにする
            //2) 素材同士の分け方：　rangeで区切る → 1マスでも繋がっている場合は同じMatrix<Range>にぶち込む (3つ以上の時の取りこぼしの対策方法が未定...)
            
        }

        private GameObject _mostRecent3DMap;

        public void DestroyMostRecent3DMap()
        {
            if (_mostRecent3DMap != null)
            {
                DestroyImmediate(_mostRecent3DMap);
                _mostRecent3DMap = null;
                Debug.Log("Destroyed one 3d map!");
            }
            else
            {
                Debug.Log("No 3d map is left to be destroyed!");
            }
        }
        
        private readonly List<Vector2> _starOutline = new List<Vector2>()
        {
            new Vector2(0,3),
            new Vector2(1,1),
            new Vector2(2.5f,1.5f),
            new Vector2(1.5f,-1),
            new Vector2(2, -2.5f),
            new Vector2(1.5f,0),
            new Vector2(-2,-2.5f),
            new Vector2(-1.5f,-1),
            new Vector2(-2.5f,1.5f),
            new Vector2(-1,1),
        };

        private readonly List<Vector2> _boxOutline = new List<Vector2>()
        {
            new Vector2(0, 0),
            new Vector2(3, 0),
            new Vector2(3, 3),
            new Vector2(0, 3)
        };

        public void CreateStar()
        {
            CreateBlock(_boxOutline, 5f, out _mostRecent3DMap);

            _mostRecent3DMap.transform.position = mapCreatePosition;
            _mostRecent3DMap.transform.localScale = Vector3.one * 0.3f;
            Debug.Log("Created a star!");
        }
        
        public void DrawStar()
        {
            //問題点：
            //星型みたいな複雑なのはtriangulateできてない
            //mappingが外側と内側のごっちゃのために描写がちゃんとできてない
            //穴あきのも描写できるかテストがまだ出来てない
            //選択肢たエリアにこのmappingを適用みたいなこともまだ
            DrawPoly(_starOutline, out GameObject poly);
            poly.transform.position = mapCreatePosition;
            poly.transform.localScale = Vector3.one * 0.3f;
            Debug.Log("Drew a star");
        }

        private void DrawPoly(List<Vector2> contour, out GameObject poly)
        {
            poly = new GameObject("2D MESH (made by code)");
            MeshFilter meshFilter = poly.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = poly.AddComponent<MeshRenderer>();
            
            meshFilter.mesh = MeshGenerator.CreateMeshFromVertices(contour);
            
            meshRenderer.material = mapMaterial;
        }

        private void CreateBlock(List<Vector2> contour, float depth, out GameObject block)
        {
            block = new GameObject("3D MAP (made by code)");
            MeshFilter meshFilter = block.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = block.AddComponent<MeshRenderer>();

            // Assign a mesh
            meshFilter.mesh = MeshGenerator.Create3DMeshFrom2DMesh(MeshGenerator.CreateMeshFromVertices(contour), depth);

            // Change the default material
            meshRenderer.material = mapMaterial;
        }
    }
}