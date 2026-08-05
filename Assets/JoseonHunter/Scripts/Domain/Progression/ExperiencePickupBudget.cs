using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Geumjul;

namespace JoseonHunter.Domain.Progression
{
    public enum ExperiencePickupTier
    {
        Small,
        Medium,
        Large
    }

    public static class ExperiencePickupBudget
    {
        public const int MaximumActivePickups = 180;

        public static ExperiencePickupTier TierFor(int value)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value >= 20) return ExperiencePickupTier.Large;
            return value >= 5 ? ExperiencePickupTier.Medium : ExperiencePickupTier.Small;
        }

        public static bool ShouldMerge(int activeCount)
        {
            if (activeCount < 0) throw new ArgumentOutOfRangeException(nameof(activeCount));
            return activeCount >= MaximumActivePickups;
        }

        public static int MergeValue(int existingValue, int incomingValue)
        {
            if (existingValue <= 0) throw new ArgumentOutOfRangeException(nameof(existingValue));
            if (incomingValue <= 0) throw new ArgumentOutOfRangeException(nameof(incomingValue));
            var sum = (long)existingValue + incomingValue;
            return sum >= int.MaxValue ? int.MaxValue : (int)sum;
        }

        public static int FindNearestMergeIndex(IReadOnlyList<Float2> positions, Float2 origin)
        {
            if (positions == null) throw new ArgumentNullException(nameof(positions));
            if (positions.Count == 0) return -1;
            var nearest = 0;
            var nearestDistance = DistanceSquared(positions[0], origin);
            for (var index = 1; index < positions.Count && index < MaximumActivePickups; index++)
            {
                var distance = DistanceSquared(positions[index], origin);
                if (distance >= nearestDistance) continue;
                nearest = index;
                nearestDistance = distance;
            }
            return nearest;
        }

        private static float DistanceSquared(Float2 left, Float2 right)
        {
            var x = left.X - right.X;
            var y = left.Y - right.Y;
            return x * x + y * y;
        }
    }
}

