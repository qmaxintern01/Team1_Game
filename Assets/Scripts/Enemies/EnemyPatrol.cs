using UnityEngine;

namespace Team1
{
    public class EnemyPatrol : MonoBehaviour, IFacingDirection
    {
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _arrivalThreshold = 0.05f;
        // EnemyBaseが同じGameObjectにあれば、そちらのDetectionRangeを優先して使う(索敵距離の二重管理を避けるため)
        [SerializeField] private float _detectionRange = 5f;
        [SerializeField] private float _minSeparationDistance = 1f;
        [SerializeField] private float _wallCollisionRadius = 0.45f;
        [SerializeField] private LayerMask _wallLayer;
        // 1秒あたりに旋回できる最大角度。これより速くプレイヤーが横移動すると、旋回が追いつかず背後に回り込める
        [SerializeField] private float _turnSpeedDegreesPerSecond = 180f;

        [Header("見た目")]
        [SerializeField] private Animator _animator;

        private GameObject _player;
        private EnemyBase _enemyBase;
        private EnemyPatrol[] _otherEnemies;
        private int _currentWaypointIndex;

        private float DetectionRange => _enemyBase != null ? _enemyBase.DetectionRange : _detectionRange;

        // 追跡・巡回移動の方向を保持し、静止中も直前の向きを維持する
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        // FacingDirectionを上下左右4方向に丸めたもの。Animatorのスプライト切り替えと同じ値
        public Vector2 DiscreteFacingDirection { get; private set; } = Vector2.down;

        private void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            _enemyBase = GetComponent<EnemyBase>();

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            // エラー確認
            Debug.Assert(_waypoints != null && _waypoints.Length > 0, $"{nameof(_waypoints)} is not assigned.", this);
            Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);
            Debug.Assert(_animator != null, $"{nameof(_animator)} is not assigned.", this);

            if (_wallLayer.value == 0)
            {
                _wallLayer = LayerMask.GetMask("Wall");
            }
        }

        private void Start()
        {
            // 他のエネミーとの重なり回避に使うため、全エネミーが初期化された後に一括取得してキャッシュする
            _otherEnemies = FindObjectsByType<EnemyPatrol>(FindObjectsInactive.Exclude);
        }

        private void Update()
        {
            // テレグラフ演出やジャンプ攻撃などでEnemyBase側が直接transformを動かしている間は移動を止める
            if (_enemyBase != null && _enemyBase.IsBusy)
            {
                return;
            }

            if (_player != null && Vector3.Distance(transform.position, _player.transform.position) <= DetectionRange)
            {
                ChasePlayer();
            }
            else
            {
                Patrol();
            }

            AvoidOverlap();
        }

        private void ChasePlayer()
        {
            Vector3 directionToTarget = _player.transform.position - transform.position;
            float distanceToPlayer = directionToTarget.magnitude;

            // AvoidOverlapが押し返す距離まで詰め寄ると、接近と押し返しが毎フレーム繰り返されて振動するため、
            // 最小分離距離のところで止まるように目標地点をプレイヤーの手前に置く
            if (distanceToPlayer > _minSeparationDistance)
            {
                Vector3 stopPosition = _player.transform.position - (Vector3)((Vector2)directionToTarget / distanceToPlayer * _minSeparationDistance);
                Vector3 nextPosition = Vector3.MoveTowards(transform.position, stopPosition, _moveSpeed * Time.deltaTime);
                transform.position = WallCollision.ResolveMovement(transform.position, nextPosition - transform.position, _wallCollisionRadius, _wallLayer);
            }

            UpdateFacingDirection(directionToTarget);
        }

        private void Patrol()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                return;
            }

            Transform target = _waypoints[_currentWaypointIndex];
            Vector3 directionToTarget = target.position - transform.position;
            Vector3 nextPosition = Vector3.MoveTowards(transform.position, target.position, _moveSpeed * Time.deltaTime);
            transform.position = WallCollision.ResolveMovement(transform.position, nextPosition - transform.position, _wallCollisionRadius, _wallLayer);
            UpdateFacingDirection(directionToTarget);

            if (Vector3.Distance(transform.position, target.position) <= _arrivalThreshold)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
            }
        }

        // AvoidOverlapによる位置補正のノイズを拾わないよう、実際の移動量ではなく目標へ向かう方向を使う
        private void UpdateFacingDirection(Vector3 directionToTarget)
        {
            bool isMoving = directionToTarget.sqrMagnitude > 0.0001f;

            if (isMoving)
            {
                Vector2 targetDirection = ((Vector2)directionToTarget).normalized;
                float currentAngle = Mathf.Atan2(FacingDirection.y, FacingDirection.x) * Mathf.Rad2Deg;
                float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
                float nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, _turnSpeedDegreesPerSecond * Time.deltaTime);
                FacingDirection = new Vector2(Mathf.Cos(nextAngle * Mathf.Deg2Rad), Mathf.Sin(nextAngle * Mathf.Deg2Rad));

                // 主軸(絶対値が大きい方)だけを残し、もう片方を0にして上下左右4方向に丸める
                DiscreteFacingDirection = Mathf.Abs(FacingDirection.x) >= Mathf.Abs(FacingDirection.y)
                    ? new Vector2(Mathf.Sign(FacingDirection.x), 0f)
                    : new Vector2(0f, Mathf.Sign(FacingDirection.y));
            }

            ApplyAnimatorDirection(isMoving);
        }

        // DiscreteFacingDirectionをAnimatorへ渡し、向きの切り替えをAnimation側(ステートマシン)に任せる。
        // XY両方を同時に渡すと斜め移動時にLeft/Right⇔Up/Downのステートを1フレームごとに往復してしまうため、
        // 主軸だけを渡し、もう片方は必ず0にして遷移条件が同時に真になるのを防ぐ(DiscreteFacingDirection側で丸め済み)
        private void ApplyAnimatorDirection(bool isMoving)
        {
            if (_animator == null)
            {
                return;
            }

            _animator.SetFloat("MoveX", DiscreteFacingDirection.x);
            _animator.SetFloat("MoveY", DiscreteFacingDirection.y);
            _animator.SetBool("isMove", isMoving);
        }

        private void AvoidOverlap()
        {
            if (_player != null)
            {
                SeparateFrom(_player.transform);
            }

            if (_otherEnemies == null)
            {
                return;
            }

            foreach (EnemyPatrol other in _otherEnemies)
            {
                if (other == null || other == this)
                {
                    continue;
                }

                SeparateFrom(other.transform);
            }
        }

        private void SeparateFrom(Transform other)
        {
            Vector3 offset = transform.position - other.position;
            float distance = offset.magnitude;

            if (distance <= 0f || distance >= _minSeparationDistance)
            {
                return;
            }

            Vector3 pushDirection = offset / distance;
            transform.position += pushDirection * (_minSeparationDistance - distance);
        }

#if UNITY_EDITOR
        // 向きをSceneビューで確認するためのデバッグ表示。シアン=連続値、マゼンタ=攻撃判定に使う4方向丸め後
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, (Vector3)FacingDirection * 2f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position, (Vector3)DiscreteFacingDirection * 1.6f);
        }
#endif
    }
}