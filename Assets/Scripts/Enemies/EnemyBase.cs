using System.Collections;
using UnityEngine;

namespace Team1
{
    [RequireComponent(typeof(Health))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("ステータス")]
        [SerializeField] protected int _maxHp = 70;
        [SerializeField] protected int _oilRecoveryAmount = 10;

        [Header("移動・索敵")]
        [SerializeField] protected float _moveSpeed = 2f;
        [SerializeField] protected float _detectionRange = 8f;
        [SerializeField] protected float _attackRange = 1.5f;
        [SerializeField] protected float _attackCooldown = 1.5f;

        [Header("攻撃判定")]
        [SerializeField] protected AttackHitbox _attackHitbox;
        [SerializeField] protected float _hitActiveDuration = 0.15f;

        [Header("演出")]
        [SerializeField] protected AttackRangeIndicator _attackRangeIndicator;

        public int OilRecoveryAmount => _oilRecoveryAmount;

        protected GameObject _player;
        protected Health _health;

        // 予備動作(テレグラフ)や着地演出の最中は、移動・次の攻撃判定を止めるためのフラグ
        protected bool _isBusy;

        private float _attackTimer;

        protected virtual void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _health = GetComponent<Health>();

            // エラー確認
            Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);
            Debug.Assert(_attackHitbox != null, $"{nameof(_attackHitbox)} is not assigned.", this);
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
                if (!_isBusy && _attackTimer <= 0f)
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
            if (_isBusy)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, _moveSpeed * Time.deltaTime);
        }

        protected abstract void PerformAttack();

        // 攻撃判定用ヒットボックスを一定時間だけ有効化し、Collider2Dのトリガーでダメージ対象を検出する
        protected IEnumerator ActivateHitboxRoutine(int damage, float radius)
        {
            if (_attackHitbox == null)
            {
                yield break;
            }

            _attackHitbox.Activate(damage, radius);
            yield return new WaitForSeconds(_hitActiveDuration);
            _attackHitbox.Deactivate();
        }

        // 危険範囲を予告表示してから、一定時間後にその範囲へダメージを与える
        protected IEnumerator TelegraphAndDealDamage(int damage, float radius, float telegraphTime)
        {
            _isBusy = true;
            if (_attackRangeIndicator != null)
            {
                _attackRangeIndicator.Show(radius);
            }

            yield return new WaitForSeconds(telegraphTime);

            if (_attackRangeIndicator != null)
            {
                _attackRangeIndicator.Hide();
            }

            yield return ActivateHitboxRoutine(damage, radius);
            _isBusy = false;
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