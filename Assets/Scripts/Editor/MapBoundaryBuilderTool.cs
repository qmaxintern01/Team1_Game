using System.Collections.Generic;
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
    /// 床が複数のTilemapに分かれて配置されている場合も、隣接判定は全Tilemapの床セルをまとめたワールド座標の
    /// 集合に対して行うため、Tilemapの境目で床が繋がっている箇所に誤って壁ができない。
    /// 連続する境目は1つのコライダーにまとめて生成数を抑える。
    /// Tools > Map > Build Map Boundary Walls から実行する。
    /// </summary>
    public static class MapBoundaryBuilderTool
    {
        private const string BoundaryObjectName = "MapBoundary";

        // ワールド座標を床セル集合のキーにする際の丸め精度(浮動小数点誤差の吸収用)
        private const float WorldKeyScale = 1000f;

        [MenuItem("Tools/Map/Build Map Boundary Walls")]
        public static void Build()
        {
            var allTilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            if (allTilemaps.Length == 0)
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

            // 既にTilemapCollider2Dを持つタイルマップ(GameMapBuilderToolが生成するTilemap_Wallsなど)は
            // 壁として自前でコライダーを持っているため、ここで境界コライダーを重ねると
            // 壁リングの内側(床向きの面)にまで余分な当たり判定ができてしまう。対象から除外する。
            var tilemaps = allTilemaps
                .Where(t => t.gameObject.layer != wallLayer && !t.TryGetComponent<TilemapCollider2D>(out _))
                .ToArray();

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

            foreach (var tilemap in tilemaps)
            {
                tilemap.CompressBounds();
            }

            HashSet<Vector3Int> floorWorldKeys = BuildFloorWorldKeys(tilemaps);

            int segmentCount = 0;
            foreach (var tilemap in tilemaps)
            {
                if (tilemap.GetUsedTilesCount() == 0)
                {
                    continue;
                }

                segmentCount += BuildEdgesForTilemap(tilemap, floorWorldKeys, boundaryRoot.transform, wallLayer);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"境界コライダーを{segmentCount}個生成しました。");
        }

        // 対象となる全Tilemapの床セルを、ワールド座標をキーにして1つの集合にまとめる
        private static HashSet<Vector3Int> BuildFloorWorldKeys(Tilemap[] tilemaps)
        {
            var keys = new HashSet<Vector3Int>();
            foreach (var tilemap in tilemaps)
            {
                BoundsInt bounds = tilemap.cellBounds;
                foreach (Vector3Int cell in bounds.allPositionsWithin)
                {
                    if (tilemap.HasTile(cell))
                    {
                        keys.Add(ToWorldKey(tilemap.GetCellCenterWorld(cell)));
                    }
                }
            }

            return keys;
        }

        private static Vector3Int ToWorldKey(Vector3 worldPosition)
        {
            return new Vector3Int(
                Mathf.RoundToInt(worldPosition.x * WorldKeyScale),
                Mathf.RoundToInt(worldPosition.y * WorldKeyScale),
                Mathf.RoundToInt(worldPosition.z * WorldKeyScale));
        }

        private static int BuildEdgesForTilemap(Tilemap tilemap, HashSet<Vector3Int> floorWorldKeys, Transform parent, int wallLayer)
        {
            BoundsInt bounds = tilemap.cellBounds;
            Vector3 cellSize = tilemap.layoutGrid.cellSize;
            int count = 0;

            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                count += EmitHorizontalRun(tilemap, floorWorldKeys, parent, wallLayer, y, bounds.xMin, bounds.xMax, Vector3Int.down, -cellSize.y, cellSize, "Bottom");
                count += EmitHorizontalRun(tilemap, floorWorldKeys, parent, wallLayer, y, bounds.xMin, bounds.xMax, Vector3Int.up, cellSize.y, cellSize, "Top");
            }

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                count += EmitVerticalRun(tilemap, floorWorldKeys, parent, wallLayer, x, bounds.yMin, bounds.yMax, Vector3Int.left, -cellSize.x, cellSize, "Left");
                count += EmitVerticalRun(tilemap, floorWorldKeys, parent, wallLayer, x, bounds.yMin, bounds.yMax, Vector3Int.right, cellSize.x, cellSize, "Right");
            }

            return count;
        }

        // y行を左から右へ走査し、床セルの隣(neighborOffset方向)にタイルが無い区間を1つのコライダーにまとめる
        private static int EmitHorizontalRun(Tilemap tilemap, HashSet<Vector3Int> floorWorldKeys, Transform parent, int wallLayer, int y, int xMin, int xMax, Vector3Int neighborOffset, float yOffset, Vector3 cellSize, string label)
        {
            int count = 0;
            int runStart = int.MinValue;

            for (int x = xMin; x <= xMax; x++)
            {
                bool needsWall = x < xMax && NeedsWall(tilemap, floorWorldKeys, x, y, neighborOffset);

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
        private static int EmitVerticalRun(Tilemap tilemap, HashSet<Vector3Int> floorWorldKeys, Transform parent, int wallLayer, int x, int yMin, int yMax, Vector3Int neighborOffset, float xOffset, Vector3 cellSize, string label)
        {
            int count = 0;
            int runStart = int.MinValue;

            for (int y = yMin; y <= yMax; y++)
            {
                bool needsWall = y < yMax && NeedsWall(tilemap, floorWorldKeys, x, y, neighborOffset);

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

        // 床セルの隣(neighborOffset方向)に、対象タイルマップ全体(floorWorldKeys)のいずれの床タイルも無いかを判定する
        private static bool NeedsWall(Tilemap tilemap, HashSet<Vector3Int> floorWorldKeys, int x, int y, Vector3Int neighborOffset)
        {
            var cell = new Vector3Int(x, y, 0);
            if (!tilemap.HasTile(cell))
            {
                return false;
            }

            Vector3 neighborWorld = tilemap.GetCellCenterWorld(cell + neighborOffset);
            return !floorWorldKeys.Contains(ToWorldKey(neighborWorld));
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
