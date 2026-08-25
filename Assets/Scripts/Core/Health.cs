using System;
using UnityEngine;

namespace Team1
{
    public class Health : MonoBehaviour, IDamageable
    {
        public event Action<int> OnDamaged;
        public event Action OnDied;

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public bool IsDead => CurrentHp <= 0;

        public void Initialize(int maxHp)
        {
            MaxHp = maxHp;
            CurrentHp = maxHp;
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            OnDamaged?.Invoke(amount);

            if (CurrentHp <= 0)
            {
                OnDied?.Invoke();
            }
        }
    }
}
