using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Team1.EditorTools
{
    /// <summary>
    /// シーン内の各Tilemapについて、タイルが置かれているセルと置かれていない隣接セルの境目(=床の外周)に
    /// 当たり判定を生成する。外接矩形で1つに囲む方式と違い、1つのTilemap内に離れた区画(部屋)が複数あっても、
    /// また複数のTilemapが離れて存在していても、それぞれの外周を個別に検出して塞ぐため隙間ができない。
    /// 連続する境目は1つのコライダーにまとめて生成数を抑える。
    /// Tools > Map > Build Map Boundary Walls から実行する。
    /// </summary>
    public static class MapBoundaryBuilderTool
    {
        private const string BoundaryObjectName = "MapBoundary";

        [MenuItem("Tools/Map/Build Map Boundary Walls")]
        public static void Build()
        {
            var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            if (tilemaps.Length == 0)
            {
                Debug.LogError("シーン内にTilemapが見つかりません。");
                return;
            }

            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer < 0)
            {
                Debug.LogError("\"Wall\" レイヤーが見つかりません。Project Settings > Tags and Layers に追加してください。");
                return;
            }

            var boundaryRoot = GameObject.Find(BoundaryObjectName);
            if (boundaryRoot == null)
            {
                boundaryRoot = new GameObject(BoundaryObjectName);
            }

            boundaryRoot.layer = wallLayer;
            foreach (Transform child in boundaryRoot.transform.Cast<Transform>().ToArray())
            {
                Object.DestroyImmediate(child.gameObject);
            }

            int segmentCount = 0;
            foreach (var tilemap in tilemaps)
            {
                tilemap.CompressBounds();
                if (tilemap.GetUsedTilesCount() == 0)
                {
                    continue;
                }

                segmentCount += BuildEdgesForTilemap(tilemap, boundaryRoot.transform, wallLayer);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"境界コライダーを{segmentCount}個生成しました。");
        }

        private static int BuildEdgesForTilemap(Tilemap tilemap, Transform parent, int wallLayer)
        {
            BoundsInt bounds = tilemap.cellBounds;
            Vector3 cellSize = tilemap.layoutGrid.cellSize;
            int count = 0;

            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                count += EmitHorizontalRun(tilemap, parent, wallLayer, y, bounds.xMin, bounds.xMax, Vector3Int.down, -cellSize.y, cellSize, "Bottom");
                count += EmitHorizontalRun(tilemap, parent, wallLayer, y, bounds.xMin, bounds.xMax, Vector3Int.up, cellSize.y, cellSize, "Top");
            }

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                count += EmitVerticalRun(tilemap, parent, wallLayer, x, bounds.yMin, bounds.yMax, Vector3Int.left, -cellSize.x, cellSize, "Left");
                count += EmitVerticalRun(tilemap, parent, wallLayer, x, bounds.yMin, bounds.yMax, Vector3Int.right, cellSize.x, cellSize, "Right");
            }

            return count;
        }

        // y行を左から右へ走査し、床セルの隣(neighborOffset方向)にタイルが無い区間を1つのコライダーにまとめる
        private static int EmitHorizontalRun(Tilemap tilemap, Transform parent, int wallLayer, int y, int xMin, int xMax, Vector3Int neighborOffset, float yOffset, Vector3 cellSize, string label)
        {
            int count = 0;
            int runStart = int.MinValue;

            for (int x = xMin; x <= xMax; x++)
            {
                bool needsWall = x < xMax && NeedsWall(tilemap, x, y, neighborOffset);

                if (needsWall)
                {
                    if (runStart == int.MinValue)
                    {
                        runStart = x;
                    }

                    continue;
                }

                if (runStart == int.MinValue)
                {
                    continue;
                }

                int runEnd = x - 1;
                Vector3 startCenter = tilemap.GetCellCenterWorld(new Vector3Int(runStart, y, 0));
                Vector3 endCenter = tilemap.GetCellCenterWorld(new Vector3Int(runEnd, y, 0));
                Vector3 center = new Vector3((startCenter.x + endCenter.x) / 2f, startCenter.y + yOffset, 0f);
                float width = (runEnd - runStart + 1) * cellSize.x;

                CreateSegment(parent, wallLayer, $"Wall_{label}_{runStart}_{y}", center, new Vector2(width, cellSize.y));
                count++;
                runStart = int.MinValue;
            }

            return count;
        }

        // x列を下から上へ走査し、床セルの隣(neighborOffset方向)にタイルが無い区間を1つのコライダーにまとめる
        private static int EmitVerticalRun(Tilemap tilemap, Transform parent, int wallLayer, int x, int yMin, int yMax, Vector3Int neighborOffset, float xOffset, Vector3 cellSize, string label)
        {
            int count = 0;
            int runStart = int.MinValue;

            for (int y = yMin; y <= yMax; y++)
            {
                bool needsWall = y < yMax && NeedsWall(tilemap, x, y, neighborOffset);

                if (needsWall)
                {
                    if (runStart == int.MinValue)
                    {
                        runStart = y;
                    }

                    continue;
                }

                if (runStart == int.MinValue)
                {
                    continue;
                }

                int runEnd = y - 1;
                Vector3 startCenter = tilemap.GetCellCenterWorld(new Vector3Int(x, runStart, 0));
                Vector3 endCenter = tilemap.GetCellCenterWorld(new Vector3Int(x, runEnd, 0));
                Vector3 center = new Vector3(startCenter.x + xOffset, (startCenter.y + endCenter.y) / 2f, 0f);
                float height = (runEnd - runStart + 1) * cellSize.y;

                CreateSegment(parent, wallLayer, $"Wall_{label}_{x}_{runStart}", center, new Vector2(cellSize.x, height));
                count++;
                runStart = int.MinValue;
            }

            return count;
        }

        private static bool NeedsWall(Tilemap tilemap, int x, int y, Vector3Int neighborOffset)
        {
            var cell = new Vector3Int(x, y, 0);
            if (!tilemap.HasTile(cell))
            {
                return false;
            }

            return !tilemap.HasTile(cell + neighborOffset);
        }

        private static void CreateSegment(Transform parent, int wallLayer, string name, Vector3 worldPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(BoxCollider2D));
            go.layer = wallLayer;
            go.transform.SetParent(parent);
            go.transform.position = worldPosition;
            go.GetComponent<BoxCollider2D>().size = size;
        }
    }
}
