using System;

namespace JoseonHunter.Domain.Progression
{
    public static class RunLoadoutRules
    {
        public const int WeaponSlotLimit = 4;
        public const int SupportSlotLimit = 3;
        public const int MaximumPlayerLevel = 35;

        public static int ReplacementLevel(int discardedLevel)
        {
            if (discardedLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(discardedLevel));

            return Math.Max(1, Math.Min(3, discardedLevel - 1));
        }
    }
}
