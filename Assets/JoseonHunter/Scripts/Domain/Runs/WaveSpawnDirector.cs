using System;

namespace JoseonHunter.Domain.Runs
{
    public readonly struct EnemyPackPlan
    {
        public EnemyPackPlan(string contentId, int count, int side)
        {
            ContentId = contentId;
            Count = count;
            Side = side;
        }

        public string ContentId { get; }
        public int Count { get; }
        public int Side { get; }
    }

    public readonly struct EnemyIntroductionPlan
    {
        public EnemyIntroductionPlan(string contentId, int spawnCount)
        {
            ContentId = contentId;
            SpawnCount = spawnCount;
        }

        public string ContentId { get; }
        public int SpawnCount { get; }
    }

    public sealed class WaveSpawnDirector
    {
        private readonly int seed;
        private Random random;
        private RunPhase? scheduledPhase;
        private float nextPackSeconds;
        private int packOrdinal;
        private int nextIntroductionIndex;
        private int introducedSpecialCount;
        private int specialOrdinal;
        private float lastSpecialSpawnSeconds;

        public WaveSpawnDirector(int seed)
        {
            this.seed = seed;
            Reset();
        }

        public void Reset()
        {
            random = new Random(seed);
            scheduledPhase = null;
            nextPackSeconds = float.PositiveInfinity;
            packOrdinal = 0;
            nextIntroductionIndex = 0;
            introducedSpecialCount = 0;
            specialOrdinal = 0;
            lastSpecialSpawnSeconds = float.NegativeInfinity;
        }

        public string SelectNormal(RunPhase phase)
        {
            return SelectWeighted(WaveSchedule.For(phase).WeightedContent);
        }

        public string SelectNormal(float elapsedSeconds)
        {
            return SelectWeighted(WaveSchedule.NormalEntriesAt(elapsedSeconds));
        }

        private string SelectWeighted(System.Collections.Generic.IReadOnlyList<WeightedEnemyEntry> entries)
        {
            if (entries.Count == 0) return string.Empty;

            var totalWeight = 0;
            for (var index = 0; index < entries.Count; index++) totalWeight += entries[index].Weight;
            var roll = random.Next(totalWeight);
            for (var index = 0; index < entries.Count; index++)
            {
                if (roll < entries[index].Weight) return entries[index].ContentId;
                roll -= entries[index].Weight;
            }

            return entries[entries.Count - 1].ContentId;
        }

        public bool TryCreatePack(float elapsedSeconds, int availableSlots, out EnemyPackPlan plan)
        {
            plan = default;
            if (float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds) || elapsedSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (availableSlots <= 0) return false;

            var phase = RunClock.PhaseAt(elapsedSeconds);
            var definition = WaveSchedule.For(phase);
            if (!definition.Pack.HasValue) return false;
            var pack = definition.Pack.Value;
            if (availableSlots < pack.MinimumSize) return false;

            if (scheduledPhase != phase)
            {
                scheduledPhase = phase;
                nextPackSeconds = PhaseStartSeconds(phase) + NextInterval(pack);
                packOrdinal = 0;
            }

            if (elapsedSeconds < nextPackSeconds) return false;

            var contentId = pack.ContentIds[packOrdinal % pack.ContentIds.Count];
            packOrdinal++;
            var count = Math.Min(availableSlots, random.Next(pack.MinimumSize, pack.MaximumSize + 1));
            plan = new EnemyPackPlan(contentId, count, random.Next(0, 4));
            nextPackSeconds = elapsedSeconds + NextInterval(pack);
            return true;
        }

        public bool TryCreateIntroduction(float elapsedSeconds, int availableSlots, out EnemyIntroductionPlan plan)
        {
            plan = default;
            ValidateElapsed(elapsedSeconds);
            if (nextIntroductionIndex >= WaveSchedule.Introductions.Count) return false;
            var introduction = WaveSchedule.Introductions[nextIntroductionIndex];
            if (elapsedSeconds < introduction.AtSeconds || availableSlots < introduction.SpawnCount) return false;

            nextIntroductionIndex++;
            introducedSpecialCount++;
            lastSpecialSpawnSeconds = elapsedSeconds;
            plan = new EnemyIntroductionPlan(introduction.ContentId, introduction.SpawnCount);
            return true;
        }

        public bool TrySelectSpecial(float elapsedSeconds, int livingNormalCount, int livingSpecialCount,
            out string contentId)
        {
            contentId = string.Empty;
            ValidateElapsed(elapsedSeconds);
            if (livingNormalCount < 0) throw new ArgumentOutOfRangeException(nameof(livingNormalCount));
            if (livingSpecialCount < 0) throw new ArgumentOutOfRangeException(nameof(livingSpecialCount));
            var specialCap = livingNormalCount / 8;
            if (introducedSpecialCount == 0 || specialCap <= 0 || livingSpecialCount >= specialCap ||
                elapsedSeconds - lastSpecialSpawnSeconds < 8f) return false;

            contentId = WaveSchedule.Introductions[specialOrdinal++ % introducedSpecialCount].ContentId;
            lastSpecialSpawnSeconds = elapsedSeconds;
            return true;
        }

        private float NextInterval(in WavePackDefinition pack) =>
            pack.MinimumIntervalSeconds +
            (float)random.NextDouble() * (pack.MaximumIntervalSeconds - pack.MinimumIntervalSeconds);

        private static float PhaseStartSeconds(RunPhase phase) => phase switch
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
}
