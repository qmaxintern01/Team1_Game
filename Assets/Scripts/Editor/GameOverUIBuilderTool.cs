using System.Linq;
using Team1.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Team1.EditorTools
{
    /// <summary>
    /// GameScene内の"HP"Canvas配下に、ゲームオーバーパネル(GameOverUI)を生成するエディタ専用ツール。
    /// Tools > UI > Build Game Over UI から実行する。
    /// </summary>
    public static class GameOverUIBuilderTool
    {
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private const string CanvasName = "HP";
        private const string ControllerName = "GameOverUI";
        private const string PanelName = "Panel";

        [MenuItem("Tools/UI/Build Game Over UI")]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault(c => c.name == CanvasName);
            if (canvas == null)
            {
                Debug.LogError($"シーン内に \"{CanvasName}\" という名前のCanvasが見つかりません。");
                return;
            }

            var existingController = canvas.transform.Find(ControllerName);
            if (existingController != null)
            {
                Object.DestroyImmediate(existingController.gameObject);
            }

            var controllerGo = CreateUIObject(ControllerName, canvas.transform);

            var panel = CreateUIObject(PanelName, controllerGo.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.8f);

            var titleGo = CreateUIObject("GameOverText", panel.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 80f);
            titleRect.sizeDelta = new Vector2(600f, 100f);
            AddText(titleGo, "ゲームオーバー", 48, Color.white, TextAnchor.MiddleCenter);

            var retryButton = CreateButton(panel.transform, "RetryButton", "リトライ", new Vector2(-90f, -60f));
            var titleButton = CreateButton(panel.transform, "TitleButton", "タイトルへ", new Vector2(90f, -60f));

            var gameOverUi = controllerGo.AddComponent<GameOverUI>();
            var so = new SerializedObject(gameOverUi);
            so.FindProperty("_panel").objectReferenceValue = panel;
            so.FindProperty("_retryButton").objectReferenceValue = retryButton;
            so.FindProperty("_titleButton").objectReferenceValue = titleButton;
            so.ApplyModifiedProperties();

            panel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("ゲームオーバーUIの生成が完了しました。");
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
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            var go = CreateUIObject(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(160f, 48f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            button.colors = colors;

            var textGo = CreateUIObject("Text", go.transform);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            AddText(textGo, label, 24, Color.white, TextAnchor.MiddleCenter);

            return button;
        }
    }
}
