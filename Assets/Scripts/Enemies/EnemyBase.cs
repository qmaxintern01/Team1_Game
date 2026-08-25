using UnityEngine;

namespace Team1
{
    [RequireComponent(typeof(Health))]
    public abstract class EnemyBase : MonoBehaviour, IFacingDirection
    {
        [Header("ステータス")]
        [SerializeField] protected int _maxHp = 70;
        [SerializeField] protected int _oilRecoveryAmount = 10;

        public int OilRecoveryAmount => _oilRecoveryAmount;

        [Header("移動・索敵")]
        [SerializeField] protected float _moveSpeed = 2f;
        [SerializeField] protected float _detectionRange = 8f;
        [SerializeField] protected float _attackRange = 1.5f;
        [SerializeField] protected float _attackCooldown = 1.5f;

        [Header("攻撃判定")]
        [SerializeField] protected LayerMask _playerLayer;

        protected GameObject _player;
        protected Health _health;
        private float _attackTimer;

        // 追跡移動の方向を保持し、静止中(攻撃中)も直前の向きを維持する
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        protected virtual void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _health = GetComponent<Health>();

            // エラー確認
            Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);
        }

        protected virtual void OnEnable()
        {
            _health.Initialize(_maxHp);
            _health.OnDied += HandleDied;
        }

        protected virtual void OnDisable()
        {
            _health.OnDied -= HandleDied;
        }

        protected virtual void Update()
        {
            if (_player == null || _health.IsDead)
            {
                return;
            }

            _attackTimer -= Time.deltaTime;

            float distance = Vector3.Distance(transform.position, _player.transform.position);
            if (distance <= _attackRange)
            {
                if (_attackTimer <= 0f)
                {
                    PerformAttack();
                    _attackTimer = _attackCooldown;
                }
            }
            else if (distance <= _detectionRange)
            {
                ChasePlayer();
            }
        }

        protected virtual void ChasePlayer()
        {
            Vector3 previousPosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, _moveSpeed * Time.deltaTime);
            UpdateFacingDirection(transform.position - previousPosition);
        }

        protected void UpdateFacingDirection(Vector3 movementDelta)
        {
            if (movementDelta.sqrMagnitude > 0.0001f)
            {
                FacingDirection = ((Vector2)movementDelta).normalized;
            }
        }

        protected abstract void PerformAttack();

        protected void DealDamageAround(int damage, float radius)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, _playerLayer);
            foreach (Collider2D hit in hits)
            {
                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(damage);
                }
            }
        }

        private void HandleDied()
        {
            if (_oilRecoveryAmount > 0 && _player != null && _player.TryGetComponent(out PlayerOil playerOil))
            {
                playerOil.AddOil(_oilRecoveryAmount);
            }

            Destroy(gameObject);
        }
    }
}
