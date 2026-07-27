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
            var state = State(weapons: new Dictionary<string, int> { ["hwando"] = 1 });

            var offers = UpgradeSelector.Select(state, 17);

            Assert.That(offers, Has.Count.EqualTo(3));
            Assert.That(offers.Select(offer => offer.Id), Is.Unique);
            Assert.That(offers, Does.Contain(new UpgradeOffer("hwando", UpgradeKind.Weapon, 2)));
        }

        [Test]
        public void MaxedAndLockedEvolutionsNeverAppear()
        {
            var state = State(
                weapons: new Dictionary<string, int> { ["hwando"] = 5 },
                supports: new Dictionary<string, int> { ["talisman"] = 5 });

            var offers = UpgradeSelector.Select(state, 17);

            Assert.That(offers.Any(offer => offer.Id == "hwando"), Is.False);
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

        private static UpgradeState State(
            IReadOnlyDictionary<string, int> weapons = null,
            IReadOnlyDictionary<string, int> supports = null,
            ISet<string> unlocked = null) =>
            new(
                weapons ?? new Dictionary<string, int>(),
                supports ?? new Dictionary<string, int>(),
                unlocked ?? new HashSet<string>());
    }
}
