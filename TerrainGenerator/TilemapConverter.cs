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
            new Vector2(3.5f, 6.5f),
            new Vector2(4.5f, 4.5f),
            new Vector2(6.0f, 5.0f),
            new Vector2(5.0f, 2.5f),
            new Vector2(5.5f, 1.0f),
            new Vector2(3.5f, 2.0f),
            new Vector2(1.5f, 1.0f),
            new Vector2(2.0f, 2.5f),
            new Vector2(1.0f, 5.0f),
            new Vector2(2.5f, 4.5f)
        };

        public void FactCheck()
        {
            Debug.LogWarning("Either FactCheck() remains empty or a modification is yet to be reflected!");
        }

        private readonly List<Vector2> _boxOutline = new List<Vector2>()
        {
            new Vector2(0, 0),
            new Vector2(0, 3),
            new Vector2(3, 3),
            new Vector2(3, 0)
        };

        public void CreateStar()
        {
            CreateBlock(_starOutline, 5f, out _mostRecent3DMap);

            _mostRecent3DMap.transform.position = mapCreatePosition;
            _mostRecent3DMap.transform.localScale = Vector3.one * 0.3f;
            _mostRecent3DMap.AddComponent<VoluntaryRotation>();
            Debug.Log("Created a star!");
        }
        
        public void CreateBox()
        {
            CreateBlock(_boxOutline, 5f, out _mostRecent3DMap);

            _mostRecent3DMap.transform.position = mapCreatePosition;
            _mostRecent3DMap.transform.localScale = Vector3.one * 0.3f;
            _mostRecent3DMap.AddComponent<VoluntaryRotation>();
            Debug.Log("Created a box!");
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