using UnityEngine;

namespace Team1
{
    // グレネードランチャーの榴弾。敵に着弾するか、信管(_fuseTime)が切れると爆発し、周囲に範囲ダメージを与える
    public class GrenadeProjectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 8f;
        [SerializeField] private float _fuseTime = 1.2f;
        [SerializeField] private float _explosionRadius = 2f;
        // 弾の画像が右向き(0度)を基準に描かれている場合の既定値。素材の向きに合わせて調整する
        [SerializeField] private float _rotationOffsetDegrees;
        [SerializeField] private Transform _visual;
        [SerializeField] private CircleCollider2D _collider;
        [SerializeField] private LayerMask _enemyLayer;

        [Header("爆発演出")]
        [SerializeField] private Color _explosionColor = new Color(1f, 0.55f, 0.15f, 0.85f);
        [SerializeField] private float _explosionEffectDuration = 0.25f;
        // 爆発後、演出が消えるまでの間だけ本体を残しておくための待機時間(_explosionEffectDurationと合わせる)
        [SerializeField] private float _destroyDelayAfterExplosion = 0.25f;

        private Vector3 _direction;
        private int _damage;
        private bool _hasExploded;

        private float _baseColliderRadius;
        private float _baseExplosionRadius;
        private float _effectiveExplosionRadius;

        private void Awake()
        {
            Debug.Assert(_visual != null, $"{nameof(_visual)} is not assigned.", this);
            Debug.Assert(_collider != null, $"{nameof(_collider)} is not assigned.", this);

            if (_collider != null)
            {
                _baseColliderRadius = _collider.radius;
            }

            _baseExplosionRadius = _explosionRadius;
            _effectiveExplosionRadius = _explosionRadius;
        }

        // AR(PlayerGunAttack/GunBullet)と同様、チャージ量に応じて見た目・着弾判定・爆発範囲をそれぞれ別の倍率で拡大する。
        // 全て見た目と同率で拡大すると過大に感じられるため、着弾判定と爆発範囲は控えめな倍率を渡す想定
        public void SetChargeScale(float visualMultiplier, float colliderMultiplier, float explosionRadiusMultiplier)
        {
            if (_visual != null)
            {
                _visual.localScale = Vector3.one * visualMultiplier;
            }

            if (_collider != null)
            {
                _collider.radius = _baseColliderRadius * colliderMultiplier;
            }

            _effectiveExplosionRadius = _baseExplosionRadius * explosionRadiusMultiplier;
        }

        public void Launch(Vector3 direction, int damage)
        {
            _direction = direction.normalized;
            _damage = damage;

            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + _rotationOffsetDegrees);

            // 何にも当たらなかった場合でも、信管の時間切れで自爆させる
            Invoke(nameof(Explode), _fuseTime);
        }

        private void Update()
        {
            transform.position += _direction * (_speed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out IDamageable _))
            {
                Explode();
            }
        }

        private void Explode()
        {
            if (_hasExploded)
            {
                return;
            }

            _hasExploded = true;
            CancelInvoke(nameof(Explode));

            AudioManager.Instance?.PlayGrenadeExplosionSe();

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _effectiveExplosionRadius, _enemyLayer);
            Debug.Log($"グレネード爆発: position={transform.position}, radius={_effectiveExplosionRadius}, hit数={hits.Length}");

            foreach (Collider2D hit in hits)
            {
                if (!hit.TryGetComponent(out IDamageable damageable))
                {
                    continue;
                }

                // 銃系の弾と同様、ナイフ撃破・背面撃破のボーナス対象外として明示的に伝える
                if (hit.TryGetComponent(out EnemyBase enemyBase))
                {
                    enemyBase.NotifyHitSource(isKnife: false, isBackstab: false);
                }

                damageable.TakeDamage(_damage);
            }

            SpawnExplosionEffect();

            if (_visual != null)
            {
                _visual.gameObject.SetActive(false);
            }

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            Destroy(gameObject, _destroyDelayAfterExplosion);
        }

        private void SpawnExplosionEffect()
        {
            var effectObject = new GameObject("GrenadeExplosionEffect");
            effectObject.transform.position = transform.position;

            GrenadeExplosionEffect effect = effectObject.AddComponent<GrenadeExplosionEffect>();
            effect.Play(_effectiveExplosionRadius, _explosionColor, _explosionEffectDuration);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _effectiveExplosionRadius);
        }
#endif
    }
}
