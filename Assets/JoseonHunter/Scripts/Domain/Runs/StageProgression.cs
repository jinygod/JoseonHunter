using System;
using System.Collections.Generic;

namespace JoseonHunter.Domain.Runs
{
    public readonly struct StageId : IEquatable<StageId>
    {
        public static readonly StageId GwigokField = new StageId("stage_01_gwigok_field");
        public static readonly StageId DokkaebiPass = new StageId("stage_02_dokkaebi_pass");
        public static readonly StageId MoonlitTomb = new StageId("stage_03_moonlit_tomb");

        public StageId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Stage ID is required.", nameof(value));
            Value = value.Trim();
        }

        public string Value { get; }
        public bool Equals(StageId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StageId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public enum StageDifficulty
    {
        Normal,
        Omen,
        GreatOmen
    }

    public readonly struct StageSelection : IEquatable<StageSelection>
    {
        public StageSelection(StageId stageId, StageDifficulty difficulty)
        {
            if (!Enum.IsDefined(typeof(StageDifficulty), difficulty))
                throw new ArgumentOutOfRangeException(nameof(difficulty));
            StageId = stageId;
            Difficulty = difficulty;
        }

        public StageId StageId { get; }
        public StageDifficulty Difficulty { get; }
        public bool Equals(StageSelection other) =>
            StageId.Equals(other.StageId) && Difficulty == other.Difficulty;
        public override bool Equals(object obj) => obj is StageSelection other && Equals(other);
        public override int GetHashCode() => (StageId.GetHashCode() * 397) ^ (int)Difficulty;
        public override string ToString() => StageId + ":" + StageDifficultyNames.StorageId(Difficulty);
    }

    public readonly struct StageDefinition
    {
        public StageDefinition(StageId id, string displayName, bool hasPlayableContent)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Stage display name is required.", nameof(displayName));
            Id = id;
            DisplayName = displayName;
            HasPlayableContent = hasPlayableContent;
        }

        public StageId Id { get; }
        public string DisplayName { get; }
        public bool HasPlayableContent { get; }
    }

    public static class StageCatalog
    {
        private static readonly IReadOnlyList<StageDefinition> Definitions = new[]
        {
            new StageDefinition(StageId.GwigokField, "귀곡 들판", true),
            new StageDefinition(StageId.DokkaebiPass, "도깨비 고갯길", true),
            new StageDefinition(StageId.MoonlitTomb, "월식 고분", true)
        };

        public static IReadOnlyList<StageDefinition> All => Definitions;

        public static bool TryGet(StageId id, out StageDefinition definition)
        {
            for (var index = 0; index < Definitions.Count; index++)
            {
                if (!Definitions[index].Id.Equals(id)) continue;
                definition = Definitions[index];
                return true;
            }

            definition = default;
            return false;
        }

        public static int IndexOf(StageId id)
        {
            for (var index = 0; index < Definitions.Count; index++)
                if (Definitions[index].Id.Equals(id)) return index;
            return -1;
        }
    }

    public static class StageDifficultyNames
    {
        public static string StorageId(StageDifficulty difficulty) => difficulty switch
        {
            StageDifficulty.Normal => "normal",
            StageDifficulty.Omen => "omen",
            StageDifficulty.GreatOmen => "great_omen",
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
        };

        public static string DisplayName(StageDifficulty difficulty) => difficulty switch
        {
            StageDifficulty.Normal => "보통",
            StageDifficulty.Omen => "흉조",
            StageDifficulty.GreatOmen => "대흉",
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
        };

        public static bool TryParse(string value, out StageDifficulty difficulty)
        {
            switch (value)
            {
                case "normal": difficulty = StageDifficulty.Normal; return true;
                case "omen": difficulty = StageDifficulty.Omen; return true;
                case "great_omen": difficulty = StageDifficulty.GreatOmen; return true;
                default: difficulty = StageDifficulty.Normal; return false;
            }
        }
    }

    public readonly struct StageClearRecord
    {
        public StageClearRecord(
            StageSelection selection,
            bool victory,
            float bestElapsed,
            int bestKills,
            int bestLevel)
        {
            if (bestElapsed < 0f || float.IsNaN(bestElapsed) || float.IsInfinity(bestElapsed))
                throw new ArgumentOutOfRangeException(nameof(bestElapsed));
            if (bestKills < 0) throw new ArgumentOutOfRangeException(nameof(bestKills));
            if (bestLevel < 0) throw new ArgumentOutOfRangeException(nameof(bestLevel));
            Selection = selection;
            VictoryAchieved = victory;
            BestElapsed = bestElapsed;
            BestKills = bestKills;
            BestLevel = bestLevel;
        }

        public StageSelection Selection { get; }
        public bool VictoryAchieved { get; }
        public float BestElapsed { get; }
        public int BestKills { get; }
        public int BestLevel { get; }

        public static StageClearRecord Victory(
            StageSelection selection,
            float bestElapsed,
            int bestKills,
            int bestLevel) =>
            new StageClearRecord(selection, true, bestElapsed, bestKills, bestLevel);

        public StageClearRecord Merge(StageClearRecord other)
        {
            if (!Selection.Equals(other.Selection))
                throw new ArgumentException("Only records for the same stage selection can be merged.", nameof(other));
            return new StageClearRecord(
                Selection,
                VictoryAchieved || other.VictoryAchieved,
                Math.Max(BestElapsed, other.BestElapsed),
                Math.Max(BestKills, other.BestKills),
                Math.Max(BestLevel, other.BestLevel));
        }
    }

    public static class StageUnlockRules
    {
        public static bool IsUnlocked(StageSelection selection, IEnumerable<StageClearRecord> records)
        {
            var stageIndex = StageCatalog.IndexOf(selection.StageId);
            if (stageIndex < 0 || !Enum.IsDefined(typeof(StageDifficulty), selection.Difficulty)) return false;

            switch (selection.Difficulty)
            {
                case StageDifficulty.Normal:
                    return stageIndex == 0 || HasVictory(
                        new StageSelection(StageCatalog.All[stageIndex - 1].Id, StageDifficulty.Normal), records);
                case StageDifficulty.Omen:
                    return HasVictory(new StageSelection(selection.StageId, StageDifficulty.Normal), records);
                case StageDifficulty.GreatOmen:
                    return HasVictory(new StageSelection(selection.StageId, StageDifficulty.Omen), records);
                default:
                    return false;
            }
        }

        public static string LockReason(StageSelection selection, IEnumerable<StageClearRecord> records)
        {
            if (IsUnlocked(selection, records)) return string.Empty;
            if (StageCatalog.IndexOf(selection.StageId) < 0) return "알 수 없는 지역입니다";
            return selection.Difficulty switch
            {
                StageDifficulty.Normal => "이전 장 보통 승리 시 해금",
                StageDifficulty.Omen => "이 장 보통 승리 시 해금",
                StageDifficulty.GreatOmen => "이 장 흉조 승리 시 해금",
                _ => "알 수 없는 난이도입니다"
            };
        }

        public static bool HasVictory(StageSelection selection, IEnumerable<StageClearRecord> records)
        {
            if (records == null) return false;
            foreach (var record in records)
                if (record.Selection.Equals(selection) && record.VictoryAchieved) return true;
            return false;
        }
    }

    public readonly struct StageDifficultyProfile
    {
        private StageDifficultyProfile(
            float enemyHealthMultiplier,
            float enemyDamageMultiplier,
            float waveDensityMultiplier,
            float coinRewardMultiplier,
            float accountExperienceMultiplier,
            float masteryRewardMultiplier,
            float eliteChanceBonus,
            int bossPressureTier)
        {
            EnemyHealthMultiplier = enemyHealthMultiplier;
            EnemyDamageMultiplier = enemyDamageMultiplier;
            WaveDensityMultiplier = waveDensityMultiplier;
            CoinRewardMultiplier = coinRewardMultiplier;
            AccountExperienceMultiplier = accountExperienceMultiplier;
            MasteryRewardMultiplier = masteryRewardMultiplier;
            EliteChanceBonus = eliteChanceBonus;
            BossPressureTier = bossPressureTier;
        }

        public float EnemyHealthMultiplier { get; }
        public float EnemyDamageMultiplier { get; }
        public float WaveDensityMultiplier { get; }
        public float CoinRewardMultiplier { get; }
        public float AccountExperienceMultiplier { get; }
        public float MasteryRewardMultiplier { get; }
        public float EliteChanceBonus { get; }
        public int BossPressureTier { get; }

        public int ScaleActiveCap(int baseCap)
        {
            if (baseCap < 0) throw new ArgumentOutOfRangeException(nameof(baseCap));
            return Math.Min(StagePacingTimeline.MobileActiveCap,
                Math.Max(0, (int)Math.Round(baseCap * WaveDensityMultiplier)));
        }

        public float ScaleSpawnInterval(float baseInterval)
        {
            if (baseInterval <= 0f || float.IsNaN(baseInterval) || float.IsInfinity(baseInterval))
                throw new ArgumentOutOfRangeException(nameof(baseInterval));
            return Math.Max(.07f, baseInterval / WaveDensityMultiplier);
        }

        public int ScaleReward(int value, float multiplier)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            return (int)Math.Min(int.MaxValue,
                Math.Round(value * (double)multiplier, MidpointRounding.AwayFromZero));
        }

        public static StageDifficultyProfile For(StageDifficulty difficulty) => difficulty switch
        {
            StageDifficulty.Normal => new StageDifficultyProfile(1f, 1f, 1f, 1f, 1f, 1f, 0f, 0),
            StageDifficulty.Omen => new StageDifficultyProfile(1.35f, 1.15f, 1.10f, 1.35f, 1.25f, 1.20f, .04f, 1),
            StageDifficulty.GreatOmen => new StageDifficultyProfile(1.75f, 1.30f, 1.20f, 1.75f, 1.50f, 1.40f, .08f, 2),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
        };
    }
}
