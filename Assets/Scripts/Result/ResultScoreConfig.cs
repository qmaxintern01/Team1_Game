using UnityEngine;

namespace Team1.Result
{
    // リザルト評価に使う配点・ランク閾値をまとめたScriptableObject。
    // プレイテストを踏まえて数値だけをここで調整できるようにし、コード側にマジックナンバーを持たせない。
    [CreateAssetMenu(fileName = "ResultScoreConfig", menuName = "Team1/Result/Score Config")]
    public class ResultScoreConfig : ScriptableObject
    {
        [Header("残量オイルボーナス")]
        [SerializeField] private float _oilScoreMax = 200f;

        [Header("クリアタイムボーナス")]
        [SerializeField] private float _timeScoreMax = 200f;
        [Tooltip("この秒数以下でクリアすると満点")]
        [SerializeField] private float _goldTimeSeconds = 90f;
        [Tooltip("この秒数以上かかると0点")]
        [SerializeField] private float _limitTimeSeconds = 420f;

        [Header("撃破数ボーナス")]
        [SerializeField] private float _killScoreMax = 200f;
        [SerializeField] private float _scorePerWeakKill = 8f;
        [SerializeField] private float _scorePerMidBossKill = 40f;

        [Header("スタイリッシュボーナス(ナイフ討伐・背後撃破)")]
        [SerializeField] private float _stylishScoreMax = 200f;
        [SerializeField] private float _scorePerKnifeKill = 15f;
        [SerializeField] private float _scorePerBackstab = 25f;

        [Header("被ダメージ(マイナス評価)")]
        [SerializeField] private float _damageBonusMax = 100f;
        [SerializeField] private float _damagePenaltyPerPoint = 0.5f;

        [Header("ランク閾値(合計スコアがこの値以上でランク到達)")]
        [SerializeField] private float _rankSThreshold = 560f;
        [SerializeField] private float _rankAThreshold = 420f;
        [SerializeField] private float _rankBThreshold = 280f;
        [SerializeField] private float _rankCThreshold = 150f;

        public float OilScoreMax => _oilScoreMax;
        public float TimeScoreMax => _timeScoreMax;
        public float GoldTimeSeconds => _goldTimeSeconds;
        public float LimitTimeSeconds => _limitTimeSeconds;
        public float KillScoreMax => _killScoreMax;
        public float ScorePerWeakKill => _scorePerWeakKill;
        public float ScorePerMidBossKill => _scorePerMidBossKill;
        public float StylishScoreMax => _stylishScoreMax;
        public float ScorePerKnifeKill => _scorePerKnifeKill;
        public float ScorePerBackstab => _scorePerBackstab;
        public float DamageBonusMax => _damageBonusMax;
        public float DamagePenaltyPerPoint => _damagePenaltyPerPoint;
        public float RankSThreshold => _rankSThreshold;
        public float RankAThreshold => _rankAThreshold;
        public float RankBThreshold => _rankBThreshold;
        public float RankCThreshold => _rankCThreshold;
    }
}
