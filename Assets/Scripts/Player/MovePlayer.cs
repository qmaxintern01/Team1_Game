using UnityEngine;
using UnityEngine.InputSystem;

namespace Team1
{
    public class MovePlayer : MonoBehaviour
    {
        [SerializeField] private GameObject _player;
        [SerializeField] private Animator _animator;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _wallCollisionRadius = 0.45f;
        [SerializeField] private LayerMask _wallLayer;

        private InputSystem_Actions _gameInputs;
        private Vector2 _moveInput;
        private PlayerDash _dash;
        private Camera _mainCamera;
        private string _currentAnimationState;

        // ナイフの背面判定や銃の照準方向、歩行アニメーションの向きは、移動入力ではなくマウスポインターの向きを基準にする(移動キーの入力方向に関わらず常にマウス方向を向く)
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        private void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player");

            // エラー確認
            Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);

            if (_player != null)
            {
                _dash = _player.GetComponent<PlayerDash>();
                _animator = _player.GetComponent<Animator>();
            }

            if (_wallLayer.value == 0)
            {
                _wallLayer = LayerMask.GetMask("Wall");
            }

            _mainCamera = Camera.main;
            Debug.Assert(_mainCamera != null, $"{nameof(_mainCamera)} is not found.", this);
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
            float speedMultiplier = _dash != null ? _dash.SpeedMultiplier : 1f;

            // マウスが右を指していれば移動入力が左であってもプレイヤーは常に右向きとして扱う(移動方向とは独立)
            UpdateFacingDirectionFromPointer();

            bool isMoving = _moveInput.sqrMagnitude > 0.0001f;

            // AnimatorControllerのMoveX/MoveYパラメータによる遷移は、立ち止まっている間は方向転換しない(Idle同士の遷移が組まれていない)ため、
            // 現在の向き(マウス基準)と移動有無から再生すべきステートを直接算出してPlayする(静止中でもマウス方向へ向きを更新できるようにする)
            string targetAnimationState = ResolveAnimationStateName(FacingDirection, isMoving);

            if (targetAnimationState != _currentAnimationState)
            {
                _animator.Play(targetAnimationState);
                _currentAnimationState = targetAnimationState;
            }

            // 移動速度倍率(ダッシュ/ゲージ切れ)に合わせて歩行アニメーションの再生速度も変える
            _animator.speed = isMoving ? speedMultiplier : 1f;

            Vector3 delta = new Vector3(_moveInput.x, _moveInput.y, 0) * _moveSpeed * speedMultiplier * Time.deltaTime;
            _player.transform.position = WallCollision.ResolveMovement(_player.transform.position, delta, _wallCollisionRadius, _wallLayer);
        }

        // Player.controller側のステート名(表記ゆれはアセット側に合わせている)
        private static string ResolveAnimationStateName(Vector2 direction, bool isMoving)
        {
            bool horizontalDominant = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y);

            if (horizontalDominant)
            {
                return direction.x >= 0f
                    ? (isMoving ? "LightWark" : "LightIdol")
                    : (isMoving ? "LeftWark" : "LeftIdol");
            }

            return direction.y >= 0f
                ? (isMoving ? "UpWark" : "UpIdol")
                : (isMoving ? "DownWalk" : "Idol");
        }

        private void UpdateFacingDirectionFromPointer()
        {
            if (_mainCamera == null)
            {
                return;
            }

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
