using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponEvolutionCatalogTests
    {
        [Test]
        public void Catalog_contains_one_evolution_for_every_weapon()
        {
            Assert.That(WeaponEvolutionCatalog.All.Count, Is.EqualTo(WeaponRoster.All.Count));
            CollectionAssert.AreEquivalent(
                WeaponRoster.All.Select(id => id.Value),
                WeaponEvolutionCatalog.All.Select(value => value.RequiredWeaponId.Value));
        }

        [Test]
        public void TryGet_returns_definition_for_known_id_and_false_for_unknown_id()
        {
            Assert.That(WeaponEvolutionCatalog.TryGet("frost_bloom_evolution", out var definition), Is.True);
            Assert.That(definition.RequiredWeaponId, Is.EqualTo(WeaponId.FrostFlask));
            Assert.That(WeaponEvolutionCatalog.TryGet("missing", out _), Is.False);
        }
    }
}
