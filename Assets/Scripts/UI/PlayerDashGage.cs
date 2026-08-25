using UnityEngine;
using UnityEngine.UI;

namespace Team1
{
    public class PlayerDashGage : MonoBehaviour
    {
        [SerializeField] private PlayerDash _playerDash;
        [SerializeField] private Image _dashGageImage;

        private void Awake()
        {
            _playerDash = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerDash>();
            _dashGageImage = GetComponent<Image>();

            Debug.Assert(_playerDash != null, $"{nameof(_playerDash)} is not assigned.", this);
            Debug.Assert(_dashGageImage != null, $"{nameof(_dashGageImage)} is not assigned.", this);
        }

        private void Update()
        {
            _dashGageImage.fillAmount = _playerDash.CurrentGauge / _playerDash.MaxGauge;
        }
    }
}
