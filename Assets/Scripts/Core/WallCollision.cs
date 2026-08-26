using UnityEngine;

namespace Team1
{
    // タイルマップの壁(Wallレイヤー)との衝突をXY軸ごとに判定し、壁に沿ってスライドできるようにする
    public static class WallCollision
    {
        public static Vector3 ResolveMovement(Vector3 currentPosition, Vector3 desiredDelta, float radius, LayerMask wallLayerMask)
        {
            Vector3 result = currentPosition;

            Vector3 horizontalStep = new Vector3(desiredDelta.x, 0f, 0f);
            if (horizontalStep.sqrMagnitude > 0f && !Physics2D.OverlapCircle(result + horizontalStep, radius, wallLayerMask))
            {
                result += horizontalStep;
            }

            Vector3 verticalStep = new Vector3(0f, desiredDelta.y, 0f);
            if (verticalStep.sqrMagnitude > 0f && !Physics2D.OverlapCircle(result + verticalStep, radius, wallLayerMask))
            {
                result += verticalStep;
            }

            return PushOutOfWalls(result, radius, wallLayerMask);
        }

        // XY軸別の判定だと壁の角(頂点)にちょうど触れて引っかかることがあるため、
        // 実際に重なっている壁が見つかった場合は、その壁から離れる方向へ食い込み分だけ押し出す
        private static Vector3 PushOutOfWalls(Vector3 position, float radius, LayerMask wallLayerMask)
        {
            Collider2D overlappingWall = Physics2D.OverlapCircle(position, radius, wallLayerMask);
            if (overlappingWall == null)
            {
                return position;
            }

            Vector2 closestPoint = overlappingWall.ClosestPoint(position);
            Vector2 offset = (Vector2)position - closestPoint;
            float distance = offset.magnitude;

            if (distance <= 0.0001f)
            {
                return position;
            }

            float penetration = radius - distance;
            if (penetration <= 0f)
            {
                return position;
            }

            Vector2 pushDirection = offset / distance;
            return position + (Vector3)(pushDirection * penetration);
        }
    }
}
