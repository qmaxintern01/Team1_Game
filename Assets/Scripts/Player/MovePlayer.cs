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
            // 設定された入力値を適用
            _moveInput = _gameInputs.Player.Move.ReadValue<Vector2>();
            float speedMultiplier = _dash != null ? _dash.SpeedMultiplier : 1f;
            _player.transform.position += new Vector3(_moveInput.x, _moveInput.y, 0) * _moveSpeed * speedMultiplier * Time.deltaTime;
        }
    }
}
