using UnityEngine;

namespace Team1
{
    public class EnemyPatrol : MonoBehaviour
    {
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _arrivalThreshold = 0.05f;
        [SerializeField] private float _detectionRange = 5f;
        [SerializeField] private float _minSeparationDistance = 1f;

        private GameObject _player;
        private EnemyPatrol[] _otherEnemies;
        private int _currentWaypointIndex;

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
            _otherEnemies = FindObjectsByType<EnemyPatrol>(FindObjectsSortMode.None);
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
            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, _moveSpeed * Time.deltaTime);
        }

        private void Patrol()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                return;
            }

            Transform target = _waypoints[_currentWaypointIndex];
            transform.position = Vector3.MoveTowards(transform.position, target.position, _moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) <= _arrivalThreshold)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
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
