using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Team1.UI
{
    /// <summary>
    /// Escキー(UIアクションマップのCancel)でポーズ画面の表示/非表示を切り替える。
    /// </summary>
    public class PauseUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _titleButton;

        private InputSystem_Actions _gameInputs;
        private bool _isPaused;

        private void Awake()
        {
            Debug.Assert(_panel != null, $"{nameof(_panel)} is not assigned.", this);
            Debug.Assert(_continueButton != null, $"{nameof(_continueButton)} is not assigned.", this);
            Debug.Assert(_titleButton != null, $"{nameof(_titleButton)} is not assigned.", this);

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            _gameInputs = new InputSystem_Actions();
            _gameInputs.Enable();
            _gameInputs.UI.Cancel.performed += HandleCancelInput;

            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(Resume);
            }

            if (_titleButton != null)
            {
                _titleButton.onClick.AddListener(GoToTitle);
            }
        }

        private void OnDisable()
        {
            _gameInputs.UI.Cancel.performed -= HandleCancelInput;
            _gameInputs.Disable();
            _gameInputs.Dispose();

            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(Resume);
            }

            if (_titleButton != null)
            {
                _titleButton.onClick.RemoveListener(GoToTitle);
            }
        }

        private void HandleCancelInput(InputAction.CallbackContext context)
        {
            if (_isPaused)
            {
                Resume();
                return;
            }

            // ゲーム開始演出やゲームオーバーなど、ポーズ以外の理由でtimeScaleが0の間はポーズを開かない
            if (Time.timeScale <= 0f)
            {
                return;
            }

            Pause();
        }

        private void Pause()
        {
            _isPaused = true;

            if (_panel != null)
            {
                _panel.SetActive(true);
            }

            Time.timeScale = 0f;
        }

        private void Resume()
        {
            _isPaused = false;

            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            Time.timeScale = 1f;
        }

        private void GoToTitle()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            SceneTransitionManager.LoadScene("TitleScene");
        }
    }
}
