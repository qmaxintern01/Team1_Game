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
        }
    }
}
