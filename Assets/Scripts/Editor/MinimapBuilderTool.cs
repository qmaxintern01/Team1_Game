using System.IO;
using System.Linq;
using Team1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Team1.EditorTools
{
    /// <summary>
    /// GameScene内の"HP"Canvas配下に、画面左上に表示するミニマップUIを生成するエディタ専用ツール。
    /// プレイヤーに追従する専用カメラをRenderTextureに描画し、それをRawImageで表示する。
    /// 「現在地(追従・拡大)」と「全体表示」の2モードをMinimapControllerで切り替えられるよう、
    /// シーン内の全Tilemapからマップ全体の範囲を計算し、全体表示モード用のカメラ設定として書き込む。
    /// ミニマップカメラのCullingMaskは"Wall"と"Minimap"レイヤーのみに絞り、Player/Enemyの実際の見た目
    /// (攻撃範囲予告・HPバー・弾なども含む)は映さず、代わりにMinimapMarkerLayerがUI上に描く固定サイズの
    /// 単色ドット(MinimapEntityMarkerが登録)のみを表示する。
    /// Tools > UI > Build Minimap から実行する。既存の生成物があれば作り直す。
    /// </summary>
    public static class MinimapBuilderTool
    {
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private const string CanvasName = "HP";
        private const string PanelName = "MinimapPanel";
        private const string CameraObjectName = "MinimapCamera";
        private const string MinimapLayerName = "Minimap";
        private const string WallLayerName = "Wall";
        private const string RenderTextureFolder = "Assets/Art/UI";
        private const string RenderTexturePath = RenderTextureFolder + "/MinimapRenderTexture.renderTexture";

        private const int RenderTextureSize = 640;
        private const float PanelSize = 320f;
        private const float FrameMargin = 12f;
        private const float MarginFromCorner = 20f;
        private const float FollowOrthographicSize = 22f;
        private const float LabelHeight = 26f;
        private const float PlayerMarkerSize = 14f;

        // マップ全体の外接矩形の半分に対して掛ける倍率。1より小さくすることで少し寄った全体表示にする
        private const float OverviewZoomFactor = 0.75f;
        private const float FallbackOverviewOrthographicSize = 45f;

        private static readonly Color FrameColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color LabelBackgroundColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color CameraBackgroundColor = new Color(0.09f, 0.12f, 0.18f, 1f);
        private static readonly Color PlayerMarkerColor = new Color(0.2f, 0.55f, 1f, 1f);

        [MenuItem("Tools/UI/Build Minimap")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault(c => c.name == CanvasName);
            if (canvas == null)
            {
                Debug.LogError($"シーン内に \"{CanvasName}\" という名前のCanvasが見つかりません。");
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("シーン内にPlayerタグのオブジェクトが見つかりません。");
                return;
            }

            int minimapLayer = LayerMask.NameToLayer(MinimapLayerName);
            if (minimapLayer < 0)
            {
                Debug.LogError($"\"{MinimapLayerName}\" レイヤーが見つかりません。Project Settings > Tags and Layers に追加してください。");
                return;
            }

            ExcludeLayerFromMainCamera(minimapLayer);
            AssignTerrainLayer();

            var renderTexture = LoadOrCreateRenderTexture();

            Vector2 overviewCenter;
            float overviewOrthographicSize;
            if (TryComputeMapBounds(out var mapCenter, out var mapSize))
            {
                overviewCenter = mapCenter;
                overviewOrthographicSize = Mathf.Max(mapSize.x, mapSize.y) / 2f * OverviewZoomFactor;
            }
            else
            {
                Debug.LogWarning("マップ全体の範囲を計算できなかったため、全体表示モードのサイズは仮の値を使用します。");
                overviewCenter = player.transform.position;
                overviewOrthographicSize = FallbackOverviewOrthographicSize;
            }

            var (modeLabel, mapArea) = BuildMinimapPanel(canvas.transform, renderTexture);
            var cameraGo = BuildMinimapCamera(player, renderTexture, modeLabel, minimapLayer, overviewCenter, overviewOrthographicSize);
            ConfigureMarkerLayer(mapArea, cameraGo.GetComponent<UnityEngine.Camera>());
            ConfigurePlayerMarker(player);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("ミニマップの生成が完了しました。");
        }

        // ミニマップ専用レイヤーのオブジェクト(プレイヤー強調マーカー)が、通常のゲーム画面には映り込まないようにする
        private static void ExcludeLayerFromMainCamera(int minimapLayer)
        {
            var mainCameraGo = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCameraGo == null || !mainCameraGo.TryGetComponent<UnityEngine.Camera>(out var mainCamera))
            {
                Debug.LogWarning("MainCameraタグのカメラが見つからないため、ミニマップ専用レイヤーの除外設定をスキップしました。");
                return;
            }

            mainCamera.cullingMask &= ~(1 << minimapLayer);
        }

        // このシーンの地形はTilemapの見た目(床・壁とも)が"Default"レイヤーのまま作られており、
        // "Wall"レイヤーは境界コライダー(MapBoundary、見た目なし)専用になっている。
        // ミニマップに地形の見た目を映すため、Tilemapの見た目もまとめて"Wall"レイヤーに寄せる。
        // いずれのTilemapもTilemapCollider2Dを持たないため、物理判定への影響はない。
        // Main CameraのCullingMaskは元々"Wall"を含む(Everything)ため、通常のゲーム画面の見た目も変わらない
        private static void AssignTerrainLayer()
        {
            int wallLayer = LayerMask.NameToLayer(WallLayerName);
            if (wallLayer < 0)
            {
                return;
            }

            var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            foreach (var tilemap in tilemaps)
            {
                tilemap.gameObject.layer = wallLayer;
            }
        }

        // プレイヤーに青ドット用のMinimapEntityMarkerを付与する。敵側はEnemyBase.Awakeで自動的に(既定色の赤で)付く
        private static void ConfigurePlayerMarker(GameObject player)
        {
            if (!player.TryGetComponent<MinimapEntityMarker>(out var marker))
            {
                marker = player.AddComponent<MinimapEntityMarker>();
            }

            var so = new SerializedObject(marker);
            so.FindProperty("_color").colorValue = PlayerMarkerColor;
            so.FindProperty("_size").floatValue = PlayerMarkerSize;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ミニマップパネル内にMinimapMarkerLayerを設置し、ミニマップカメラとRawImageの矩形を紐づける
        private static void ConfigureMarkerLayer(RectTransform mapArea, UnityEngine.Camera minimapCamera)
        {
            if (!mapArea.TryGetComponent<MinimapMarkerLayer>(out var layer))
            {
                layer = mapArea.gameObject.AddComponent<MinimapMarkerLayer>();
            }

            var so = new SerializedObject(layer);
            so.FindProperty("_minimapCamera").objectReferenceValue = minimapCamera;
            so.FindProperty("_mapArea").objectReferenceValue = mapArea;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // シーン内の全Tilemapのセル範囲からワールド座標での外接矩形を求める(床・壁を問わず全て含める)
        private static bool TryComputeMapBounds(out Vector2 center, out Vector2 size)
        {
            var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

            bool hasBounds = false;
            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;

            foreach (var tilemap in tilemaps)
            {
                tilemap.CompressBounds();
                if (tilemap.GetUsedTilesCount() == 0)
                {
                    continue;
                }

                var cellBounds = tilemap.cellBounds;
                var worldMin = tilemap.CellToWorld(new Vector3Int(cellBounds.xMin, cellBounds.yMin, 0));
                var worldMax = tilemap.CellToWorld(new Vector3Int(cellBounds.xMax, cellBounds.yMax, 0));

                if (!hasBounds)
                {
                    min = worldMin;
                    max = worldMax;
                    hasBounds = true;
                }
                else
                {
                    min = Vector3.Min(min, worldMin);
                    max = Vector3.Max(max, worldMax);
                }
            }

            if (!hasBounds)
            {
                center = Vector2.zero;
                size = Vector2.zero;
                return false;
            }

            center = new Vector2((min.x + max.x) / 2f, (min.y + max.y) / 2f);
            size = new Vector2(max.x - min.x, max.y - min.y);
            return true;
        }

        private static RenderTexture LoadOrCreateRenderTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (existing != null)
            {
                if (existing.width != RenderTextureSize || existing.height != RenderTextureSize)
                {
                    existing.Release();
                    existing.width = RenderTextureSize;
                    existing.height = RenderTextureSize;
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssets();
                }

                return existing;
            }

            if (!AssetDatabase.IsValidFolder(RenderTextureFolder))
            {
                Directory.CreateDirectory(RenderTextureFolder);
            }

            var renderTexture = new RenderTexture(RenderTextureSize, RenderTextureSize, 16)
            {
                name = "MinimapRenderTexture",
                filterMode = FilterMode.Bilinear,
            };

            AssetDatabase.CreateAsset(renderTexture, RenderTexturePath);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        }

        private static GameObject BuildMinimapCamera(GameObject player, RenderTexture renderTexture, Text modeLabel, int minimapLayer, Vector2 overviewCenter, float overviewOrthographicSize)
        {
            var existing = GameObject.Find(CameraObjectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var cameraGo = new GameObject(CameraObjectName, typeof(UnityEngine.Camera));
            cameraGo.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);

            var camera = cameraGo.GetComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.orthographicSize = FollowOrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBackgroundColor;
            camera.targetTexture = renderTexture;
            camera.depth = -2;
            camera.cullingMask = BuildCullingMask(minimapLayer);

            var controller = cameraGo.AddComponent<MinimapController>();
            var so = new SerializedObject(controller);
            so.FindProperty("_player").objectReferenceValue = player;
            so.FindProperty("_modeLabel").objectReferenceValue = modeLabel;
            so.FindProperty("_followOrthographicSize").floatValue = FollowOrthographicSize;
            so.FindProperty("_overviewCenter").vector2Value = overviewCenter;
            so.FindProperty("_overviewOrthographicSize").floatValue = overviewOrthographicSize;
            so.ApplyModifiedPropertiesWithoutUndo();

            return cameraGo;
        }

        // マップ地形("Wall")とミニマップ専用ドット("Minimap")のみを映す。Player/Enemyの実体、攻撃範囲予告、
        // 弾、床・装飾タイルマップ(いずれもDefaultレイヤー)は描画対象から外れる
        private static int BuildCullingMask(int minimapLayer)
        {
            int mask = 1 << minimapLayer;

            int wallLayer = LayerMask.NameToLayer(WallLayerName);
            if (wallLayer >= 0)
            {
                mask |= 1 << wallLayer;
            }
            else
            {
                Debug.LogWarning($"\"{WallLayerName}\" レイヤーが見つからないため、ミニマップに壁の地形を表示できません。");
            }

            return mask;
        }

        private static (Text ModeLabel, RectTransform MapArea) BuildMinimapPanel(Transform canvasTransform, RenderTexture renderTexture)
        {
            var existingPanel = canvasTransform.Find(PanelName);
            if (existingPanel != null)
            {
                Object.DestroyImmediate(existingPanel.gameObject);
            }

            var frame = CreateUIObject(PanelName, canvasTransform);
            var frameRect = frame.GetComponent<RectTransform>();
            AnchorTopLeft(frameRect, new Vector2(MarginFromCorner, -MarginFromCorner), new Vector2(PanelSize + FrameMargin, PanelSize + FrameMargin));
            var frameImage = frame.AddComponent<Image>();
            frameImage.color = FrameColor;

            var rawImageGo = CreateUIObject("MinimapImage", frame.transform);
            var rawImageRect = rawImageGo.GetComponent<RectTransform>();
            AnchorFillWithMargin(rawImageRect, FrameMargin / 2f);
            var rawImage = rawImageGo.AddComponent<RawImage>();
            rawImage.texture = renderTexture;
            rawImage.raycastTarget = false;

            var labelBackgroundGo = CreateUIObject("ModeLabelBackground", frame.transform);
            var labelBackgroundRect = labelBackgroundGo.GetComponent<RectTransform>();
            AnchorBottomStretch(labelBackgroundRect, LabelHeight);
            var labelBackgroundImage = labelBackgroundGo.AddComponent<Image>();
            labelBackgroundImage.color = LabelBackgroundColor;

            var labelTextGo = CreateUIObject("ModeLabelText", labelBackgroundGo.transform);
            var labelTextRect = labelTextGo.GetComponent<RectTransform>();
            AnchorFillWithMargin(labelTextRect, 0f);
            var modeLabel = AddText(labelTextGo, "現在地 (M)", 16, Color.white, TextAnchor.MiddleCenter);

            return (modeLabel, rawImageRect);
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Text AddText(GameObject go, string content, int fontSize, Color color, TextAnchor alignment)
        {
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void AnchorTopLeft(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void AnchorFillWithMargin(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        private static void AnchorBottomStretch(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, FrameMargin / 2f);
            rect.sizeDelta = new Vector2(-FrameMargin, height);
        }
    }
}
