using UnityEngine;

namespace Team1
{
    // 雑魚敵: HP70 / オイル回復10 / 銃でこちらを狙ってくる(威力2)
    public class WeakEnemy : EnemyBase
    {
        [Header("銃攻撃")]
        [SerializeField] private EnemyProjectile _projectilePrefab;
        [SerializeField] private int _gunDamage = 2;

        protected override void Awake()
        {
            base.Awake();

            // エラー確認
            Debug.Assert(_projectilePrefab != null, $"{nameof(_projectilePrefab)} is not assigned.", this);
        }

        private void Reset()
        {
            _maxHp = 70;
            _oilRecoveryAmount = 10;
            _attackRange = 6f;
            _detectionRange = 8f;
        }

        protected override void PerformAttack()
        {
            if (_projectilePrefab == null || _player == null)
            {
                return;
            }

            Vector3 direction = _player.transform.position - transform.position;
            EnemyProjectile projectile = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
            projectile.Launch(direction, _gunDamage);
        }
    }
}
