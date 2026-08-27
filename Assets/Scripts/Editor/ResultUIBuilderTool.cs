using Team1.Result;
using Team1.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Team1.EditorTools
{
    /// <summary>
    /// ResultScene内にリザルト画面のUI一式(Canvas, ランク表示, 総合ゲージ, 内訳ゲージ, ボタン)を生成するエディタ専用ツール。
    /// Tools > UI > Build Result UI から実行する。GameSceneには一切触れない。
    /// </summary>
    public static class ResultUIBuilderTool
    {
        private const string ResultScenePath = "Assets/Scenes/ResultScene.unity";
        private const string CanvasName = "ResultCanvas";
        private const string ControllerName = "ResultController";
        private const string ConfigAssetPath = "Assets/Data/ResultScoreConfig.asset";
        private const string BuiltinFontPath = "LegacyRuntime.ttf";

        [MenuItem("Tools/UI/Build Result UI")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);

            foreach (var existing in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (existing.name == CanvasName)
                {
                    Object.DestroyImmediate(existing.gameObject);
                }
            }

            var canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                // プロジェクトのActive Input HandlingはInput System Package(New)のみのため、
                // 旧Input Manager前提のStandaloneInputModuleではなくInputSystemUIInputModuleを使う
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            var background = CreateUIObject("Background", canvasGo.transform);
            StretchFull(background);
            background.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.95f);

            var controllerGo = CreateUIObject(ControllerName, canvasGo.transform);
            StretchFull(controllerGo);

            // ランク表示
            var rankPanel = CreateUIObject("RankPanel", controllerGo.transform);
            var rankPanelRect = rankPanel.GetComponent<RectTransform>();
            rankPanelRect.anchorMin = new Vector2(0.5f, 1f);
            rankPanelRect.anchorMax = new Vector2(0.5f, 1f);
            rankPanelRect.anchoredPosition = new Vector2(0f, -160f);
            rankPanelRect.sizeDelta = new Vector2(900f, 220f);

            var rankText = CreateTextObject(rankPanel.transform, "RankText", "S", 140, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(300f, 160f));
            var titleText = CreateTextObject(rankPanel.transform, "TitleText", "称号", 32, new Color(0.95f, 0.85f, 0.6f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(900f, 60f));

            // 総合ゲージ
            var totalGaugePanel = CreateUIObject("TotalGaugePanel", controllerGo.transform);
            var totalGaugeRect = totalGaugePanel.GetComponent<RectTransform>();
            totalGaugeRect.anchorMin = new Vector2(0.5f, 0.62f);
            totalGaugeRect.anchorMax = new Vector2(0.5f, 0.62f);
            totalGaugeRect.anchoredPosition = Vector2.zero;
            totalGaugeRect.sizeDelta = new Vector2(900f, 70f);

            var totalGaugeBg = CreateUIObject("Background", totalGaugePanel.transform);
            StretchFull(totalGaugeBg);
            totalGaugeBg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 1f);

            var totalGaugeFill = CreateFilledImage(totalGaugePanel.transform, new Color(0.95f, 0.55f, 0.15f, 1f));
            var totalScoreText = CreateTextObject(totalGaugePanel.transform, "ScoreText", "総合スコア 0", 28, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 70f));

            // 内訳(5項目)
            var breakdownPanel = CreateUIObject("BreakdownPanel", controllerGo.transform);
            var breakdownRect = breakdownPanel.GetComponent<RectTransform>();
            breakdownRect.anchorMin = new Vector2(0.5f, 0.62f);
            breakdownRect.anchorMax = new Vector2(0.5f, 0.62f);
            breakdownRect.anchoredPosition = new Vector2(0f, -80f);
            breakdownRect.sizeDelta = new Vector2(900f, 260f);

            var (oilFill, oilText) = CreateBreakdownRow(breakdownPanel.transform, "OilRow", 0, "残量オイル 0", new Color(0.3f, 0.75f, 0.9f, 1f));
            var (timeFill, timeText) = CreateBreakdownRow(breakdownPanel.transform, "TimeRow", 1, "クリアタイム 0", new Color(0.5f, 0.85f, 0.4f, 1f));
            var (killFill, killText) = CreateBreakdownRow(breakdownPanel.transform, "KillRow", 2, "撃破数 0", new Color(0.85f, 0.75f, 0.3f, 1f));
            var (stylishFill, stylishText) = CreateBreakdownRow(breakdownPanel.transform, "StylishRow", 3, "スタイリッシュ 0", new Color(0.85f, 0.35f, 0.75f, 1f));
            var (damageFill, damageText) = CreateBreakdownRow(breakdownPanel.transform, "DamageRow", 4, "被ダメージ 0", new Color(0.9f, 0.35f, 0.35f, 1f));

            // 次回の目標
            var adviceText = CreateTextObject(controllerGo.transform, "AdviceText", "", 26, new Color(0.9f, 0.9f, 0.7f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(1100f, 50f));

            // ボタン
            var retryButton = CreateButton(controllerGo.transform, "RetryButton", "もう一度", new Vector2(-140f, 40f));
            var titleButton = CreateButton(controllerGo.transform, "TitleButton", "タイトルへ", new Vector2(140f, 40f));

            var config = LoadOrCreateConfig();

            var resultController = controllerGo.AddComponent<ResultController>();
            var so = new SerializedObject(resultController);
            so.FindProperty("_config").objectReferenceValue = config;
            so.FindProperty("_rankText").objectReferenceValue = rankText;
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_totalGaugeFill").objectReferenceValue = totalGaugeFill;
            so.FindProperty("_totalScoreText").objectReferenceValue = totalScoreText;
            so.FindProperty("_oilGaugeFill").objectReferenceValue = oilFill;
            so.FindProperty("_oilScoreText").objectReferenceValue = oilText;
            so.FindProperty("_timeGaugeFill").objectReferenceValue = timeFill;
            so.FindProperty("_timeScoreText").objectReferenceValue = timeText;
            so.FindProperty("_killGaugeFill").objectReferenceValue = killFill;
            so.FindProperty("_killScoreText").objectReferenceValue = killText;
            so.FindProperty("_stylishGaugeFill").objectReferenceValue = stylishFill;
            so.FindProperty("_stylishScoreText").objectReferenceValue = stylishText;
            so.FindProperty("_damageGaugeFill").objectReferenceValue = damageFill;
            so.FindProperty("_damageScoreText").objectReferenceValue = damageText;
            so.FindProperty("_adviceText").objectReferenceValue = adviceText;
            so.FindProperty("_retryButton").objectReferenceValue = retryButton;
            so.FindProperty("_titleButton").objectReferenceValue = titleButton;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("リザルトUIの生成が完了しました。");
        }

        private static ResultScoreConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ResultScoreConfig>(ConfigAssetPath);
            if (config != null)
            {
                return config;
            }

            const string folder = "Assets/Data";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            config = ScriptableObject.CreateInstance<ResultScoreConfig>();
            AssetDatabase.CreateAsset(config, ConfigAssetPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text CreateTextObject(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Color color,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var go = CreateUIObject(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(BuiltinFontPath);
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Image CreateFilledImage(Transform parent, Color color)
        {
            var go = CreateUIObject("Fill", parent);
            StretchFull(go);
            var image = go.AddComponent<Image>();
            // spriteが未設定のままだとImageはType(Filled等)を無視して常に矩形全体を描画してしまうため、
            // fillAmountを機能させるにはsprite割り当てが必須
            image.sprite = GetFillSprite();
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 0f;
            return image;
        }

        private static Sprite GetFillSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static (Image fill, Text text) CreateBreakdownRow(Transform parent, string name, int index, string label, Color color)
        {
            var row = CreateUIObject(name, parent);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 1f);
            rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -index * 48f);
            rowRect.sizeDelta = new Vector2(900f, 36f);

            var bg = CreateUIObject("Background", row.transform);
            StretchFull(bg);
            bg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 1f);

            var fill = CreateFilledImage(row.transform, color);

            var text = CreateStretchedText(row.transform, label, 20, Color.white, TextAnchor.MiddleLeft, 16f, 16f);

            return (fill, text);
        }

        private static Text CreateStretchedText(Transform parent, string content, int fontSize, Color color, TextAnchor alignment, float paddingLeft, float paddingRight)
        {
            var go = CreateUIObject("Text", parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(paddingLeft, 0f);
            rect.offsetMax = new Vector2(-paddingRight, 0f);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(BuiltinFontPath);
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            var go = CreateUIObject(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(220f, 56f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            button.colors = colors;

            var textGo = CreateUIObject("Text", go.transform);
            StretchFull(textGo);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(BuiltinFontPath);
            text.text = label;
            text.fontSize = 26;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            return button;
        }
    }
}
