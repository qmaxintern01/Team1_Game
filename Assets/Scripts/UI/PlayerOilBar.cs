using UnityEngine;
using UnityEngine.UI;

namespace Team1.UI
{
    public class PlayerOilBar : MonoBehaviour
    {
        [SerializeField] private PlayerOil _playerOil;
        [SerializeField] private Slider _slider;

        private void Awake()
        {
            _playerOil = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerOil>();
            Debug.Assert(_playerOil != null, $"{nameof(_playerOil)} is not assigned.", this);
            Debug.Assert(_slider != null, $"{nameof(_slider)} is not assigned.", this);
        }

        private void OnEnable()
        {
            if (_playerOil == null || _slider == null)
            {
                return;
            }

            _playerOil.OnOilChanged += HandleOilChanged;
            HandleOilChanged(_playerOil.CurrentOil, _playerOil.MaxOil);
        }

        private void OnDisable()
        {
            if (_playerOil == null)
            {
                return;
            }

            _playerOil.OnOilChanged -= HandleOilChanged;
        }

        private void HandleOilChanged(int current, int max)
        {
            _slider.maxValue = max;
            _slider.value = current;
        }
    }
}
