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
            var copy = data.Copy(); copy.Coins -= cost; copy.EvolutionNodeRanks[nodeId]++; copy.EvolutionSpentCoins[nodeId] += cost; data.CopyFrom(copy); return new ProgressionResult(true, ProgressionError.None);
        }
        public ProgressionResult Reset()
        {
            var copy = data.Copy(); var refund = 0;
            foreach (var spent in copy.EvolutionSpentCoins.Values) refund += spent;
            foreach (var id in new System.Collections.Generic.List<string>(copy.EvolutionNodeRanks.Keys)) copy.EvolutionNodeRanks[id] = 0;
            foreach (var id in new System.Collections.Generic.List<string>(copy.EvolutionSpentCoins.Keys)) copy.EvolutionSpentCoins[id] = 0;
            copy.Coins += refund; data.CopyFrom(copy); return new ProgressionResult(true, ProgressionError.None);
        }
    }
}
