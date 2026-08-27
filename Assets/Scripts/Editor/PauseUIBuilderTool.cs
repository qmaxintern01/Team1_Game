using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Team1.EditorTools
{
    /// <summary>
    /// 現在開いているシーンに、Esc(UIアクションマップのCancel)で開閉するポーズ画面のCanvas一式を生成する。
    /// ui_pouse.png(見出し)、ui_gemetudukeru.png(続ける)、ui_taitolback.png(タイトルへ戻る)を
    /// 画像そのものをボタン絵として配置し、PauseUIコンポーネントの参照も自動でひも付ける。
    /// Tools > UI > Build Pause Menu から実行する。既存の生成物があれば作り直す。
    /// </summary>
    public static class PauseUIBuilderTool
    {
        private const string RootObjectName = "PauseCanvas";
        private const string ArtFolder = "Assets/Art/UI";
        private const string HeaderSpritePath = ArtFolder + "/ui_pouse.png";
        private const string ContinueSpritePath = ArtFolder + "/ui_gemetudukeru.png";
        private const string TitleBackSpritePath = ArtFolder + "/ui_taitolback.png";

        private const float HeaderWidth = 500f;
        private const float ButtonWidth = 420f;

        // 単色の黒だと彩度が無く印象が薄いため、金色のボタン画像と馴染む彩度のある濃紫にする
        private static readonly Color PanelBackgroundColor = new Color(0.12f, 0.03f, 0.20f, 0.75f);

        [MenuItem("Tools/UI/Build Pause Menu")]
        public static void Build()
        {
            var headerSprite = LoadUiSprite(HeaderSpritePath);
            var continueSprite = LoadUiSprite(ContinueSpritePath);
            var titleBackSprite = LoadUiSprite(TitleBackSpritePath);

            if (headerSprite == null || continueSprite == null || titleBackSprite == null)
            {
                Debug.LogError("ポーズ画面用の画像を読み込めませんでした。Assets/Art/UI 配下の画像を確認してください。");
                return;
            }

            var existingRoot = GameObject.Find(RootObjectName);
            if (existingRoot != null)
            {
                Object.DestroyImmediate(existingRoot);
            }

            var canvasGo = new GameObject(RootObjectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvasRoot = (RectTransform)canvasGo.transform;

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var panel = CreateUIObject("PausePanel", canvasRoot);
            StretchFull(panel);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = PanelBackgroundColor;
            panel.gameObject.SetActive(false);

            var header = CreateImageObject("HeaderImage", panel, headerSprite, HeaderWidth);
            AnchorTopCenter(header, new Vector2(0f, -50f));

            var continueButtonRect = CreateImageObject("ContinueButton", panel, continueSprite, ButtonWidth);
            var continueButton = continueButtonRect.gameObject.AddComponent<Button>();
            continueButton.targetGraphic = continueButtonRect.GetComponent<Image>();
            AnchorCenter(continueButtonRect, new Vector2(0f, 40f));

            var titleButtonRect = CreateImageObject("TitleButton", panel, titleBackSprite, ButtonWidth);
            var titleButton = titleButtonRect.gameObject.AddComponent<Button>();
            titleButton.targetGraphic = titleButtonRect.GetComponent<Image>();
            AnchorCenter(titleButtonRect, new Vector2(0f, -120f));

            var pauseUi = canvasGo.AddComponent<Team1.UI.PauseUI>();
            var serialized = new SerializedObject(pauseUi);
            serialized.FindProperty("_panel").objectReferenceValue = panel.gameObject;
            serialized.FindProperty("_continueButton").objectReferenceValue = continueButton;
            serialized.FindProperty("_titleButton").objectReferenceValue = titleButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("ポーズ画面を生成しました。");
        }

        // 画像未インポート(.metaなし)の場合も含め、UI用のSprite(Single)として読み込む
        private static Sprite LoadUiSprite(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"画像が見つかりません: {assetPath}");
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            return rect;
        }

        private static RectTransform CreateImageObject(string name, RectTransform parent, Sprite sprite, float targetWidth)
        {
            var rect = CreateUIObject(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;

            float aspect = sprite.rect.height / sprite.rect.width;
            rect.sizeDelta = new Vector2(targetWidth, targetWidth * aspect);
            return rect;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void AnchorTopCenter(RectTransform rect, Vector2 anchoredPosition)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
        }

        private static void AnchorCenter(RectTransform rect, Vector2 anchoredPosition)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
        }
    }
}
