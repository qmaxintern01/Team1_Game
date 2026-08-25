using UnityEngine;

namespace Team1
{
    // ナイフの背面判定など、移動方向ベースの向きを外部から参照するためのインターフェース
    public interface IFacingDirection
    {
        Vector2 FacingDirection { get; }
    }
}
