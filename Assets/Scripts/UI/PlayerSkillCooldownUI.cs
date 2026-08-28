using UnityEngine;
using UnityEngine.UI;

namespace Team1.UI
{
    // スキルアイコンのクールダウン表示。クールタイム中はアイコンを暗くする
    public class PlayerSkillCooldownUI : MonoBehaviour
    {
        private enum SkillSlot
        {
            Heal,
            InfiniteAmmo,
        }

        [SerializeField] private SkillSlot _skill;
        [SerializeField] private PlayerSkills _playerSkills;
        [SerializeField] private Image _iconImage;

        [Tooltip("スキル2の発動中を示す枠などの画像(任意)。スキル1では未設定でよい。")]
        [SerializeField] private Image _activeHighlightImage;

        [Header("クールダウン中の見た目")]
        [SerializeField] private Color _readyColor = Color.white;
        [SerializeField] private Color _cooldownColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [Header("スキル2発動中の点滅(継続時間中)")]
        [SerializeField] private Color _activeBlinkColor = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private float _activeBlinkSpeed = 4f;

        private void Awake()
        {
            if (_playerSkills == null)
            {
                _playerSkills = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerSkills>();
            }

            Debug.Assert(_playerSkills != null, $"{nameof(_playerSkills)} is not assigned.", this);
            Debug.Assert(_iconImage != null, $"{nameof(_iconImage)} is not assigned.", this);
        }

        private void Update()
        {
            bool isActive = _skill == SkillSlot.InfiniteAmmo && _playerSkills.IsInfiniteAmmoActive;

            if (isActive)
            {
                // 継続時間中はreadyColorとactiveBlinkColorの間で明滅させる
                float blink = (Mathf.Sin(Time.time * _activeBlinkSpeed) + 1f) * 0.5f;
                _iconImage.color = Color.Lerp(_readyColor, _activeBlinkColor, blink);
            }
            else
            {
                bool isReady = _skill == SkillSlot.Heal ? _playerSkills.IsSkill1Ready : _playerSkills.IsSkill2Ready;
                _iconImage.color = isReady ? _readyColor : _cooldownColor;
            }

            if (_activeHighlightImage != null)
            {
                _activeHighlightImage.enabled = isActive;
            }
        }
    }
}
