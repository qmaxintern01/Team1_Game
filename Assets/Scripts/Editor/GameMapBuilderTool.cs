using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Team1.EditorTools
{
    /// <summary>
    /// SteampunkFactoryTileset からスライス済みのTileアセットを使って、
    /// GameScene内の仮配置(Stagesのプレースホルダー矩形)を実際のマップに置き換えるエディタ専用ツール。
    /// Tools > Map > Build Game Scene Map から実行する。
    /// </summary>
    public static class GameMapBuilderTool
    {
        private const string TilesFolder = "Assets/Art/Tilesets/Tiles";
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";

        private const int FloorBase = 8;
        private const int FloorAccentRivet = 0;
        private const int FloorAccentEmblem = 42;
        private const int Wall = 4;
        private const int DecorGear = 73;
        private const int DecorArchMachine = 459;
        private const int DecorControlPanel = 461;

        private const float AccentChance = 0.08f;
        private const int RandomSeed = 20260826;

        // 既存の"Stages"プレースホルダー(Stage_1〜4)と同じ範囲を、部屋の矩形として定義する。
        // 中央(Stage_1)を起点に右(Stage_2)・左(Stage_3)・上(Stage_4)へ十字型に配置。
        private static readonly RectInt[] Rooms =
        {
            new RectInt(-9, -5, 18, 10), // Stage_1: 中央
            new RectInt(9, -5, 18, 10),  // Stage_2: 右
            new RectInt(-27, -5, 18, 10), // Stage_3: 左
            new RectInt(-9, 5, 18, 10),  // Stage_4: 上
        };

        private static readonly (int x, int y, int tileIndex)[] DecorPlacements =
        {
            (-8, -4, DecorArchMachine),
            (7, 3, DecorControlPanel),
            (-3, -3, DecorGear),
            (20, -3, DecorControlPanel),
            (24, 2, DecorGear),
            (-22, 2, DecorGear),
            (-15, -3, DecorControlPanel),
            (0, 11, DecorArchMachine),
            (5, 9, DecorControlPanel),
        };

        [MenuItem("Tools/Map/Build Game Scene Map")]
        public static void Build()
        {
            var floorBaseTile = LoadTile(FloorBase);
            var floorAccentRivetTile = LoadTile(FloorAccentRivet);
            var floorAccentEmblemTile = LoadTile(FloorAccentEmblem);
            var wallTile = LoadTile(Wall);

            if (floorBaseTile == null || wallTile == null)
            {
                Debug.LogError($"{TilesFolder} からタイルを読み込めませんでした。先に Tools > Tileset > Setup Steampunk Factory Tileset を実行してください。");
                return;
            }

            var floorCells = BuildFloorCells();
            var wallCells = BuildWallCells(floorCells);

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            var grid = UnityEngine.Object.FindObjectsByType<Grid>(FindObjectsSortMode.None).FirstOrDefault();
            if (grid == null)
            {
                Debug.LogError("シーン内に Grid が見つかりません。先に Tools > Tileset > Setup Steampunk Factory Tileset を実行してください。");
                return;
            }

            var groundTilemap = FindOrCreateTilemap(grid.transform, "Tilemap_Ground", sortingOrder: 0, withCollider: false);
            var wallsTilemap = FindOrCreateTilemap(grid.transform, "Tilemap_Walls", sortingOrder: 1, withCollider: true);
            var decorTilemap = FindOrCreateTilemap(grid.transform, "Tilemap_Decor", sortingOrder: 2, withCollider: false);

            // プレイヤー・敵の移動側(WallCollision)がこのレイヤーを壁判定に使うため、必ず"Wall"レイヤーへ設定する
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer >= 0)
            {
                wallsTilemap.gameObject.layer = wallLayer;
            }
            else
            {
                Debug.LogWarning("\"Wall\" レイヤーが見つかりません。Project Settings > Tags and Layers に追加してください。");
            }

            groundTilemap.ClearAllTiles();
            wallsTilemap.ClearAllTiles();
            decorTilemap.ClearAllTiles();

            var random = new System.Random(RandomSeed);
            var decorCells = new HashSet<Vector3Int>(DecorPlacements.Select(d => new Vector3Int(d.x, d.y, 0)));

            foreach (var cell in floorCells)
            {
                TileBase tile = floorBaseTile;
                if (!decorCells.Contains(cell))
                {
                    double roll = random.NextDouble();
                    if (roll < AccentChance * 0.5f)
                    {
                        tile = floorAccentEmblemTile;
                    }
                    else if (roll < AccentChance)
                    {
                        tile = floorAccentRivetTile;
                    }
                }

                groundTilemap.SetTile(cell, tile);
            }

            foreach (var cell in wallCells)
            {
                wallsTilemap.SetTile(cell, wallTile);
            }

            foreach (var (x, y, tileIndex) in DecorPlacements)
            {
                var decorTile = LoadTile(tileIndex);
                if (decorTile != null)
                {
                    decorTilemap.SetTile(new Vector3Int(x, y, 0), decorTile);
                }
            }

            RemovePlaceholderStages();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"マップ生成が完了しました。床:{floorCells.Count} / 壁:{wallCells.Count} / 装飾:{DecorPlacements.Length}");
        }

        private static HashSet<Vector3Int> BuildFloorCells()
        {
            var cells = new HashSet<Vector3Int>();
            foreach (var room in Rooms)
            {
                for (int x = room.xMin; x < room.xMax; x++)
                {
                    for (int y = room.yMin; y < room.yMax; y++)
                    {
                        cells.Add(new Vector3Int(x, y, 0));
                    }
                }
            }

            return cells;
        }

        private static readonly Vector2Int[] NeighborOffsets =
        {
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
            new Vector2Int(-1, 0), new Vector2Int(1, 0),
            new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
        };

        private static HashSet<Vector3Int> BuildWallCells(HashSet<Vector3Int> floorCells)
        {
            var wallCells = new HashSet<Vector3Int>();
            foreach (var cell in floorCells)
            {
                foreach (var offset in NeighborOffsets)
                {
                    var neighbor = new Vector3Int(cell.x + offset.x, cell.y + offset.y, 0);
                    if (!floorCells.Contains(neighbor))
                    {
                        wallCells.Add(neighbor);
                    }
                }
            }

            return wallCells;
        }

        private static Tilemap FindOrCreateTilemap(Transform gridTransform, string name, int sortingOrder, bool withCollider)
        {
            var existing = gridTransform.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(gridTransform);
                go.transform.localPosition = Vector3.zero;
            }

            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;

            if (withCollider)
            {
                if (go.GetComponent<TilemapCollider2D>() == null)
                {
                    go.AddComponent<TilemapCollider2D>();
                }

                var rigidbody = go.GetComponent<Rigidbody2D>();
                if (rigidbody == null)
                {
                    rigidbody = go.AddComponent<Rigidbody2D>();
                }

                rigidbody.bodyType = RigidbodyType2D.Static;
            }

            return go.GetComponent<Tilemap>();
        }

        private static void RemovePlaceholderStages()
        {
            var stages = GameObject.Find("Stages");
            if (stages != null)
            {
                UnityEngine.Object.DestroyImmediate(stages);
            }
        }

        private static TileBase LoadTile(int index)
        {
            return AssetDatabase.LoadAssetAtPath<TileBase>($"{TilesFolder}/SteampunkFactoryTileset_{index}.asset");
        }
    }
}
