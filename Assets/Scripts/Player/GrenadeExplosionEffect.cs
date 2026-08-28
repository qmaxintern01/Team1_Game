using System.Collections;
using UnityEngine;

namespace Team1
{
    // グレネードの爆発演出。指定した半径いっぱいに円形スプライトを表示し、フェードアウトしながら自身を破棄する
    [RequireComponent(typeof(SpriteRenderer))]
    public class GrenadeExplosionEffect : MonoBehaviour
    {
        private static Sprite _sharedCircleSprite;

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.sprite = GetOrCreateCircleSprite();
        }

        public void Play(float radius, Color color, float duration)
        {
            float spriteDiameter = _spriteRenderer.sprite.rect.width / _spriteRenderer.sprite.pixelsPerUnit;
            float scale = (radius * 2f) / spriteDiameter;
            transform.localScale = new Vector3(scale, scale, 1f);

            // 敵の描画順よりは必ず前面になるよう、プレイヤーと同じY基準のSortingOrder計算式に1を足して底上げする
            _spriteRenderer.sortingOrder = YSortConfig.CalculateSortingOrder(transform.position.y) + 1;
            _spriteRenderer.color = color;

            StartCoroutine(FadeAndDestroyRoutine(color, duration));
        }

        private IEnumerator FadeAndDestroyRoutine(Color color, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Color faded = color;
                faded.a *= 1f - Mathf.Clamp01(elapsed / duration);
                _spriteRenderer.color = faded;
                yield return null;
            }

            Destroy(gameObject);
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
                    float dx = x - center.x;
                    float dy = y - center.y;
                    bool inCircle = dx * dx + dy * dy <= sqrRadius;
                    texture.SetPixel(x, y, inCircle ? Color.white : Color.clear);
                }
            }

            texture.Apply();

            _sharedCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            return _sharedCircleSprite;
        }
    }
}
