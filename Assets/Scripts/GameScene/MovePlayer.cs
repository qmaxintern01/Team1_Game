using UnityEngine;
using UnityEngine.InputSystem;

namespace Team1
{
    public class MovePlayer : MonoBehaviour
    {
        [SerializeField] private GameObject _player;
        [SerializeField] private float _moveSpeed = 5f;

        private const float OIL_MAX_VALUE = 200.0f;
        private const float OIL_INITIAL_VALUE = 100.0f;

        private InputSystem_Actions _gameInputs;
        private Vector2 _moveInput;

        private float _oilValue = OIL_INITIAL_VALUE;

        private void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player");

            // エラー確認
            Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);
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
            _player.transform.position += new Vector3(_moveInput.x, _moveInput.y, 0) * _moveSpeed * Time.deltaTime;
        }
    }
}
