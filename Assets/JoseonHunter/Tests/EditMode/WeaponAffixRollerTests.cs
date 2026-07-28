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
        public void New_weapon_roll_adds_one_compatible_general_affix()
        {
            var state = new WeaponRunAffixState();
            var result = WeaponAffixRoller.RollAndApply(
                state, WeaponId.JangseungWard,
                new SequenceAffixRandom(new[] { .10, .99 }, new[] { 0 }));

            Assert.That(result.General.Stat, Is.Not.EqualTo(WeaponAffixStat.ProjectileSpeed));
            Assert.That(state.ProfileFor(WeaponId.JangseungWard).GeneralRolls.Count, Is.EqualTo(1));
        }

        [Test]
        public void Jackpot_can_fill_three_distinct_lines_in_one_roll()
        {
            var state = new WeaponRunAffixState();
            var result = WeaponAffixRoller.RollAndApply(
                state, WeaponId.HwandoFlyingBlade,
                new SequenceAffixRandom(new[] { .99, .01, .01, .001 }, new[] { 0, 0, 1, 2 }));

            Assert.That(result.NewPotentials.Count, Is.EqualTo(3));
            Assert.That(result.NewPotentials.Distinct().Count(), Is.EqualTo(3));
            Assert.That(state.ProfileFor(WeaponId.HwandoFlyingBlade).PotentialIds.Count, Is.EqualTo(3));
        }

        [TestCase(WeaponAffixStat.Damage, 10d, 30d)]
        [TestCase(WeaponAffixStat.Cooldown, -12d, -5d)]
        [TestCase(WeaponAffixStat.Area, 8d, 20d)]
        [TestCase(WeaponAffixStat.ProjectileSpeed, 10d, 30d)]
        [TestCase(WeaponAffixStat.Duration, 10d, 25d)]
        public void General_roll_values_stay_in_approved_range(WeaponAffixStat stat, double minimum, double maximum)
        {
            var weapon = WeaponRoster.All.First(id => WeaponAffixCatalog.CompatibleStats(id).Contains(stat));
            var index = WeaponAffixCatalog.CompatibleStats(weapon).ToList().IndexOf(stat);

            foreach (var unit in new[] { 0d, 1d })
            {
                var result = WeaponAffixRoller.RollAndApply(
                    new WeaponRunAffixState(), weapon,
                    new SequenceAffixRandom(new[] { unit, .99 }, new[] { index }));

                Assert.That(result.General.Value, Is.EqualTo(unit == 0d ? minimum : maximum));
            }
        }

        [Test]
        public void Catalog_has_the_exact_three_approved_potentials_for_every_weapon()
        {
            AssertPotentialMap(WeaponId.HwandoFlyingBlade, WeaponPotentialId.HwandoVenomFang, WeaponPotentialId.HwandoReturningAfterimage, WeaponPotentialId.HwandoFlyingBladeDance);
            AssertPotentialMap(WeaponId.GakgungShot, WeaponPotentialId.GakgungArmorBreakArrowhead, WeaponPotentialId.GakgungSplitFletching, WeaponPotentialId.GakgungFullDraw);
            AssertPotentialMap(WeaponId.TalismanThrow, WeaponPotentialId.TalismanFiveElementCycle, WeaponPotentialId.TalismanSealTransfer, WeaponPotentialId.TalismanVengefulGhostBurst);
            AssertPotentialMap(WeaponId.ThunderCrashBomb, WeaponPotentialId.ThunderEarthCurrent, WeaponPotentialId.ThunderOverchargedCore, WeaponPotentialId.ThunderLightningRod);
            AssertPotentialMap(WeaponId.JangseungWard, WeaponPotentialId.JangseungGhostFace, WeaponPotentialId.JangseungFourDirectionBarrier, WeaponPotentialId.JangseungGuardianDescent);
            AssertPotentialMap(WeaponId.SingijeonVolley, WeaponPotentialId.SingijeonPowderTrail, WeaponPotentialId.SingijeonSubmunitionSplit, WeaponPotentialId.SingijeonChainIgnition);
            AssertPotentialMap(WeaponId.FrostFlask, WeaponPotentialId.FrostCrackMark, WeaponPotentialId.FrostSpread, WeaponPotentialId.FrostMist);
            AssertPotentialMap(WeaponId.WindThunderFan, WeaponPotentialId.FanVacuumEdge, WeaponPotentialId.FanDistantThunder, WeaponPotentialId.FanReturningChain);
        }

        [TestCase(0, .05)]
        [TestCase(1, .02)]
        [TestCase(2, .005)]
        public void Jackpot_chance_uses_strict_less_than_boundaries(int existingLines, double chance)
        {
            var successfulState = StateWithPotentialLines(existingLines);
            var successUnits = existingLines == 2
                ? new[] { .2, chance - .000001 }
                : new[] { .2, chance - .000001, .99 };
            var success = new SequenceAffixRandom(successUnits, new[] { 0, 0 });
            var successfulResult = WeaponAffixRoller.RollAndApply(successfulState, WeaponId.HwandoFlyingBlade, success);
            Assert.That(successfulResult.NewPotentials.Count, Is.EqualTo(1));
            Assert.That(success.RemainingUnits, Is.EqualTo(0));
            Assert.That(success.RemainingIndices, Is.EqualTo(0));

            var failedState = StateWithPotentialLines(existingLines);
            var failure = new SequenceAffixRandom(new[] { .2, chance }, new[] { 0 });
            var failedResult = WeaponAffixRoller.RollAndApply(failedState, WeaponId.HwandoFlyingBlade, failure);
            Assert.That(failedResult.NewPotentials, Is.Empty);
            Assert.That(failure.RemainingUnits, Is.EqualTo(0));
            Assert.That(failure.RemainingIndices, Is.EqualTo(0));
        }

        [Test]
        public void Second_line_continuation_uses_strict_eight_percent_boundary()
        {
            var success = new SequenceAffixRandom(new[] { .2, 0d, .08 - .000001, .99 }, new[] { 0, 0, 1 });
            var successResult = WeaponAffixRoller.RollAndApply(new WeaponRunAffixState(), WeaponId.HwandoFlyingBlade, success);
            Assert.That(successResult.NewPotentials.Count, Is.EqualTo(2));
            Assert.That(success.RemainingUnits, Is.EqualTo(0));
            Assert.That(success.RemainingIndices, Is.EqualTo(0));

            var failure = new SequenceAffixRandom(new[] { .2, 0d, .08 }, new[] { 0, 0 });
            var failureResult = WeaponAffixRoller.RollAndApply(new WeaponRunAffixState(), WeaponId.HwandoFlyingBlade, failure);
            Assert.That(failureResult.NewPotentials.Count, Is.EqualTo(1));
            Assert.That(failure.RemainingUnits, Is.EqualTo(0));
            Assert.That(failure.RemainingIndices, Is.EqualTo(0));
        }

        [Test]
        public void Third_line_continuation_uses_strict_one_percent_boundary()
        {
            var success = new SequenceAffixRandom(new[] { .2, 0d, 0d, .01 - .000001 }, new[] { 0, 0, 1, 2 });
            var successResult = WeaponAffixRoller.RollAndApply(new WeaponRunAffixState(), WeaponId.HwandoFlyingBlade, success);
            Assert.That(successResult.NewPotentials.Count, Is.EqualTo(3));
            Assert.That(success.RemainingUnits, Is.EqualTo(0));
            Assert.That(success.RemainingIndices, Is.EqualTo(0));

            var failure = new SequenceAffixRandom(new[] { .2, 0d, 0d, .01 }, new[] { 0, 0, 1 });
            var failureResult = WeaponAffixRoller.RollAndApply(new WeaponRunAffixState(), WeaponId.HwandoFlyingBlade, failure);
            Assert.That(failureResult.NewPotentials.Count, Is.EqualTo(2));
            Assert.That(failure.RemainingUnits, Is.EqualTo(0));
            Assert.That(failure.RemainingIndices, Is.EqualTo(0));
        }

        [Test]
        public void Filling_the_third_line_does_not_consume_continuation_draws()
        {
            var fromTwoLines = new SequenceAffixRandom(new[] { .2, 0d }, new[] { 0, 0 });
            WeaponAffixRoller.RollAndApply(StateWithPotentialLines(2), WeaponId.HwandoFlyingBlade, fromTwoLines);
            Assert.That(fromTwoLines.RemainingUnits, Is.EqualTo(0));
            Assert.That(fromTwoLines.RemainingIndices, Is.EqualTo(0));

            var fromOneLine = new SequenceAffixRandom(new[] { .2, 0d, 0d }, new[] { 0, 0, 1 });
            WeaponAffixRoller.RollAndApply(StateWithPotentialLines(1), WeaponId.HwandoFlyingBlade, fromOneLine);
            Assert.That(fromOneLine.RemainingUnits, Is.EqualTo(0));
            Assert.That(fromOneLine.RemainingIndices, Is.EqualTo(0));
        }

        [Test]
        public void Repeated_jackpots_have_no_dead_rolls_and_never_exceed_three_lines()
        {
            var state = new WeaponRunAffixState();
            for (var i = 0; i < 3; i++)
            {
                var result = WeaponAffixRoller.RollAndApply(
                    state, WeaponId.HwandoFlyingBlade,
                    new SequenceAffixRandom(new[] { .2, 0d, .99 }, new[] { 0, 0 }));
                Assert.That(result.NewPotentials.Count, Is.EqualTo(1));
            }

            var cappedResult = WeaponAffixRoller.RollAndApply(
                state, WeaponId.HwandoFlyingBlade,
                new SequenceAffixRandom(new[] { .2 }, new[] { 0 }));

            Assert.That(cappedResult.NewPotentials, Is.Empty);
            Assert.That(state.ProfileFor(WeaponId.HwandoFlyingBlade).PotentialIds.Count, Is.EqualTo(3));
            Assert.That(state.ProfileFor(WeaponId.HwandoFlyingBlade).PotentialIds.Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void Every_roster_weapon_only_rolls_compatible_stats_and_potentials()
        {
            foreach (var weapon in WeaponRoster.All)
            for (var seed = 0; seed < 100; seed++)
            {
                var state = new WeaponRunAffixState();
                var result = WeaponAffixRoller.RollAndApply(state, weapon, new SeededAffixRandom(seed));

                Assert.That(WeaponAffixCatalog.CompatibleStats(weapon), Does.Contain(result.General.Stat));
                Assert.That(result.NewPotentials.All(id => WeaponAffixCatalog.CompatiblePotentials(weapon).Contains(id)), Is.True);
            }
        }

        [Test]
        public void Seeded_random_produces_repeatable_rolls_and_clear_resets_profiles()
        {
            var first = RollSequence(17);
            var second = RollSequence(17);
            Assert.That(first, Is.EqualTo(second));

            var state = new WeaponRunAffixState();
            WeaponAffixRoller.RollAndApply(state, WeaponId.FrostFlask, new SeededAffixRandom(3));
            state.Clear();
            Assert.That(state.ProfileFor(WeaponId.FrostFlask).GeneralRolls, Is.Empty);
            Assert.That(state.ProfileFor(WeaponId.FrostFlask).PotentialIds, Is.Empty);
        }

        private static string RollSequence(int seed)
        {
            var random = new SeededAffixRandom(seed);
            var state = new WeaponRunAffixState();
            return string.Join("|", Enumerable.Range(0, 6).Select(_ =>
            {
                var result = WeaponAffixRoller.RollAndApply(state, WeaponId.TalismanThrow, random);
                return $"{result.General.Stat}:{result.General.Tier}:{result.General.Value:R}:{string.Join(",", result.NewPotentials.Select(id => id.Value))}";
            }));
        }

        private static WeaponRunAffixState StateWithPotentialLines(int lineCount)
        {
            var state = new WeaponRunAffixState();
            for (var line = 0; line < lineCount; line++)
            {
                WeaponAffixRoller.RollAndApply(
                    state,
                    WeaponId.HwandoFlyingBlade,
                    new SequenceAffixRandom(new[] { .2, 0d, .99 }, new[] { 0, 0 }));
            }

            return state;
        }

        private static void AssertPotentialMap(WeaponId weapon, params WeaponPotentialId[] expected) =>
            CollectionAssert.AreEqual(expected, WeaponAffixCatalog.CompatiblePotentials(weapon));

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
