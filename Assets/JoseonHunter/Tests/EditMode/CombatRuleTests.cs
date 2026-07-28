using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Runtime.Combat;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class CombatRuleTests
    {
        [Test]
        public void LevelOneHwandoOneShotsRat()
        {
            var result = DamageResolver.Resolve(new DamageRequest(8, 0, false, 1f));

            Assert.That(result.FinalDamage, Is.EqualTo(8));
            Assert.That(result.IsCritical, Is.False);
        }

        [Test]
        public void DamageRoundsAwayFromZeroAndClampsToOne()
        {
            var rounded = DamageResolver.Resolve(new DamageRequest(3, 0, true, 1.5f));
            var clamped = DamageResolver.Resolve(new DamageRequest(0, 0, false, 0f));

            Assert.That(rounded.FinalDamage, Is.EqualTo(5));
            Assert.That(rounded.IsCritical, Is.True);
            Assert.That(clamped.FinalDamage, Is.EqualTo(1));
        }

        [TestCase(1, 5)]
        [TestCase(2, 8)]
        [TestCase(3, 12)]
        [TestCase(4, 18)]
        [TestCase(5, 26)]
        [TestCase(6, 36)]
        [TestCase(7, 48)]
        [TestCase(8, 62)]
        public void ExperienceCurveUsesApprovedThresholds(int level, int expected)
        {
            Assert.That(ExperienceCurve.GetThresholdForNextLevel(level), Is.EqualTo(expected));
        }

        [Test]
        public void OwnedNonMaxWeaponAppearsInThreeOffers()
        {
            var state = State(weapons: new Dictionary<string, int> { ["hwando_flying_blade"] = 1 });

            var offers = UpgradeSelector.Select(state, 17);

            Assert.That(offers, Has.Count.EqualTo(3));
            Assert.That(offers.Select(offer => offer.Id), Is.Unique);
            Assert.That(offers, Does.Contain(new UpgradeOffer("hwando_flying_blade", UpgradeKind.Weapon, 2)));
        }

        [Test]
        public void StartingLoadoutAlwaysReceivesAnUnownedWeaponOffer()
        {
            var state = State(weapons: new Dictionary<string, int> { ["hwando_flying_blade"] = 1 });

            for (var seed = 0; seed < 100; seed++)
            {
                var offers = UpgradeSelector.Select(state, seed);

                Assert.That(
                    offers.Any(offer => offer.Kind == UpgradeKind.Weapon && offer.NextLevel == 1),
                    Is.True,
                    $"Seed {seed} did not offer a new weapon.");
            }
        }

        [Test]
        public void MaxedAndLockedEvolutionsNeverAppear()
        {
            var state = State(
                weapons: MaxedLaunchWeapons(),
                supports: new Dictionary<string, int> { ["talisman"] = 4 });

            var offers = UpgradeSelector.Select(state, 17);

            Assert.That(offers.Any(offer => offer.Id == "hwando_flying_blade"), Is.False);
            Assert.That(offers.Any(offer => offer.Kind == UpgradeKind.Evolution), Is.False);
        }

        [Test]
        public void SameSeedProducesSameOfferOrder()
        {
            var state = State();

            var first = UpgradeSelector.Select(state, 99);
            var second = UpgradeSelector.Select(state, 99);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void FullyExhaustedOfferStateThrowsStableDiagnostic()
        {
            var state = State(
                weapons: new Dictionary<string, int>
                {
                    ["hwando_flying_blade"] = 5, ["gakgung_shot"] = 5, ["talisman_throw"] = 5,
                    ["thunder_crash_bomb"] = 5, ["jangseung_ward"] = 5, ["singijeon_volley"] = 5,
                    ["frost_flask"] = 5, ["wind_thunder_fan"] = 5
                },
                supports: new Dictionary<string, int>
                {
                    ["talisman"] = 5, ["boots"] = 5, ["warding_bell"] = 5
                });

            var exception = Assert.Throws<System.InvalidOperationException>(() => UpgradeSelector.Select(state, 7));

            Assert.That(exception.Message, Is.EqualTo("At least three distinct eligible upgrades are required."));
        }

        [Test]
        public void TwoCandidateOfferStateThrowsStableDiagnostic()
        {
            var state = State(
                weapons: new Dictionary<string, int>
                {
                    ["hwando_flying_blade"] = 5, ["gakgung_shot"] = 5, ["talisman_throw"] = 5,
                    ["thunder_crash_bomb"] = 5, ["jangseung_ward"] = 5, ["singijeon_volley"] = 5,
                    ["frost_flask"] = 5, ["wind_thunder_fan"] = 5
                },
                supports: new Dictionary<string, int>
                {
                    ["talisman"] = 4, ["boots"] = 4, ["warding_bell"] = 5
                });

            Assert.That(
                () => UpgradeSelector.Select(state, 7),
                Throws.InvalidOperationException.With.Message.EqualTo("At least three distinct eligible upgrades are required."));
        }

        [Test]
        public void AcquiredUnlockedEvolutionNeverAppearsWhenThreeAlternativesExist()
        {
            var state = State(
                weapons: MaxedLaunchWeapons(),
                unlocked: new HashSet<string> { "hwando_evolution" },
                acquired: new HashSet<string> { "hwando_evolution" });

            var offers = UpgradeSelector.Select(state, 3);

            Assert.That(offers, Has.Count.EqualTo(3));
            Assert.That(offers.Any(offer => offer.Id == "hwando_evolution"), Is.False);
        }

        [Test]
        public void UpgradeStateSnapshotsCallerCollections()
        {
            var weapons = new Dictionary<string, int> { ["hwando_flying_blade"] = 1 };
            var supports = new Dictionary<string, int> { ["talisman"] = 1 };
            var unlocked = new HashSet<string>();
            var acquired = new HashSet<string>();
            var state = new UpgradeState(weapons, supports, unlocked, acquired);
            var expectedOffers = UpgradeSelector.Select(state, 11);

            weapons["hwando_flying_blade"] = 5;
            supports["talisman"] = 5;
            unlocked.Add("hwando_evolution");
            acquired.Add("hwando_evolution");

            Assert.That(state.WeaponLevels["hwando_flying_blade"], Is.EqualTo(1));
            Assert.That(state.SupportLevels["talisman"], Is.EqualTo(1));
            Assert.That(state.UnlockedIds.Contains("hwando_evolution"), Is.False);
            Assert.That(state.AcquiredEvolutionIds.Contains("hwando_evolution"), Is.False);
            Assert.That(UpgradeSelector.Select(state, 11), Is.EqualTo(expectedOffers));
        }

        [Test]
        public void DamageRequestHasStructuralValueCompatibility()
        {
            var first = new DamageRequest(8, 2, true, 1.5f);
            var second = new DamageRequest(8, 2, true, 1.5f);
            var (baseDamage, flatBonus, isCritical, multiplier) = first;

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first == second, Is.True);
            Assert.That(first != second, Is.False);
            Assert.That((baseDamage, flatBonus, isCritical, multiplier), Is.EqualTo((8, 2, true, 1.5f)));
        }

        [Test]
        public void Every_evolution_changes_two_or_more_independent_mechanic_dimensions()
        {
            foreach (var evolution in WeaponEvolutionCatalog.All)
                Assert.That(evolution.ChangedDimensions.Distinct().Count(), Is.GreaterThanOrEqualTo(2), evolution.DisplayName);
        }

        [Test]
        public void Weapon_affix_catalog_has_exact_launch_balance_and_imported_contact_assets()
        {
            Assert.That(WeaponRoster.All, Has.Count.EqualTo(8));
            var potentials = WeaponRoster.All.SelectMany(WeaponAffixCatalog.CompatiblePotentials).ToArray();
            Assert.That(potentials, Has.Length.EqualTo(24));
            Assert.That(potentials, Is.Unique, "A potential ID must belong to exactly one launch weapon.");

            AssertExactStats(WeaponId.HwandoFlyingBlade, WeaponAffixStat.Damage, WeaponAffixStat.Cooldown, WeaponAffixStat.Area, WeaponAffixStat.ProjectileSpeed);
            AssertExactStats(WeaponId.GakgungShot, WeaponAffixStat.Damage, WeaponAffixStat.Cooldown, WeaponAffixStat.Area, WeaponAffixStat.ProjectileSpeed);
            AssertExactStats(WeaponId.TalismanThrow, WeaponAffixStat.Damage, WeaponAffixStat.Cooldown, WeaponAffixStat.Area, WeaponAffixStat.ProjectileSpeed);
            AssertExactStats(WeaponId.ThunderCrashBomb, WeaponAffixStat.Damage, WeaponAffixStat.Cooldown, WeaponAffixStat.Area, WeaponAffixStat.Duration);
            AssertExactStats(WeaponId.JangseungWard, WeaponAffixStat.Damage, WeaponAffixStat.Cooldown, WeaponAffixStat.Area, WeaponAffixStat.Duration);
            AssertExactStats(WeaponId.SingijeonVolley, WeaponAffixStat.Damage, WeaponAffixStat.Cooldown, WeaponAffixStat.Area, WeaponAffixStat.ProjectileSpeed);
            AssertExactStats(WeaponId.FrostFlask, WeaponAffixStat.Damage, WeaponAffixStat.Cooldown, WeaponAffixStat.Area, WeaponAffixStat.Duration);
            AssertExactStats(WeaponId.WindThunderFan, WeaponAffixStat.Damage, WeaponAffixStat.Cooldown, WeaponAffixStat.Area, WeaponAffixStat.Duration);
            foreach (var weapon in WeaponRoster.All) Assert.That(WeaponAffixCatalog.CompatiblePotentials(weapon), Has.Count.EqualTo(3));

            AssertExactRange(WeaponId.HwandoFlyingBlade, WeaponAffixStat.Damage, 10d, 30d);
            AssertExactRange(WeaponId.HwandoFlyingBlade, WeaponAffixStat.Cooldown, -5d, -12d);
            AssertExactRange(WeaponId.HwandoFlyingBlade, WeaponAffixStat.Area, 8d, 20d);
            AssertExactRange(WeaponId.GakgungShot, WeaponAffixStat.ProjectileSpeed, 10d, 30d);
            AssertExactRange(WeaponId.JangseungWard, WeaponAffixStat.Duration, 10d, 25d);

            AssertJackpotThreshold(0.049999d, true);
            AssertJackpotThreshold(0.05d, false);
            AssertJackpotThreshold(0.02d, false, WeaponPotentialId.HwandoVenomFang);
            AssertJackpotThreshold(0.019999d, true, WeaponPotentialId.HwandoVenomFang);
            AssertJackpotThreshold(0.005d, false, WeaponPotentialId.HwandoVenomFang, WeaponPotentialId.HwandoReturningAfterimage);
            AssertJackpotThreshold(0.004999d, true, WeaponPotentialId.HwandoVenomFang, WeaponPotentialId.HwandoReturningAfterimage);
            AssertContinuationThreshold(.079999d, .009999d, 3);
            AssertContinuationThreshold(.08d, .009999d, 1);
            AssertContinuationThreshold(.079999d, .01d, 2);
            AssertThreeLineCapConsumesNoPotentialRandomness();

            var presentation = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            Assert.That(presentation, Is.Not.Null);
            Assert.That(presentation.Validate(potentials), Is.Empty);
            foreach (var potential in potentials)
            {
                var texture = presentation.MaskForPotential(potential);
                Assert.That(texture, Is.Not.Null, potential.Value);
                var mask = PixelHitMask.FromTexture(texture, Vector2.zero, 1f);
                Assert.That(Enumerable.Range(0, mask.Width * mask.Height).Any(i => mask.IsActive(i % mask.Width, i / mask.Width)), Is.True, potential.Value);
            }
        }

        private static void AssertExactRange(WeaponId weapon, WeaponAffixStat expectedStat, double expectedMin, double expectedMax)
        {
            var statIndex = WeaponAffixCatalog.CompatibleStats(weapon).ToList().IndexOf(expectedStat);
            Assert.That(statIndex, Is.GreaterThanOrEqualTo(0));
            var min = WeaponAffixRoller.RollAndApply(new WeaponRunAffixState(), weapon, new FixedAffixRandom(statIndex, 0d)).General;
            var max = WeaponAffixRoller.RollAndApply(new WeaponRunAffixState(), weapon, new FixedAffixRandom(statIndex, .999999d)).General;
            Assert.That(min.Value, Is.EqualTo(expectedMin).Within(.0001d));
            Assert.That(max.Value, Is.EqualTo(expectedMax).Within(.001d));
        }

        private static void AssertExactStats(WeaponId weapon, params WeaponAffixStat[] expected) =>
            Assert.That(WeaponAffixCatalog.CompatibleStats(weapon), Is.EqualTo(expected), weapon.Value);

        private static void AssertContinuationThreshold(double secondLineRoll, double thirdLineRoll, int expectedLines)
        {
            var result = WeaponAffixRoller.RollAndApply(new WeaponRunAffixState(), WeaponId.HwandoFlyingBlade,
                new FixedAffixRandom(0, .5d, 0d, secondLineRoll, thirdLineRoll));
            Assert.That(result.NewPotentials, Has.Count.EqualTo(expectedLines));
        }

        private static void AssertThreeLineCapConsumesNoPotentialRandomness()
        {
            var state = new WeaponRunAffixState();
            var fill = WeaponAffixRoller.RollAndApply(state, WeaponId.HwandoFlyingBlade, new FixedAffixRandom(0, .5d, 0d, 0d, 0d));
            Assert.That(fill.NewPotentials, Has.Count.EqualTo(3));
            var random = new CountingAffixRandom();
            var capped = WeaponAffixRoller.RollAndApply(state, WeaponId.HwandoFlyingBlade, random);
            Assert.That(capped.NewPotentials, Is.Empty);
            Assert.That(random.IndexCalls, Is.EqualTo(1));
            Assert.That(random.UnitCalls, Is.EqualTo(1));
        }

        private static void AssertJackpotThreshold(double initialRoll, bool expected, params WeaponPotentialId[] existing)
        {
            var state = new WeaponRunAffixState();
            if (existing.Length > 0)
            {
                var seedUnits = existing.Length == 1
                    ? new[] { .5d, 0d, .99d }
                    : new[] { .5d, 0d, 0d, 0d, .99d };
                WeaponAffixRoller.RollAndApply(state, WeaponId.HwandoFlyingBlade, new FixedAffixRandom(0, seedUnits));
            }
            var result = WeaponAffixRoller.RollAndApply(state, WeaponId.HwandoFlyingBlade, new FixedAffixRandom(0, .5d, initialRoll));
            Assert.That(result.NewPotentials.Count > 0, Is.EqualTo(expected));
        }

        private sealed class FixedAffixRandom : IAffixRandom
        {
            private readonly int statIndex;
            private readonly double[] units;
            private int unitIndex;
            public FixedAffixRandom(int statIndex, params double[] units) { this.statIndex = statIndex; this.units = units; }
            public int NextIndex(int exclusiveMax) => Mathf.Clamp(statIndex, 0, exclusiveMax - 1);
            public double NextUnit() => units[Mathf.Min(unitIndex++, units.Length - 1)];
        }

        private sealed class CountingAffixRandom : IAffixRandom
        {
            public int IndexCalls { get; private set; }
            public int UnitCalls { get; private set; }
            public int NextIndex(int exclusiveMax) { IndexCalls++; return 0; }
            public double NextUnit() { UnitCalls++; return .5d; }
        }

        private static UpgradeState State(
            IReadOnlyDictionary<string, int> weapons = null,
            IReadOnlyDictionary<string, int> supports = null,
            ISet<string> unlocked = null,
            ISet<string> acquired = null) =>
            new(
                weapons ?? new Dictionary<string, int>(),
                supports ?? new Dictionary<string, int>(),
                unlocked ?? new HashSet<string>(),
                acquired ?? new HashSet<string>());

        private static IReadOnlyDictionary<string, int> MaxedLaunchWeapons() =>
            new Dictionary<string, int>
            {
                ["hwando_flying_blade"] = 5, ["gakgung_shot"] = 5, ["talisman_throw"] = 5,
                ["thunder_crash_bomb"] = 5, ["jangseung_ward"] = 5, ["singijeon_volley"] = 5,
                ["frost_flask"] = 5, ["wind_thunder_fan"] = 5
            };
    }
}
