using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public static class EnemyDensityProfile
    {
        public const int MaximumActiveEnemies = 140;

        public static float SpawnInterval(float normalizedRunTime) =>
            Mathf.Lerp(.22f, .09f, Mathf.Clamp01(normalizedRunTime));

        public static int BatchSize(float normalizedRunTime)
        {
            var progress = Mathf.Clamp01(normalizedRunTime);
            return progress >= .70f ? 4 : progress >= .35f ? 3 : 2;
        }
    }
}
