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
        public void Full_loadout_marks_new_weapon_for_replacement_and_never_offers_discarded_weapon()
        {
            var state = new UpgradeState(
                new Dictionary<string, int>
                {
                    [WeaponId.HwandoFlyingBlade.Value] = 2,
                    [WeaponId.GakgungShot.Value] = 2,
                    [WeaponId.TalismanThrow.Value] = 2,
                    [WeaponId.ThunderCrashBomb.Value] = 2
                },
                new Dictionary<string, int>(),
                new HashSet<string>(WeaponRoster.All.Select(id => id.Value)),
                new HashSet<string> { "frost_bloom_evolution" },
                new HashSet<string> { WeaponId.FrostFlask.Value });

            var offers = UpgradeSelector.Select(state, 27, playerLevel: 4);

            Assert.That(offers, Has.Count.EqualTo(3));
            Assert.That(offers.Any(offer => offer.Kind == UpgradeKind.Evolution), Is.False);
            Assert.That(offers.Any(offer => offer.Id == WeaponId.FrostFlask.Value), Is.False);
            Assert.That(offers.Where(offer => offer.Kind == UpgradeKind.Weapon && offer.NextLevel == 1), Is.Not.Empty);
            Assert.That(offers.Where(offer => offer.Kind == UpgradeKind.Weapon && offer.NextLevel == 1)
                .All(offer => offer.RequiresReplacement), Is.True);
        }

        [Test]
        public void New_weapon_does_not_require_replacement_while_a_weapon_slot_is_empty()
        {
            var state = new UpgradeState(
                new Dictionary<string, int>
                {
                    [WeaponId.HwandoFlyingBlade.Value] = 2,
                    [WeaponId.GakgungShot.Value] = 2,
                    [WeaponId.TalismanThrow.Value] = 2
                },
                new Dictionary<string, int>(),
                new HashSet<string>(WeaponRoster.All.Select(id => id.Value)),
                new HashSet<string>(),
                new HashSet<string>());

            var offers = UpgradeSelector.Select(state, 27, playerLevel: 4);

            Assert.That(offers.Where(offer => offer.Kind == UpgradeKind.Weapon && offer.NextLevel == 1), Is.Not.Empty);
            Assert.That(offers.Where(offer => offer.Kind == UpgradeKind.Weapon && offer.NextLevel == 1)
                .All(offer => !offer.RequiresReplacement), Is.True);
        }

        [Test]
        public void Owning_all_three_supports_never_creates_a_fourth_support_slot_offer()
        {
            var state = new UpgradeState(
                new Dictionary<string, int> { [WeaponId.HwandoFlyingBlade.Value] = 2 },
                new Dictionary<string, int>
                {
                    ["talisman"] = 5,
                    ["boots"] = 5,
                    ["warding_bell"] = 5
                },
                new HashSet<string>(WeaponRoster.All.Select(id => id.Value)),
                new HashSet<string>(),
                new HashSet<string>());

            var offers = UpgradeSelector.Select(state, 31);

            Assert.That(offers.Any(offer => offer.Kind == UpgradeKind.Support), Is.False);
        }
    }
}
