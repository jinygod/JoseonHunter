using System;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Domain.Progression
{
    public interface IAffixRandom
    {
        double NextUnit();
        int NextIndex(int exclusiveMax);
    }

    public sealed class SeededAffixRandom : IAffixRandom
    {
        private readonly Random random;

        public SeededAffixRandom(int seed) => random = new Random(seed);
        public double NextUnit() => random.NextDouble();
        public int NextIndex(int exclusiveMax) => random.Next(exclusiveMax);
    }

    public static class WeaponAffixRoller
    {
        public static int StableSeed(WeaponId weaponId, int level, int kills, int ordinal)
        {
            unchecked
            {
                var hash = 17;
                foreach (var character in weaponId.Value) hash = hash * 31 + character;
                return (((hash * 31) + level) * 31 + kills) * 31 + ordinal;
            }
        }

        public static WeaponAffixRollResult RollAndApply(WeaponRunAffixState state, WeaponId weaponId, IAffixRandom random)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var profile = state.ProfileFor(weaponId);
            var stats = WeaponAffixCatalog.CompatibleStats(weaponId);
            var stat = stats[random.NextIndex(stats.Count)];
            var valueUnit = random.NextUnit();
            var general = new WeaponAffixRoll(stat, TierFor(valueUnit), ValueFor(stat, valueUnit));
            profile.AddGeneral(general);
            return new WeaponAffixRollResult(general, Array.Empty<WeaponPotentialId>());
        }

        private static WeaponAffixTier TierFor(double valueUnit) =>
            valueUnit >= .95 ? WeaponAffixTier.Perfect : valueUnit >= .75 ? WeaponAffixTier.High : WeaponAffixTier.Standard;

        private static double ValueFor(WeaponAffixStat stat, double valueUnit)
        {
            var clampedUnit = Math.Max(0d, Math.Min(1d, valueUnit));
            return stat switch
            {
                WeaponAffixStat.Damage => Interpolate(10d, 30d, clampedUnit),
                WeaponAffixStat.Cooldown => -Interpolate(5d, 12d, clampedUnit),
                WeaponAffixStat.Area => Interpolate(8d, 20d, clampedUnit),
                WeaponAffixStat.ProjectileSpeed => Interpolate(10d, 30d, clampedUnit),
                WeaponAffixStat.Duration => Interpolate(10d, 25d, clampedUnit),
                _ => throw new ArgumentOutOfRangeException(nameof(stat))
            };
        }

        private static double Interpolate(double min, double max, double unit) => min + ((max - min) * unit);
    }
}
