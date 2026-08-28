using UnityEngine;
using UnityEngine.InputSystem;

namespace Team1
{
    [RequireComponent(typeof(PlayerOil))]
    public class PlayerKnifeAttack : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponSwitcher _weaponSwitcher;
        [SerializeField] private MovePlayer _movePlayer;
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _range = 1.2f;
        [SerializeField] private float _radius = 0.8f;
        [SerializeField] private LayerMask _enemyLayer;

        private InputSystem_Actions _gameInputs;
        private PlayerOil _oil;

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
        }

        private void OnEnable()
        {
            _gameInputs = new InputSystem_Actions();
            _gameInputs.Enable();
            _gameInputs.Player.Attack.performed += HandleAttackInput;
        }

        private void OnDisable()
        {
            _gameInputs.Player.Attack.performed -= HandleAttackInput;
            _gameInputs.Disable();
            _gameInputs.Dispose();
        }

        private void HandleAttackInput(InputAction.CallbackContext context)
        {
            // InputActionのイベントはTime.timeScaleに関係なく発火するため、演出停止中は明示的に無視する
            if (Time.timeScale <= 0f)
            {
                return;
            }

            if (_weaponSwitcher != null && _weaponSwitcher.CurrentWeapon != WeaponType.Knife)
            {
                return;
            }

            Attack();
        }

        private void Attack()
        {
            AudioManager.Instance?.PlayKnifeAttackSe();

            Vector2 facing = _movePlayer != null ? _movePlayer.FacingDirection : Vector2.down;
            Vector2 origin = (Vector2)transform.position + facing * (_range * 0.5f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, _radius, _enemyLayer);
            Debug.Log($"ナイフ攻撃発生: origin={origin}, radius={_radius}, hit数={hits.Length}");

            foreach (Collider2D hit in hits)
            {
                if (!hit.TryGetComponent(out IDamageable damageable))
                {
                    continue;
                }

                // 攻撃方向(自分の向き)と敵の向きが同じ=敵の背後から攻撃している、とみなす
                bool isBackstab = hit.TryGetComponent(out IFacingDirection enemyFacing)
                    && Vector2.Dot(enemyFacing.FacingDirection, facing) > 0.5f;

                int damage = isBackstab ? _damage * 2 : _damage;

                hit.TryGetComponent(out Health health);
                hit.TryGetComponent(out EnemyBase enemyBase);
                bool wasAlive = health != null && !health.IsDead;

                // 倒された際にオイルドロップを抑制するか・リザルト実績(ナイフ撃破/背面撃破)に計上するかの判定に使うため、ダメージを与える前に伝えておく
                if (enemyBase != null)
                {
                    enemyBase.NotifyHitSource(isKnife: true, isBackstab: isBackstab);
                }

                damageable.TakeDamage(damage);

                if (wasAlive && health.IsDead && enemyBase != null)
                {
                    // ナイフでの撃破は通常の1.5倍のオイルを獲得する(EnemyBase側の等倍付与に0.5倍分を追加する)
                    int bonusOil = Mathf.RoundToInt(enemyBase.OilRecoveryAmount * 0.5f);
                    _oil.AddOil(bonusOil);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector2 facing = _movePlayer != null ? _movePlayer.FacingDirection : Vector2.down;
            Vector2 origin = (Vector2)transform.position + facing * (_range * 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, _radius);
        }
#endif
    }
}
