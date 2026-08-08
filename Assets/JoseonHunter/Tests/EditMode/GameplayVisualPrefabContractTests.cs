using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Editor.Scenes;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GameplayVisualPrefabContractTests
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";
        private const string PreviewScenePath = "Assets/JoseonHunter/Scenes/GameplayVisualPreview.unity";
        private const string LibraryPath = "Assets/JoseonHunter/Resources/Gameplay/GameplayVisualPrefabLibrary.asset";
        private const string PreviewPrefabPath = "Assets/JoseonHunter/Prefabs/Gameplay/GameplayAuthoringPreview.prefab";

        private const string PlayerPrefabPath = "Assets/JoseonHunter/Prefabs/Gameplay/PlayerVisual.prefab";
        private const string EnemyPrefabPath = "Assets/JoseonHunter/Prefabs/Gameplay/EnemyVisual.prefab";
        private const string HealthBarPrefabPath = "Assets/JoseonHunter/Prefabs/Gameplay/WorldHealthBar.prefab";
        private const string ShieldBarPrefabPath = "Assets/JoseonHunter/Prefabs/Gameplay/WorldShieldBar.prefab";
        private const string ExperiencePickupPrefabPath = "Assets/JoseonHunter/Prefabs/Gameplay/ExperiencePickup.prefab";
        private const string YeopjeonPickupPrefabPath = "Assets/JoseonHunter/Prefabs/Gameplay/YeopjeonPickup.prefab";
        private const string MagnetPickupPrefabPath = "Assets/JoseonHunter/Prefabs/Gameplay/MagnetPickup.prefab";

        private static readonly PrefabReference[] ExpectedLibraryEntries =
        {
            new("playerVisual", PlayerPrefabPath),
            new("enemyVisual", EnemyPrefabPath),
            new("worldHealthBar", HealthBarPrefabPath),
            new("worldShieldBar", ShieldBarPrefabPath),
            new("experiencePickup", ExperiencePickupPrefabPath),
            new("yeopjeonPickup", YeopjeonPickupPrefabPath),
            new("magnetPickup", MagnetPickupPrefabPath)
        };

        [Test]
        public void ResourcesLibraryReferencesEveryAuthoredGameplayVisualPrefab()
        {
            var library = LoadRequiredAsset<ScriptableObject>(LibraryPath);

            Assert.That(library.GetType().Name, Is.EqualTo("GameplayVisualPrefabLibrary"));
            Assert.That(
                Resources.Load<ScriptableObject>("Gameplay/GameplayVisualPrefabLibrary"),
                Is.SameAs(library),
                "Directly-created controllers must be able to resolve the shared library through Resources.");

            var serializedLibrary = new SerializedObject(library);
            foreach (var expected in ExpectedLibraryEntries)
            {
                var property = serializedLibrary.FindProperty(expected.PropertyName);
                Assert.That(
                    property,
                    Is.Not.Null,
                    $"GameplayVisualPrefabLibrary is missing serialized field '{expected.PropertyName}'.");
                Assert.That(
                    property.objectReferenceValue,
                    Is.SameAs(LoadRequiredPrefab(expected.AssetPath)),
                    $"GameplayVisualPrefabLibrary.{expected.PropertyName} must reference {expected.AssetPath}.");
            }
        }

        [TestCase(PlayerPrefabPath, true, false)]
        [TestCase(EnemyPrefabPath, false, true)]
        public void CombatantPrefabsContainTheBindableAuthoredHierarchy(
            string prefabPath,
            bool requiresPlayerAura,
            bool requiresShieldAnchor)
        {
            var prefab = LoadRequiredPrefab(prefabPath);

            var view = RequireSingleRootComponent(prefab, "CombatantVisualView");
            var shadow = RequireSpriteRenderer(RequireDirectChild(prefab, "Soft Shadow"), prefabPath);
            var outline = RequireSpriteRenderer(RequireDirectChild(prefab, "Silhouette Outline"), prefabPath);
            var pivot = RequireDirectChild(prefab, "Visual Pivot");
            var body = RequireSpriteRenderer(pivot, prefabPath);
            var healthAnchor = RequireDirectChild(prefab, "HealthBarAnchor");

            AssertSerializedReference(view, "visualPivot", pivot);
            AssertSerializedReference(view, "bodyRenderer", body);
            AssertSerializedReference(view, "shadowRenderer", shadow);
            AssertSerializedReference(view, "outlineRenderer", outline);
            AssertSerializedReference(view, "healthBarAnchor", healthAnchor);

            var aura = prefab.transform.Find("Player Aura");
            if (requiresPlayerAura)
            {
                var auraRenderer = RequireSpriteRenderer(RequireDirectChild(prefab, "Player Aura"), prefabPath);
                AssertSerializedReference(view, "auraRenderer", auraRenderer);
            }
            else
            {
                Assert.That(aura, Is.Null, $"{prefabPath} must not contain the player-only aura.");
                AssertSerializedReference(view, "auraRenderer", null);
            }

            var shieldAnchor = prefab.transform.Find("ShieldBarAnchor");
            if (requiresShieldAnchor)
            {
                Assert.That(shieldAnchor, Is.Not.Null, $"{prefabPath} requires a ShieldBarAnchor.");
                AssertSerializedReference(view, "shieldBarAnchor", shieldAnchor);
            }
            else
            {
                Assert.That(shieldAnchor, Is.Null, $"{prefabPath} must not contain an unused ShieldBarAnchor.");
                AssertSerializedReference(view, "shieldBarAnchor", null);
            }

            Assert.That(
                prefab.GetComponent<SpriteRenderer>(),
                Is.Null,
                $"{prefabPath} must keep its visual renderer under Visual Pivot so motion remains bindable.");
        }

        [TestCase(HealthBarPrefabPath)]
        [TestCase(ShieldBarPrefabPath)]
        public void WorldBarPrefabsOwnTheirEditableBackgroundAndFillGeometry(string prefabPath)
        {
            var prefab = LoadRequiredPrefab(prefabPath);

            var view = RequireSingleRootComponent(prefab, "WorldBarView");
            var background = RequireSpriteRenderer(RequireDirectChild(prefab, "Background"), prefabPath);
            var fill = RequireSpriteRenderer(RequireDirectChild(prefab, "Fill"), prefabPath);

            AssertSerializedReference(view, "backgroundRenderer", background);
            AssertSerializedReference(view, "fillRenderer", fill);

            Assert.That(prefab.GetComponent<SpriteRenderer>(), Is.Null, prefabPath);
            Assert.That(background.transform.localScale.x, Is.GreaterThan(fill.transform.localScale.x), prefabPath);
            Assert.That(background.transform.localScale.y, Is.GreaterThan(fill.transform.localScale.y), prefabPath);
            Assert.That(fill.transform.localScale.x, Is.GreaterThan(0f), prefabPath);
            Assert.That(fill.transform.localScale.y, Is.GreaterThan(0f), prefabPath);
        }

        [TestCase(ExperiencePickupPrefabPath, true)]
        [TestCase(YeopjeonPickupPrefabPath, false)]
        [TestCase(MagnetPickupPrefabPath, false)]
        public void PickupPrefabsOwnAVisualChildAndOnlyExperienceKeepsTheRootTrail(
            string prefabPath,
            bool requiresTrail)
        {
            var prefab = LoadRequiredPrefab(prefabPath);

            var view = RequireSingleRootComponent(prefab, "PickupVisualView");
            var visual = RequireSpriteRenderer(RequireDirectChild(prefab, "Visual"), prefabPath);
            AssertSerializedReference(view, "visualRenderer", visual);
            Assert.That(prefab.GetComponent<SpriteRenderer>(), Is.Null, prefabPath);
            Assert.That(prefab.transform.localScale.x, Is.GreaterThan(0f), prefabPath);
            Assert.That(prefab.transform.localScale.y, Is.GreaterThan(0f), prefabPath);

            var trails = prefab.GetComponentsInChildren<TrailRenderer>(true);
            if (requiresTrail)
            {
                Assert.That(trails, Has.Length.EqualTo(1), prefabPath);
                Assert.That(trails[0].gameObject, Is.SameAs(prefab), "Experience trail must remain on the pickup root.");
                AssertSerializedReference(view, "trailRenderer", trails[0]);
            }
            else
            {
                Assert.That(trails, Is.Empty, $"{prefabPath} must not carry the experience-only trail.");
                AssertSerializedReference(view, "trailRenderer", null);
            }
        }

        [Test]
        public void GameplaySceneSerializesTheSharedVisualPrefabLibraryOnItsController()
        {
            var expectedLibrary = LoadRequiredAsset<ScriptableObject>(LibraryPath);
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var controller = FindSingleComponentInScene<FirstPlayableController>(scene);
                var property = new SerializedObject(controller).FindProperty("gameplayVisualPrefabs");

                Assert.That(property, Is.Not.Null, "FirstPlayableController must expose the prefab library as a serialized dependency.");
                Assert.That(property.objectReferenceValue, Is.SameAs(expectedLibrary));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void PreviewPrefabUsesNestedProductionVisualPrefabsWithoutLiveGameplayFlow()
        {
            var preview = LoadRequiredPrefab(PreviewPrefabPath);

            Assert.That(preview.GetComponentsInChildren<FirstPlayableController>(true), Is.Empty);
            Assert.That(preview.GetComponentsInChildren<MetaGameSession>(true), Is.Empty);

            foreach (var expected in ExpectedLibraryEntries)
            {
                var productionPrefab = LoadRequiredPrefab(expected.AssetPath);
                Assert.That(
                    CountNestedInstancesOf(preview, productionPrefab),
                    Is.GreaterThanOrEqualTo(1),
                    $"Preview must use a nested instance of {expected.AssetPath}, not a disconnected copy.");
            }

            Assert.That(
                CountNestedInstancesOf(preview, LoadRequiredPrefab(EnemyPrefabPath)),
                Is.GreaterThanOrEqualTo(2),
                "Preview requires separate normal-enemy and enlarged elite/boss examples.");
        }

        [Test]
        public void PreviewWorldBarsAreConnectedProductionPrefabsUnderCombatantAnchors()
        {
            var preview = LoadRequiredPrefab(PreviewPrefabPath);
            var healthBar = LoadRequiredPrefab(HealthBarPrefabPath);
            var shieldBar = LoadRequiredPrefab(ShieldBarPrefabPath);

            AssertConnectedBarUnderAnchor(preview, "Player Preview", false, healthBar);
            AssertConnectedBarUnderAnchor(preview, "Normal Enemy Preview", false, healthBar);
            AssertConnectedBarUnderAnchor(preview, "Elite Boss Scale Preview", false, healthBar);
            AssertConnectedBarUnderAnchor(preview, "Elite Boss Scale Preview", true, shieldBar);
        }

        [Test]
        public void PreviewUsesTheSamePlayerEnemyAndFinalBossScaleMultipliersAsGameplay()
        {
            var preview = LoadRequiredPrefab(PreviewPrefabPath);
            var playerPrefab = LoadRequiredPrefab(PlayerPrefabPath);
            var enemyPrefab = LoadRequiredPrefab(EnemyPrefabPath);
            var profile = CombatVisualScaleProfile.MobilePortrait;

            var player = RequireNamedCombatantView(preview, "Player Preview").transform;
            var enemy = RequireNamedCombatantView(preview, "Normal Enemy Preview").transform;
            var boss = RequireNamedCombatantView(preview, "Elite Boss Scale Preview").transform;
            AssertVector3Approximately(
                player.localScale,
                Vector3.Scale(playerPrefab.transform.localScale, Vector3.one * profile.PlayerScale));
            AssertVector3Approximately(
                enemy.localScale,
                Vector3.Scale(enemyPrefab.transform.localScale, Vector3.one * profile.NormalEnemyScale));
            AssertVector3Approximately(
                boss.localScale,
                Vector3.Scale(
                    enemyPrefab.transform.localScale,
                    Vector3.one * profile.NormalEnemyScale *
                    BossScaleProfile.MultiplierFor(BossCombatRole.FinalBoss)));
        }

        [Test]
        public void PreviewSceneInstantiatesThePreviewPrefabAndIsExcludedFromBuildSettings()
        {
            Assert.That(
                EditorBuildSettings.scenes.Select(scene => scene.path),
                Does.Not.Contain(PreviewScenePath),
                "The visual authoring preview must never become a shipping scene.");

            var previewPrefab = LoadRequiredPrefab(PreviewPrefabPath);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(PreviewScenePath), Is.Not.Null, PreviewScenePath);

            var scene = EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.That(
                    roots.SelectMany(root => root.GetComponentsInChildren<Component>(true))
                        .Any(component => component == null),
                    Is.False,
                    $"{PreviewScenePath} contains a Missing Script component.");
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<FirstPlayableController>(true)), Is.Empty);
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<MetaGameSession>(true)), Is.Empty);
                Assert.That(
                    roots.Count(root =>
                        PrefabUtility.GetCorrespondingObjectFromOriginalSource(root) == previewPrefab),
                    Is.EqualTo(1),
                    "Preview scene must contain one connected instance of GameplayAuthoringPreview.prefab.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void BuilderRefusesToModifyADirtyLoadedPreviewScene()
        {
            var scene = EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Additive);
            try
            {
                var unsaved = new GameObject("Unsaved Preview Authoring Change");
                SceneManager.MoveGameObjectToScene(unsaved, scene);
                EditorSceneManager.MarkSceneDirty(scene);

                Assert.That(
                    () => GameplayVisualPrefabBuilder.CreateOrValidateVisualPrefabs(),
                    Throws.TypeOf<InvalidOperationException>());
                Assert.That(scene.isDirty, Is.True);
                Assert.That(
                    scene.GetRootGameObjects().Any(root => root.name == unsaved.name),
                    Is.True,
                    "The builder must leave unsaved preview authoring objects untouched.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void RevalidatingProductionPrefabsDoesNotRewriteDesignerOwnedAssets()
        {
            var paths = ExpectedLibraryEntries.Select(entry => entry.AssetPath).Distinct().ToArray();
            var before = new Dictionary<string, byte[]>();
            foreach (var path in paths)
                before.Add(path, File.ReadAllBytes(AbsoluteAssetPath(path)));

            GameplayVisualPrefabBuilder.CreateOrValidateProductionPrefabs();

            foreach (var path in paths)
            {
                CollectionAssert.AreEqual(
                    before[path],
                    File.ReadAllBytes(AbsoluteAssetPath(path)),
                    $"Revalidation must not rewrite designer-owned Prefab {path}.");
            }
        }

        private static T LoadRequiredAsset<T>(string assetPath) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            Assert.That(asset, Is.Not.Null, $"Required gameplay authoring asset is missing: {assetPath}");
            return asset;
        }

        private static GameObject LoadRequiredPrefab(string assetPath)
        {
            var prefab = LoadRequiredAsset<GameObject>(assetPath);
            Assert.That(PrefabUtility.IsPartOfPrefabAsset(prefab), Is.True, $"{assetPath} must be a saved prefab asset.");
            Assert.That(
                prefab.GetComponentsInChildren<Component>(true).Any(component => component == null),
                Is.False,
                $"{assetPath} contains a Missing Script component.");
            return prefab;
        }

        private static string AbsoluteAssetPath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            Assert.That(projectRoot, Is.Not.Null, "Unity project root could not be resolved from Application.dataPath.");
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static Component RequireSingleRootComponent(GameObject root, string componentTypeName)
        {
            var matches = root.GetComponents<Component>()
                .Where(component => component != null && component.GetType().Name == componentTypeName)
                .ToArray();
            Assert.That(
                matches,
                Has.Length.EqualTo(1),
                $"{AssetDatabase.GetAssetPath(root)} requires exactly one root {componentTypeName}.");
            return matches[0];
        }

        private static void AssertSerializedReference(
            Component owner,
            string propertyName,
            Object expected)
        {
            var property = new SerializedObject(owner).FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"{owner.GetType().Name} is missing '{propertyName}'.");
            Assert.That(
                property.objectReferenceValue,
                Is.SameAs(expected),
                $"{owner.GetType().Name}.{propertyName} must reference the authored hierarchy object.");
        }

        private static Transform RequireDirectChild(GameObject root, string childName)
        {
            var child = root.transform.Find(childName);
            Assert.That(
                child,
                Is.Not.Null,
                $"{AssetDatabase.GetAssetPath(root)} is missing direct child '{childName}'.");
            Assert.That(child.parent, Is.SameAs(root.transform));
            return child;
        }

        private static SpriteRenderer RequireSpriteRenderer(Transform child, string ownerPath)
        {
            var renderer = child.GetComponent<SpriteRenderer>();
            Assert.That(renderer, Is.Not.Null, $"{ownerPath}/{child.name} requires a SpriteRenderer.");
            return renderer;
        }

        private static T FindSingleComponentInScene<T>(Scene scene) where T : Component
        {
            var matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
            Assert.That(matches, Has.Length.EqualTo(1), $"{scene.path} requires exactly one {typeof(T).Name}.");
            return matches[0];
        }

        private static int CountNestedInstancesOf(GameObject previewPrefab, GameObject productionPrefab)
        {
            var count = 0;
            foreach (var candidate in previewPrefab.GetComponentsInChildren<Transform>(true))
            {
                var originalSource = PrefabUtility.GetCorrespondingObjectFromOriginalSource(candidate.gameObject);
                if (originalSource == productionPrefab)
                    count++;
            }

            return count;
        }

        private static void AssertConnectedBarUnderAnchor(
            GameObject preview,
            string combatantName,
            bool shield,
            GameObject expectedBarPrefab)
        {
            var view = preview.GetComponentsInChildren<CombatantVisualView>(true)
                .Single(candidate => candidate.gameObject.name == combatantName);
            var anchor = shield ? view.ShieldBarAnchor : view.HealthBarAnchor;
            Assert.That(anchor, Is.Not.Null, $"{combatantName} is missing its {(shield ? "shield" : "health")} bar anchor.");

            var connectedBars = anchor.GetComponentsInChildren<WorldBarView>(true)
                .Where(bar => PrefabUtility.GetCorrespondingObjectFromOriginalSource(bar.gameObject) == expectedBarPrefab)
                .ToArray();
            Assert.That(
                connectedBars,
                Has.Length.EqualTo(1),
                $"{combatantName}/{anchor.name} must contain one connected {expectedBarPrefab.name} instance.");

            var nestedRoot = connectedBars[0].transform;
            Assert.That(
                nestedRoot.localPosition,
                Is.EqualTo(expectedBarPrefab.transform.localPosition),
                $"{combatantName}/{anchor.name} must preserve the world-bar Prefab root position in Preview.");
            Assert.That(
                nestedRoot.localRotation,
                Is.EqualTo(expectedBarPrefab.transform.localRotation),
                $"{combatantName}/{anchor.name} must preserve the world-bar Prefab root rotation in Preview.");
            Assert.That(
                nestedRoot.localScale,
                Is.EqualTo(expectedBarPrefab.transform.localScale),
                $"{combatantName}/{anchor.name} must preserve the world-bar Prefab root scale in Preview.");
        }

        private static CombatantVisualView RequireNamedCombatantView(GameObject preview, string name)
        {
            var matches = preview.GetComponentsInChildren<CombatantVisualView>(true)
                .Where(view => view.gameObject.name == name)
                .ToArray();
            Assert.That(matches, Has.Length.EqualTo(1), $"Preview requires one '{name}' CombatantVisualView.");
            return matches[0];
        }

        private static void AssertVector3Approximately(Vector3 actual, Vector3 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(.0001f));
        }

        private readonly struct PrefabReference
        {
            public PrefabReference(string propertyName, string assetPath)
            {
                PropertyName = propertyName;
                AssetPath = assetPath;
            }

            public string PropertyName { get; }
            public string AssetPath { get; }
        }
    }
}
