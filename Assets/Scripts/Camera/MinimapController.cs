using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Team1
{
    /// <summary>
    /// ミニマップ用カメラを制御する。「現在地(プレイヤー追従・拡大表示)」と「全体表示」の
    /// 2モードをMキー(またはゲームパッドのSelectボタン)で切り替える。
    /// InputSystem_Actions.inputactionsのC#ラッパーはgenerateWrapperCodeが無効で自動更新されないため、
    /// アセットを変更せずInputActionをコード側で直接生成して扱う。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MinimapController : MonoBehaviour
    {
        [SerializeField] private GameObject _player;
        [SerializeField] private Text _modeLabel;

        [Header("現在地モード")]
        [SerializeField] private float _followOrthographicSize = 22f;

        [Header("全体表示モード")]
        [SerializeField] private Vector2 _overviewCenter;
        [SerializeField] private float _overviewOrthographicSize = 50f;

        private Camera _camera;
        private InputAction _toggleModeAction;
        private bool _isOverviewMode;
        private float _fixedZ;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _fixedZ = transform.position.z;

            if (_player == null)
            {
                _player = GameObject.FindGameObjectWithTag("Player");
            }

            Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);

            _toggleModeAction = new InputAction(name: "ToggleMinimapMode", type: InputActionType.Button);
            _toggleModeAction.AddBinding("<Keyboard>/m");
            _toggleModeAction.AddBinding("<Gamepad>/select");
        }

        private void OnEnable()
        {
            _toggleModeAction.performed += HandleToggleMode;
            _toggleModeAction.Enable();

            ApplyMode();
        }

        private void OnDisable()
        {
            _toggleModeAction.performed -= HandleToggleMode;
            _toggleModeAction.Disable();
        }

        private void OnDestroy()
        {
            _toggleModeAction?.Dispose();
        }

        private void LateUpdate()
        {
            if (_isOverviewMode || _player == null)
            {
                return;
            }

            transform.position = new Vector3(_player.transform.position.x, _player.transform.position.y, _fixedZ);
        }

        private void HandleToggleMode(InputAction.CallbackContext context)
        {
            _isOverviewMode = !_isOverviewMode;
            ApplyMode();
        }

        private void ApplyMode()
        {
            if (_isOverviewMode)
            {
                transform.position = new Vector3(_overviewCenter.x, _overviewCenter.y, _fixedZ);
                _camera.orthographicSize = _overviewOrthographicSize;
            }
            else
            {
                _camera.orthographicSize = _followOrthographicSize;

                if (_player != null)
                {
                    transform.position = new Vector3(_player.transform.position.x, _player.transform.position.y, _fixedZ);
                }
            }

            if (_modeLabel != null)
            {
                _modeLabel.text = _isOverviewMode ? "全体 (M)" : "現在地 (M)";
            }
        }
    }
}
