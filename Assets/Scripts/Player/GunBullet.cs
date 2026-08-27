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
        // 見た目(スプライト)のみを拡大する子オブジェクト。当たり判定(ルートのCollider2D)とは別に拡大率を指定できるようにする
        [SerializeField] private Transform _visual;
        [SerializeField] private CircleCollider2D _collider;

        private Vector3 _direction;
        private int _damage;
        private float _baseColliderRadius;

        private void Awake()
        {
            Debug.Assert(_visual != null, $"{nameof(_visual)} is not assigned.", this);
            Debug.Assert(_collider != null, $"{nameof(_collider)} is not assigned.", this);

            if (_collider != null)
            {
                _baseColliderRadius = _collider.radius;
            }
        }

        // 見た目とコライダーそれぞれの拡大率を個別に適用する。
        // コライダーを見た目と同じ倍率で拡大すると当たり判定が過大に感じられ、
        // 逆にまったく拡大しないと見た目に対して当たり判定が小さすぎて外れやすくなるため、別々に指定する。
        public void SetChargeScale(float visualMultiplier, float colliderMultiplier)
        {
            if (_visual != null)
            {
                _visual.localScale = Vector3.one * visualMultiplier;
            }

            if (_collider != null)
            {
                _collider.radius = _baseColliderRadius * colliderMultiplier;
            }
        }

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

                // 銃の弾は背面攻撃・ナイフ撃破の対象外。ナイフ攻撃直後に銃で倒された場合でも誤計上されないよう、フラグを明示的に解除する
                if (other.TryGetComponent(out EnemyBase enemyBase))
                {
                    enemyBase.NotifyHitSource(isKnife: false, isBackstab: false);
                }

                damageable.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
