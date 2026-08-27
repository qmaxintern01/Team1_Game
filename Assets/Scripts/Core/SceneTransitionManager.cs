using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Team1
{
    /// <summary>
    /// シーン遷移時にフェード演出を挟む。DontDestroyOnLoadで生存させ、
    /// どのシーンから再生を始めてもRuntimeInitializeOnLoadMethodで自動生成する。
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        private const float DefaultFadeDuration = 0.5f;
        private const float BlackHoldDuration = 0.15f;
        private const int FadeCanvasSortingOrder = 9999;

        private static SceneTransitionManager instance;

        private CanvasGroup canvasGroup;
        private bool isTransitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureExists()
        {
            if (instance != null)
            {
                return;
            }

            var go = new GameObject(nameof(SceneTransitionManager));
            instance = go.AddComponent<SceneTransitionManager>();
        }

        public static void LoadScene(string sceneName, float fadeDuration = DefaultFadeDuration)
        {
            EnsureExists();
            instance.StartCoroutine(instance.FadeAndLoad(sceneName, fadeDuration));
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            BuildFadeCanvas();
        }

        private void BuildFadeCanvas()
        {
            var canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(transform);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = FadeCanvasSortingOrder;
            canvasGo.AddComponent<CanvasScaler>();

            var imageGo = new GameObject("FadeImage");
            imageGo.transform.SetParent(canvasGo.transform, worldPositionStays: false);

            var image = imageGo.AddComponent<Image>();
            image.color = Color.black;

            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        private IEnumerator FadeAndLoad(string sceneName, float fadeDuration)
        {
            if (isTransitioning)
            {
                yield break;
            }

            isTransitioning = true;
            canvasGroup.blocksRaycasts = true;

            yield return Fade(0f, 1f, fadeDuration);

            // 新シーンのAwake/Start/Updateが暗転中に進んでしまい、
            // フェードインした瞬間には既に動いている「唐突さ」を防ぐため、
            // 読み込みからフェードイン完了まではゲーム内時間を止める。
            // ここで1fに戻すと、遷移先シーンが独自にtimeScaleを0に保持したい場合
            // (例: GameStartCountdownの開始演出)を上書きしてしまうため、
            // timeScaleを1に戻す責任は各シーンの入り口側コンポーネントに委ねる。
            Time.timeScale = 0f;

            var loadOperation = SceneManager.LoadSceneAsync(sceneName);
            while (loadOperation != null && !loadOperation.isDone)
            {
                yield return null;
            }

            yield return WaitRealtime(BlackHoldDuration);
            yield return Fade(1f, 0f, fadeDuration);

            canvasGroup.blocksRaycasts = false;
            isTransitioning = false;
        }

        private static IEnumerator WaitRealtime(float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                canvasGroup.alpha = to;
                yield break;
            }

            // Time.timeScaleが0の場面（ポーズ/ゲームオーバー中）から呼ばれても動くよう無視して進める
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = to;
        }
    }
}
