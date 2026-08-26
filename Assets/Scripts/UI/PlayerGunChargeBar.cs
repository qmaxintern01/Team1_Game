using UnityEngine;
using UnityEngine.UI;

namespace Team1.UI
{
    public class PlayerGunChargeBar : MonoBehaviour
    {
        [SerializeField] private PlayerGunAttack _gunAttack;
        [SerializeField] private Slider _slider;

        private void Awake()
        {
            _gunAttack = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerGunAttack>();
            Debug.Assert(_gunAttack != null, $"{nameof(_gunAttack)} is not assigned.", this);
            Debug.Assert(_slider != null, $"{nameof(_slider)} is not assigned.", this);

            _slider.minValue = 0f;
            _slider.maxValue = 1f;
        }

        private void Update()
        {
            _slider.value = _gunAttack.ChargeProgress01;
        }
    }
}
