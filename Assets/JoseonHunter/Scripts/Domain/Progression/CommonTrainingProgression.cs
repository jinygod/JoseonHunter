using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Save;

namespace JoseonHunter.Domain.Progression
{
    public enum CommonTrainingId
    {
        Vitality,
        Power,
        Footwork,
        Learning,
        Guard,
        Resonance
    }

    public sealed class CommonTrainingProgression
    {
        public const int MaximumRankPerTrack = 20;
        public const int MaximumTotalRanks = 100;
        private static readonly int[] CostsForFirstFive = { 100, 180, 280, 420, 600 };
        public static readonly IReadOnlyList<int> Costs = Array.AsReadOnly(BuildCosts());
        private readonly SaveDataV1 data;

        public CommonTrainingProgression(SaveDataV1 data) =>
            this.data = data ?? throw new ArgumentNullException(nameof(data));

        public int TotalRanks
        {
            get
            {
                var total = 0;
                foreach (CommonTrainingId id in Enum.GetValues(typeof(CommonTrainingId)))
                    total += Rank(id);
                return total;
            }
        }

        public int Capacity => Math.Min(
            AccountProgression.StateFor(data.AccountExperience).Level * 5,
            MaximumTotalRanks);

        public int NextCapacityLevel => Math.Min(
            20,
            AccountProgression.StateFor(data.AccountExperience).Level + 1);

        public int Rank(CommonTrainingId id)
        {
            if (!data.CommonTrainingRanks.TryGetValue(id.ToString(), out var rank)) return 0;
            return Math.Max(0, Math.Min(MaximumRankPerTrack, rank));
        }

        public static int CostForRank(int oneBasedRank)
        {
            if (oneBasedRank < 1 || oneBasedRank > MaximumRankPerTrack)
                throw new ArgumentOutOfRangeException(nameof(oneBasedRank));
            if (oneBasedRank <= 5) return CostsForFirstFive[oneBasedRank - 1];
            var distance = oneBasedRank - 5;
            return 600 + 35 * distance + 8 * distance * distance;
        }

        public static float BonusForRank(int rank)
        {
            var normalized = Math.Max(0, Math.Min(MaximumRankPerTrack, rank));
            if (normalized <= 5) return normalized * .02f;
            if (normalized <= 10) return .10f + (normalized - 5) * .006f;
            return .13f + (normalized - 10) * .002f;
        }

        public ProgressionResult Purchase(CommonTrainingId id)
        {
            var key = id.ToString();
            if (!data.CommonTrainingRanks.TryGetValue(key, out var rank) ||
                !data.CommonTrainingSpentCoins.TryGetValue(key, out var spent))
                return new ProgressionResult(false, ProgressionError.UnknownId);
            if (rank < 0 || spent < 0)
                return new ProgressionResult(false, ProgressionError.InvalidAmount);
            if (rank >= MaximumRankPerTrack)
                return new ProgressionResult(false, ProgressionError.MaximumReached);
            if (TotalRanks >= Capacity)
                return new ProgressionResult(false, ProgressionError.AccountLevelRequired);

            var cost = CostForRank(rank + 1);
            if (data.Coins < cost)
                return new ProgressionResult(false, ProgressionError.InsufficientCoins);
            if ((long)spent + cost > int.MaxValue)
                return new ProgressionResult(false, ProgressionError.InvalidAmount);

            var copy = data.Copy();
            copy.Coins -= cost;
            copy.CommonTrainingRanks[key] = rank + 1;
            copy.CommonTrainingSpentCoins[key] = spent + cost;
            data.CopyFrom(copy);
            return new ProgressionResult(true, ProgressionError.None);
        }

        public ProgressionResult Reset()
        {
            long refund = 0;
            foreach (var spent in data.CommonTrainingSpentCoins.Values) refund += spent;
            if (refund < 0 || (long)data.Coins + refund > int.MaxValue)
                return new ProgressionResult(false, ProgressionError.InvalidAmount);

            var copy = data.Copy();
            copy.Coins += (int)refund;
            foreach (CommonTrainingId id in Enum.GetValues(typeof(CommonTrainingId)))
            {
                copy.CommonTrainingRanks[id.ToString()] = 0;
                copy.CommonTrainingSpentCoins[id.ToString()] = 0;
            }
            data.CopyFrom(copy);
            return new ProgressionResult(true, ProgressionError.None);
        }

        public float Multiplier(CommonTrainingId id)
        {
            return 1f + BonusForRank(Rank(id));
        }

        public float DamageTakenMultiplier() => 1f - BonusForRank(Rank(CommonTrainingId.Guard));

        private static int[] BuildCosts()
        {
            var costs = new int[MaximumRankPerTrack];
            for (var rank = 1; rank <= MaximumRankPerTrack; rank++)
                costs[rank - 1] = rank <= 5
                    ? CostsForFirstFive[rank - 1]
                    : 600 + 35 * (rank - 5) + 8 * (rank - 5) * (rank - 5);
            return costs;
        }
    }
}
