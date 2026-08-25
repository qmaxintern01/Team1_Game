using UnityEngine;
using UnityEngine.UI;

namespace Team1.UI
{
    public class PlayerHealthBar : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Slider _slider;

        private void Awake()
        {
            Debug.Assert(_health != null, $"{nameof(_health)} is not assigned.", this);
            Debug.Assert(_slider != null, $"{nameof(_slider)} is not assigned.", this);
        }

        private void OnEnable()
        {
            if (_health == null || _slider == null)
            {
                return;
            }

            _health.OnHpChanged += HandleHpChanged;
            HandleHpChanged(_health.CurrentHp, _health.MaxHp);
        }

        private void OnDisable()
        {
            if (_health == null)
            {
                return;
            }

            _health.OnHpChanged -= HandleHpChanged;
        }

        private void HandleHpChanged(int current, int max)
        {
            _slider.maxValue = max;
            _slider.value = current;
        }
    }
}
