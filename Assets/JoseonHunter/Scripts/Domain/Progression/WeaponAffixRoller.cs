using System;
using System.Collections.Generic;
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

            var newPotentials = new List<WeaponPotentialId>();
            if (profile.PotentialIds.Count < 3 && random.NextUnit() < JackpotChance(profile.PotentialIds.Count))
            {
                AddPotential(profile, weaponId, random, newPotentials);
                if (profile.PotentialIds.Count >= 3) return new WeaponAffixRollResult(general, newPotentials.AsReadOnly());

                if (newPotentials.Count > 0 && random.NextUnit() < .08)
                {
                    AddPotential(profile, weaponId, random, newPotentials);
                    if (profile.PotentialIds.Count >= 3) return new WeaponAffixRollResult(general, newPotentials.AsReadOnly());
                }

                if (newPotentials.Count > 1 && random.NextUnit() < .01) AddPotential(profile, weaponId, random, newPotentials);
            }

            return new WeaponAffixRollResult(general, newPotentials.AsReadOnly());
        }

        private static void AddPotential(WeaponRunAffixProfile profile, WeaponId weaponId, IAffixRandom random, List<WeaponPotentialId> added)
        {
            if (profile.PotentialIds.Count >= 3) return;
            var potentials = WeaponAffixCatalog.CompatiblePotentials(weaponId);
            var startIndex = random.NextIndex(potentials.Count);
            for (var offset = 0; offset < potentials.Count; offset++)
            {
                var candidate = potentials[(startIndex + offset) % potentials.Count];
                if (profile.AddPotential(candidate))
                {
                    added.Add(candidate);
                    return;
                }
            }
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
        private static double JackpotChance(int potentialCount) => potentialCount == 0 ? .05 : potentialCount == 1 ? .02 : potentialCount == 2 ? .005 : 0d;
    }
}
