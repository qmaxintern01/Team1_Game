using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Team1
{
    /// <summary>
    /// ミニマップ上のプレイヤー・敵の位置を、ワールド空間のオブジェクトではなく
    /// ミニマップパネル内のUI要素(固定ピクセルサイズの円)として描画する。
    /// ミニマップカメラのビューポート座標(0〜1)をパネルの矩形サイズに変換して配置するため、
    /// ワールド座標に巨大なスプライトを置く必要がなく、Scene Viewや通常のゲーム画面に影響しない。
    /// </summary>
    public class MinimapMarkerLayer : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera _minimapCamera;
        [SerializeField] private RectTransform _mapArea;

        private static MinimapMarkerLayer _instance;
        private static Sprite _sharedDotSprite;

        private readonly Dictionary<Transform, RectTransform> _markers = new Dictionary<Transform, RectTransform>();

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static void Register(Transform target, Color color, float size)
        {
            if (_instance == null || target == null)
            {
                return;
            }

            _instance.RegisterInternal(target, color, size);
        }

        public static void Unregister(Transform target)
        {
            if (_instance == null || target == null)
            {
                return;
            }

            _instance.UnregisterInternal(target);
        }

        private void RegisterInternal(Transform target, Color color, float size)
        {
            if (_mapArea == null || _markers.ContainsKey(target))
            {
                return;
            }

            var dotGo = new GameObject("MinimapDot", typeof(RectTransform));
            var rect = (RectTransform)dotGo.transform;
            rect.SetParent(_mapArea, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            var image = dotGo.AddComponent<Image>();
            image.sprite = GetOrCreateDotSprite();
            image.color = color;
            image.raycastTarget = false;

            _markers[target] = rect;
        }

        private void UnregisterInternal(Transform target)
        {
            if (_markers.TryGetValue(target, out var rect))
            {
                if (rect != null)
                {
                    Destroy(rect.gameObject);
                }

                _markers.Remove(target);
            }
        }

        private void LateUpdate()
        {
            if (_minimapCamera == null || _mapArea == null || _markers.Count == 0)
            {
                return;
            }

            Vector2 areaSize = _mapArea.rect.size;
            List<Transform> stale = null;

            foreach (var pair in _markers)
            {
                if (pair.Key == null)
                {
                    (stale ??= new List<Transform>()).Add(pair.Key);
                    continue;
                }

                Vector3 viewport = _minimapCamera.WorldToViewportPoint(pair.Key.position);
                Vector2 clamped = new Vector2(Mathf.Clamp01(viewport.x), Mathf.Clamp01(viewport.y));
                pair.Value.anchoredPosition = new Vector2((clamped.x - 0.5f) * areaSize.x, (clamped.y - 0.5f) * areaSize.y);
            }

            // 破棄済みの対象(倒された敵など)のドットが取りこぼされた場合の保険。通常はMinimapEntityMarker.OnDisableで片付く
            if (stale != null)
            {
                foreach (var key in stale)
                {
                    if (_markers.TryGetValue(key, out var rect) && rect != null)
                    {
                        Destroy(rect.gameObject);
                    }

                    _markers.Remove(key);
                }
            }
        }

        private static Sprite GetOrCreateDotSprite()
        {
            if (_sharedDotSprite != null)
            {
                return _sharedDotSprite;
            }

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
            };

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    texture.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            _sharedDotSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return _sharedDotSprite;
        }
    }
}
