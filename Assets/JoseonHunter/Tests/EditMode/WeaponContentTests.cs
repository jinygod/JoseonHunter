using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponContentTests
    {
        private static readonly IReadOnlyDictionary<WeaponId, string> DefinitionPaths =
            new Dictionary<WeaponId, string>
            {
                { WeaponId.HwandoFlyingBlade, "Assets/JoseonHunter/Content/Weapons/HwandoFlyingBlade.asset" },
                { WeaponId.GakgungShot, "Assets/JoseonHunter/Content/Weapons/GakgungShot.asset" },
                { WeaponId.TalismanThrow, "Assets/JoseonHunter/Content/Weapons/TalismanThrow.asset" },
                { WeaponId.ThunderCrashBomb, "Assets/JoseonHunter/Content/Weapons/ThunderCrashBomb.asset" },
                { WeaponId.JangseungWard, "Assets/JoseonHunter/Content/Weapons/JangseungWard.asset" },
                { WeaponId.SingijeonVolley, "Assets/JoseonHunter/Content/Weapons/SingijeonVolley.asset" },
                { WeaponId.FrostFlask, "Assets/JoseonHunter/Content/Weapons/FrostFlask.asset" },
                { WeaponId.WindThunderFan, "Assets/JoseonHunter/Content/Weapons/WindThunderFan.asset" }
            };

        private static readonly IReadOnlyDictionary<WeaponId, string[]> ExpectedPresentationPaths =
            new Dictionary<WeaponId, string[]>
            {
                {
                    WeaponId.HwandoFlyingBlade,
                    Paths("Hwando", "hwando_blade", 4)
                        .Concat(Paths("Hwando", "hwando_afterimage", 4))
                        .Concat(Paths("Hwando", "hwando_contact_spark", 4))
                        .ToArray()
                },
                {
                    WeaponId.GakgungShot,
                    Paths("Gakgung", "gakgung_aim_glint", 3)
                        .Concat(Paths("Gakgung", "gakgung_arrow", 3))
                        .Concat(Paths("Gakgung", "gakgung_impact_splinter", 5))
                        .ToArray()
                },
                {
                    WeaponId.TalismanThrow,
                    Paths("Talisman", "talisman_rotate", 4)
                        .Concat(Paths("Talisman", "talisman_seal_pulse", 5))
                        .Concat(Paths("Talisman", "talisman_binding", 5))
                        .ToArray()
                },
                {
                    WeaponId.ThunderCrashBomb,
                    Paths("Thunder", "thunder_lob", 6)
                        .Concat(Paths("Thunder", "thunder_warning", 4))
                        .Concat(Paths("Thunder", "thunder_blast", 6))
                        .Concat(Paths("Thunder", "thunder_ground_current", 5))
                        .ToArray()
                },
                {
                    WeaponId.JangseungWard,
                    Paths("Jangseung", "jangseung_rise", 5)
                        .Concat(Paths("Jangseung", "jangseung_ward", 4))
                        .Concat(Paths("Jangseung", "jangseung_strike", 5))
                        .ToArray()
                },
                {
                    WeaponId.SingijeonVolley,
                    Paths("Singijeon", "singijeon_rocket", 4)
                        .Concat(Paths("Singijeon", "singijeon_ember", 5))
                        .Concat(Paths("Singijeon", "singijeon_explosion", 6))
                        .ToArray()
                },
                {
                    WeaponId.FrostFlask,
                    Paths("Frost", "frost_flask", 6)
                        .Concat(Paths("Frost", "frost_growth", 5))
                        .Concat(Paths("Frost", "frost_shatter", 6))
                        .ToArray()
                },
                {
                    WeaponId.WindThunderFan,
                    Paths("Fan", "fan_gust", 5)
                        .Concat(Paths("Fan", "fan_target", 4))
                        .Concat(Paths("Fan", "fan_lightning", 6))
                        .ToArray()
                }
            };

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

        [TestCaseSource(nameof(AllWeaponIds))]
        public void PolishPresentationFrames_MatchDeclaredPartCountAndExactOrder(WeaponId id)
        {
            var definition = LoadDefinition(id);

            Assert.That(definition.PresentationSprites.Count, Is.EqualTo(WeaponVisualPartIndex.RequiredCount(id)));
            Assert.That(definition.PresentationSprites.All(sprite => sprite != null), Is.True);
            CollectionAssert.AreEqual(
                ExpectedPresentationPaths[id],
                definition.PresentationSprites.Select(AssetDatabase.GetAssetPath));
        }

        [Test]
        public void GakgungUiUsesDedicatedSimplifiedIconInsteadOfCombatAimFrame()
        {
            var definition = LoadDefinition(WeaponId.GakgungShot);
            var uiIcon = new SerializedObject(definition).FindProperty("uiIcon");

            Assert.That(uiIcon, Is.Not.Null);
            Assert.That(uiIcon.objectReferenceValue, Is.Not.Null);
            Assert.That(uiIcon.objectReferenceValue, Is.Not.SameAs(definition.PresentationSprites[0]));
            Assert.That(AssetDatabase.GetAssetPath(uiIcon.objectReferenceValue),
                Does.EndWith("gakgung_shot/ui-icon.png"));
        }

        [Test]
        public void ThunderCrashBombUsesApprovedAreaDamageCurve()
        {
            var definition = LoadDefinition(WeaponId.ThunderCrashBomb);

            CollectionAssert.AreEqual(
                new[] { 12f, 15f, 18f, 21f, 24f },
                definition.Levels.Select(level => level.BaseDamage).ToArray());
        }

        [Test]
        public void ResolveWeaponPresentationSprite_ValidCanonicalIndexReturnsExactFrame()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogAsset>(
                "Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset");
            var owner = new GameObject("weapon-presentation-resolver-test");
            try
            {
                var controller = owner.AddComponent<FirstPlayableController>();
                SetPrivateField(controller, "weaponCatalog", catalog);
                var expected = LoadDefinition(WeaponId.FrostFlask).PresentationSprites[WeaponVisualPartIndex.FrostFlask.Impact + 4];

                var actual = ResolvePresentationSprite(
                    controller,
                    WeaponId.FrostFlask,
                    WeaponVisualPartIndex.FrostFlask.Impact + 4);

                Assert.That(actual, Is.SameAs(expected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ResolveWeaponPresentationSprite_InvalidIndexWarnsAndReturnsRepresentative()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogAsset>(
                "Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset");
            var owner = new GameObject("weapon-presentation-resolver-test");
            try
            {
                var controller = owner.AddComponent<FirstPlayableController>();
                SetPrivateField(controller, "weaponCatalog", catalog);
                var definition = LoadDefinition(WeaponId.HwandoFlyingBlade);
                const int invalidIndex = 99;
                LogAssert.Expect(
                    LogType.Warning,
                    new System.Text.RegularExpressions.Regex("hwando_flying_blade.*99"));

                var actual = ResolvePresentationSprite(controller, WeaponId.HwandoFlyingBlade, invalidIndex);

                Assert.That(actual, Is.SameAs(definition.PresentationSprites[0]));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void RebuildWeaponExecutors_HwandoCueLevelUsesCatalogLevelNotProjectileCount()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogAsset>(
                "Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset");
            var owner = new GameObject("weapon-level-forwarding-test");
            try
            {
                var controller = owner.AddComponent<FirstPlayableController>();
                var registry = new CombatTargetRegistry();
                var damage = new CombatDamageService(registry);
                var mask = new PixelHitMask(1, 1, Vector2.zero, 1f, new[] { 1u });
                SetPrivateField(controller, "weaponCatalog", catalog);
                SetPrivateField(controller, "combatTargets", registry);
                SetPrivateField(controller, "combatDamageService", damage);
                SetPrivateField(controller, "weaponRuntime", new WeaponRuntimeController(registry, damage, mask));

                controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 3);

                var executor = (FlyingBladeExecutor)typeof(WeaponRuntimeController)
                    .GetMethod("ExecutorForTests", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(controller.WeaponRuntime, new object[] { WeaponId.HwandoFlyingBlade });
                Assert.That(executor.Level, Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void RebuildWeaponExecutors_FrostUsesAuthoredSlowFraction()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalogAsset>(
                "Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset");
            var owner = new GameObject("frost-slow-forwarding-test");
            try
            {
                var controller = owner.AddComponent<FirstPlayableController>();
                var registry = new CombatTargetRegistry();
                var damage = new CombatDamageService(registry);
                var mask = new PixelHitMask(1, 1, Vector2.zero, 1f, new[] { 1u });
                var runtime = new WeaponRuntimeController(registry, damage, mask);
                SetPrivateField(controller, "weaponCatalog", catalog);
                SetPrivateField(controller, "combatTargets", registry);
                SetPrivateField(controller, "combatDamageService", damage);
                SetPrivateField(controller, "weaponRuntime", runtime);

                controller.SetWeaponLevelForTests(WeaponId.FrostFlask, 1);

                var executor = (FrostFlaskExecutor)typeof(WeaponRuntimeController)
                    .GetMethod("ExecutorForTests", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(controller.WeaponRuntime, new object[] { WeaponId.FrostFlask });
                var slowProperty = typeof(FrostFlaskExecutor).GetProperty("SlowFraction");
                Assert.That(slowProperty, Is.Not.Null);
                Assert.That((float)slowProperty.GetValue(executor), Is.EqualTo(.35f).Within(.001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        private static IEnumerable<WeaponId> AllWeaponIds() => WeaponRoster.All;

        private static WeaponDefinitionAsset LoadDefinition(WeaponId id) =>
            AssetDatabase.LoadAssetAtPath<WeaponDefinitionAsset>(DefinitionPaths[id]);

        private static string[] Paths(string family, string stem, int count) =>
            Enumerable.Range(1, count)
                .Select(index =>
                    $"Assets/JoseonHunter/Art/Weapons/Runtime/Polish/{family}/{stem}{(index == 1 && !stem.EndsWith("_01", StringComparison.Ordinal) && (family == "Hwando" || family == "Gakgung") ? string.Empty : $"_{index:00}")}.png")
                .ToArray();

        private static void SetPrivateField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);

        private static Sprite ResolvePresentationSprite(FirstPlayableController controller, WeaponId id, int partIndex) =>
            (Sprite)typeof(FirstPlayableController)
                .GetMethod("ResolveWeaponPresentationSprite", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, new object[] { id, partIndex });
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
