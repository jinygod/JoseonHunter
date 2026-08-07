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
            WavePackDefinition? pack = null)
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
        }

        public int ActiveCap { get; }
        public IReadOnlyList<string> ContentIds { get; }
        public IReadOnlyList<WeightedEnemyEntry> WeightedContent { get; }
        public WavePackDefinition? Pack { get; }
    }

    public readonly struct EnemyIntroductionDefinition
    {
        public EnemyIntroductionDefinition(float atSeconds, string contentId, int spawnCount)
        {
            if (float.IsNaN(atSeconds) || float.IsInfinity(atSeconds) || atSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(atSeconds));
            if (string.IsNullOrWhiteSpace(contentId))
                throw new ArgumentException("Enemy content ID is required.", nameof(contentId));
            if (spawnCount <= 0) throw new ArgumentOutOfRangeException(nameof(spawnCount));
            AtSeconds = atSeconds;
            ContentId = contentId;
            SpawnCount = spawnCount;
        }

        public float AtSeconds { get; }
        public string ContentId { get; }
        public int SpawnCount { get; }
    }

    public readonly struct TimedWaveDefinition
    {
        public TimedWaveDefinition(float atSeconds, WaveDefinition definition)
        {
            if (float.IsNaN(atSeconds) || float.IsInfinity(atSeconds) || atSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(atSeconds));
            AtSeconds = atSeconds;
            Definition = definition;
        }

        public float AtSeconds { get; }
        public WaveDefinition Definition { get; }
    }

    public readonly struct TimedEnemyRoster
    {
        public TimedEnemyRoster(float atSeconds, IReadOnlyList<WeightedEnemyEntry> entries)
        {
            if (float.IsNaN(atSeconds) || float.IsInfinity(atSeconds) || atSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(atSeconds));
            AtSeconds = atSeconds;
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        public float AtSeconds { get; }
        public IReadOnlyList<WeightedEnemyEntry> Entries { get; }
    }

    public sealed class StageWaveProfile
    {
        private readonly IReadOnlyList<TimedWaveDefinition> windows;
        private readonly IReadOnlyList<TimedEnemyRoster> normalRosters;

        public StageWaveProfile(
            IReadOnlyList<TimedWaveDefinition> windows,
            IReadOnlyList<EnemyIntroductionDefinition> introductions,
            IReadOnlyList<TimedEnemyRoster> normalRosters = null)
        {
            if (windows == null || windows.Count == 0)
                throw new ArgumentException("At least one wave window is required.", nameof(windows));
            Introductions = introductions ?? throw new ArgumentNullException(nameof(introductions));
            this.windows = windows;
            if (normalRosters == null)
            {
                var copied = new TimedEnemyRoster[windows.Count];
                for (var index = 0; index < windows.Count; index++)
                    copied[index] = new TimedEnemyRoster(windows[index].AtSeconds,
                        windows[index].Definition.WeightedContent);
                this.normalRosters = Array.AsReadOnly(copied);
            }
            else
            {
                this.normalRosters = normalRosters;
            }
        }

        public IReadOnlyList<EnemyIntroductionDefinition> Introductions { get; }

        public WaveDefinition WaveAt(float elapsedSeconds) => windows[WindowIndexAt(elapsedSeconds)].Definition;

        public IReadOnlyList<WeightedEnemyEntry> NormalEntriesAt(float elapsedSeconds)
        {
            ValidateElapsed(elapsedSeconds);
            var result = normalRosters[0].Entries;
            for (var index = 1; index < normalRosters.Count; index++)
            {
                if (elapsedSeconds < normalRosters[index].AtSeconds) break;
                result = normalRosters[index].Entries;
            }
            return result;
        }

        public int WindowIndexAt(float elapsedSeconds)
        {
            ValidateElapsed(elapsedSeconds);
            var result = 0;
            for (var index = 1; index < windows.Count; index++)
            {
                if (elapsedSeconds < windows[index].AtSeconds) break;
                result = index;
            }
            return result;
        }

        public float WindowStartSecondsAt(float elapsedSeconds) => windows[WindowIndexAt(elapsedSeconds)].AtSeconds;

        public WaveDefinition For(RunPhase phase, bool normalWavesStopped = false)
        {
            if (normalWavesStopped) return new WaveDefinition(0, Entries());
            return WaveAt(PhaseStartSeconds(phase));
        }

        public static TimedWaveDefinition Window(
            float atSeconds,
            int activeCap,
            IReadOnlyList<WeightedEnemyEntry> entries,
            WavePackDefinition? pack = null) =>
            new TimedWaveDefinition(atSeconds, new WaveDefinition(activeCap, entries, pack));

        public static TimedEnemyRoster Roster(
            float atSeconds,
            IReadOnlyList<WeightedEnemyEntry> entries) => new TimedEnemyRoster(atSeconds, entries);

        public static IReadOnlyList<WeightedEnemyEntry> Entries(
            params (string ContentId, int Weight)[] entries)
        {
            var result = new WeightedEnemyEntry[entries.Length];
            for (var index = 0; index < entries.Length; index++)
                result[index] = new WeightedEnemyEntry(entries[index].ContentId, entries[index].Weight);
            return Array.AsReadOnly(result);
        }

        public static WavePackDefinition Pack(
            string[] contentIds,
            int minimumSize,
            int maximumSize,
            float minimumIntervalSeconds,
            float maximumIntervalSeconds) => new WavePackDefinition(
            Array.AsReadOnly(contentIds), minimumSize, maximumSize,
            minimumIntervalSeconds, maximumIntervalSeconds);

        public static float PhaseStartSeconds(RunPhase phase) => phase switch
        {
            RunPhase.WaveOne => 0f,
            RunPhase.WaveTwo => 120f,
            RunPhase.WaveThree => 300f,
            RunPhase.Peak => 600f,
            RunPhase.BossWarning => 840f,
            RunPhase.Boss => 900f,
            RunPhase.Expired => 960f,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
        };

        private static void ValidateElapsed(float elapsedSeconds)
        {
            if (float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds) || elapsedSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        }
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
            Pack(new[] { "vengeful_spirit" }, 10, 14, 10f, 14f));
        private static readonly WaveDefinition WaveThreeDefinition = Definition(128, LearnedEntries,
            Pack(new[] { "dokkaebi" }, 10, 16, 9f, 13f));
        private static readonly WaveDefinition PeakDefinition = Definition(140, PeakEntries,
            Pack(new[] { "vengeful_spirit", "dokkaebi" }, 12, 18, 8f, 12f));
        private static readonly WaveDefinition BossDefinition = Definition(36, BossEntries);
        private static readonly IReadOnlyList<EnemyIntroductionDefinition> EnemyIntroductions =
            Array.AsReadOnly(new[]
            {
                new EnemyIntroductionDefinition(420f, "shield_dokkaebi", 1),
                new EnemyIntroductionDefinition(510f, "charging_horn_ghost", 1),
                new EnemyIntroductionDefinition(660f, "spirit_shaman", 1),
                new EnemyIntroductionDefinition(735f, "splitting_rat", 1)
            });

        private static readonly StageWaveProfile GwigokProfile = new StageWaveProfile(
            new[]
            {
                new TimedWaveDefinition(0f, WaveOneDefinition),
                new TimedWaveDefinition(120f, WaveTwoDefinition),
                new TimedWaveDefinition(300f, WaveThreeDefinition),
                new TimedWaveDefinition(600f, PeakDefinition),
                new TimedWaveDefinition(840f, BossDefinition),
                new TimedWaveDefinition(900f, BossDefinition),
                new TimedWaveDefinition(960f, EmptyDefinition)
            }, EnemyIntroductions,
            new[]
            {
                new TimedEnemyRoster(0f, RatOnlyEntries),
                new TimedEnemyRoster(120f, SpiritOnlyEntries),
                new TimedEnemyRoster(150f, RatSpiritEntries),
                new TimedEnemyRoster(300f, DokkaebiOnlyEntries),
                new TimedEnemyRoster(330f, LearnedEntries),
                new TimedEnemyRoster(600f, PeakEntries),
                new TimedEnemyRoster(840f, BossEntries),
                new TimedEnemyRoster(960f, EmptyEntries)
            });

        public static StageWaveProfile Profile => GwigokProfile;

        public static IReadOnlyList<EnemyIntroductionDefinition> Introductions => EnemyIntroductions;

        public static WaveDefinition For(RunPhase phase, bool normalWavesStopped) =>
            GwigokProfile.For(phase, normalWavesStopped);

        public static WaveDefinition For(RunPhase phase, RunTick tick) =>
            For(phase, tick.NormalWavesStopped);

        public static WaveDefinition For(RunPhase phase) => GwigokProfile.For(phase);

        public static IReadOnlyList<WeightedEnemyEntry> NormalEntriesAt(float elapsedSeconds)
        {
            if (float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds) || elapsedSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (elapsedSeconds < 120f) return RatOnlyEntries;
            if (elapsedSeconds < 150f) return SpiritOnlyEntries;
            if (elapsedSeconds < 300f) return RatSpiritEntries;
            if (elapsedSeconds < 330f) return DokkaebiOnlyEntries;
            if (elapsedSeconds < 600f) return LearnedEntries;
            return PeakEntries;
        }

        private static WaveDefinition Definition(
            int activeCap,
            IReadOnlyList<WeightedEnemyEntry> entries,
            WavePackDefinition? pack = null) => new(activeCap, entries, pack);

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
