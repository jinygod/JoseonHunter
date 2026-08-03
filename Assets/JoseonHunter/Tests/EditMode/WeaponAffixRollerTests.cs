using System;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponAffixRollerTests
    {
        [Test]
        public void Every_roll_adds_one_general_affix_without_random_potentials()
        {
            var state = new WeaponRunAffixState();
            var random = new SequenceAffixRandom(new[] { .99 }, new[] { 0 });

            var result = WeaponAffixRoller.RollAndApply(state, WeaponId.HwandoFlyingBlade, random);

            Assert.That(state.ProfileFor(WeaponId.HwandoFlyingBlade).GeneralRolls, Has.Count.EqualTo(1));
            Assert.That(result.NewPotentials, Is.Empty);
            Assert.That(state.ProfileFor(WeaponId.HwandoFlyingBlade).PotentialIds, Is.Empty);
            Assert.That(random.RemainingUnits, Is.EqualTo(0));
            Assert.That(random.RemainingIndices, Is.EqualTo(0));
        }

        [TestCase(WeaponAffixStat.Damage, 10d, 30d)]
        [TestCase(WeaponAffixStat.Cooldown, -5d, -12d)]
        [TestCase(WeaponAffixStat.Area, 8d, 20d)]
        [TestCase(WeaponAffixStat.ProjectileSpeed, 10d, 30d)]
        [TestCase(WeaponAffixStat.Duration, 10d, 25d)]
        public void General_roll_values_stay_in_approved_range(
            WeaponAffixStat stat,
            double minimum,
            double maximum)
        {
            var weapon = WeaponRoster.All.First(id => WeaponAffixCatalog.CompatibleStats(id).Contains(stat));
            var index = WeaponAffixCatalog.CompatibleStats(weapon).ToList().IndexOf(stat);

            foreach (var unit in new[] { 0d, 1d })
            {
                var result = WeaponAffixRoller.RollAndApply(
                    new WeaponRunAffixState(),
                    weapon,
                    new SequenceAffixRandom(new[] { unit }, new[] { index }));

                Assert.That(result.General.Value, Is.EqualTo(unit == 0d ? minimum : maximum));
                Assert.That(result.NewPotentials, Is.Empty);
            }
        }

        [Test]
        public void Removing_a_weapon_profile_discards_all_of_its_run_affixes()
        {
            var state = new WeaponRunAffixState();
            WeaponAffixRoller.RollAndApply(
                state,
                WeaponId.FrostFlask,
                new SequenceAffixRandom(new[] { .5 }, new[] { 0 }));

            Assert.That(state.Remove(WeaponId.FrostFlask), Is.True);
            Assert.That(state.TryProfileFor(WeaponId.FrostFlask, out _), Is.False);
            Assert.That(state.Remove(WeaponId.FrostFlask), Is.False);
        }

        [Test]
        public void Seeded_random_produces_repeatable_general_rolls_and_clear_resets_profiles()
        {
            Assert.That(RollSequence(17), Is.EqualTo(RollSequence(17)));

            var state = new WeaponRunAffixState();
            WeaponAffixRoller.RollAndApply(state, WeaponId.FrostFlask, new SeededAffixRandom(3));
            state.Clear();
            Assert.That(state.TryProfileFor(WeaponId.FrostFlask, out _), Is.False);
        }

        private static string RollSequence(int seed)
        {
            var random = new SeededAffixRandom(seed);
            var state = new WeaponRunAffixState();
            return string.Join("|", Enumerable.Range(0, 6).Select(_ =>
            {
                var result = WeaponAffixRoller.RollAndApply(state, WeaponId.TalismanThrow, random);
                return $"{result.General.Stat}:{result.General.Tier}:{result.General.Value:R}";
            }));
        }

        private sealed class SequenceAffixRandom : IAffixRandom
        {
            private readonly Queue<double> units;
            private readonly Queue<int> indices;

            public SequenceAffixRandom(IEnumerable<double> units, IEnumerable<int> indices)
            {
                this.units = new Queue<double>(units);
                this.indices = new Queue<int>(indices);
            }

            public double NextUnit()
            {
                if (units.Count == 0) throw new InvalidOperationException("Test did not provide a unit random value.");
                return units.Dequeue();
            }

            public int NextIndex(int exclusiveMax)
            {
                if (indices.Count == 0) throw new InvalidOperationException("Test did not provide an index random value.");
                var index = indices.Dequeue();
                if (index < 0 || index >= exclusiveMax) throw new InvalidOperationException("Test index is out of range.");
                return index;
            }

            public int RemainingUnits => units.Count;
            public int RemainingIndices => indices.Count;
        }
    }
}
