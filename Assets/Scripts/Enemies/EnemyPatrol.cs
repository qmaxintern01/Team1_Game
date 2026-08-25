using UnityEngine;

namespace Team1
{
    public class EnemyPatrol : MonoBehaviour, IFacingDirection
    {
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _arrivalThreshold = 0.05f;
        [SerializeField] private float _detectionRange = 5f;
        [SerializeField] private float _minSeparationDistance = 1f;

        private GameObject _player;
        private EnemyPatrol[] _otherEnemies;
        private int _currentWaypointIndex;

        // 追跡・巡回移動の方向を保持し、静止中も直前の向きを維持する
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        private void Awake()
        {
            _player = GameObject.FindGameObjectWithTag("Player");

            // エラー確認
            Debug.Assert(_waypoints != null && _waypoints.Length > 0, $"{nameof(_waypoints)} is not assigned.", this);
            Debug.Assert(_player != null, $"{nameof(_player)} is not assigned.", this);
        }

        private void Start()
        {
            // 他のエネミーとの重なり回避に使うため、全エネミーが初期化された後に一括取得してキャッシュする
            _otherEnemies = FindObjectsByType<EnemyPatrol>(FindObjectsInactive.Exclude);
        }

        private void Update()
        {
            if (_player != null && Vector3.Distance(transform.position, _player.transform.position) <= _detectionRange)
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
            Vector3 previousPosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, _moveSpeed * Time.deltaTime);
            UpdateFacingDirection(transform.position - previousPosition);
        }

        private void Patrol()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                return;
            }

            Transform target = _waypoints[_currentWaypointIndex];
            Vector3 previousPosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, target.position, _moveSpeed * Time.deltaTime);
            UpdateFacingDirection(transform.position - previousPosition);

            if (Vector3.Distance(transform.position, target.position) <= _arrivalThreshold)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
            }
        }

        private void UpdateFacingDirection(Vector3 movementDelta)
        {
            if (movementDelta.sqrMagnitude > 0.0001f)
            {
                FacingDirection = ((Vector2)movementDelta).normalized;
            }
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
