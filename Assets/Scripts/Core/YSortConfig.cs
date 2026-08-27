using UnityEngine;

namespace Team1
{
    // プレイヤーと敵のSpriteRendererの描画順を、Y座標から同じ計算式で揃えるための共有設定。
    // SortingLayer/SortingOrderが同値の場合、UnityはZ位置(カメラ距離)などで前後を決めてしまい、
    // 意図せず特定のキャラクターが常に手前/奥に固定されることがあるため、Y座標基準で明示的に上書きする。
    public static class YSortConfig
    {
        // SpriteRenderer.sortingOrderは内部的に16bit(-32768〜32767)にクランプされるため、
        // マップのY範囲(現状 約-91〜89)に十分な余裕を持たせつつこの範囲に収まる値にする。
        // マップのTilemap(Ground/Walls/Decor)はSortingOrder 0〜2を使用しているため、
        // Y座標の変動でこの値を下回らないようにする
        public const int BaseOrder = 20000;
        public const float PrecisionMultiplier = 50f;

        // HPバーなど、キャラクターの前後関係に関わらず常に最前面に表示したいワールド空間UI用。
        // キャラクターのSortingOrderが取りうる最大値(BaseOrder付近)より確実に大きい値にする
        public const int WorldSpaceUIOrder = 30000;

        // Y座標が小さい(画面下寄り = 手前)ほど前面に表示されるようにする
        public static int CalculateSortingOrder(float yPosition)
        {
            return BaseOrder - Mathf.RoundToInt(yPosition * PrecisionMultiplier);
        }
    }
}
