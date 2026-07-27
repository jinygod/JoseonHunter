using System.Linq;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Combat;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponContentTests
    {
        [Test]
        public void CatalogAcceptsEightDistinctFiveLevelLaunchDefinitions()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponCatalogAsset>();
            catalog.SetDefinitionsForTests(TestWeaponFactory.CreateLaunchDefinitions());

            Assert.That(catalog.ValidateLaunchRoster(), Is.Empty);
            Assert.That(WeaponRoster.All.All(id => catalog.TryGet(id, out _)), Is.True);
        }

        [Test]
        public void CatalogRejectsMissingDuplicateOrMechanicallyIdenticalLaunchDefinitions()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponCatalogAsset>();
            var definitions = TestWeaponFactory.CreateLaunchDefinitions();

            catalog.SetDefinitionsForTests(definitions.Take(7).ToArray());
            Assert.That(catalog.ValidateLaunchRoster(), Does.Contain("launch catalog must contain exactly eight weapons"));

            definitions[7] = definitions[0];
            catalog.SetDefinitionsForTests(definitions);
            Assert.That(catalog.ValidateLaunchRoster(), Does.Contain("launch catalog contains duplicate weapon ID 'hwando_flying_blade'"));
        }

        [Test]
        public void CatalogRejectsMechanicallyIdenticalDefinitions()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponCatalogAsset>();
            var definitions = TestWeaponFactory.CreateLaunchDefinitions();
            definitions[7] = TestWeaponFactory.CreateDefinition(
                WeaponId.WindThunderFan,
                WeaponTargeting.Nearest,
                WeaponGeometry.ReturningPath,
                ContactPhase.Outbound,
                RepeatHitPolicy.OncePerPhase);
            catalog.SetDefinitionsForTests(definitions);

            Assert.That(catalog.ValidateLaunchRoster(), Does.Contain("launch catalog contains mechanically identical definitions"));
        }

        [Test]
        public void DefinitionRejectsLevelsThatDoNotBelongToItsWeapon()
        {
            var definition = TestWeaponFactory.CreateDefinition(
                WeaponId.HwandoFlyingBlade,
                WeaponTargeting.Nearest,
                WeaponGeometry.ReturningPath,
                ContactPhase.Outbound,
                RepeatHitPolicy.OncePerPhase);
            var levels = TestWeaponFactory.CreateLevels(WeaponId.GakgungShot);
            definition.SetLevelsForTests(levels);

            Assert.That(definition.Validate(), Does.Contain("level 1 weapon ID must match definition ID 'hwando_flying_blade'"));
        }
    }

    internal static class TestWeaponFactory
    {
        public static WeaponDefinitionAsset[] CreateLaunchDefinitions() => new[]
        {
            CreateDefinition(WeaponId.HwandoFlyingBlade, WeaponTargeting.Nearest, WeaponGeometry.ReturningPath, ContactPhase.Outbound, RepeatHitPolicy.OncePerPhase),
            CreateDefinition(WeaponId.GakgungShot, WeaponTargeting.HighestThreat, WeaponGeometry.NarrowLine, ContactPhase.Direct, RepeatHitPolicy.OncePerInstance),
            CreateDefinition(WeaponId.TalismanThrow, WeaponTargeting.NearestUnmarked, WeaponGeometry.SequentialHop, ContactPhase.Attach, RepeatHitPolicy.OncePerPhase),
            CreateDefinition(WeaponId.ThunderCrashBomb, WeaponTargeting.DensestCenter, WeaponGeometry.ExpandingCircle, ContactPhase.Blast, RepeatHitPolicy.OncePerInstance),
            CreateDefinition(WeaponId.JangseungWard, WeaponTargeting.PlayerBoundary, WeaponGeometry.Boundary, ContactPhase.BoundaryCrossing, RepeatHitPolicy.BoundaryReentry),
            CreateDefinition(WeaponId.SingijeonVolley, WeaponTargeting.DensestDirection, WeaponGeometry.MultiLane, ContactPhase.Direct, RepeatHitPolicy.OncePerInstance),
            CreateDefinition(WeaponId.FrostFlask, WeaponTargeting.PredictedCrowd, WeaponGeometry.PersistentCircle, ContactPhase.Tick, RepeatHitPolicy.TimedTicks),
            CreateDefinition(WeaponId.WindThunderFan, WeaponTargeting.DangerousSector, WeaponGeometry.ConeThenLinks, ContactPhase.Wind, RepeatHitPolicy.OncePerPhase)
        };

        public static WeaponDefinitionAsset CreateDefinition(
            WeaponId id,
            WeaponTargeting targeting,
            WeaponGeometry geometry,
            ContactPhase contactPhase,
            RepeatHitPolicy repeatHitPolicy)
        {
            var definition = ScriptableObject.CreateInstance<WeaponDefinitionAsset>();
            definition.SetForTests(id, targeting, geometry, contactPhase, DamageElement.Physical, repeatHitPolicy, CreateLevels(id));
            return definition;
        }

        public static WeaponLevelData[] CreateLevels(WeaponId id) => Enumerable.Range(1, 5)
            .Select(level => new WeaponLevelData(id.Value, level, 1f, 1f, 1f, 1, 1f, 1f, 0, 0, 0f, 0f, 0f))
            .ToArray();
    }
}
