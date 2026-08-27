using Team1.Result;
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
            _health.OnDamaged += HandleDamaged;
            _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnDied -= HandleDied;
        }

        private void HandleDamaged(int amount)
        {
            RunResultTracker.Instance?.NotifyPlayerDamaged(amount);
            Debug.Log($"敵の攻撃がプレイヤーに命中: {amount}ダメージ (残りHP: {_health.CurrentHp}/{_health.MaxHp})");
        }

        private void HandleDied()
        {
            // ゲームオーバーによるリザルト遷移。OnDisableで行うとシーン遷移時のオブジェクト破棄でも
            // 誤って発火し、クリア実績を敗北で上書きしてしまうため、実際の死亡時のみ実行する
            if (RunResultTracker.Instance != null)
            {
                RunResultStore.Current = RunResultTracker.Instance.BuildResult(isDefeated: true);
            }

            // Resultシーンへ移行
            SceneTransitionManager.LoadScene("ResultScene");

            // SetActiveにより、入力・移動・当たり判定を含めた全コンポーネントの動作が停止し、見た目も消える
            gameObject.SetActive(false);
        }
    }
}
