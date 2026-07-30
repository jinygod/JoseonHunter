using System;

namespace JoseonHunter.Domain.Runs
{
    public enum StageMilestone
    {
        FirstSurge,
        FirstMidBoss,
        SecondSurge,
        SecondMidBoss,
        FinalSurge,
        FinalBossWarning,
        FinalBoss
    }

    public readonly struct StagePacingSnapshot
    {
        public StagePacingSnapshot(
            int activeCap,
            float spawnIntervalSeconds,
            int batchSize,
            float eliteChance,
            int surgeIntensity)
        {
            ActiveCap = activeCap;
            SpawnIntervalSeconds = spawnIntervalSeconds;
            BatchSize = batchSize;
            EliteChance = eliteChance;
            SurgeIntensity = surgeIntensity;
        }

        public int ActiveCap { get; }
        public float SpawnIntervalSeconds { get; }
        public int BatchSize { get; }
        public float EliteChance { get; }
        public int SurgeIntensity { get; }
        public float EnemiesPerSecond => BatchSize / SpawnIntervalSeconds;
    }

    /// <summary>
    /// Maps every run to the authored fifteen-minute pacing coordinate system.
    /// The one-minute vertical slice reserves its final ten seconds for the boss fight.
    /// </summary>
    public readonly struct StagePacingTimeline
    {
        public const float CanonicalDurationSeconds = 900f;
        public const int MobileActiveCap = 140;
        private const float PreviewDurationSeconds = 60f;
        private const float PreviewEventWindowSeconds = 50f;

        private StagePacingTimeline(float runDurationSeconds, float eventWindowSeconds)
        {
            RunDurationSeconds = runDurationSeconds;
            EventWindowSeconds = eventWindowSeconds;
        }

        public float RunDurationSeconds { get; }
        public float EventWindowSeconds { get; }

        public static StagePacingTimeline ForDuration(float runDurationSeconds)
        {
            if (float.IsNaN(runDurationSeconds) || float.IsInfinity(runDurationSeconds) ||
                runDurationSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(runDurationSeconds),
                    "Stage duration must be finite and positive.");
            }

            var eventWindow = runDurationSeconds <= PreviewDurationSeconds
                ? runDurationSeconds * (PreviewEventWindowSeconds / PreviewDurationSeconds)
                : runDurationSeconds;
            return new StagePacingTimeline(runDurationSeconds, eventWindow);
        }

        public bool Crossed(float previousElapsedSeconds, float elapsedSeconds, StageMilestone milestone)
        {
            if (elapsedSeconds < previousElapsedSeconds) return false;
            var threshold = ToRunSeconds(CanonicalTime(milestone));
            return previousElapsedSeconds < threshold && elapsedSeconds >= threshold;
        }

        public StagePacingSnapshot Sample(float elapsedSeconds)
        {
            var canonical = ToCanonicalSeconds(elapsedSeconds);
            var progress = Clamp01(canonical / CanonicalDurationSeconds);
            var surge = SurgeIntensityAt(canonical);
            var activeCap = Math.Min(MobileActiveCap,
                84 + (int)Math.Round(progress * 48f) + surge * 10);
            var batchSize = progress >= .80f ? 5 :
                progress >= .55f ? 4 :
                progress >= .25f ? 3 : 2;
            batchSize = Math.Min(6, batchSize + (surge > 0 ? 1 : 0));

            var spawnInterval = Lerp(.22f, .095f, progress);
            if (surge > 0) spawnInterval *= surge == 1 ? .58f : surge == 2 ? .52f : .47f;
            spawnInterval = Math.Max(.07f, spawnInterval);
            var eliteChance = Math.Min(.18f, Lerp(.035f, .12f, progress) + surge * .018f);

            return new StagePacingSnapshot(
                activeCap,
                spawnInterval,
                batchSize,
                eliteChance,
                surge);
        }

        public float ToRunSeconds(float canonicalSeconds) =>
            Math.Max(0f, canonicalSeconds) * EventWindowSeconds / CanonicalDurationSeconds;

        private float ToCanonicalSeconds(float elapsedSeconds)
        {
            if (elapsedSeconds <= 0f) return 0f;
            return Math.Min(CanonicalDurationSeconds,
                elapsedSeconds * CanonicalDurationSeconds / EventWindowSeconds);
        }

        private static int SurgeIntensityAt(float canonicalSeconds)
        {
            if (canonicalSeconds >= 720f && canonicalSeconds < 755f) return 3;
            if (canonicalSeconds >= 420f && canonicalSeconds < 450f) return 2;
            if (canonicalSeconds >= 120f && canonicalSeconds < 145f) return 1;
            return 0;
        }

        private static float CanonicalTime(StageMilestone milestone)
        {
            return milestone switch
            {
                StageMilestone.FirstSurge => 120f,
                StageMilestone.FirstMidBoss => 300f,
                StageMilestone.SecondSurge => 420f,
                StageMilestone.SecondMidBoss => 600f,
                StageMilestone.FinalSurge => 720f,
                StageMilestone.FinalBossWarning => 840f,
                StageMilestone.FinalBoss => 900f,
                _ => throw new ArgumentOutOfRangeException(nameof(milestone), milestone, null)
            };
        }

        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
        private static float Lerp(float from, float to, float t) => from + (to - from) * Clamp01(t);
    }
}
