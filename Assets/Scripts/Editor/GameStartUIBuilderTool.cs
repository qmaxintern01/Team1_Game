using System.Linq;
using Team1.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Team1.EditorTools
{
    /// <summary>
    /// GameScene内の"HP"Canvas配下に、開始カウントダウンパネル(GameStartCountdown)を生成するエディタ専用ツール。
    /// Tools > UI > Build Game Start UI から実行する。
    /// </summary>
    public static class GameStartUIBuilderTool
    {
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private const string CanvasName = "HP";
        private const string ControllerName = "GameStartCountdown";
        private const string PanelName = "Panel";

        [MenuItem("Tools/UI/Build Game Start UI")]
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
            // ほかのUI(HPバー等)より手前に表示されるよう末尾に配置する
            controllerGo.transform.SetAsLastSibling();

            var panel = CreateUIObject(PanelName, controllerGo.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.6f);

            var textGo = CreateUIObject("CountdownText", panel.transform);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(600f, 200f);
            var countdownText = AddText(textGo, "3", 96, Color.white, TextAnchor.MiddleCenter);

            var countdown = controllerGo.AddComponent<GameStartCountdown>();
            var so = new SerializedObject(countdown);
            so.FindProperty("_panel").objectReferenceValue = panel;
            so.FindProperty("_countdownText").objectReferenceValue = countdownText;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("ゲーム開始カウントダウンUIの生成が完了しました。");
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
    }
}
