using UnityEngine;

namespace Team1
{
    // WeakEnemyの銃攻撃が生成する弾
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 8f;
        [SerializeField] private float _lifeTime = 5f;
        [SerializeField] private LayerMask _targetLayer;

        private Vector3 _direction;
        private int _damage;

        public void Launch(Vector3 direction, int damage)
        {
            _direction = direction.normalized;
            _damage = damage;
            Destroy(gameObject, _lifeTime);
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
