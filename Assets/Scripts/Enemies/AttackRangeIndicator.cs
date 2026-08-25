using UnityEngine;

namespace Team1
{
    // 攻撃が来る前に、ヒットする範囲を半透明の円で予告表示するテレグラフ演出
    [RequireComponent(typeof(SpriteRenderer))]
    public class AttackRangeIndicator : MonoBehaviour
    {
        [SerializeField] private Color _color = new Color(1f, 0f, 0f, 0.35f);

        private static Sprite _sharedCircleSprite;

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.sprite = GetOrCreateCircleSprite();
            _spriteRenderer.color = _color;
            _spriteRenderer.enabled = false;
        }

        public void Show(float radius)
        {
            float spriteDiameter = _spriteRenderer.sprite.rect.width / _spriteRenderer.sprite.pixelsPerUnit;
            float scale = (radius * 2f) / spriteDiameter;
            transform.localScale = new Vector3(scale, scale, 1f);
            _spriteRenderer.enabled = true;
        }

        public void Hide()
        {
            _spriteRenderer.enabled = false;
        }

        private static Sprite GetOrCreateCircleSprite()
        {
            if (_sharedCircleSprite != null)
            {
                return _sharedCircleSprite;
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
                    float sqrDistance = (x - center.x) * (x - center.x) + (y - center.y) * (y - center.y);
                    texture.SetPixel(x, y, sqrDistance <= sqrRadius ? Color.white : Color.clear);
                }
            }

            texture.Apply();

            _sharedCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            return _sharedCircleSprite;
        }
    }
}
