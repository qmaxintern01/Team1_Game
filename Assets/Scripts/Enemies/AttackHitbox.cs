using System.Collections.Generic;
using UnityEngine;

namespace Team1
{
    // 敵の攻撃範囲に追従する子オブジェクト用。有効化されている間だけCollider2Dのトリガーでダメージ対象を検出する
    [RequireComponent(typeof(CircleCollider2D))]
    public class AttackHitbox : MonoBehaviour
    {
        [SerializeField] private LayerMask _targetLayer;
        // 敵の正面からこの角度以内(片側)だけ攻撃が当たる。90度=前方半円。180度にすると全方位(従来動作)になる
        [SerializeField, Range(0f, 180f)] private float _frontHalfAngle = 90f;

        private CircleCollider2D _collider;
        private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
        private int _damage;
        private Vector2 _facingDirection = Vector2.up;

        private void Awake()
        {
            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _collider.enabled = false;
        }

        // 指定した威力・半径で当たり判定を有効化する。有効化中に既にヒットした対象は再度ダメージを受けない
        // facingDirection: 敵の正面方向。背後に回り込んだ対象には攻撃が当たらないようにするために使う
        public void Activate(int damage, float radius, Vector2 facingDirection)
        {
            _damage = damage;
            _collider.radius = radius;
            _facingDirection = facingDirection.sqrMagnitude > 0.0001f ? facingDirection.normalized : Vector2.up;
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

            // transform.position(このヒットボックス)から見たtargetへの方向であって、targetからこのヒットボックスへの方向ではない点に注意
            Vector2 directionToTarget = (Vector2)other.transform.position - (Vector2)transform.position;
            if (directionToTarget.sqrMagnitude > 0.0001f && Vector2.Angle(_facingDirection, directionToTarget) > _frontHalfAngle)
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
