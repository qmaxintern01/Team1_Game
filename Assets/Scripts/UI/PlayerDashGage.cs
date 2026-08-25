using UnityEngine;
using UnityEngine.UI;

namespace Team1
{
    public class PlayerDashGage : MonoBehaviour
    {
        [SerializeField] private PlayerDash _playerDash;
        [SerializeField] private Image _dashGageImage;

        [SerializeField]private float _dashInputCounter = 3.0f;
        private float _counter = 3.0f; // ダッシュ入力のカウンター


        private void Awake()
        {
            _playerDash = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerDash>();
            _dashGageImage = GetComponent<Image>();

            Debug.Assert(_playerDash != null, $"{nameof(_playerDash)} is not assigned.", this);
            Debug.Assert(_dashGageImage != null, $"{nameof(_dashGageImage)} is not assigned.", this);
        }

        private void Update()
        {
            bool isGaugeFull = _playerDash.CurrentGauge >= _playerDash.MaxGauge;

            if (_playerDash.IsDashing || !isGaugeFull)
            {
                // ダッシュ中、または満タンでない(回復中)間は表示し、非表示までのカウントをリセットする
                _counter = 0f;
                _dashGageImage.enabled = true;
            }
            else
            {
                _counter += Time.deltaTime;

                if (_counter > _dashInputCounter)
                {
                    // 満タンになってから一定時間経過したら非表示
                    _dashGageImage.enabled = false;
                }
            }

            _dashGageImage.fillAmount = _playerDash.CurrentGauge / _playerDash.MaxGauge;
        }
    }
}
