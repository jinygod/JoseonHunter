using System;
using JoseonHunter.Domain.Runs;

namespace JoseonHunter.Domain.Progression
{
    public readonly struct AccountLevelState
    {
        public AccountLevelState(
            int level,
            int currentLevelExperience,
            int nextLevelRequirement,
            int totalExperience,
            bool isMaximumLevel)
        {
            Level = level;
            CurrentLevelExperience = currentLevelExperience;
            NextLevelRequirement = nextLevelRequirement;
            TotalExperience = totalExperience;
            IsMaximumLevel = isMaximumLevel;
        }

        public int Level { get; }
        public int CurrentLevelExperience { get; }
        public int NextLevelRequirement { get; }
        public int TotalExperience { get; }
        public bool IsMaximumLevel { get; }
    }

    public static class AccountProgression
    {
        public const int MaximumLevel = 100;

        public static int RewardFor(RunSettlement settlement)
        {
            var timeExperience = (int)Math.Min(150d, Math.Floor(settlement.Elapsed / 6d));
            var killExperience = Math.Min(200, settlement.Kills / 4);
            var reward = timeExperience + killExperience + (settlement.Victory ? 250 : 0);
            var baseReward = settlement.Abandoned ? reward / 4 : reward;
            return StageRewardCalculator.AccountExperience(baseReward, settlement.StageSelection);
        }

        public static int RequiredForNextLevel(int level)
        {
            if (level < 1 || level >= MaximumLevel)
                throw new ArgumentOutOfRangeException(nameof(level));

            var offset = level - 1;
            return 100 + 40 * offset + 2 * offset * offset;
        }

        public static int TotalExperienceForLevel(int level)
        {
            if (level < 1 || level > MaximumLevel)
                throw new ArgumentOutOfRangeException(nameof(level));

            var total = 0;
            for (var current = 1; current < level; current++)
                total += RequiredForNextLevel(current);
            return total;
        }

        public static AccountLevelState StateFor(int totalExperience)
        {
            var maximumExperience = TotalExperienceForLevel(MaximumLevel);
            var normalized = Math.Max(0, Math.Min(maximumExperience, totalExperience));
            var level = 1;
            var levelStart = 0;

            while (level < MaximumLevel)
            {
                var requirement = RequiredForNextLevel(level);
                if (normalized < levelStart + requirement) break;
                levelStart += requirement;
                level++;
            }

            if (level == MaximumLevel)
                return new AccountLevelState(level, 0, 0, normalized, true);

            return new AccountLevelState(
                level,
                normalized - levelStart,
                RequiredForNextLevel(level),
                normalized,
                false);
        }

        public static bool TryAdd(int currentExperience, int reward, out int nextExperience)
        {
            nextExperience = 0;
            if (currentExperience < 0 || reward < 0) return false;

            var maximumExperience = TotalExperienceForLevel(MaximumLevel);
            nextExperience = (int)Math.Min(maximumExperience, (long)currentExperience + reward);
            return true;
        }
    }
}
