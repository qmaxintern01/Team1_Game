using UnityEngine;
using UnityEngine.UI;

namespace Team1.UI
{
    // 敵の頭上に追従表示するHPバー。敵GameObjectの子として配置する想定
    [RequireComponent(typeof(Canvas))]
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Slider _slider;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 1.2f, 0f);

        private Transform _target;

        private void Awake()
        {
            if (_health == null)
            {
                _health = GetComponentInParent<Health>();
            }

            _target = _health != null ? _health.transform : transform.parent;

            // エラー確認
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

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            // 敵本体の回転・スケールに影響されず、常に頭上へ正立表示させる
            transform.SetPositionAndRotation(_target.position + _worldOffset, Quaternion.identity);
        }

        private void HandleHpChanged(int current, int max)
        {
            _slider.maxValue = max;
            _slider.value = current;
        }
    }
}
