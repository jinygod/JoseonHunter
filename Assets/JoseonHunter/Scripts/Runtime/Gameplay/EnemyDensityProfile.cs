using JoseonHunter.Domain.Runs;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public static class EnemyDensityProfile
    {
        public const int MaximumActiveEnemies = StagePacingTimeline.MobileActiveCap;

        public static float SpawnInterval(in StagePacingSnapshot snapshot) =>
            Mathf.Max(.07f, snapshot.SpawnIntervalSeconds);

        public static int BatchSize(in StagePacingSnapshot snapshot) =>
            Mathf.Clamp(snapshot.BatchSize, 1, 6);

        public static float SpawnInterval(float normalizedRunTime) =>
            Mathf.Lerp(.22f, .09f, Mathf.Clamp01(normalizedRunTime));

        public static int BatchSize(float normalizedRunTime)
        {
            var progress = Mathf.Clamp01(normalizedRunTime);
            return progress >= .70f ? 4 : progress >= .35f ? 3 : 2;
        }
    }
}
