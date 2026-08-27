using UnityEngine;

namespace Team1
{
    /// <summary>
    /// 自身の位置をミニマップ上に固定ピクセルサイズの色付きドットとして表示するよう、
    /// MinimapMarkerLayerに登録する。実体はワールド空間に一切生成しないため、
    /// Scene Viewや通常のゲーム画面に巨大な図形が映り込むことはない。
    /// </summary>
    public class MinimapEntityMarker : MonoBehaviour
    {
        // 既定は敵用の赤。プレイヤーはMinimapBuilderToolが青に上書きする
        [SerializeField] private Color _color = new Color(1f, 0.23f, 0.19f, 1f);
        [SerializeField] private float _size = 10f;

        private void OnEnable()
        {
            MinimapMarkerLayer.Register(transform, _color, _size);
        }

        private void OnDisable()
        {
            MinimapMarkerLayer.Unregister(transform);
        }
    }
}
