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

    public sealed class WaveSpawnDirector
    {
        private readonly int seed;
        private Random random;
        private RunPhase? scheduledPhase;
        private float nextPackSeconds;
        private int packOrdinal;

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
        }

        public string SelectNormal(RunPhase phase)
        {
            var entries = WaveSchedule.For(phase).WeightedContent;
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

        private float NextInterval(in WavePackDefinition pack) =>
            pack.MinimumIntervalSeconds +
            (float)random.NextDouble() * (pack.MaximumIntervalSeconds - pack.MinimumIntervalSeconds);

        private static float PhaseStartSeconds(RunPhase phase) => phase switch
        {
            RunPhase.WaveOne => 0f,
            RunPhase.WaveTwo => 45f,
            RunPhase.WaveThree => 90f,
            RunPhase.Peak => 135f,
            RunPhase.BossWarning => 165f,
            RunPhase.Boss => 180f,
            RunPhase.Expired => 240f,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, null)
        };
    }
}
