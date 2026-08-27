using UnityEngine;

namespace Team1.Result
{
    public enum ResultRank
    {
        D,
        C,
        B,
        A,
        S,
    }

    public readonly struct ResultScoreBreakdown
    {
        public readonly float OilScore;
        public readonly float TimeScore;
        public readonly float KillScore;
        public readonly float StylishScore;
        public readonly float DamageBonus;
        public readonly float TotalScore;
        public readonly ResultRank Rank;
        public readonly string Title;
        public readonly string NextTargetAdvice;

        public ResultScoreBreakdown(
            float oilScore,
            float timeScore,
            float killScore,
            float stylishScore,
            float damageBonus,
            float totalScore,
            ResultRank rank,
            string title,
            string nextTargetAdvice)
        {
            OilScore = oilScore;
            TimeScore = timeScore;
            KillScore = killScore;
            StylishScore = stylishScore;
            DamageBonus = damageBonus;
            TotalScore = totalScore;
            Rank = rank;
            Title = title;
            NextTargetAdvice = nextTargetAdvice;
        }
    }

    // クリア実績(RunResultData)からリザルト評価を算出する純粋なロジック。
    // MonoBehaviourやシーンに依存しないため、Edit Modeテストで単体検証できる。
    //
    // 「じっくり雑魚を狩ってオイルを溜めるプレイ(オイル+撃破が高い)」と
    // 「ナイフで最速撃破するプレイ(タイム+スタイリッシュが高い)」の
    // どちらの軸でも合計スコアがS帯に届くよう、各カテゴリの満点を同じ200点に揃えている。
    public static class ResultScoreCalculator
    {
        public static ResultScoreBreakdown Calculate(RunResultData data, ResultScoreConfig config)
        {
            float oilScore = CalculateOilScore(data, config);
            float timeScore = CalculateTimeScore(data, config);
            float killScore = CalculateKillScore(data, config);
            float stylishScore = CalculateStylishScore(data, config);
            float damageBonus = CalculateDamageBonus(data, config);

            float total = oilScore + timeScore + killScore + stylishScore + damageBonus;

            // 敵にやられてゲームオーバーになった場合は、内訳スコアに関わらず最低評価(Dランク)にする
            ResultRank rank = data.IsDefeated ? ResultRank.D : DetermineRank(total, config);
            string title = data.IsDefeated
                ? "力尽きた挑戦者"
                : DetermineTitle(oilScore, timeScore, killScore, stylishScore, damageBonus, config, rank);
            string advice = data.IsDefeated
                ? "敵の攻撃を避けて生き延びよう。"
                : BuildNextTargetAdvice(data, config, oilScore, stylishScore, timeScore, total, rank);

            return new ResultScoreBreakdown(oilScore, timeScore, killScore, stylishScore, damageBonus, total, rank, title, advice);
        }

        private static float CalculateOilScore(RunResultData data, ResultScoreConfig config)
        {
            if (data.MaxOil <= 0)
            {
                return 0f;
            }

            float ratio = Mathf.Clamp01((float)data.RemainingOil / data.MaxOil);
            return ratio * config.OilScoreMax;
        }

        private static float CalculateTimeScore(RunResultData data, ResultScoreConfig config)
        {
            float gold = config.GoldTimeSeconds;
            float limit = Mathf.Max(config.LimitTimeSeconds, gold + 0.01f);

            if (data.ClearTimeSeconds <= gold)
            {
                return config.TimeScoreMax;
            }

            if (data.ClearTimeSeconds >= limit)
            {
                return 0f;
            }

            float ratio = 1f - (data.ClearTimeSeconds - gold) / (limit - gold);
            return ratio * config.TimeScoreMax;
        }

        private static float CalculateKillScore(RunResultData data, ResultScoreConfig config)
        {
            float raw = data.WeakKillCount * config.ScorePerWeakKill + data.MidBossKillCount * config.ScorePerMidBossKill;
            return Mathf.Min(raw, config.KillScoreMax);
        }

        private static float CalculateStylishScore(RunResultData data, ResultScoreConfig config)
        {
            float raw = data.KnifeKillCount * config.ScorePerKnifeKill + data.BackstabKillCount * config.ScorePerBackstab;
            return Mathf.Min(raw, config.StylishScoreMax);
        }

        private static float CalculateDamageBonus(RunResultData data, ResultScoreConfig config)
        {
            float raw = config.DamageBonusMax - data.DamageTaken * config.DamagePenaltyPerPoint;
            return Mathf.Clamp(raw, 0f, config.DamageBonusMax);
        }

        private static ResultRank DetermineRank(float total, ResultScoreConfig config)
        {
            if (total >= config.RankSThreshold)
            {
                return ResultRank.S;
            }

            if (total >= config.RankAThreshold)
            {
                return ResultRank.A;
            }

            if (total >= config.RankBThreshold)
            {
                return ResultRank.B;
            }

            if (total >= config.RankCThreshold)
            {
                return ResultRank.C;
            }

            return ResultRank.D;
        }

        private static string DetermineTitle(
            float oilScore,
            float timeScore,
            float killScore,
            float stylishScore,
            float damageBonus,
            ResultScoreConfig config,
            ResultRank rank)
        {
            bool oilDominant = oilScore >= config.OilScoreMax * 0.8f;
            bool timeDominant = timeScore >= config.TimeScoreMax * 0.8f;
            bool stylishDominant = stylishScore >= config.StylishScoreMax * 0.5f;
            bool killDominant = killScore >= config.KillScoreMax * 0.8f;
            bool lowDamage = damageBonus >= config.DamageBonusMax * 0.8f;
            bool oilWasteful = oilScore <= config.OilScoreMax * 0.15f;

            if (stylishDominant && timeDominant)
            {
                return "閃光のナイフ使い";
            }

            if (oilDominant && killDominant)
            {
                return "真のオイルハンター";
            }

            if (oilWasteful && killDominant)
            {
                return "無駄遣いスレイヤー";
            }

            if (lowDamage && rank >= ResultRank.A)
            {
                return "無傷の撃破者";
            }

            if (timeDominant)
            {
                return "疾風のスピードランナー";
            }

            switch (rank)
            {
                case ResultRank.S:
                    return "伝説のオイルハンター";
                case ResultRank.A:
                    return "熟練の討伐者";
                case ResultRank.B:
                    return "見習いハンター";
                case ResultRank.C:
                    return "駆け出しの生存者";
                default:
                    return "オイル切れ寸前の挑戦者";
            }
        }

        private static string BuildNextTargetAdvice(
            RunResultData data,
            ResultScoreConfig config,
            float oilScore,
            float stylishScore,
            float timeScore,
            float total,
            ResultRank rank)
        {
            if (rank == ResultRank.S)
            {
                return "Sランク達成！ 別のプレイスタイルでも挑戦してみよう。";
            }

            float nextThreshold;
            char nextRankLabel;
            switch (rank)
            {
                case ResultRank.A:
                    nextThreshold = config.RankSThreshold;
                    nextRankLabel = 'S';
                    break;
                case ResultRank.B:
                    nextThreshold = config.RankAThreshold;
                    nextRankLabel = 'A';
                    break;
                case ResultRank.C:
                    nextThreshold = config.RankBThreshold;
                    nextRankLabel = 'B';
                    break;
                default:
                    nextThreshold = config.RankCThreshold;
                    nextRankLabel = 'C';
                    break;
            }

            float gap = Mathf.Max(0f, nextThreshold - total);
            if (gap <= 0f)
            {
                return $"{nextRankLabel}ランクまであと一歩！";
            }

            // オイル保有・ナイフ討伐・タイム短縮のうち、最も分かりやすく提示できる項目を優先して1つだけ提案する
            if (data.MaxOil > 0 && oilScore < config.OilScoreMax)
            {
                int extraOilNeeded = Mathf.CeilToInt(gap / config.OilScoreMax * data.MaxOil);
                if (extraOilNeeded <= data.MaxOil)
                {
                    return $"あとオイル{extraOilNeeded}保有で{nextRankLabel}ランク！";
                }
            }

            if (stylishScore < config.StylishScoreMax)
            {
                int extraKnifeKillsNeeded = Mathf.CeilToInt(gap / Mathf.Max(config.ScorePerKnifeKill, 0.01f));
                return $"あとナイフ討伐{extraKnifeKillsNeeded}体で{nextRankLabel}ランク！";
            }

            if (timeScore < config.TimeScoreMax)
            {
                float extraTimeSecondsToSave = gap / config.TimeScoreMax * (config.LimitTimeSeconds - config.GoldTimeSeconds);
                return $"クリアタイムを{extraTimeSecondsToSave:0}秒短縮で{nextRankLabel}ランク！";
            }

            int extraWeakKillsNeeded = Mathf.CeilToInt(gap / Mathf.Max(config.ScorePerWeakKill, 0.01f));
            return $"あと{extraWeakKillsNeeded}体討伐で{nextRankLabel}ランク！";
        }
    }
}
