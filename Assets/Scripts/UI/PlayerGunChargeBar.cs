using UnityEngine;
using UnityEngine.UI;

namespace Team1.UI
{
    // AR(PlayerGunAttack)とグレネードランチャー(PlayerGrenadeAttack)は同じ仕組みでチャージするため、
    // ゲージUIも1つを共用し、現在装備中の武器に応じてどちらの進捗を表示するか切り替える
    public class PlayerGunChargeBar : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponSwitcher _weaponSwitcher;
        [SerializeField] private PlayerGunAttack _gunAttack;
        [SerializeField] private PlayerGrenadeAttack _grenadeAttack;
        [SerializeField] private Slider _slider;

        private void Awake()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            _weaponSwitcher = player.GetComponent<PlayerWeaponSwitcher>();
            _gunAttack = player.GetComponent<PlayerGunAttack>();
            _grenadeAttack = player.GetComponent<PlayerGrenadeAttack>();

            Debug.Assert(_weaponSwitcher != null, $"{nameof(_weaponSwitcher)} is not assigned.", this);
            Debug.Assert(_gunAttack != null, $"{nameof(_gunAttack)} is not assigned.", this);
            Debug.Assert(_grenadeAttack != null, $"{nameof(_grenadeAttack)} is not assigned.", this);
            Debug.Assert(_slider != null, $"{nameof(_slider)} is not assigned.", this);

            _slider.minValue = 0f;
            _slider.maxValue = 1f;
        }

        private void Update()
        {
            _slider.value = GetCurrentChargeProgress01();
        }

        private float GetCurrentChargeProgress01()
        {
            if (_weaponSwitcher == null)
            {
                return 0f;
            }

            switch (_weaponSwitcher.CurrentWeapon)
            {
                case WeaponType.AssaultRifle:
                    return _gunAttack != null ? _gunAttack.ChargeProgress01 : 0f;
                case WeaponType.GrenadeLauncher:
                    return _grenadeAttack != null ? _grenadeAttack.ChargeProgress01 : 0f;
                default:
                    return 0f;
            }
        }
    }
}
