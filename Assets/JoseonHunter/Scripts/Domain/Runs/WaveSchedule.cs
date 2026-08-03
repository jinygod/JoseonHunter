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
        private static readonly IReadOnlyList<WeightedEnemyEntry> EmptyEntries = Entries();
        private static readonly IReadOnlyList<WeightedEnemyEntry> RatOnlyEntries =
            Entries(("plague_rat", 100));
        private static readonly IReadOnlyList<WeightedEnemyEntry> SpiritOnlyEntries =
            Entries(("vengeful_spirit", 100));
        private static readonly IReadOnlyList<WeightedEnemyEntry> RatSpiritEntries =
            Entries(("plague_rat", 60), ("vengeful_spirit", 40));
        private static readonly IReadOnlyList<WeightedEnemyEntry> DokkaebiOnlyEntries =
            Entries(("dokkaebi", 100));
        private static readonly IReadOnlyList<WeightedEnemyEntry> LearnedEntries =
            Entries(("plague_rat", 25), ("vengeful_spirit", 40), ("dokkaebi", 35));
        private static readonly IReadOnlyList<WeightedEnemyEntry> PeakEntries =
            Entries(("plague_rat", 20), ("vengeful_spirit", 40), ("dokkaebi", 40));
        private static readonly IReadOnlyList<WeightedEnemyEntry> BossEntries =
            Entries(("fallen_general", 100));

        private static readonly WaveDefinition EmptyDefinition = Definition(0, EmptyEntries);
        private static readonly WaveDefinition WaveOneDefinition = Definition(72, RatOnlyEntries,
            Pack(new[] { "plague_rat" }, 8, 12, 7f, 11f));
        private static readonly WaveDefinition WaveTwoDefinition = Definition(104, RatSpiritEntries,
            Pack(new[] { "vengeful_spirit" }, 10, 14, 10f, 14f),
            new[] { "shield_dokkaebi", "spirit_shaman" }, 1);
        private static readonly WaveDefinition WaveThreeDefinition = Definition(128, LearnedEntries,
            Pack(new[] { "dokkaebi" }, 10, 16, 9f, 13f),
            new[] { "charging_horn_ghost", "splitting_rat" }, 1);
        private static readonly WaveDefinition PeakDefinition = Definition(140, PeakEntries,
            Pack(new[] { "vengeful_spirit", "dokkaebi" }, 12, 18, 8f, 12f),
            new[] { "shield_dokkaebi", "spirit_shaman", "charging_horn_ghost", "splitting_rat" }, 2);
        private static readonly WaveDefinition BossDefinition = Definition(36, BossEntries);

        public static WaveDefinition For(RunPhase phase, bool normalWavesStopped) =>
            normalWavesStopped ? EmptyDefinition : For(phase);

        public static WaveDefinition For(RunPhase phase, RunTick tick) =>
            For(phase, tick.NormalWavesStopped);

        public static WaveDefinition For(RunPhase phase) => phase switch
        {
            RunPhase.WaveOne => WaveOneDefinition,
            RunPhase.WaveTwo => WaveTwoDefinition,
            RunPhase.WaveThree => WaveThreeDefinition,
            RunPhase.Peak => PeakDefinition,
            RunPhase.BossWarning => BossDefinition,
            RunPhase.Boss => BossDefinition,
            RunPhase.Expired => EmptyDefinition,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
        };

        public static IReadOnlyList<WeightedEnemyEntry> NormalEntriesAt(float elapsedSeconds)
        {
            if (float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds) || elapsedSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (elapsedSeconds < 45f) return RatOnlyEntries;
            if (elapsedSeconds < 53f) return SpiritOnlyEntries;
            if (elapsedSeconds < 90f) return RatSpiritEntries;
            if (elapsedSeconds < 100f) return DokkaebiOnlyEntries;
            if (elapsedSeconds < 135f) return LearnedEntries;
            return PeakEntries;
        }

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
