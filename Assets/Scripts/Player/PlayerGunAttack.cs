using UnityEngine;
using UnityEngine.InputSystem;

namespace Team1
{
    // AR(アサルトライフル)。右クリック長押しでチャージし、左クリックで発砲する
    [RequireComponent(typeof(PlayerOil), typeof(PlayerSkills))]
    public class PlayerGunAttack : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponSwitcher _weaponSwitcher;
        [SerializeField] private MovePlayer _movePlayer;
        [SerializeField] private GunBullet _bulletPrefab;

        [SerializeField] private int _damage = 5;
        [SerializeField] private int _oilCost = 1;
        [SerializeField] private float _infiniteAmmoDamageMultiplier = 0.5f;

        [Header("チャージ")]
        [SerializeField] private float _chargeInterval = 1f;
        [SerializeField] private int _maxChargeLevel = 5;
        [SerializeField] private float _damagePerChargeLevel = 1.5f;
        [SerializeField] private float _sizePerChargeLevel = 1.5f;
        // 当たり判定の拡大率は見た目より控えめにする(見た目と同率だと過大に、0だと見た目より小さすぎて外れやすくなるため)
        [SerializeField] private float _colliderSizePerChargeLevel = 0.3f;
        [SerializeField] private int _oilCostPerChargeLevel = 1;

        [SerializeField] private Animator _animator;
        [SerializeField] private float _keepAnimationDuration = 2f;

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
        private PlayerSkills _skills;

        private bool _isCharging;
        private float _chargeTimer;
        private int _chargeLevel;

        private void Awake()
        {
            _oil = GetComponent<PlayerOil>();
            _skills = GetComponent<PlayerSkills>();

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

            Debug.Assert(_bulletPrefab != null, $"{nameof(_bulletPrefab)} is not assigned.", this);
        }

#if UNITY_EDITOR
        // Prefabアセットは実行時にFindできないため、未設定時はエディタ上でのみ自動検索して補完する
        private void OnValidate()
        {
            if (_bulletPrefab == null)
            {
                foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab"))
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    GunBullet bullet = UnityEditor.AssetDatabase.LoadAssetAtPath<GunBullet>(path);

                    if (bullet != null)
                    {
                        _bulletPrefab = bullet;
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

            // 右クリック長押し(UI/RightClick)でチャージし、左クリック(Player/Attack、ナイフと共用)で発砲する
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

            CancelInvoke(nameof(ResetIsKeep));
        }

        private void Update()
        {
            if (!_isCharging)
            {
                return;
            }

            if (_weaponSwitcher != null && _weaponSwitcher.CurrentWeapon != WeaponType.AssaultRifle)
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
                Debug.Log($"ARチャージ: レベル{_chargeLevel}");
            }
        }

        private void HandleChargeStart(InputAction.CallbackContext context)
        {
            // InputActionのイベントはTime.timeScaleに関係なく発火するため、演出停止中は明示的に無視する
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (_weaponSwitcher != null && _weaponSwitcher.CurrentWeapon != WeaponType.AssaultRifle)
            {
                return;
            }

            _isCharging = true;
            _chargeTimer = 0f;
            _chargeLevel = 0;
        }

        private void HandleChargeCancel(InputAction.CallbackContext context)
        {
            // 右クリックを離してもチャージレベルは保持し、発砲は左クリック(Attack)側で行う
            _isCharging = false;
        }

        private void HandleFireInput(InputAction.CallbackContext context)
        {
            // InputActionのイベントはTime.timeScaleに関係なく発火するため、演出停止中は明示的に無視する
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (_weaponSwitcher != null && _weaponSwitcher.CurrentWeapon != WeaponType.AssaultRifle)
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
            Debug.Log($"AR発砲キャンセル: 武器切替のためチャージ解除(レベル{_chargeLevel})");
            _isCharging = false;
            _chargeTimer = 0f;
            _chargeLevel = 0;
        }

        private void Fire(int chargeLevel)
        {
            if (_bulletPrefab == null)
            {
                Debug.Log($"AR発砲失敗: 弾丸プレハブが未設定です");

                return;
            }

            // スキル2発動中は、オイル消費なしで威力半減の弾を撃てる
            bool infiniteAmmo = _skills != null && _skills.IsInfiniteAmmoActive;
            int totalOilCost = _oilCost + chargeLevel * _oilCostPerChargeLevel;

            if (!infiniteAmmo && !_oil.TrySpendOil(totalOilCost))
            {
                Debug.Log($"AR発砲失敗: オイル不足(必要{totalOilCost})");
                return;
            }

            int damage = Mathf.RoundToInt(_damage + chargeLevel * _damagePerChargeLevel);

            if (infiniteAmmo)
            {
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * _infiniteAmmoDamageMultiplier));
            }

            float visualSizeMultiplier = 1f + chargeLevel * _sizePerChargeLevel;
            float colliderSizeMultiplier = 1f + chargeLevel * _colliderSizePerChargeLevel;

            Vector2 facing = _movePlayer != null ? _movePlayer.FacingDirection : Vector2.down;
            Vector3 spawnPosition = transform.position;

            GunBullet bullet = Instantiate(_bulletPrefab, spawnPosition, Quaternion.identity);
            bullet.SetChargeScale(visualSizeMultiplier, colliderSizeMultiplier);
            bullet.Launch(facing, damage);

            Debug.Log($"AR発砲: チャージレベル{chargeLevel}, damage={damage}, visualSizeMultiplier={visualSizeMultiplier}, colliderSizeMultiplier={colliderSizeMultiplier}, oilCost={(infiniteAmmo ? 0 : totalOilCost)}, infiniteAmmo={infiniteAmmo}");
            _animator.SetTrigger("isAttack");

            _animator.SetBool("isKeep", true);
            CancelInvoke(nameof(ResetIsKeep));
            Invoke(nameof(ResetIsKeep), _keepAnimationDuration);
        }

        private void ResetIsKeep()
        {
            _animator.SetBool("isKeep", false);
        }
    }
}
