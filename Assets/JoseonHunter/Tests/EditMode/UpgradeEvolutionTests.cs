using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class UpgradeEvolutionTests
    {
        [Test]
        public void Max_level_unlocked_weapon_offers_its_evolution()
        {
            var state = new UpgradeState(
                new Dictionary<string, int> { [WeaponId.FrostFlask.Value] = 5 },
                new Dictionary<string, int>(),
                new HashSet<string> { "frost_bloom_evolution" },
                new HashSet<string>());

            var offers = UpgradeSelector.Select(state, 27);

            Assert.That(offers, Has.Some.Matches<UpgradeOffer>(
                offer => offer.Kind == UpgradeKind.Evolution && offer.Id == "frost_bloom_evolution"));
        }

        [Test]
        public void Level_above_max_does_not_offer_its_evolution()
        {
            var state = new UpgradeState(
                new Dictionary<string, int> { [WeaponId.FrostFlask.Value] = 6 },
                new Dictionary<string, int>(),
                new HashSet<string> { "frost_bloom_evolution" },
                new HashSet<string>());

            var offers = UpgradeSelector.Select(state, 27);

            Assert.That(offers, Has.None.Matches<UpgradeOffer>(
                offer => offer.Kind == UpgradeKind.Evolution && offer.Id == "frost_bloom_evolution"));
        }

        [Test]
        public void Eligible_evolution_is_the_first_offer_without_breaking_weapon_guarantees()
        {
            var state = new UpgradeState(
                new Dictionary<string, int>
                {
                    [WeaponId.FrostFlask.Value] = 5,
                    [WeaponId.HwandoFlyingBlade.Value] = 2
                },
                new Dictionary<string, int>(),
                new HashSet<string> { "frost_bloom_evolution" },
                new HashSet<string>());

            var offers = UpgradeSelector.Select(state, 27);

            Assert.That(offers, Has.Count.EqualTo(3));
            Assert.That(offers.Select(offer => offer.Id), Is.Unique);
            Assert.That(offers[0], Is.EqualTo(new UpgradeOffer("frost_bloom_evolution", UpgradeKind.Evolution, 1)));
            Assert.That(offers, Does.Contain(new UpgradeOffer(WeaponId.HwandoFlyingBlade.Value, UpgradeKind.Weapon, 3)));
            Assert.That(offers.Any(offer => offer.Kind == UpgradeKind.Weapon && offer.NextLevel == 1), Is.True);
        }
    }
}
