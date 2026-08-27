namespace Team1.Result
{
    // GameSceneでのプレイ実績をResultSceneへ橋渡しするための静的な受け渡し場所。
    // 現時点ではGameScene側(EnemyBase/PlayerKnifeAttack/BossCheckerなど)からCurrentを設定する配線は未実施。
    // そのため通常はCurrentがnullのままResultSceneに遷移し、ResultController側のデバッグ用データで表示される。
    // 将来GameSceneのクリア処理から本Currentへ実績を設定することで、実データ表示に切り替えられる。
    public static class RunResultStore
    {
        public static RunResultData Current { get; set; }

        public static void Clear()
        {
            Current = null;
        }
    }
}
