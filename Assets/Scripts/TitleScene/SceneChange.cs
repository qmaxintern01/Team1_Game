using Team1;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    private InputSystem_Actions _gameInputs;

    private void Awake()
    {
        // ゲームオーバー等でTime.timeScaleが0のまま遷移してきても、タイトル操作が止まらないようにする
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        _gameInputs = new InputSystem_Actions();
        _gameInputs.Enable();
        _gameInputs.UI.Cancel.performed += HandleCancelInput;
    }

    private void OnDisable()
    {
        _gameInputs.UI.Cancel.performed -= HandleCancelInput;
        _gameInputs.Disable();
        _gameInputs.Dispose();
    }

    public void EnterGame()
    {
        SceneTransitionManager.LoadScene("TutorialScene");
    }

    private void HandleCancelInput(InputAction.CallbackContext context)
    {
        QuitGame();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
