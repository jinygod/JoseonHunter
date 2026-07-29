using JoseonHunter.Content.Weapons;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using NUnit.Framework;
using UnityEditor;

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

        [Test]
        public void EveryEvolutionWeaponHasFiveLevelsAndCompletePresentationFrames()
        {
            foreach (var evolution in WeaponEvolutionCatalog.All)
            {
                var definition = FindDefinition(evolution.RequiredWeaponId);
                Assert.That(definition, Is.Not.Null, evolution.Id);
                Assert.That(definition.Levels.Count, Is.EqualTo(5), evolution.Id);
                Assert.That(
                    definition.PresentationSprites.Count,
                    Is.EqualTo(WeaponVisualPartIndex.RequiredCount(evolution.RequiredWeaponId)),
                    evolution.Id);
                Assert.That(definition.PresentationSprites.All(sprite => sprite != null), Is.True, evolution.Id);
            }
        }

        private static WeaponDefinitionAsset FindDefinition(WeaponId id)
        {
            var guids = AssetDatabase.FindAssets("t:WeaponDefinitionAsset", new[] { "Assets/JoseonHunter/Content/Weapons" });
            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<WeaponDefinitionAsset>)
                .SingleOrDefault(definition => definition != null && definition.Id.Equals(id));
        }
    }
}
