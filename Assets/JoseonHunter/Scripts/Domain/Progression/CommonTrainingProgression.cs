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
        public static readonly IReadOnlyList<int> Costs = Array.AsReadOnly(new[] { 100, 180, 280, 420, 600 });
        private readonly SaveDataV1 data;

        public CommonTrainingProgression(SaveDataV1 data) =>
            this.data = data ?? throw new ArgumentNullException(nameof(data));

        public ProgressionResult Purchase(CommonTrainingId id)
        {
            var key = id.ToString();
            if (!data.CommonTrainingRanks.TryGetValue(key, out var rank) ||
                !data.CommonTrainingSpentCoins.TryGetValue(key, out var spent))
                return new ProgressionResult(false, ProgressionError.UnknownId);
            if (rank >= Costs.Count)
                return new ProgressionResult(false, ProgressionError.MaximumReached);

            var cost = Costs[rank];
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
            var rank = data.CommonTrainingRanks.TryGetValue(id.ToString(), out var value) ? value : 0;
            return 1f + Math.Min(5, Math.Max(0, rank)) * .02f;
        }
    }
}
