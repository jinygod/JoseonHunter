using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Runs
{
    public readonly struct StageStatProfile
    {
        public StageStatProfile(
            float enemyHealthMultiplier,
            float enemyDamageMultiplier,
            float enemyExperienceMultiplier)
        {
            EnemyHealthMultiplier = Positive(enemyHealthMultiplier, nameof(enemyHealthMultiplier));
            EnemyDamageMultiplier = Positive(enemyDamageMultiplier, nameof(enemyDamageMultiplier));
            EnemyExperienceMultiplier = Positive(enemyExperienceMultiplier, nameof(enemyExperienceMultiplier));
        }

        public float EnemyHealthMultiplier { get; }
        public float EnemyDamageMultiplier { get; }
        public float EnemyExperienceMultiplier { get; }

        public int ScaleEnemyExperience(int baseValue)
        {
            if (baseValue < 0) throw new ArgumentOutOfRangeException(nameof(baseValue));
            if (baseValue == 0) return 0;
            return Math.Max(1, (int)Math.Min(int.MaxValue,
                Math.Ceiling(baseValue * (double)EnemyExperienceMultiplier)));
        }

        private static float Positive(float value, string parameter)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameter);
            return value;
        }
    }

    public readonly struct StageRewardProfile
    {
        public StageRewardProfile(
            float coinMultiplier,
            float accountExperienceMultiplier,
            float masteryMultiplier)
        {
            CoinMultiplier = Positive(coinMultiplier, nameof(coinMultiplier));
            AccountExperienceMultiplier = Positive(accountExperienceMultiplier, nameof(accountExperienceMultiplier));
            MasteryMultiplier = Positive(masteryMultiplier, nameof(masteryMultiplier));
        }

        public float CoinMultiplier { get; }
        public float AccountExperienceMultiplier { get; }
        public float MasteryMultiplier { get; }

        private static float Positive(float value, string parameter)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameter);
            return value;
        }
    }

    public sealed class StageCombatDefinition
    {
        public StageCombatDefinition(
            StageId stageId,
            StageWaveProfile waves,
            StageBattlefieldDefinition battlefield,
            IReadOnlyList<StageBossDefinition> bosses,
            StageStatProfile stats,
            StageRewardProfile rewards,
            bool presentationReady)
        {
            StageId = stageId;
            Waves = waves ?? throw new ArgumentNullException(nameof(waves));
            Battlefield = battlefield;
            Bosses = bosses ?? throw new ArgumentNullException(nameof(bosses));
            Stats = stats;
            Rewards = rewards;
            PresentationReady = presentationReady;
        }

        public StageId StageId { get; }
        public StageWaveProfile Waves { get; }
        public StageBattlefieldDefinition Battlefield { get; }
        public IReadOnlyList<StageBossDefinition> Bosses { get; }
        public StageStatProfile Stats { get; }
        public StageRewardProfile Rewards { get; }
        public bool PresentationReady { get; }
    }

    public static class StageRewardCalculator
    {
        public static int Coins(int baseValue, StageSelection selection) => Scale(
            baseValue,
            StageDifficultyProfile.For(selection.Difficulty).CoinRewardMultiplier,
            StageCombatCatalog.For(selection.StageId).Rewards.CoinMultiplier);

        public static int AccountExperience(int baseValue, StageSelection selection) => Scale(
            baseValue,
            StageDifficultyProfile.For(selection.Difficulty).AccountExperienceMultiplier,
            StageCombatCatalog.For(selection.StageId).Rewards.AccountExperienceMultiplier);

        public static int Mastery(int baseValue, StageSelection selection) => Scale(
            baseValue,
            StageDifficultyProfile.For(selection.Difficulty).MasteryRewardMultiplier,
            StageCombatCatalog.For(selection.StageId).Rewards.MasteryMultiplier);

        private static int Scale(int value, float difficultyMultiplier, float stageMultiplier)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            return (int)Math.Min(int.MaxValue,
                Math.Round(value * (double)difficultyMultiplier * stageMultiplier,
                    MidpointRounding.AwayFromZero));
        }
    }
}
