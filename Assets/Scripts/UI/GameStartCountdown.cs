using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Team1.UI
{
    /// <summary>
    /// GameScene開始直後にカウントダウン演出を挟み、いきなり動き出す唐突さを防ぐ。
    /// 演出中はTime.timeScaleを0にして、プレイヤー操作や敵の動きを止める。
    /// </summary>
    public class GameStartCountdown : MonoBehaviour
    {
        private static readonly string[] JapaneseFontNames = { "Yu Gothic UI", "Yu Gothic", "Meiryo", "MS Gothic" };

        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _countdownText;

        [Header("演出設定")]
        [SerializeField] private string[] _countdownLabels = { "3", "2", "1", "スタート！" };
        [SerializeField] private float _secondsPerLabel = 1f;
        [SerializeField] private float _startLabelHoldSeconds = 0.5f;

        private void Awake()
        {
            Debug.Assert(_panel != null, $"{nameof(_panel)} is not assigned.", this);
            Debug.Assert(_countdownText != null, $"{nameof(_countdownText)} is not assigned.", this);

            ApplyJapaneseFont();

            // 演出が終わるまで敵・プレイヤーの動きを止める
            Time.timeScale = 0f;

            if (_panel != null)
            {
                _panel.SetActive(true);
            }
        }

        private void Start()
        {
            StartCoroutine(RunCountdown());
        }

        private void OnDestroy()
        {
            // 演出の途中でシーン遷移等が起きても、timeScaleが0のまま残らないようにする
            Time.timeScale = 1f;
        }

        private IEnumerator RunCountdown()
        {
            for (int i = 0; i < _countdownLabels.Length; i++)
            {
                bool isLast = i == _countdownLabels.Length - 1;
                if (_countdownText != null)
                {
                    _countdownText.text = _countdownLabels[i];
                }

                yield return WaitRealtime(isLast ? _startLabelHoldSeconds : _secondsPerLabel);
            }

            Time.timeScale = 1f;

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private static IEnumerator WaitRealtime(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Time.timeScale = 0中でも一定速度で演出を進めるためUnscaledDeltaTimeを使う
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void ApplyJapaneseFont()
        {
            if (_countdownText == null)
            {
                return;
            }

            var font = Font.CreateDynamicFontFromOSFont(JapaneseFontNames, 32);
            if (font != null)
            {
                _countdownText.font = font;
            }
        }
    }
}
