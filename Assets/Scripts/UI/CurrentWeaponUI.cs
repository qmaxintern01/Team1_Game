using UnityEngine;
using UnityEngine.UI;

namespace Team1
{
public class CurrentWeaponUI : MonoBehaviour
{

    [SerializeField] private PlayerWeaponSwitcher _playerWeaponSwitcher;
    [SerializeField] private Image _weaponImage;
    [SerializeField] private Text _weaponNameText;

    [Tooltip("武器の画像を設定する。武器の種類の順番と同じ順番で設定すること。")]
    [SerializeField] private Sprite[] _weaponSprites = new Sprite[3];
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _playerWeaponSwitcher = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerWeaponSwitcher>();
        _weaponImage = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        int weaponIndex = (int)_playerWeaponSwitcher.CurrentWeapon;
        if (weaponIndex >= 0 && weaponIndex < _weaponSprites.Length)
        {
            _weaponImage.sprite = _weaponSprites[weaponIndex];
        }
        //_weaponNameText.text = _playerWeaponSwitcher.GetCurrentWeaponName();
    }
}
}
