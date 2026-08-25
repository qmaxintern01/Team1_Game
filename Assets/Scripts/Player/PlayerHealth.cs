using UnityEngine;

namespace Team1
{
    [RequireComponent(typeof(Health))]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int _maxHp = 100;

        private Health _health;

        public Health Health => _health;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _health.Initialize(_maxHp);
            _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            _health.OnDied -= HandleDied;
        }

        private void HandleDied()
        {
            // SetActiveにより、入力・移動・当たり判定を含めた全コンポーネントの動作が停止し、見た目も消える
            gameObject.SetActive(false);
        }
    }
}
