using UnityEngine;

namespace Team1
{
    // ナイフの背面判定など、移動方向ベースの向きを外部から参照するためのインターフェース
    public interface IFacingDirection
    {
        Vector2 FacingDirection { get; }

        // Animatorが実際に表示している上下左右4方向に丸めた向き。見た目(スプライト)と一致させたい判定に使う
        Vector2 DiscreteFacingDirection { get; }
    }
}
