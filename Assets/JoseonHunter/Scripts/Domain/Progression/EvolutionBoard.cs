using System;
using JoseonHunter.Domain.Save;

namespace JoseonHunter.Domain.Progression
{
    public sealed class EvolutionBoard
    {
        private readonly SaveDataV1 data;
        public EvolutionBoard(SaveDataV1 data) { this.data = data ?? throw new ArgumentNullException(nameof(data)); }
        public int NodeCount => 12;
        public ProgressionResult Purchase(string nodeId, int cost)
        {
            if (cost < 0) return new ProgressionResult(false, ProgressionError.InvalidAmount);
            if (!data.EvolutionNodeRanks.ContainsKey(nodeId)) return new ProgressionResult(false, ProgressionError.UnknownId);
            if (data.Coins < cost) return new ProgressionResult(false, ProgressionError.InsufficientCoins);
            var nextRank = (long)data.EvolutionNodeRanks[nodeId] + 1;
            var nextSpent = (long)data.EvolutionSpentCoins[nodeId] + cost;
            if (nextRank > int.MaxValue || nextSpent > int.MaxValue) return new ProgressionResult(false, ProgressionError.InvalidAmount);
            var copy = data.Copy(); copy.Coins -= cost; copy.EvolutionNodeRanks[nodeId] = (int)nextRank; copy.EvolutionSpentCoins[nodeId] = (int)nextSpent; data.CopyFrom(copy); return new ProgressionResult(true, ProgressionError.None);
        }
        public ProgressionResult Reset()
        {
            var copy = data.Copy(); long refund = 0;
            foreach (var spent in copy.EvolutionSpentCoins.Values) refund += spent;
            var refundedCoins = (long)copy.Coins + refund;
            if (refund < 0 || refundedCoins < 0 || refundedCoins > int.MaxValue) return new ProgressionResult(false, ProgressionError.InvalidAmount);
            foreach (var id in new System.Collections.Generic.List<string>(copy.EvolutionNodeRanks.Keys)) copy.EvolutionNodeRanks[id] = 0;
            foreach (var id in new System.Collections.Generic.List<string>(copy.EvolutionSpentCoins.Keys)) copy.EvolutionSpentCoins[id] = 0;
            copy.Coins = (int)refundedCoins; data.CopyFrom(copy); return new ProgressionResult(true, ProgressionError.None);
        }
    }
}
