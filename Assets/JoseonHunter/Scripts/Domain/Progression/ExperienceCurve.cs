using System;

namespace JoseonHunter.Domain.Progression
{
    public static class ExperienceCurve
    {
        public static int GetThresholdForNextLevel(int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));

            var threshold = 8L + 2L * level + (long)level * level / 4L;
            return threshold >= int.MaxValue ? int.MaxValue : (int)threshold;
        }
    }
}
