using UnityEngine;
using UnityEngine.UI;

namespace Team1
{
public class CurrentWeaponUI : MonoBehaviour
{

    [SerializeField] private PlayerWeaponSwitcher _playerWeaponSwitcher;
    //[SerializeField] private Image _weaponImage;
    [SerializeField] private Text _weaponNameText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerWeaponSwitcher = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerWeaponSwitcher>();
        _weaponNameText = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        _weaponNameText.text = _playerWeaponSwitcher.GetCurrentWeaponName();
    }
}
}
