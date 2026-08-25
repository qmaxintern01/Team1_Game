using UnityEngine;

namespace Team1
{
    // ARの弾。チャージ量に応じたダメージ・サイズはPlayerGunAttack側で設定してから発射する
    public class GunBullet : MonoBehaviour
    {
        [SerializeField] private float _speed = 12f;
        [SerializeField] private float _lifeTime = 3f;

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
            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
