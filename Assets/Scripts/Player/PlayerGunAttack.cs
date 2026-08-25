using UnityEngine;
using UnityEngine.InputSystem;

namespace Team1
{
    // AR(アサルトライフル)。右クリック長押しでチャージし、離すと発砲する
    [RequireComponent(typeof(PlayerOil))]
    public class PlayerGunAttack : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponSwitcher _weaponSwitcher;
        [SerializeField] private MovePlayer _movePlayer;
        [SerializeField] private GunBullet _bulletPrefab;

        [SerializeField] private int _damage = 5;
        [SerializeField] private int _oilCost = 1;

        [Header("チャージ")]
        [SerializeField] private float _chargeInterval = 1f;
        [SerializeField] private float _damagePerChargeLevel = 1.5f;
        [SerializeField] private float _sizePerChargeLevel = 1.5f;
        [SerializeField] private int _oilCostPerChargeLevel = 1;

        private InputSystem_Actions _gameInputs;
        private PlayerOil _oil;

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

            Debug.Assert(_bulletPrefab != null, $"{nameof(_bulletPrefab)} is not assigned.", this);
        }

        private void OnEnable()
        {
            _gameInputs = new InputSystem_Actions();
            _gameInputs.Enable();

            // Player/Attackは既にナイフで使用中のため、AR発砲のトリガーはUI/RightClick(右クリック長押し→離して発砲)を流用する
            _gameInputs.UI.RightClick.started += HandleChargeStart;
            _gameInputs.UI.RightClick.canceled += HandleChargeReleaseAndFire;
        }

        private void OnDisable()
        {
            _gameInputs.UI.RightClick.started -= HandleChargeStart;
            _gameInputs.UI.RightClick.canceled -= HandleChargeReleaseAndFire;

            _gameInputs.Disable();
            _gameInputs.Dispose();
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

            _chargeTimer += Time.deltaTime;

            while (_chargeTimer >= _chargeInterval)
            {
                _chargeTimer -= _chargeInterval;
                _chargeLevel++;
                Debug.Log($"ARチャージ: レベル{_chargeLevel}");
            }
        }

        private void HandleChargeStart(InputAction.CallbackContext context)
        {
            if (_weaponSwitcher != null && _weaponSwitcher.CurrentWeapon != WeaponType.AssaultRifle)
            {
                return;
            }

            _isCharging = true;
            _chargeTimer = 0f;
            _chargeLevel = 0;
        }

        private void HandleChargeReleaseAndFire(InputAction.CallbackContext context)
        {
            if (!_isCharging)
            {
                return;
            }

            _isCharging = false;
            Fire(_chargeLevel);
            _chargeLevel = 0;
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
                return;
            }

            int totalOilCost = _oilCost + chargeLevel * _oilCostPerChargeLevel;

            if (!_oil.TrySpendOil(totalOilCost))
            {
                Debug.Log($"AR発砲失敗: オイル不足(必要{totalOilCost})");
                return;
            }

            int damage = Mathf.RoundToInt(_damage + chargeLevel * _damagePerChargeLevel);
            float sizeMultiplier = 1f + chargeLevel * _sizePerChargeLevel;

            Vector2 facing = _movePlayer != null ? _movePlayer.FacingDirection : Vector2.down;
            Vector3 spawnPosition = transform.position;

            GunBullet bullet = Instantiate(_bulletPrefab, spawnPosition, Quaternion.identity);
            bullet.transform.localScale *= sizeMultiplier;
            bullet.Launch(facing, damage);

            Debug.Log($"AR発砲: チャージレベル{chargeLevel}, damage={damage}, sizeMultiplier={sizeMultiplier}, oilCost={totalOilCost}");
        }
    }
}
