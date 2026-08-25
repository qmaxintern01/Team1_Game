using System.Collections;
using UnityEngine;

namespace Team1
{
    // 中ボス: HP300 / オイル回復50 / 近距離攻撃(威力5) / 溜め攻撃(威力10)
    public class MidBoss : EnemyBase
    {
        [Header("近距離攻撃")]
        [SerializeField] private int _meleeDamage = 5;
        [SerializeField] private float _meleeRadius = 1.5f;

        [Header("溜め攻撃")]
        [SerializeField] private int _chargeDamage = 10;
        [SerializeField] private float _chargeRadius = 2f;
        [SerializeField] private float _chargeWindUpTime = 1f;
        [SerializeField, Range(0f, 1f)] private float _chargeAttackChance = 0.3f;

        private bool _isCharging;

        private void Reset()
        {
            _maxHp = 300;
            _oilRecoveryAmount = 50;
            _attackRange = 1.5f;
            _detectionRange = 10f;
        }

        protected override void ChasePlayer()
        {
            if (_isCharging)
            {
                return;
            }

            base.ChasePlayer();
        }

        protected override void PerformAttack()
        {
            if (_isCharging)
            {
                return;
            }

            if (Random.value < _chargeAttackChance)
            {
                StartCoroutine(ChargeAttackRoutine());
            }
            else
            {
                DealDamageAround(_meleeDamage, _meleeRadius);
            }
        }

        private IEnumerator ChargeAttackRoutine()
        {
            _isCharging = true;
            yield return new WaitForSeconds(_chargeWindUpTime);

            DealDamageAround(_chargeDamage, _chargeRadius);
            _isCharging = false;
        }
    }
}
