using UnityEngine;
using UnityEngine.InputSystem;

namespace Team1
{
    public class MovePlayer : MonoBehaviour
    {
        [SerializeField] private GameObject _player;
        [SerializeField] private float _moveSpeed = 5f;

        private InputSystem_Actions _gameInputs;
        private Vector2 _moveInput;
        private PlayerDash _dash;

        // ナイフの背面判定や銃の照準方向は、直近の移動入力方向を基準にする(静止中は最後の向きを維持)
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

            // 設定された入力値を適用
            _moveInput = _gameInputs.Player.Move.ReadValue<Vector2>();

            if (_moveInput.sqrMagnitude > 0.0001f)
            {
                FacingDirection = _moveInput.normalized;
            }

            float speedMultiplier = _dash != null ? _dash.SpeedMultiplier : 1f;
            _player.transform.position += new Vector3(_moveInput.x, _moveInput.y, 0) * _moveSpeed * speedMultiplier * Time.deltaTime;
        }
    }
}
