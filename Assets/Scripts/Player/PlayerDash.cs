using UnityEngine;

namespace Team1
{
    // ダッシュゲージの増減と、それに応じた移動速度倍率・緊急回避可否を管理する
    public class PlayerDash : MonoBehaviour
    {
        [SerializeField] private float _maxGauge = 100f;
        [SerializeField] private float _currentGauge = 100f;
        [SerializeField] private float _drainPerSecond = 5f;
        [SerializeField] private float _regenPerSecond = 5f;
        [SerializeField] private float _dashSpeedMultiplier = 1.5f;
        [SerializeField] private float _depletedSpeedMultiplier = 0.7f;

        private InputSystem_Actions _gameInputs;
        private bool _isDepleted;

        public float CurrentGauge => _currentGauge;
        public float MaxGauge => _maxGauge;
        public bool IsDashing { get; private set; }

        // ゲージが0になった後、全回復するまで緊急回避不可
        public bool CanEmergencyDodge => !_isDepleted;

        // ゲージが切れて回復中かどうか(全回復するまでtrue)
        public bool IsDepleted => _isDepleted;

        public float SpeedMultiplier
        {
            get
            {
                if (IsDashing)
                {
                    return _dashSpeedMultiplier;
                }

                return _isDepleted ? _depletedSpeedMultiplier : 1f;
            }
        }

        private void Awake()
        {
            _currentGauge = Mathf.Clamp(_currentGauge, 0f, _maxGauge);
        }

        private void OnEnable()
        {
            _gameInputs = new InputSystem_Actions();
            _gameInputs.Enable();
        }

        private void OnDisable()
        {
            _gameInputs.Disable();
            _gameInputs.Dispose();
        }

        private void Update()
        {
            //Debug.Log($"CurrentGauge: {_currentGauge}, IsDashing: {IsDashing}, IsDepleted: {_isDepleted}");
            bool wantsDash = !_isDepleted && _currentGauge > 0f && _gameInputs.Player.Sprint.IsPressed();

            if (wantsDash)
            {
                // ダッシュ中
                IsDashing = true;
                _currentGauge -= _drainPerSecond * Time.deltaTime;

                if (_currentGauge <= 0f)
                {
                    _currentGauge = 0f;
                    _isDepleted = true;
                    IsDashing = false;
                }
            }
            else
            {
                // ダッシュしていない
                IsDashing = false;
                _currentGauge = Mathf.Min(_maxGauge, _currentGauge + _regenPerSecond * Time.deltaTime);

                if (_isDepleted && _currentGauge >= _maxGauge)
                {
                    _isDepleted = false;
                }
            }
        }
    }
}
