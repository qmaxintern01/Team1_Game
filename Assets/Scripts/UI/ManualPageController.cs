using Team1;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Team1.UI
{
    // TutorialScene上のManual_1/Manual_2をスペースキー/左クリックで切り替え、Manual_2の状態で押すとGameSceneへ遷移する
    public class ManualPageController : MonoBehaviour
    {
        [SerializeField] private GameObject _manual1;
        [SerializeField] private GameObject _manual2;

        private int _currentPage;

        private void Awake()
        {
            Debug.Assert(_manual1 != null, $"{nameof(_manual1)} is not assigned.", this);
            Debug.Assert(_manual2 != null, $"{nameof(_manual2)} is not assigned.", this);

            ShowPage(0);
        }

        private void Update()
        {
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool leftClickPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

            if (spacePressed || leftClickPressed)
            {
                AdvancePage();
            }
        }

        private void AdvancePage()
        {
            if (_currentPage == 0)
            {
                ShowPage(1);
                return;
            }

            SceneTransitionManager.LoadScene("GameScene");
        }

        private void ShowPage(int page)
        {
            _currentPage = page;

            if (_manual1 != null)
            {
                _manual1.SetActive(page == 0);
            }

            if (_manual2 != null)
            {
                _manual2.SetActive(page == 1);
            }
        }
    }
}
