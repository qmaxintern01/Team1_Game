using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Team1.EditorTools
{
    /// <summary>
    /// SteampunkFactoryTileset.jpg をスライスし、Tileアセットと Tile Palette を生成し、
    /// GameScene に描画用の Grid / Tilemap を用意するエディタ専用ツール。
    /// Tools > Tileset > Setup Steampunk Factory Tileset から実行する。
    /// </summary>
    public static class TilesetSetupTool
    {
        private const string TexturePath = "Assets/Art/Tilesets/SteampunkFactoryTileset.png";
        private const int CellSize = 32;
        private const int PixelsPerUnit = 32;
        private const string TilesFolder = "Assets/Art/Tilesets/Tiles";
        private const string PaletteFolder = "Assets/Art/Tilesets/Palettes";
        private const string PaletteName = "SteampunkFactoryPalette";
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";

        [MenuItem("Tools/Tileset/Setup Steampunk Factory Tileset")]
        public static void Setup()
        {
            SliceTexture(out int columns, out int rows);

            var sprites = LoadSlicedSprites();
            if (sprites.Count == 0)
            {
                Debug.LogError($"{TexturePath} からスプライトを取得できませんでした。スライス設定を確認してください。");
                return;
            }

            var tiles = CreateTiles(sprites);
            CreatePalette(tiles, columns);
            AddTilemapToGameScene();

            Debug.Log($"タイルセットのセットアップが完了しました。スプライト数:{sprites.Count} / Tile数:{tiles.Count} / パレット:{PaletteFolder}/{PaletteName}.prefab (grid {columns}x{rows})");
        }

        private static void SliceTexture(out int columns, out int rows)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            if (importer == null)
            {
                throw new FileNotFoundException($"テクスチャが見つかりません: {TexturePath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.isReadable = true;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            int width = texture.width;
            int height = texture.height;
            columns = width / CellSize;
            rows = height / CellSize;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = new List<SpriteRect>();
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                // Unityのテクスチャ原点は左下のため、画像上から数えた行を反転させる。
                int y = height - (row + 1) * CellSize;
                for (int col = 0; col < columns; col++)
                {
                    int x = col * CellSize;
                    spriteRects.Add(new SpriteRect
                    {
                        name = $"SteampunkFactoryTileset_{index}",
                        spriteID = GUID.Generate(),
                        rect = new Rect(x, y, CellSize, CellSize),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    });
                    index++;
                }
            }

            dataProvider.SetSpriteRects(spriteRects.ToArray());

            var nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            var pairs = spriteRects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList();
            nameFileIdProvider.SetNameFileIdPairs(pairs);

            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        private static int ExtractIndex(string spriteName)
        {
            int underscoreIndex = spriteName.LastIndexOf('_');
            if (underscoreIndex >= 0 && int.TryParse(spriteName.Substring(underscoreIndex + 1), out int result))
            {
                return result;
            }
            return 0;
        }

        private static List<Sprite> LoadSlicedSprites()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(TexturePath);
            var sprites = assets.OfType<Sprite>().ToList();
            sprites.Sort((a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));
            return sprites;
        }

        private static List<TileBase> CreateTiles(List<Sprite> sprites)
        {
            if (!AssetDatabase.IsValidFolder(TilesFolder))
            {
                Directory.CreateDirectory(TilesFolder);
                AssetDatabase.Refresh();
            }

            var tiles = new List<TileBase>();
            foreach (var sprite in sprites)
            {
                string assetPath = $"{TilesFolder}/{sprite.name}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, assetPath);
                }
                tile.sprite = sprite;
                tile.color = Color.white;
                EditorUtility.SetDirty(tile);
                tiles.Add(tile);
            }

            AssetDatabase.SaveAssets();
            return tiles;
        }

        private static void CreatePalette(List<TileBase> tiles, int columns)
        {
            if (!AssetDatabase.IsValidFolder(PaletteFolder))
            {
                Directory.CreateDirectory(PaletteFolder);
                AssetDatabase.Refresh();
            }

            float cellSize = (float)CellSize / PixelsPerUnit;

            var gridGo = new GameObject(PaletteName, typeof(Grid));
            var grid = gridGo.GetComponent<Grid>();
            grid.cellSize = new Vector3(cellSize, cellSize, 0f);

            var tilemapGo = new GameObject("Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
            tilemapGo.transform.SetParent(gridGo.transform);
            var tilemap = tilemapGo.GetComponent<Tilemap>();

            for (int i = 0; i < tiles.Count; i++)
            {
                int x = i % columns;
                int y = -(i / columns);
                tilemap.SetTile(new Vector3Int(x, y, 0), tiles[i]);
            }

            string palettePath = $"{PaletteFolder}/{PaletteName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(gridGo, palettePath);
            Object.DestroyImmediate(gridGo);
        }

        private static void AddTilemapToGameScene()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            var existingGrid = Object.FindObjectsByType<Grid>(FindObjectsSortMode.None).FirstOrDefault();
            if (existingGrid == null)
            {
                float cellSize = (float)CellSize / PixelsPerUnit;
                var gridGo = new GameObject("Grid", typeof(Grid));
                var grid = gridGo.GetComponent<Grid>();
                grid.cellSize = new Vector3(cellSize, cellSize, 0f);

                var tilemapGo = new GameObject("Tilemap_Ground", typeof(Tilemap), typeof(TilemapRenderer));
                tilemapGo.transform.SetParent(gridGo.transform);

                EditorSceneManager.MarkSceneDirty(scene);
            }

            EditorSceneManager.SaveScene(scene);
        }
    }
}
