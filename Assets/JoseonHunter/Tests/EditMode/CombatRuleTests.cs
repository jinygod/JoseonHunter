using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using NUnit.Framework;

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
