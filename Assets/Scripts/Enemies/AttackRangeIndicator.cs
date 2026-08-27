using UnityEngine;

namespace Team1
{
    // 攻撃が来る前に、ヒットする範囲を半透明の扇形で予告表示するテレグラフ演出
    // AttackHitboxの前方半円判定と見た目を一致させるため、全方位の円ではなく正面向きの扇形を表示する
    [RequireComponent(typeof(SpriteRenderer))]
    public class AttackRangeIndicator : MonoBehaviour
    {
        [SerializeField] private Color _color = new Color(1f, 0f, 0f, 0.35f);

        private static Sprite _sharedSectorSprite;

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.sprite = GetOrCreateSectorSprite();
            _spriteRenderer.color = _color;
            _spriteRenderer.enabled = false;
        }

        // facingDirection: 敵の正面方向。AttackHitboxに渡すものと同じ値を渡し、表示と判定を一致させる
        public void Show(float radius, Vector2 facingDirection)
        {
            float spriteDiameter = _spriteRenderer.sprite.rect.width / _spriteRenderer.sprite.pixelsPerUnit;
            float scale = (radius * 2f) / spriteDiameter;
            transform.localScale = new Vector3(scale, scale, 1f);

            if (facingDirection.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            _spriteRenderer.enabled = true;
        }

        public void Hide()
        {
            _spriteRenderer.enabled = false;
        }

        // スプライトのローカル+X方向(未回転時は右向き)を前方として、右半分だけを塗った半円を生成する
        // Showで transform.rotation を facingDirection に合わせることで、この+X方向を実際の正面へ向ける
        private static Sprite GetOrCreateSectorSprite()
        {
            if (_sharedSectorSprite != null)
            {
                return _sharedSectorSprite;
            }

            const int size = 128;
            const float pixelsPerUnit = 128f;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float sqrRadius = center.x * center.x;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float sqrDistance = dx * dx + dy * dy;
                    bool inFrontHalf = sqrDistance <= sqrRadius && dx >= 0f;
                    texture.SetPixel(x, y, inFrontHalf ? Color.white : Color.clear);
                }
            }

            texture.Apply();

            _sharedSectorSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            return _sharedSectorSprite;
        }
    }
}
