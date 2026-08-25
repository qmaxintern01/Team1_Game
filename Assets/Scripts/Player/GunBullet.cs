using UnityEngine;

namespace Team1
{
    // ARの弾。チャージ量に応じたダメージ・サイズはPlayerGunAttack側で設定してから発射する
    public class GunBullet : MonoBehaviour
    {
        [SerializeField] private float _speed = 12f;
        [SerializeField] private float _lifeTime = 3f;
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
            if (other.TryGetComponent(out IDamageable damageable))
            {
                Debug.Log($"Bullet hit {other.gameObject.name} for {_damage} damage.");
                damageable.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
