using UnityEngine;

namespace Team1
{
    // WeakEnemyの銃攻撃が生成する弾
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 8f;
        [SerializeField] private float _lifeTime = 5f;
        [SerializeField] private LayerMask _targetLayer;
        // 弾の画像が右向き(0度)を基準に描かれている場合の既定値。素材の向きに合わせて調整する
        [SerializeField] private float _rotationOffsetDegrees;

        private Vector3 _direction;
        private int _damage;

        public void Launch(Vector3 direction, int damage)
        {
            _direction = direction.normalized;
            _damage = damage;
            Destroy(gameObject, _lifeTime);

            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + _rotationOffsetDegrees);
        }

        private void Update()
        {
            transform.position += _direction * (_speed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // _targetLayerに含まれない対象(発射元の敵同士など)は無視する
            if ((_targetLayer.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }

            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
