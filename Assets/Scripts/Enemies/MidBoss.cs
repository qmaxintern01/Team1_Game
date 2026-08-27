using UnityEngine;

namespace Team1
{
    // 中ボス: HP300 / オイル回復50 / 近距離攻撃(威力5) / 溜め攻撃(威力10)
    public class MidBoss : EnemyBase
    {
        [Header("近距離攻撃")]
        [SerializeField] private int _meleeDamage = 5;
        [SerializeField] private float _meleeRadius = 1.5f;
        [SerializeField] private float _meleeTelegraphTime = 0.3f;

        [Header("溜め攻撃")]
        [SerializeField] private int _chargeDamage = 10;
        [SerializeField] private float _chargeRadius = 2f;
        [SerializeField] private float _chargeWindUpTime = 1f;
        [SerializeField] private float _chargeRecoveryTime = 0.4f;
        [SerializeField, Range(0f, 1f)] private float _chargeAttackChance = 0.3f;

        private void Reset()
        {
            _maxHp = 300;
            _oilRecoveryAmount = 50;
            _attackRange = 1.5f;
            _detectionRange = 10f;
        }

        protected override void PerformAttack()
        {
            if (Random.value < _chargeAttackChance)
            {
                StartCoroutine(TelegraphAndDealDamage(_chargeDamage, _chargeRadius, _chargeWindUpTime, _chargeRecoveryTime));
            }
            else
            {
                StartCoroutine(TelegraphAndDealDamage(_meleeDamage, _meleeRadius, _meleeTelegraphTime));
            }
        }
    }
}
