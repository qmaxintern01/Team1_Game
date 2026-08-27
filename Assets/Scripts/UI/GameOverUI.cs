using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Team1.UI
{
    /// <summary>
    /// プレイヤーのHPが0になったらゲームオーバーパネルを表示する。
    /// パネル配下のTextは、日本語表示のためOS標準フォントへ差し替える。
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        private static readonly string[] JapaneseFontNames = { "Yu Gothic UI", "Yu Gothic", "Meiryo", "MS Gothic" };

        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _titleButton;

        private Health _health;

        private void Awake()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            _health = player != null ? player.GetComponent<Health>() : null;

            Debug.Assert(_health != null, $"{nameof(_health)} is not assigned.", this);
            Debug.Assert(_panel != null, $"{nameof(_panel)} is not assigned.", this);
            Debug.Assert(_retryButton != null, $"{nameof(_retryButton)} is not assigned.", this);
            Debug.Assert(_titleButton != null, $"{nameof(_titleButton)} is not assigned.", this);

            ApplyJapaneseFont();

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnDied += HandleGameOver;
            }

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
            if (_health != null)
            {
                _health.OnDied -= HandleGameOver;
            }

            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(Retry);
            }

            if (_titleButton != null)
            {
                _titleButton.onClick.RemoveListener(GoToTitle);
            }
        }

        private void HandleGameOver()
        {
            if (_panel != null)
            {
                _panel.SetActive(true);
            }

            Time.timeScale = 0f;
        }

        private void Retry()
        {
            Time.timeScale = 1f;
            SceneTransitionManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void GoToTitle()
        {
            Time.timeScale = 1f;
            SceneTransitionManager.LoadScene("TitleScene");
        }

        private void ApplyJapaneseFont()
        {
            if (_panel == null)
            {
                return;
            }

            var font = Font.CreateDynamicFontFromOSFont(JapaneseFontNames, 32);
            if (font == null)
            {
                return;
            }

            var texts = _panel.GetComponentsInChildren<Text>(includeInactive: true);
            foreach (var text in texts)
            {
                text.font = font;
            }
        }
    }
}
