using System.IO;
using System.Linq;
using Team1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Team1.EditorTools
{
    /// <summary>
    /// GameScene内の"HP"Canvas配下に、画面左上に表示するミニマップUIを生成するエディタ専用ツール。
    /// プレイヤーに追従する専用カメラをRenderTextureに描画し、それをRawImageで表示する。
    /// Tools > UI > Build Minimap から実行する。既存の生成物があれば作り直す。
    /// </summary>
    public static class MinimapBuilderTool
    {
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private const string CanvasName = "HP";
        private const string PanelName = "MinimapPanel";
        private const string CameraObjectName = "MinimapCamera";
        private const string RenderTextureFolder = "Assets/Art/UI";
        private const string RenderTexturePath = RenderTextureFolder + "/MinimapRenderTexture.renderTexture";

        private const float PanelSize = 220f;
        private const float FrameMargin = 8f;
        private const float MarginFromCorner = 16f;
        private const float OrthographicSize = 25f;

        private static readonly Color FrameColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color CameraBackgroundColor = new Color(0.09f, 0.12f, 0.18f, 1f);

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

            var renderTexture = LoadOrCreateRenderTexture();

            var minimapCamera = BuildMinimapCamera(player, renderTexture);
            BuildMinimapPanel(canvas.transform, renderTexture);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("ミニマップの生成が完了しました。");
        }

        private static RenderTexture LoadOrCreateRenderTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.IsValidFolder(RenderTextureFolder))
            {
                Directory.CreateDirectory(RenderTextureFolder);
            }

            var renderTexture = new RenderTexture(512, 512, 16)
            {
                name = "MinimapRenderTexture",
                filterMode = FilterMode.Bilinear,
            };

            AssetDatabase.CreateAsset(renderTexture, RenderTexturePath);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
        }

        private static GameObject BuildMinimapCamera(GameObject player, RenderTexture renderTexture)
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
            camera.orthographicSize = OrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBackgroundColor;
            camera.targetTexture = renderTexture;
            camera.depth = -2;

            var follow = cameraGo.AddComponent<MinimapFollow>();
            var so = new SerializedObject(follow);
            so.FindProperty("_player").objectReferenceValue = player;
            so.ApplyModifiedPropertiesWithoutUndo();

            return cameraGo;
        }

        private static void BuildMinimapPanel(Transform canvasTransform, RenderTexture renderTexture)
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
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
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
    }
}
