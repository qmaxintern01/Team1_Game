using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Team1
{
    [RequireComponent(typeof(Health), typeof(PlayerOil), typeof(PlayerDash))]
    public class PlayerDodge : MonoBehaviour
    {
        [SerializeField] private int _oilCost = 5;
        [SerializeField] private float _invincibleDuration = 0.3f;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Color _invincibleColor = new Color(1f, 1f, 1f, 0.4f);

        private InputSystem_Actions _gameInputs;
        private Health _health;
        private PlayerOil _oil;
        private PlayerDash _dash;
        private Coroutine _invincibleRoutine;
        private Color _defaultColor;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _oil = GetComponent<PlayerOil>();
            _dash = GetComponent<PlayerDash>();

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_spriteRenderer != null)
            {
                _defaultColor = _spriteRenderer.color;
            }
        }

        private void OnEnable()
        {
            _gameInputs = new InputSystem_Actions();
            _gameInputs.Enable();

            // 緊急回避(スペース)は、操作方法の仕様に合わせて既存のJumpアクション(Space割当済み)を流用する
            _gameInputs.Player.Jump.performed += HandleDodgeInput;
        }

        private void OnDisable()
        {
            _gameInputs.Player.Jump.performed -= HandleDodgeInput;
            _gameInputs.Disable();
            _gameInputs.Dispose();
        }

        private void HandleDodgeInput(InputAction.CallbackContext context)
        {
            // InputActionのイベントはTime.timeScaleに関係なく発火するため、演出停止中は明示的に無視する
            if (Time.timeScale <= 0f)
            {
                return;
            }

            TryDodge();
        }

        public bool TryDodge()
        {
            if (_health.IsDead || !_dash.CanEmergencyDodge)
            {
                Debug.Log("緊急回避 失敗: 死亡中、またはダッシュゲージ枯渇による回避不可状態です");
                return false;
            }

            if (!_oil.TrySpendOil(_oilCost))
            {
                Debug.Log($"緊急回避 失敗: オイル不足 (残オイル={_oil.CurrentOil})");
                return false;
            }

            if (_invincibleRoutine != null)
            {
                StopCoroutine(_invincibleRoutine);
            }

            _invincibleRoutine = StartCoroutine(InvincibleForDuration());
            Debug.Log($"緊急回避 発動 (残オイル={_oil.CurrentOil})");
            return true;
        }

        private IEnumerator InvincibleForDuration()
        {
            _health.IsInvincible = true;
            // 色を変える
            SetSpriteColor(_invincibleColor);
            Debug.Log("無敵化 開始");
            yield return new WaitForSeconds(_invincibleDuration);
            _health.IsInvincible = false;
            // 色を元に戻す
            SetSpriteColor(_defaultColor);
            Debug.Log("無敵化 終了");
            _invincibleRoutine = null;
        }

        private void SetSpriteColor(Color color)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = color;
            }
        }
    }
}
