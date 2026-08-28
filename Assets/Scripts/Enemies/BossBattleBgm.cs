using UnityEngine;

namespace Team1
{
    // ボス部屋を覆うCollider2D(Is Trigger)を持つGameObjectに付ける。
    // プレイヤーが範囲に入るとボス専用BGMへ切り替え、ボスを倒すか範囲外に出ると通常BGMへ戻す。
    // トラック名はAudioManagerのBGMリストに登録した名前と一致させる。
    [RequireComponent(typeof(Collider2D))]
    public class BossBattleBgm : MonoBehaviour
    {
        [SerializeField] private string _bossBgmTrackName;
        [SerializeField] private string _normalBgmTrackName;
        [SerializeField] private EnemyBase _boss;

        private Health _bossHealth;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;

            if (_boss != null)
            {
                _bossHealth = _boss.GetComponent<Health>();
            }
        }

        private void OnEnable()
        {
            if (_bossHealth != null)
            {
                _bossHealth.OnDied += HandleBossDied;
            }
        }

        private void OnDisable()
        {
            if (_bossHealth != null)
            {
                _bossHealth.OnDied -= HandleBossDied;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && !string.IsNullOrEmpty(_bossBgmTrackName))
            {
                AudioManager.Instance?.PlayBgm(_bossBgmTrackName);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") && !string.IsNullOrEmpty(_normalBgmTrackName))
            {
                AudioManager.Instance?.PlayBgm(_normalBgmTrackName);
            }
        }

        private void HandleBossDied()
        {
            if (!string.IsNullOrEmpty(_normalBgmTrackName))
            {
                AudioManager.Instance?.PlayBgm(_normalBgmTrackName);
            }
        }
    }
}
