using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Team1
{
    // WeakEnemyをマップ上にランダム出現させ、上限数を維持するマネージャー。
    // 1体倒されるとクールタイムの後に1体だけ補充されるため、常に上限数を超えない。
    public class EnemyManager : MonoBehaviour
    {
        // Playerと同じZ座標(-0.01)に統一し、スプライトの描画順を安定させる
        private const float SpawnZ = -0.01f;

        [Header("出現させる敵")]
        [SerializeField] private WeakEnemy _enemyPrefab;

        [Header("出現数")]
        [SerializeField] private int _maxEnemyCount = 20;
        [SerializeField] private float _respawnCooldown = 20f;

        [Header("出現範囲")]
        // Groundとして使用しているTilemapをInspectorで割り当てる。同名の"Tilemap"がシーン内に複数存在するため自動検索はしない
        [SerializeField] private Tilemap _groundTilemap;
        [SerializeField] private float _spawnCheckRadius = 0.5f;
        [SerializeField] private int _maxSpawnAttempts = 30;
        [SerializeField] private LayerMask _wallLayer;

        [Header("BigBoss周辺の出現除外")]
        // BigBossを中心とした矩形(半径ではなく半辺長)で、ボスの部屋全体を出現候補から除外する
        [SerializeField] private Vector2 _bossExclusionHalfExtents = new Vector2(9f, 5f);

        private readonly List<Vector3Int> _floorCells = new List<Vector3Int>();
        private BigBoss _bigBoss;
        private int _aliveCount;

        private void Awake()
        {
            // エラー確認
            Debug.Assert(_enemyPrefab != null, $"{nameof(_enemyPrefab)} is not assigned.", this);
            Debug.Assert(_groundTilemap != null, $"{nameof(_groundTilemap)} is not assigned.", this);

            if (_wallLayer.value == 0)
            {
                _wallLayer = LayerMask.GetMask("Wall");
            }

            _bigBoss = FindAnyObjectByType<BigBoss>();
            CacheFloorCells();
        }

        private void Start()
        {
            if (_enemyPrefab == null || _floorCells.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _maxEnemyCount; i++)
            {
                SpawnEnemy();
            }
        }

        // 壁で囲われた実際の床セルだけを出現候補として集める。マップ外の空白地帯を出現範囲に含めないため
        private void CacheFloorCells()
        {
            _floorCells.Clear();

            if (_groundTilemap == null)
            {
                return;
            }

            BoundsInt bounds = _groundTilemap.cellBounds;
            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (!_groundTilemap.HasTile(cell))
                {
                    continue;
                }

                if (_bigBoss != null)
                {
                    Vector3 worldPosition = _groundTilemap.GetCellCenterWorld(cell);
                    if (IsInsideBossExclusionArea(worldPosition))
                    {
                        continue;
                    }
                }

                _floorCells.Add(cell);
            }
        }

        private bool IsInsideBossExclusionArea(Vector3 worldPosition)
        {
            Vector3 offset = worldPosition - _bigBoss.transform.position;
            return Mathf.Abs(offset.x) < _bossExclusionHalfExtents.x && Mathf.Abs(offset.y) < _bossExclusionHalfExtents.y;
        }

        private void SpawnEnemy()
        {
            if (!TryFindSpawnPosition(out Vector3 position))
            {
                Debug.LogWarning($"{nameof(EnemyManager)}: 出現位置が見つかりませんでした。出現範囲・壁レイヤーの設定を確認してください。", this);
                return;
            }

            WeakEnemy enemy = Instantiate(_enemyPrefab, position, Quaternion.identity);
            _aliveCount++;

            Health health = enemy.GetComponent<Health>();
            if (health != null)
            {
                health.OnDied += HandleEnemyDied;
            }
        }

        private void HandleEnemyDied()
        {
            _aliveCount--;
            StartCoroutine(RespawnAfterCooldown());
        }

        private IEnumerator RespawnAfterCooldown()
        {
            yield return new WaitForSeconds(_respawnCooldown);

            if (_aliveCount < _maxEnemyCount)
            {
                SpawnEnemy();
            }
        }

        // 床セルの中からランダムに選び、壁(_wallLayer)と重ならない位置が見つかるまで試行する
        private bool TryFindSpawnPosition(out Vector3 position)
        {
            for (int i = 0; i < _maxSpawnAttempts && _floorCells.Count > 0; i++)
            {
                Vector3Int cell = _floorCells[Random.Range(0, _floorCells.Count)];
                Vector3 candidate = _groundTilemap.GetCellCenterWorld(cell);
                candidate.z = SpawnZ;

                if (Physics2D.OverlapCircle(candidate, _spawnCheckRadius, _wallLayer) == null)
                {
                    position = candidate;
                    return true;
                }
            }

            position = Vector3.zero;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (_groundTilemap != null)
            {
                Gizmos.color = Color.yellow;
                foreach (Vector3Int cell in _floorCells)
                {
                    Gizmos.DrawWireCube(_groundTilemap.GetCellCenterWorld(cell), _groundTilemap.cellSize);
                }
            }

            // 再生前でもボス除外範囲をScene上で確認・調整できるようにする
            BigBoss boss = _bigBoss != null ? _bigBoss : FindAnyObjectByType<BigBoss>();
            if (boss != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(boss.transform.position, new Vector3(_bossExclusionHalfExtents.x * 2f, _bossExclusionHalfExtents.y * 2f, 0f));
            }
        }
    }
}
