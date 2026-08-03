using System;
using System.Collections.Generic;

namespace JoseonHunter.Domain.Runs
{
    public enum RunPhase
    {
        WaveOne, WaveTwo, WaveThree, Peak, BossWarning, Boss, Expired
    }

    public readonly struct WeightedEnemyEntry
    {
        public WeightedEnemyEntry(string contentId, int weight)
        {
            if (string.IsNullOrWhiteSpace(contentId))
                throw new ArgumentException("Enemy content ID is required.", nameof(contentId));
            if (weight <= 0)
                throw new ArgumentOutOfRangeException(nameof(weight), "Enemy weight must be positive.");
            ContentId = contentId;
            Weight = weight;
        }

        public string ContentId { get; }
        public int Weight { get; }
    }

    public readonly struct WavePackDefinition
    {
        public WavePackDefinition(
            IReadOnlyList<string> contentIds,
            int minimumSize,
            int maximumSize,
            float minimumIntervalSeconds,
            float maximumIntervalSeconds)
        {
            if (contentIds == null || contentIds.Count == 0)
                throw new ArgumentException("Pack content IDs are required.", nameof(contentIds));
            if (minimumSize <= 0 || maximumSize < minimumSize)
                throw new ArgumentOutOfRangeException(nameof(minimumSize), "Pack sizes must form a positive range.");
            if (minimumIntervalSeconds <= 0f || maximumIntervalSeconds < minimumIntervalSeconds)
                throw new ArgumentOutOfRangeException(nameof(minimumIntervalSeconds), "Pack intervals must form a positive range.");
            ContentIds = contentIds;
            MinimumSize = minimumSize;
            MaximumSize = maximumSize;
            MinimumIntervalSeconds = minimumIntervalSeconds;
            MaximumIntervalSeconds = maximumIntervalSeconds;
        }

        public IReadOnlyList<string> ContentIds { get; }
        public int MinimumSize { get; }
        public int MaximumSize { get; }
        public float MinimumIntervalSeconds { get; }
        public float MaximumIntervalSeconds { get; }
    }

    public readonly struct WaveDefinition
    {
        public WaveDefinition(
            int activeCap,
            IReadOnlyList<WeightedEnemyEntry> weightedContent,
            WavePackDefinition? pack = null,
            IReadOnlyList<string> specialContentIds = null,
            int maximumSpecialFamilies = 0)
        {
            if (activeCap < 0) throw new ArgumentOutOfRangeException(nameof(activeCap));
            if (weightedContent == null) throw new ArgumentNullException(nameof(weightedContent));
            ActiveCap = activeCap;
            WeightedContent = weightedContent;
            var ids = new string[weightedContent.Count];
            for (var index = 0; index < weightedContent.Count; index++)
                ids[index] = weightedContent[index].ContentId;
            ContentIds = Array.AsReadOnly(ids);
            Pack = pack;
            SpecialContentIds = specialContentIds ?? Array.Empty<string>();
            MaximumSpecialFamilies = Math.Max(0, Math.Min(maximumSpecialFamilies, SpecialContentIds.Count));
        }

        public int ActiveCap { get; }
        public IReadOnlyList<string> ContentIds { get; }
        public IReadOnlyList<WeightedEnemyEntry> WeightedContent { get; }
        public WavePackDefinition? Pack { get; }
        public IReadOnlyList<string> SpecialContentIds { get; }
        public int MaximumSpecialFamilies { get; }
    }

    public static class WaveSchedule
    {
        public static WaveDefinition For(RunPhase phase, bool normalWavesStopped) =>
            normalWavesStopped ? Definition(0, Entries()) : For(phase);

        public static WaveDefinition For(RunPhase phase, RunTick tick) =>
            For(phase, tick.NormalWavesStopped);

        public static WaveDefinition For(RunPhase phase) => phase switch
        {
            RunPhase.WaveOne => Definition(72,
                Entries(("plague_rat", 100)),
                Pack(new[] { "plague_rat" }, 8, 12, 7f, 11f)),
            RunPhase.WaveTwo => Definition(104,
                Entries(("plague_rat", 65), ("vengeful_spirit", 35)),
                Pack(new[] { "vengeful_spirit" }, 10, 14, 10f, 14f),
                new[] { "shield_dokkaebi", "spirit_shaman" }, 1),
            RunPhase.WaveThree => Definition(128,
                Entries(("plague_rat", 20), ("vengeful_spirit", 45), ("sakkat_specter", 35)),
                Pack(new[] { "vengeful_spirit", "sakkat_specter" }, 10, 16, 9f, 13f),
                new[] { "charging_horn_ghost", "splitting_rat" }, 1),
            RunPhase.Peak => Definition(140,
                Entries(("sakkat_specter", 35), ("dokkaebi", 35), ("bandit", 30)),
                Pack(new[] { "sakkat_specter", "dokkaebi", "bandit" }, 12, 18, 8f, 12f),
                new[] { "shield_dokkaebi", "spirit_shaman", "charging_horn_ghost", "splitting_rat" }, 2),
            RunPhase.BossWarning => Definition(36, Entries(("fallen_general", 100))),
            RunPhase.Boss => Definition(36, Entries(("fallen_general", 100))),
            RunPhase.Expired => Definition(0, Entries()),
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
        };

        private static WaveDefinition Definition(
            int activeCap,
            IReadOnlyList<WeightedEnemyEntry> entries,
            WavePackDefinition? pack = null,
            IReadOnlyList<string> specialContentIds = null,
            int maximumSpecialFamilies = 0) => new(activeCap, entries, pack, specialContentIds, maximumSpecialFamilies);

        private static IReadOnlyList<WeightedEnemyEntry> Entries(
            params (string ContentId, int Weight)[] entries)
        {
            var result = new WeightedEnemyEntry[entries.Length];
            for (var index = 0; index < entries.Length; index++)
                result[index] = new WeightedEnemyEntry(entries[index].ContentId, entries[index].Weight);
            return Array.AsReadOnly(result);
        }

        private static WavePackDefinition Pack(
            string[] contentIds,
            int minimumSize,
            int maximumSize,
            float minimumIntervalSeconds,
            float maximumIntervalSeconds) => new(
            Array.AsReadOnly(contentIds),
            minimumSize,
            maximumSize,
            minimumIntervalSeconds,
            maximumIntervalSeconds);
    }
}
