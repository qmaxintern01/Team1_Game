using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Team1
{
    // スキル1: オイル消費でHP回復。スキル2: 一定時間、AR(PlayerGunAttack)の弾を無限に(威力半減で)撃てるようにする
    [RequireComponent(typeof(PlayerOil), typeof(Health))]
    public class PlayerSkills : MonoBehaviour
    {
        [Header("スキル1: HP回復")]
        [SerializeField] private int _healOilCost = 10;
        [SerializeField] private int _healAmount = 5;
        [SerializeField] private float _healCooldown = 10f;

        [Header("スキル2: 弾無限(威力半減)")]
        [SerializeField] private int _infiniteAmmoOilCost = 50;
        [SerializeField] private float _infiniteAmmoDuration = 5f;
        [SerializeField] private float _infiniteAmmoCooldown = 60f;

        private InputAction _skill1Action;
        private InputAction _skill2Action;

        private PlayerOil _oil;
        private Health _health;

        private float _healCooldownTimer;
        private float _infiniteAmmoCooldownTimer;
        private Coroutine _infiniteAmmoRoutine;

        // AR側(PlayerGunAttack)がこれを見て、オイル消費なし・威力半減で発砲する
        public bool IsInfiniteAmmoActive { get; private set; }

        // UI表示用: クールダウンの設定値・必要オイル量
        public float Skill1Cooldown => _healCooldown;
        public float Skill2Cooldown => _infiniteAmmoCooldown;
        public int Skill1OilCost => _healOilCost;
        public int Skill2OilCost => _infiniteAmmoOilCost;

        // UI表示用: クールダウンの残り時間
        public float Skill1CooldownRemaining => Mathf.Max(0f, _healCooldownTimer);
        public float Skill2CooldownRemaining => Mathf.Max(0f, _infiniteAmmoCooldownTimer);

        // UI表示用: クールダウンの進捗(0=使用直後, 1=使用可能)
        public float Skill1CooldownProgress01 => _healCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(_healCooldownTimer / _healCooldown);
        public float Skill2CooldownProgress01 => _infiniteAmmoCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(_infiniteAmmoCooldownTimer / _infiniteAmmoCooldown);

        public bool IsSkill1Ready => _healCooldownTimer <= 0f;
        public bool IsSkill2Ready => _infiniteAmmoCooldownTimer <= 0f && !IsInfiniteAmmoActive;

        private void Awake()
        {
            _oil = GetComponent<PlayerOil>();
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            // スキル用のキー(Q/E)は既存のInput Actionsアセットに未定義のため、PlayerWeaponSwitcherと同様にコードでInputActionを生成する
            // (Eキーは既存のPlayer/Interactアクションにも割り当てられているが、Interactは未使用のため競合しない)
            _skill1Action = new InputAction("Skill1", binding: "<Keyboard>/q");
            _skill2Action = new InputAction("Skill2", binding: "<Keyboard>/e");

            _skill1Action.performed += HandleSkill1Input;
            _skill2Action.performed += HandleSkill2Input;

            _skill1Action.Enable();
            _skill2Action.Enable();
        }

        private void OnDisable()
        {
            _skill1Action.performed -= HandleSkill1Input;
            _skill2Action.performed -= HandleSkill2Input;

            _skill1Action.Dispose();
            _skill2Action.Dispose();

            if (_infiniteAmmoRoutine != null)
            {
                StopCoroutine(_infiniteAmmoRoutine);
                IsInfiniteAmmoActive = false;
                _infiniteAmmoRoutine = null;
            }
        }

        private void Update()
        {
            if (_healCooldownTimer > 0f)
            {
                _healCooldownTimer -= Time.deltaTime;
            }

            if (_infiniteAmmoCooldownTimer > 0f)
            {
                _infiniteAmmoCooldownTimer -= Time.deltaTime;
            }
        }

        private void HandleSkill1Input(InputAction.CallbackContext context)
        {
            // InputActionのイベントはTime.timeScaleに関係なく発火するため、演出停止中は明示的に無視する
            if (Time.timeScale <= 0f)
            {
                return;
            }

            TryUseHealSkill();
        }

        private void HandleSkill2Input(InputAction.CallbackContext context)
        {
            if (Time.timeScale <= 0f)
            {
                return;
            }

            TryUseInfiniteAmmoSkill();
        }

        public bool TryUseHealSkill()
        {
            if (_health.IsDead)
            {
                return false;
            }

            if (_healCooldownTimer > 0f)
            {
                Debug.Log($"スキル1失敗: クールタイム中(残り{_healCooldownTimer:F1}秒)");
                return false;
            }

            if (_health.CurrentHp >= _health.MaxHp)
            {
                Debug.Log("スキル1失敗: HPが満タンです");
                return false;
            }

            if (!_oil.TrySpendOil(_healOilCost))
            {
                Debug.Log($"スキル1失敗: オイル不足(必要{_healOilCost})");
                return false;
            }

            _health.Heal(_healAmount);
            _healCooldownTimer = _healCooldown;
            Debug.Log($"スキル1発動: HP{_healAmount}回復(残オイル={_oil.CurrentOil}, 現在HP={_health.CurrentHp})");
            return true;
        }

        public bool TryUseInfiniteAmmoSkill()
        {
            if (_health.IsDead || IsInfiniteAmmoActive)
            {
                return false;
            }

            if (_infiniteAmmoCooldownTimer > 0f)
            {
                Debug.Log($"スキル2失敗: クールタイム中(残り{_infiniteAmmoCooldownTimer:F1}秒)");
                return false;
            }

            if (!_oil.TrySpendOil(_infiniteAmmoOilCost))
            {
                Debug.Log($"スキル2失敗: オイル不足(必要{_infiniteAmmoOilCost})");
                return false;
            }

            _infiniteAmmoCooldownTimer = _infiniteAmmoCooldown;
            _infiniteAmmoRoutine = StartCoroutine(InfiniteAmmoForDuration());
            Debug.Log($"スキル2発動: 弾無限(威力半減)を{_infiniteAmmoDuration}秒間(残オイル={_oil.CurrentOil})");
            return true;
        }

        private IEnumerator InfiniteAmmoForDuration()
        {
            IsInfiniteAmmoActive = true;
            yield return new WaitForSeconds(_infiniteAmmoDuration);
            IsInfiniteAmmoActive = false;
            _infiniteAmmoRoutine = null;
            Debug.Log("スキル2終了: 弾無限モード解除");
        }
    }
}
