using System;
using System.Collections.Generic;

namespace JoseonHunter.Domain.Runs
{
    public readonly struct BossScheduleEntry
    {
        public BossScheduleEntry(string bossId, float warningSeconds, float spawnSeconds, int difficultyTier, bool isFinal)
        {
            BossId = bossId;
            WarningSeconds = warningSeconds;
            SpawnSeconds = spawnSeconds;
            DifficultyTier = difficultyTier;
            IsFinal = isFinal;
        }

        public string BossId { get; }
        public float WarningSeconds { get; }
        public float SpawnSeconds { get; }
        public int DifficultyTier { get; }
        public bool IsFinal { get; }
    }

    public sealed class RunProfile
    {
        public RunProfile(float durationSeconds, IReadOnlyList<BossScheduleEntry> bosses)
        {
            ValidateDuration(durationSeconds);
            if (bosses == null) throw new ArgumentNullException(nameof(bosses));

            var copiedBosses = new BossScheduleEntry[bosses.Count];
            for (var index = 0; index < bosses.Count; index++)
                copiedBosses[index] = bosses[index];

            ValidateBosses(durationSeconds, copiedBosses);
            DurationSeconds = durationSeconds;
            Bosses = Array.AsReadOnly(copiedBosses);
        }

        public float DurationSeconds { get; }
        public IReadOnlyList<BossScheduleEntry> Bosses { get; }

        public static RunProfile Test60Seconds() => new RunProfile(
            60f,
            new[] { new BossScheduleEntry("fallen_general_test", 45f, 50f, 1, true) });

        public static RunProfile Production15Minutes() => new RunProfile(
            900f,
            new[]
            {
                new BossScheduleEntry("boss_first", 285f, 300f, 1, false),
                new BossScheduleEntry("boss_second", 585f, 600f, 2, false),
                new BossScheduleEntry("boss_final", 885f, 900f, 3, true)
            });

        private static void ValidateDuration(float durationSeconds)
        {
            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds) || durationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Run duration must be finite and positive.");
        }

        private static void ValidateBosses(float durationSeconds, IReadOnlyList<BossScheduleEntry> bosses)
        {
            if (bosses.Count == 0) throw new ArgumentException("A run profile requires a final boss.", nameof(bosses));

            var bossIds = new HashSet<string>(StringComparer.Ordinal);
            var finalBossCount = 0;
            var previousSpawnSeconds = -1f;
            var previousDifficultyTier = 0;

            foreach (var boss in bosses)
            {
                if (string.IsNullOrWhiteSpace(boss.BossId) || !bossIds.Add(boss.BossId))
                    throw new ArgumentException("Boss IDs must be non-empty and unique.", nameof(bosses));
                if (!IsFinite(boss.WarningSeconds) || !IsFinite(boss.SpawnSeconds) ||
                    boss.WarningSeconds < 0f || boss.WarningSeconds >= boss.SpawnSeconds ||
                    boss.SpawnSeconds > durationSeconds)
                    throw new ArgumentException("Boss warnings must precede finite spawn times within the run duration.", nameof(bosses));
                if (boss.SpawnSeconds <= previousSpawnSeconds)
                    throw new ArgumentException("Boss schedules must be sorted by spawn time.", nameof(bosses));
                if (boss.DifficultyTier <= previousDifficultyTier)
                    throw new ArgumentException("Boss difficulty tiers must strictly increase.", nameof(bosses));

                previousSpawnSeconds = boss.SpawnSeconds;
                previousDifficultyTier = boss.DifficultyTier;
                if (boss.IsFinal) finalBossCount++;
            }

            if (finalBossCount != 1)
                throw new ArgumentException("A run profile requires exactly one final boss.", nameof(bosses));
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
