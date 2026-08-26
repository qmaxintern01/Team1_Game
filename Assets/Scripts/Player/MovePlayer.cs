using UnityEngine;
using UnityEngine.InputSystem;

namespace Team1
{
    public class MovePlayer : MonoBehaviour
    {
        [SerializeField] private GameObject _player;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _wallCollisionRadius = 0.45f;
        [SerializeField] private LayerMask _wallLayer;

        private InputSystem_Actions _gameInputs;
        private Vector2 _moveInput;
        private PlayerDash _dash;
        private Camera _mainCamera;

        // ナイフの背面判定や銃の照準方向は、移動入力ではなくマウスポインターの向きを基準にする(移動キーの入力方向に関わらず常にマウス方向を向く)
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        private void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player");

            // エラー確認
            Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);

            if (_player != null)
            {
                _dash = _player.GetComponent<PlayerDash>();
            }

            _mainCamera = Camera.main;
            Debug.Assert(_mainCamera != null, $"{nameof(_mainCamera)} is not found.", this);

            if (_wallLayer.value == 0)
            {
                _wallLayer = LayerMask.GetMask("Wall");
            }
        }

        private void OnEnable()
        {
            // 入力処理追加
            _gameInputs = new InputSystem_Actions();
            _gameInputs.Enable();
        }

        private void OnDisable()
        {
            _gameInputs.Disable();
            _gameInputs.Dispose();
        }

        private void Update()
        {
            if (_player == null)
            {
                return;
            }

            // 設定された入力値を適用(移動方向はWASD/スティックのまま、向きはマウス基準に切り離す)
            _moveInput = _gameInputs.Player.Move.ReadValue<Vector2>();

            UpdateFacingDirectionFromPointer();

            float speedMultiplier = _dash != null ? _dash.SpeedMultiplier : 1f;
            Vector3 delta = new Vector3(_moveInput.x, _moveInput.y, 0) * _moveSpeed * speedMultiplier * Time.deltaTime;
            _player.transform.position = WallCollision.ResolveMovement(_player.transform.position, delta, _wallCollisionRadius, _wallLayer);
        }

        private void UpdateFacingDirectionFromPointer()
        {
            if (_mainCamera == null)
            {
                return;
            }

            // マウスが右を指していれば移動入力が左であってもプレイヤーは常に右向きとして扱う(移動方向とは独立)
            Vector2 pointerScreenPosition = _gameInputs.UI.Point.ReadValue<Vector2>();
            float distanceToPlayer = Mathf.Abs(_mainCamera.transform.position.z - _player.transform.position.z);
            Vector3 pointerWorldPosition = _mainCamera.ScreenToWorldPoint(new Vector3(pointerScreenPosition.x, pointerScreenPosition.y, distanceToPlayer));

            Vector2 direction = (Vector2)pointerWorldPosition - (Vector2)_player.transform.position;

            if (direction.sqrMagnitude > 0.0001f)
            {
                FacingDirection = direction.normalized;
            }
        }
    }
}
