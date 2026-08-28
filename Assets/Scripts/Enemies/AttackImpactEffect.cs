using System.Collections;
using UnityEngine;

namespace Team1
{
    // 攻撃判定が実際に発生した瞬間の演出。AttackRangeIndicator(予告表示)とは別に、
    // 判定範囲を明るい色で一瞬だけ表示してすぐ消すことで「今攻撃が来た」ことを分かりやすくする
    [RequireComponent(typeof(SpriteRenderer))]
    public class AttackImpactEffect : MonoBehaviour
    {
        [SerializeField] private Color _color = new Color(1f, 1f, 0.85f, 0.9f);
        [SerializeField] private float _duration = 0.15f;
        // 発生直後にやや広がって見えるよう、開始・終了のスケール倍率をずらす
        [SerializeField] private float _startScaleMultiplier = 0.75f;
        [SerializeField] private float _endScaleMultiplier = 1.15f;

        private SpriteRenderer _spriteRenderer;
        private Coroutine _playRoutine;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.sprite = AttackRangeIndicator.GetOrCreateSectorSprite();
            _spriteRenderer.enabled = false;
        }

        // radius, facingDirection: AttackHitbox.Activateに渡すものと同じ値を渡し、判定範囲と見た目を一致させる
        public void Play(float radius, Vector2 facingDirection)
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
            }

            _playRoutine = StartCoroutine(PlayRoutine(radius, facingDirection));
        }

        private IEnumerator PlayRoutine(float radius, Vector2 facingDirection)
        {
            float spriteDiameter = _spriteRenderer.sprite.rect.width / _spriteRenderer.sprite.pixelsPerUnit;
            float baseScale = (radius * 2f) / spriteDiameter;

            if (facingDirection.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            _spriteRenderer.enabled = true;

            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);

                float scale = baseScale * Mathf.Lerp(_startScaleMultiplier, _endScaleMultiplier, t);
                transform.localScale = new Vector3(scale, scale, 1f);

                Color color = _color;
                color.a *= (1f - t);
                _spriteRenderer.color = color;

                yield return null;
            }

            _spriteRenderer.enabled = false;
            _playRoutine = null;
        }
    }
}
