using System;
using UnityEngine;

namespace Team1
{
    public class Health : MonoBehaviour, IDamageable
    {
        // インスペクターで初期最大HPを設定できるように追加
        [SerializeField] private int defaultMaxHp = 100;

        public event Action<int> OnDamaged;
        public event Action OnDied;
        public event Action<int, int> OnHpChanged;

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public bool IsDead => CurrentHp <= 0;
        public bool IsInvincible { get; set; }

        private void Awake()
        {
            // 開始時にまだ初期化されていなければ、設定値で初期化する
            if (MaxHp == 0)
            {
                Initialize(defaultMaxHp);
            }
        }

        public void Initialize(int maxHp)
        {
            MaxHp = maxHp;
            CurrentHp = maxHp;
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0 || IsInvincible)
            {
                return;
            }

            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            OnDamaged?.Invoke(amount);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);

            if (CurrentHp <= 0)
            {
                OnDied?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }
    }
}