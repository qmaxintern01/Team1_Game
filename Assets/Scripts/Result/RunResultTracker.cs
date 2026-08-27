using Team1;
using UnityEngine;

namespace Team1.Result
{
    // GameScene中のプレイ実績(撃破数・被ダメージ・経過時間)を集計するハブ。
    // GameSceneのGameObjectに配置しておくと、EnemyBase/PlayerHealthなどが
    // Instance経由で実績を積み上げ、シーン遷移時にBuildResult()でRunResultDataへまとめる。
    public class RunResultTracker : MonoBehaviour
    {
        public static RunResultTracker Instance { get; private set; }

        [SerializeField] private PlayerOil _playerOil;

        private float _startTime;
        private int _weakKillCount;
        private int _midBossKillCount;
        private int _knifeKillCount;
        private int _backstabKillCount;
        private int _damageTaken;

        private void Awake()
        {
            Instance = this;
            _startTime = Time.time;

            if (_playerOil == null)
            {
                _playerOil = FindAnyObjectByType<PlayerOil>();
            }

            Debug.Assert(_playerOil != null, $"{nameof(_playerOil)} is not assigned.", this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void NotifyEnemyKilled(bool isMidBoss, bool wasKnifeKill, bool wasBackstab)
        {
            if (isMidBoss)
            {
                _midBossKillCount++;
            }
            else
            {
                _weakKillCount++;
            }

            if (wasKnifeKill)
            {
                _knifeKillCount++;
            }

            if (wasBackstab)
            {
                _backstabKillCount++;
            }
        }

        public void NotifyPlayerDamaged(int amount)
        {
            _damageTaken += amount;
        }

        public RunResultData BuildResult(bool isDefeated = false)
        {
            return new RunResultData
            {
                RemainingOil = _playerOil != null ? _playerOil.CurrentOil : 0,
                MaxOil = _playerOil != null ? _playerOil.MaxOil : 200,
                ClearTimeSeconds = Time.time - _startTime,
                WeakKillCount = _weakKillCount,
                MidBossKillCount = _midBossKillCount,
                KnifeKillCount = _knifeKillCount,
                BackstabKillCount = _backstabKillCount,
                DamageTaken = _damageTaken,
                IsDefeated = isDefeated,
            };
        }
    }
}
