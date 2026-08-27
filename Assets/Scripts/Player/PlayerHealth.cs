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

            // ゲームオーバーによるリザルト遷移。Instanceがnullの場合(シーンアンロードに伴う再入時など)は
            // 既に設定済みの実績を上書きしないよう、そのまま遷移のみ行う
            if (RunResultTracker.Instance != null)
            {
                RunResultStore.Current = RunResultTracker.Instance.BuildResult(isDefeated: true);
            }

            
            // Resultシーンへ移行
            SceneTransitionManager.LoadScene("ResultScene");
        }

        private void HandleDamaged(int amount)
        {
            RunResultTracker.Instance?.NotifyPlayerDamaged(amount);
            Debug.Log($"敵の攻撃がプレイヤーに命中: {amount}ダメージ (残りHP: {_health.CurrentHp}/{_health.MaxHp})");
        }

        private void HandleDied()
        {
            // SetActiveにより、入力・移動・当たり判定を含めた全コンポーネントの動作が停止し、見た目も消える
            gameObject.SetActive(false);
        }
    }
}
