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

        [Header("ドロップアイテム")]
        [SerializeField] protected OilRecoveryItem _dropItemPrefab;

        [Header("移動・索敵")]
        [SerializeField] protected float _detectionRange = 8f;
        [SerializeField] protected float _attackRange = 1.5f;
        [SerializeField] protected float _attackCooldown = 1.5f;
        [SerializeField] protected float _wallCollisionRadius = 0.45f;
        [SerializeField] protected LayerMask _wallLayer;

        // EnemyPatrolが追跡開始距離として参照する。索敵距離の情報源をここに一本化する
        public float DetectionRange => _detectionRange;

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

        // テレグラフ演出やジャンプ攻撃など、EnemyBase側が直接transformを動かしている間はEnemyPatrolの移動を止めるために公開する
        public bool IsBusy => _isBusy;

        private float _attackTimer;

        protected virtual void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _health = GetComponent<Health>();

            // エラー確認
            Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);
            Debug.Assert(_attackHitbox != null, $"{nameof(_attackHitbox)} is not assigned.", this);

            if (_wallLayer.value == 0)
            {
                _wallLayer = LayerMask.GetMask("Wall");
            }
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

            // 移動はEnemyPatrolが一元管理するため、ここでは攻撃可否の判定のみ行う
            float distance = Vector3.Distance(transform.position, _player.transform.position);
            if (distance <= _attackRange && !_isBusy && _attackTimer <= 0f)
            {
                PerformAttack();
                _attackTimer = _attackCooldown;
            }
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
            if (_oilRecoveryAmount > 0 && _dropItemPrefab != null)
            {
                OilRecoveryItem drop = Instantiate(_dropItemPrefab, transform.position, Quaternion.identity);
                drop.SetRecoveryAmount(_oilRecoveryAmount);
            }

            Destroy(gameObject);
        }
    }
}