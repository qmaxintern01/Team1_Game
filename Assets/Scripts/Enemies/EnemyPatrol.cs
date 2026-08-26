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

        [Header("見た目")]
        [SerializeField] private Animator _animator;

        private GameObject _player;
        private EnemyBase _enemyBase;
        private EnemyPatrol[] _otherEnemies;
        private int _currentWaypointIndex;

        private float DetectionRange => _enemyBase != null ? _enemyBase.DetectionRange : _detectionRange;

        // 追跡・巡回移動の方向を保持し、静止中も直前の向きを維持する
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

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
            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, _moveSpeed * Time.deltaTime);
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
            transform.position = Vector3.MoveTowards(transform.position, target.position, _moveSpeed * Time.deltaTime);
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
                FacingDirection = ((Vector2)directionToTarget).normalized;
            }

            ApplyAnimatorDirection(isMoving);
        }

        // 移動方向をAnimatorへ渡し、向きの切り替えをAnimation側(ブレンドツリー等)に任せる
        private void ApplyAnimatorDirection(bool isMoving)
        {
            if (_animator == null)
            {
                return;
            }

            _animator.SetFloat("MoveX", FacingDirection.x);
            _animator.SetFloat("MoveY", FacingDirection.y);
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
    }
}
