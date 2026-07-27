using System;

namespace JoseonHunter.Domain.Progression
{
    public static class ExperienceCurve
    {
        private static readonly int[] Thresholds = { 5, 8, 12, 18, 26, 36, 48, 62 };

        public static int GetThresholdForNextLevel(int level)
        {
            if (level < 1 || level > Thresholds.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            return Thresholds[level - 1];
        }
    }
}
