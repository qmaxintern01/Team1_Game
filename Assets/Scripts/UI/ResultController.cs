using Team1.Result;
using UnityEngine;
using UnityEngine.UI;

namespace Team1.UI
{
    /// <summary>
    /// ResultSceneのUIを制御する。
    /// RunResultStore.Currentに実績データがあればそれを使用し、
    /// 無ければ_debugDataのダミー値でリザルト画面単体の動作確認ができるようにしている。
    /// </summary>
    public class ResultController : MonoBehaviour
    {
        private static readonly string[] JapaneseFontNames = { "Yu Gothic UI", "Yu Gothic", "Meiryo", "MS Gothic" };

        private static readonly Color[] RankColors =
        {
            new Color(0.55f, 0.55f, 0.55f), // D
            new Color(0.45f, 0.75f, 0.4f),  // C
            new Color(0.3f, 0.6f, 0.9f),    // B
            new Color(0.95f, 0.75f, 0.2f),  // A
            new Color(0.95f, 0.3f, 0.3f),   // S
        };

        [SerializeField] private ResultScoreConfig _config;

        [Tooltip("RunResultStore.Currentが未設定の場合に使うデバッグ用データ")]
        [SerializeField]
        private RunResultData _debugData = new RunResultData
        {
            RemainingOil = 120,
            MaxOil = 200,
            ClearTimeSeconds = 150f,
            WeakKillCount = 18,
            MidBossKillCount = 2,
            KnifeKillCount = 6,
            BackstabKillCount = 3,
            DamageTaken = 25,
        };

        [Header("ランク表示")]
        [SerializeField] private Text _rankText;
        [SerializeField] private Text _titleText;

        [Header("総合ゲージ")]
        [SerializeField] private Image _totalGaugeFill;
        [SerializeField] private Text _totalScoreText;

        [Header("内訳")]
        [SerializeField] private Image _oilGaugeFill;
        [SerializeField] private Text _oilScoreText;
        [SerializeField] private Image _timeGaugeFill;
        [SerializeField] private Text _timeScoreText;
        [SerializeField] private Image _killGaugeFill;
        [SerializeField] private Text _killScoreText;
        [SerializeField] private Image _stylishGaugeFill;
        [SerializeField] private Text _stylishScoreText;
        [SerializeField] private Image _damageGaugeFill;
        [SerializeField] private Text _damageScoreText;

        [Header("次回の目標")]
        [SerializeField] private Text _adviceText;

        [Header("ボタン")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _titleButton;

        [Header("演出設定")]
        [SerializeField] private float _gaugeAnimationDuration = 1.2f;

        private void Awake()
        {
            // ゲームオーバー等でTime.timeScaleが0のまま遷移してきても、リザルト演出が止まらないようにする
            Time.timeScale = 1f;

            ApplyJapaneseFont();

            Debug.Assert(_config != null, $"{nameof(_config)} is not assigned.", this);
            Debug.Assert(_rankText != null, $"{nameof(_rankText)} is not assigned.", this);
            Debug.Assert(_retryButton != null, $"{nameof(_retryButton)} is not assigned.", this);
            Debug.Assert(_titleButton != null, $"{nameof(_titleButton)} is not assigned.", this);
        }

        private void OnEnable()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.AddListener(Retry);
            }

            if (_titleButton != null)
            {
                _titleButton.onClick.AddListener(GoToTitle);
            }
        }

        private void OnDisable()
        {
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(Retry);
            }

            if (_titleButton != null)
            {
                _titleButton.onClick.RemoveListener(GoToTitle);
            }
        }

        private void Start()
        {
            if (_config == null)
            {
                return;
            }

            RunResultData data = RunResultStore.Current ?? _debugData;
            ResultScoreBreakdown result = ResultScoreCalculator.Calculate(data, _config);

            HideRankReveal();
            PlayGaugeAnimations(result);
            StartCoroutine(RevealRankAfterGaugesStop(result));
        }

        private void HideRankReveal()
        {
            if (_rankText != null)
            {
                _rankText.text = string.Empty;
            }

            if (_titleText != null)
            {
                _titleText.text = string.Empty;
            }

            if (_adviceText != null)
            {
                _adviceText.text = string.Empty;
            }
        }

        // ランク・称号・次回の目標は、全ゲージ(内訳5項目+総合)の演出が止まってから表示する
        private System.Collections.IEnumerator RevealRankAfterGaugesStop(ResultScoreBreakdown result)
        {
            yield return new WaitForSecondsRealtime(_gaugeAnimationDuration);

            if (_rankText != null)
            {
                _rankText.text = result.Rank.ToString();
                _rankText.color = RankColors[(int)result.Rank];
            }

            if (_titleText != null)
            {
                _titleText.text = result.Title;
            }

            if (_adviceText != null)
            {
                _adviceText.text = result.NextTargetAdvice;
            }
        }

        private void PlayGaugeAnimations(ResultScoreBreakdown result)
        {
            float totalMax = _config.OilScoreMax + _config.TimeScoreMax + _config.KillScoreMax + _config.StylishScoreMax + _config.DamageBonusMax;

            StartCoroutine(AnimateGauge(_totalGaugeFill, _totalScoreText, result.TotalScore, totalMax, "総合スコア"));
            StartCoroutine(AnimateGauge(_oilGaugeFill, _oilScoreText, result.OilScore, _config.OilScoreMax, "残量オイル"));
            StartCoroutine(AnimateGauge(_timeGaugeFill, _timeScoreText, result.TimeScore, _config.TimeScoreMax, "クリアタイム"));
            StartCoroutine(AnimateGauge(_killGaugeFill, _killScoreText, result.KillScore, _config.KillScoreMax, "撃破数"));
            StartCoroutine(AnimateGauge(_stylishGaugeFill, _stylishScoreText, result.StylishScore, _config.StylishScoreMax, "スタイリッシュ"));
            StartCoroutine(AnimateGauge(_damageGaugeFill, _damageScoreText, result.DamageBonus, _config.DamageBonusMax, "被ダメージ"));
        }

        private System.Collections.IEnumerator AnimateGauge(Image fill, Text label, float value, float max, string prefix)
        {
            float ratio = max > 0f ? Mathf.Clamp01(value / max) : 0f;
            float elapsed = 0f;

            while (elapsed < _gaugeAnimationDuration)
            {
                // Time.timeScaleに関わらず一定速度で演出させるためUnscaledDeltaTimeを使う
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _gaugeAnimationDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                if (fill != null)
                {
                    fill.fillAmount = ratio * eased;
                }

                if (label != null)
                {
                    label.text = $"{prefix} {Mathf.RoundToInt(value * eased)}";
                }

                yield return null;
            }

            if (fill != null)
            {
                fill.fillAmount = ratio;
            }

            if (label != null)
            {
                label.text = $"{prefix} {Mathf.RoundToInt(value)}";
            }
        }

        private void Retry()
        {
            Time.timeScale = 1f;
            RunResultStore.Clear();
            SceneTransitionManager.LoadScene("GameScene");
        }

        private void GoToTitle()
        {
            Time.timeScale = 1f;
            RunResultStore.Clear();
            SceneTransitionManager.LoadScene("TitleScene");
        }

        private void ApplyJapaneseFont()
        {
            var font = Font.CreateDynamicFontFromOSFont(JapaneseFontNames, 32);
            if (font == null)
            {
                return;
            }

            foreach (var text in GetComponentsInChildren<Text>(includeInactive: true))
            {
                text.font = font;
            }
        }
    }
}
