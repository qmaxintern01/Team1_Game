using System.Collections;
using UnityEngine;

namespace Team1
{
    // 大ボス: HP700 / オイル回復なし / 近距離攻撃(威力7) / 範囲薙ぎ祓い(威力14) / ジャンプスタンプ(威力21)
    public class BigBoss : EnemyBase
    {
        [Header("近距離攻撃")]
        [SerializeField] private int _meleeDamage = 7;
        [SerializeField] private float _meleeRadius = 1.5f;
        [SerializeField] private float _meleeTelegraphTime = 0.3f;

        [Header("範囲薙ぎ祓い")]
        [SerializeField] private int _sweepDamage = 14;
        [SerializeField] private float _sweepRadius = 3f;
        [SerializeField] private float _sweepTelegraphTime = 0.5f;

        [Header("ジャンプスタンプ")]
        [SerializeField] private int _stompDamage = 21;
        [SerializeField] private float _stompRadius = 4f;
        [SerializeField] private float _jumpHeight = 4f;
        [SerializeField] private float _jumpUpTime = 0.5f;
        [SerializeField] private float _jumpDownTime = 0.4f;

        private void Reset()
        {
            _maxHp = 700;
            _oilRecoveryAmount = 0;
            _attackRange = 1.5f;
            _detectionRange = 12f;
        }

        protected override void PerformAttack()
        {
            switch (Random.Range(0, 3))
            {
                case 0:
                    StartCoroutine(TelegraphAndDealDamage(_meleeDamage, _meleeRadius, _meleeTelegraphTime));
                    break;
                case 1:
                    StartCoroutine(TelegraphAndDealDamage(_sweepDamage, _sweepRadius, _sweepTelegraphTime));
                    break;
                default:
                    StartCoroutine(StompAttackRoutine());
                    break;
            }
        }

        private IEnumerator StompAttackRoutine()
        {
            _isBusy = true;
            if (_attackRangeIndicator != null)
            {
                _attackRangeIndicator.Show(_stompRadius);
            }

            Vector3 origin = transform.position;
            Vector3 peak = origin + Vector3.up * _jumpHeight;

            yield return MoveOverTime(origin, peak, _jumpUpTime);
            yield return MoveOverTime(peak, origin, _jumpDownTime);

            if (_attackRangeIndicator != null)
            {
                _attackRangeIndicator.Hide();
            }

            yield return ActivateHitboxRoutine(_stompDamage, _stompRadius);
            _isBusy = false;
        }

        private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            transform.position = to;
        }
    }
}
