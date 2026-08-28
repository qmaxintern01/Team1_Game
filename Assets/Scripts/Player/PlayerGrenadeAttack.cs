using UnityEngine;
using UnityEngine.InputSystem;

namespace Team1
{
    // グレネードランチャー。AR(PlayerGunAttack)と同じく右クリック長押しでチャージし、左クリック(Attack)で発射する
    [RequireComponent(typeof(PlayerOil))]
    public class PlayerGrenadeAttack : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponSwitcher _weaponSwitcher;
        [SerializeField] private MovePlayer _movePlayer;
        [SerializeField] private GrenadeProjectile _grenadePrefab;

        [SerializeField] private int _damage = 25;
        [SerializeField] private int _oilCost = 5;
        // 発射直後の連射を防ぐための最低間隔(チャージ0での連射制限)
        [SerializeField] private float _fireCooldown = 0.8f;

        [Header("チャージ")]
        [SerializeField] private float _chargeInterval = 1f;
        [SerializeField] private int _maxChargeLevel = 5;
        [SerializeField] private float _damagePerChargeLevel = 5f;
        [SerializeField] private float _sizePerChargeLevel = 0.3f;
        // 着弾判定の拡大率は見た目より控えめにする(見た目と同率だと過大に感じられるため)
        [SerializeField] private float _colliderSizePerChargeLevel = 0.15f;
        [SerializeField] private float _explosionRadiusPerChargeLevel = 0.3f;
        [SerializeField] private int _oilCostPerChargeLevel = 2;

        [SerializeField] private Animator _animator;

        // 次の1チャージ分が溜まるまでの進捗(0〜1)。ゲージUI表示用
        public float ChargeProgress01
        {
            get
            {
                if (!_isCharging || _chargeLevel >= _maxChargeLevel)
                {
                    return _isCharging ? 1f : 0f;
                }

                return Mathf.Clamp01(_chargeTimer / _chargeInterval);
            }
        }

        private InputSystem_Actions _gameInputs;
        private PlayerOil _oil;
        private float _cooldownTimer;

        private bool _isCharging;
        private float _chargeTimer;
        private int _chargeLevel;

        private void Awake()
        {
            _oil = GetComponent<PlayerOil>();

            if (_weaponSwitcher == null)
            {
                _weaponSwitcher = GetComponent<PlayerWeaponSwitcher>();
            }

            if (_movePlayer == null)
            {
                _movePlayer = FindAnyObjectByType<MovePlayer>();
            }

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            Debug.Assert(_grenadePrefab != null, $"{nameof(_grenadePrefab)} is not assigned.", this);
        }

#if UNITY_EDITOR
        // Prefabアセットは実行時にFindできないため、未設定時はエディタ上でのみ自動検索して補完する
        private void OnValidate()
        {
            if (_grenadePrefab == null)
            {
                foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab"))
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    GrenadeProjectile grenade = UnityEditor.AssetDatabase.LoadAssetAtPath<GrenadeProjectile>(path);

                    if (grenade != null)
                    {
                        _grenadePrefab = grenade;
                        break;
                    }
                }
            }
        }
#endif

        private void OnEnable()
        {
            _gameInputs = new InputSystem_Actions();
            _gameInputs.Enable();

            // 右クリック長押し(UI/RightClick)でチャージし、左クリック(Player/Attack、ナイフ・ARと共用)で発射する
            _gameInputs.UI.RightClick.started += HandleChargeStart;
            _gameInputs.UI.RightClick.canceled += HandleChargeCancel;
            _gameInputs.Player.Attack.performed += HandleFireInput;
        }

        private void OnDisable()
        {
            _gameInputs.UI.RightClick.started -= HandleChargeStart;
            _gameInputs.UI.RightClick.canceled -= HandleChargeCancel;
            _gameInputs.Player.Attack.performed -= HandleFireInput;

            _gameInputs.Disable();
            _gameInputs.Dispose();
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }

            if (!_isCharging)
            {
                return;
            }

            if (_weaponSwitcher != null && _weaponSwitcher.CurrentWeapon != WeaponType.GrenadeLauncher)
            {
                CancelCharge();
                return;
            }

            if (_chargeLevel >= _maxChargeLevel)
            {
                return;
            }

            _chargeTimer += Time.deltaTime;

            while (_chargeTimer >= _chargeInterval && _chargeLevel < _maxChargeLevel)
            {
                _chargeTimer -= _chargeInterval;
                _chargeLevel++;
                Debug.Log($"グレネードチャージ: レベル{_chargeLevel}");
            }
        }

        private void HandleChargeStart(InputAction.CallbackContext context)
        {
            // InputActionのイベントはTime.timeScaleに関係なく発火するため、演出停止中は明示的に無視する
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (_weaponSwitcher != null && _weaponSwitcher.CurrentWeapon != WeaponType.GrenadeLauncher)
            {
                return;
            }

            _isCharging = true;
            _chargeTimer = 0f;
            _chargeLevel = 0;
        }

        private void HandleChargeCancel(InputAction.CallbackContext context)
        {
            // 右クリックを離してもチャージレベルは保持し、発射は左クリック(Attack)側で行う
            _isCharging = false;
        }

        private void HandleFireInput(InputAction.CallbackContext context)
        {
            // InputActionのイベントはTime.timeScaleに関係なく発火するため、演出停止中は明示的に無視する
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (_weaponSwitcher != null && _weaponSwitcher.CurrentWeapon != WeaponType.GrenadeLauncher)
            {
                return;
            }

            _isCharging = false;
            Fire(_chargeLevel);
            _chargeLevel = 0;
            _chargeTimer = 0f;
        }

        private void CancelCharge()
        {
            Debug.Log($"グレネード発射キャンセル: 武器切替のためチャージ解除(レベル{_chargeLevel})");
            _isCharging = false;
            _chargeTimer = 0f;
            _chargeLevel = 0;
        }

        private void Fire(int chargeLevel)
        {
            if (_cooldownTimer > 0f)
            {
                return;
            }

            if (_grenadePrefab == null)
            {
                Debug.Log("グレネード発射失敗: 弾頭プレハブが未設定です");
                return;
            }

            int totalOilCost = _oilCost + chargeLevel * _oilCostPerChargeLevel;

            if (!_oil.TrySpendOil(totalOilCost))
            {
                Debug.Log($"グレネード発射失敗: オイル不足(必要{totalOilCost})");
                return;
            }

            _cooldownTimer = _fireCooldown;

            int damage = Mathf.RoundToInt(_damage + chargeLevel * _damagePerChargeLevel);
            float visualSizeMultiplier = 1f + chargeLevel * _sizePerChargeLevel;
            float colliderSizeMultiplier = 1f + chargeLevel * _colliderSizePerChargeLevel;
            float explosionRadiusMultiplier = 1f + chargeLevel * _explosionRadiusPerChargeLevel;

            Vector2 facing = _movePlayer != null ? _movePlayer.FacingDirection : Vector2.down;
            Vector3 spawnPosition = transform.position;

            AudioManager.Instance?.PlayGrenadeAttackSe();

            GrenadeProjectile grenade = Instantiate(_grenadePrefab, spawnPosition, Quaternion.identity);
            grenade.SetChargeScale(visualSizeMultiplier, colliderSizeMultiplier, explosionRadiusMultiplier);
            grenade.Launch(facing, damage);

            Debug.Log($"グレネード発射: チャージレベル{chargeLevel}, damage={damage}, explosionRadiusMultiplier={explosionRadiusMultiplier}, oilCost={totalOilCost}");

            if (_animator != null)
            {
                _animator.SetTrigger("isAttack");
            }
        }
    }
}
