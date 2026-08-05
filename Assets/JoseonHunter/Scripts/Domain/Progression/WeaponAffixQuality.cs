using System;
using System.Collections.Generic;

namespace JoseonHunter.Domain.Progression
{
    public enum WeaponAffixQualityBand
    {
        Ash,
        Green,
        Blue,
        Crimson,
        Gold
    }

    public static class WeaponAffixQuality
    {
        public static float Score(IReadOnlyList<WeaponAffixRoll> rolls)
        {
            if (rolls == null || rolls.Count == 0) return 0f;

            var total = 0d;
            for (var index = 0; index < rolls.Count; index++)
            {
                var roll = rolls[index];
                var range = RangeFor(roll.Stat);
                var normalized = (Math.Abs(roll.Value) - range.Minimum) /
                                 (range.Maximum - range.Minimum);
                total += Math.Max(0d, Math.Min(1d, normalized));
            }

            return (float)(total / rolls.Count);
        }

        public static WeaponAffixQualityBand BandFor(float score)
        {
            var clamped = Math.Max(0f, Math.Min(1f, score));
            if (clamped >= .90f) return WeaponAffixQualityBand.Gold;
            if (clamped >= .70f) return WeaponAffixQualityBand.Crimson;
            if (clamped >= .50f) return WeaponAffixQualityBand.Blue;
            return clamped >= .30f ? WeaponAffixQualityBand.Green : WeaponAffixQualityBand.Ash;
        }

        private static AffixRange RangeFor(WeaponAffixStat stat) => stat switch
        {
            WeaponAffixStat.Damage => new AffixRange(10d, 30d),
            WeaponAffixStat.Cooldown => new AffixRange(5d, 12d),
            WeaponAffixStat.Area => new AffixRange(8d, 20d),
            WeaponAffixStat.ProjectileSpeed => new AffixRange(10d, 30d),
            WeaponAffixStat.Duration => new AffixRange(10d, 25d),
            _ => throw new ArgumentOutOfRangeException(nameof(stat))
        };

        private readonly struct AffixRange
        {
            public AffixRange(double minimum, double maximum)
            {
                Minimum = minimum;
                Maximum = maximum;
            }

            public double Minimum { get; }
            public double Maximum { get; }
        }
    }
}
