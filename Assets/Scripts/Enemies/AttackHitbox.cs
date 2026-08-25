using System.Collections.Generic;
using UnityEngine;

namespace Team1
{
    // 敵の攻撃範囲に追従する子オブジェクト用。有効化されている間だけCollider2Dのトリガーでダメージ対象を検出する
    [RequireComponent(typeof(CircleCollider2D))]
    public class AttackHitbox : MonoBehaviour
    {
        [SerializeField] private LayerMask _targetLayer;

        private CircleCollider2D _collider;
        private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
        private int _damage;

        private void Awake()
        {
            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _collider.enabled = false;
        }

        // 指定した威力・半径で当たり判定を有効化する。有効化中に既にヒットした対象は再度ダメージを受けない
        public void Activate(int damage, float radius)
        {
            _damage = damage;
            _collider.radius = radius;
            _hitTargets.Clear();
            _collider.enabled = true;
        }

        public void Deactivate()
        {
            _collider.enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((_targetLayer.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            if (other.TryGetComponent(out IDamageable damageable) && _hitTargets.Add(damageable))
            {
                damageable.TakeDamage(_damage);
            }
        }
    }
}
