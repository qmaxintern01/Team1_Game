using System;

namespace Team1.Result
{
    // 1プレイ分のクリア実績を表す。GameScene側からRunResultStore.Currentへ設定される想定だが、
    // 現時点ではGameScene側の配線が未実施のため、ResultController._debugDataによるダミー値でも動作する。
    [Serializable]
    public class RunResultData
    {
        public int RemainingOil;
        public int MaxOil = 200;
        public float ClearTimeSeconds;
        public int WeakKillCount;
        public int MidBossKillCount;
        public int KnifeKillCount;
        public int BackstabKillCount;
        public int DamageTaken;
    }
}
