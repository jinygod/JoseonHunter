using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class GameplayVisualPrefabPlayModeTests
    {
        private FirstPlayableController controller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;

            controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);
            controller.ConfigureSeparationLoadScenarioForTests();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            controller?.Flow?.ResetToPlaying();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerAndEnemyUseBoundPrefabViewsWithoutDuplicateVisualChildren()
        {
            var player = controller.transform.Find("RuntimeObjects/Han Yeonhwa");
            Assert.That(player, Is.Not.Null);
            AssertCombatantView(player, isPlayer: true);
            Assert.That(player.GetComponentsInChildren<WorldBarView>(true), Has.Length.EqualTo(1),
                "The player must receive exactly one runtime health-bar prefab.");

            var existingViews = new HashSet<CombatantVisualView>(Object
                .FindObjectsByType<CombatantVisualView>(FindObjectsInactive.Include));

            controller.SpawnEnemyForTests(new Vector2(5f, 0f));
            yield return null;

            var enemyView = Object
                .FindObjectsByType<CombatantVisualView>(FindObjectsInactive.Include)
                .Single(view => !existingViews.Contains(view));
            AssertCombatantView(enemyView.transform, isPlayer: false);
            Assert.That(enemyView.GetComponentsInChildren<WorldBarView>(true), Is.Empty,
                "A normal enemy must not receive a duplicate or unnecessary health bar.");
        }

        [UnityTest]
        public IEnumerator PlayerHealthRatioPreservesPrefabAuthoredHeightAndOffset()
        {
            var player = controller.transform.Find("RuntimeObjects/Han Yeonhwa");
            Assert.That(player, Is.Not.Null);
            var bar = player.GetComponentInChildren<WorldBarView>(true);
            Assert.That(bar, Is.Not.Null);

            var fill = RequireNamedDescendant(bar.transform, "Fill");
            var authoredScale = fill.localScale;
            var authoredPosition = fill.localPosition;
            Assert.That(authoredScale.x, Is.GreaterThan(0f));

            var damageMultiplier = controller.StartingIncomingDamageMultiplierForTests;
            Assert.That(damageMultiplier, Is.GreaterThan(0f));
            controller.DamagePlayerForTests(controller.UiState.MaximumHealth * .5f / damageMultiplier);
            yield return null;

            var ratio = controller.UiState.Health / controller.UiState.MaximumHealth;
            Assert.That(ratio, Is.EqualTo(.5f).Within(.01f));
            Assert.That(fill.localScale.x, Is.EqualTo(authoredScale.x * ratio).Within(.001f));
            Assert.That(fill.localScale.y, Is.EqualTo(authoredScale.y).Within(.001f));
            Assert.That(fill.localScale.z, Is.EqualTo(authoredScale.z).Within(.001f));
            Assert.That(fill.localPosition.x,
                Is.EqualTo(authoredPosition.x + authoredScale.x * (ratio - 1f) * .5f).Within(.001f));
            Assert.That(fill.localPosition.y, Is.EqualTo(authoredPosition.y).Within(.001f));
            Assert.That(fill.localPosition.z, Is.EqualTo(authoredPosition.z).Within(.001f));
        }

        [UnityTest]
        public IEnumerator WorldBarKeepsPrefabAuthoredBackgroundAndFillSprites()
        {
            var player = controller.transform.Find("RuntimeObjects/Han Yeonhwa");
            Assert.That(player, Is.Not.Null);
            var runtimeBar = player.GetComponentInChildren<WorldBarView>(true);
            var library = Resources.Load<GameplayVisualPrefabLibrary>(
                "Gameplay/GameplayVisualPrefabLibrary");
            Assert.That(library, Is.Not.Null);
            var authoredBar = library.WorldHealthBar.GetComponent<WorldBarView>();

            Assert.That(runtimeBar.BackgroundRenderer.sprite, Is.SameAs(authoredBar.BackgroundRenderer.sprite));
            Assert.That(runtimeBar.FillRenderer.sprite, Is.SameAs(authoredBar.FillRenderer.sprite));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExperiencePrefabKeepsRootTrailAndReusesTheSamePooledInstance()
        {
            controller.SpawnExperiencePickupForTests(Vector2.zero, 1);
            var first = GameObject.Find("Experience Flame");
            Assert.That(first, Is.Not.Null);
            Assert.That(first.GetComponent<PickupVisualView>(), Is.Not.Null,
                "The active pickup root must retain its prefab view marker.");
            Assert.That(first.GetComponent<TrailRenderer>(), Is.Not.Null,
                "Existing attraction code and tests require the XP trail on the pickup root.");
            Assert.That(first.GetComponentsInChildren<SpriteRenderer>(true), Has.Length.EqualTo(1));
            Assert.That(RequireNamedDescendant(first.transform, "Visual").GetComponent<SpriteRenderer>(), Is.Not.Null);
            Assert.That(controller.TickGameplayIfRunningForTests(.02f), Is.True);
            Assert.That(first.activeSelf, Is.False, "Collection must return the prefab instance to the existing pool.");

            var respawnPosition = new Vector2(3f, 2f);
            controller.SpawnExperiencePickupForTests(respawnPosition, 1);
            var reused = GameObject.Find("Experience Flame");
            Assert.That(reused, Is.Not.Null);
            Assert.That(reused, Is.SameAs(first),
                "Spawning another XP pickup must reactivate the pooled prefab instead of instantiating another one.");
            Assert.That((Vector2)reused.transform.position, Is.EqualTo(respawnPosition));
            Assert.That(reused.GetComponent<TrailRenderer>().emitting, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuthoredPlayerAndNestedHealthBarAreReusedWithoutInstantiation()
        {
            var library = Resources.Load<GameplayVisualPrefabLibrary>(
                "Gameplay/GameplayVisualPrefabLibrary");
            Assert.That(library, Is.Not.Null);
            var authoredPlayer = Object.Instantiate(library.PlayerVisual);
            var playerView = authoredPlayer.GetComponent<CombatantVisualView>();
            Assert.That(playerView, Is.Not.Null);
            var authoredBar = Object.Instantiate(library.WorldHealthBar, playerView.HealthBarAnchor, false);
            var authoredPlayerId = authoredPlayer.GetEntityId();
            var authoredBarId = authoredBar.GetEntityId();
            var factory = new GameplayVisualFactory(
                library,
                null,
                playerView.BodyRenderer.sprite,
                (_, _) => { });

            var boundPlayer = factory.BindAuthoredCombatant(
                authoredPlayer,
                "Han Yeonhwa",
                playerView.BodyRenderer.sprite,
                10,
                MotionWeight.Light,
                0f,
                out var visualRig,
                CombatantVisualRole.Player);
            var fill = factory.CreateHealthBar(
                boundPlayer.transform,
                new Vector3(0f, -.30f, 0f),
                .58f);
            yield return null;

            Assert.That(boundPlayer.GetEntityId(), Is.EqualTo(authoredPlayerId));
            Assert.That(visualRig, Is.Not.Null);
            Assert.That(playerView.HealthBarAnchor.GetComponentsInChildren<WorldBarView>(true), Has.Length.EqualTo(1));
            Assert.That(playerView.HealthBarAnchor.GetComponentInChildren<WorldBarView>(true).gameObject.GetEntityId(),
                Is.EqualTo(authoredBarId));
            Assert.That(fill, Is.SameAs(authoredBar.GetComponent<WorldBarView>().Fill));

            Object.Destroy(authoredPlayer);
        }

        [UnityTest]
        public IEnumerator FactoryStillCreatesEnemyAndReusesExperiencePickupTrailContract()
        {
            var library = Resources.Load<GameplayVisualPrefabLibrary>(
                "Gameplay/GameplayVisualPrefabLibrary");
            Assert.That(library, Is.Not.Null);
            var root = new GameObject("Factory Visual Root").transform;
            var enemySprite = library.EnemyVisual.GetComponent<CombatantVisualView>().BodyRenderer.sprite;
            var factory = new GameplayVisualFactory(library, null, enemySprite, (_, _) => { });

            var enemy = factory.CreateCombatant(
                "Factory Enemy",
                enemySprite,
                new Vector2(4f, 2f),
                8,
                root,
                MotionWeight.Medium,
                .25f,
                out var visualRig,
                CombatantVisualRole.Enemy);
            var pickup = factory.CreatePickup(
                GameplayPickupVisualKind.Experience,
                "Factory Experience",
                library.ExperiencePickup.GetComponent<PickupVisualView>().VisualRenderer.sprite,
                new Vector2(3f, 1f),
                root,
                out var pickupView);
            yield return null;

            Assert.That(enemy.GetComponent<CombatantVisualView>(), Is.Not.Null);
            Assert.That(visualRig, Is.Not.Null);
            Assert.That((Vector2)enemy.transform.position, Is.EqualTo(new Vector2(4f, 2f)));
            Assert.That(pickup.GetComponent<PickupVisualView>(), Is.SameAs(pickupView));
            Assert.That(pickup.GetComponent<TrailRenderer>(), Is.SameAs(pickupView.TrailRenderer));

            Object.Destroy(root.gameObject);
        }

        [UnityTest]
        public IEnumerator MissingSerializedLibraryUsesResourcesFallbackAndStillInitializesPrefabViews()
        {
            var libraryField = typeof(FirstPlayableController).GetField(
                "gameplayVisualPrefabs",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(libraryField, Is.Not.Null,
                "The controller must expose one serialized GameplayVisualPrefabLibrary dependency.");

            libraryField.SetValue(controller, null);
            controller.ResetRunForTests();
            yield return null;

            var player = controller.transform.Find("RuntimeObjects/Han Yeonhwa");
            Assert.That(player, Is.Not.Null);
            AssertCombatantView(player, isPlayer: true);

            controller.SpawnExperiencePickupForTests(new Vector2(2f, 0f), 1);
            var pickup = GameObject.Find("Experience Flame");
            Assert.That(pickup, Is.Not.Null);
            Assert.That(pickup.GetComponent<PickupVisualView>(), Is.Not.Null,
                "Resources fallback must initialize pickups through the same production prefab path.");
        }

        [UnityTest]
        public IEnumerator InvalidIndividualPlayerPrefabWarnsAndUsesTheLegacyFallback()
        {
            var production = Resources.Load<GameplayVisualPrefabLibrary>(
                "Gameplay/GameplayVisualPrefabLibrary");
            Assert.That(production, Is.Not.Null);
            var invalidPlayer = new GameObject("Invalid Player Visual");
            var incompleteLibrary = ScriptableObject.CreateInstance<GameplayVisualPrefabLibrary>();
            incompleteLibrary.Configure(
                invalidPlayer,
                production.EnemyVisual,
                production.WorldHealthBar,
                production.WorldShieldBar,
                production.ExperiencePickup,
                production.YeopjeonPickup,
                production.MagnetPickup);
            var libraryField = typeof(FirstPlayableController).GetField(
                "gameplayVisualPrefabs",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(libraryField, Is.Not.Null);
            libraryField.SetValue(controller, incompleteLibrary);

            LogAssert.Expect(
                LogType.Warning,
                "Gameplay visual prefab 'Invalid Player Visual' has invalid CombatantVisualView bindings for role 'Player'. Using the legacy visual fallback.");
            controller.ResetRunForTests();
            yield return null;

            var player = controller.transform.Find("RuntimeObjects/Han Yeonhwa");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<CombatantVisualView>(), Is.Null);
            AssertSingleRendererChild(player, "Soft Shadow");
            AssertSingleRendererChild(player, "Silhouette Outline");
            AssertSingleRendererChild(player, "Visual Pivot");
            Assert.That(player.GetComponentsInChildren<WorldBarView>(true), Has.Length.EqualTo(1));

            Object.Destroy(invalidPlayer);
            Object.Destroy(incompleteLibrary);
        }

        [UnityTest]
        public IEnumerator InvalidWorldBarPrefabWarnsAndUsesTheLegacyFallback()
        {
            var production = Resources.Load<GameplayVisualPrefabLibrary>(
                "Gameplay/GameplayVisualPrefabLibrary");
            Assert.That(production, Is.Not.Null);
            var invalidBar = new GameObject("Invalid Health Bar");
            invalidBar.AddComponent<WorldBarView>();
            var incompleteLibrary = ScriptableObject.CreateInstance<GameplayVisualPrefabLibrary>();
            incompleteLibrary.Configure(
                production.PlayerVisual,
                production.EnemyVisual,
                invalidBar,
                production.WorldShieldBar,
                production.ExperiencePickup,
                production.YeopjeonPickup,
                production.MagnetPickup);
            SetVisualLibrary(incompleteLibrary);

            LogAssert.Expect(
                LogType.Warning,
                "Gameplay visual prefab 'Invalid Health Bar' has invalid WorldBarView bindings. Using the legacy visual fallback.");
            controller.ResetRunForTests();
            yield return null;

            var player = controller.transform.Find("RuntimeObjects/Han Yeonhwa");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponentsInChildren<WorldBarView>(true), Is.Empty);
            var healthBar = RequireNamedDescendant(player, "Health Bar");
            Assert.That(healthBar.GetComponentsInChildren<SpriteRenderer>(true), Has.Length.EqualTo(2));

            Object.Destroy(invalidBar);
            Object.Destroy(incompleteLibrary);
        }

        [UnityTest]
        public IEnumerator InvalidPickupHierarchyWarnsAndUsesTheLegacyFallback()
        {
            var production = Resources.Load<GameplayVisualPrefabLibrary>(
                "Gameplay/GameplayVisualPrefabLibrary");
            Assert.That(production, Is.Not.Null);
            var invalidPickup = new GameObject("Invalid Experience Pickup");
            var wrongRenderer = invalidPickup.AddComponent<SpriteRenderer>();
            var trail = invalidPickup.AddComponent<TrailRenderer>();
            var view = invalidPickup.AddComponent<PickupVisualView>();
            view.Configure(wrongRenderer, trail, .72f);
            var incompleteLibrary = ScriptableObject.CreateInstance<GameplayVisualPrefabLibrary>();
            incompleteLibrary.Configure(
                production.PlayerVisual,
                production.EnemyVisual,
                production.WorldHealthBar,
                production.WorldShieldBar,
                invalidPickup,
                production.YeopjeonPickup,
                production.MagnetPickup);
            SetVisualLibrary(incompleteLibrary);

            LogAssert.Expect(
                LogType.Warning,
                "Gameplay visual prefab 'Invalid Experience Pickup' has invalid PickupVisualView bindings. Using the legacy visual fallback.");
            controller.SpawnExperiencePickupForTests(new Vector2(3f, 0f), 1);
            yield return null;

            var pickup = GameObject.Find("Experience Flame");
            Assert.That(pickup, Is.Not.Null);
            Assert.That(pickup.GetComponent<PickupVisualView>(), Is.Null);
            Assert.That(pickup.GetComponent<SpriteRenderer>(), Is.Not.Null);
            Assert.That(pickup.GetComponent<TrailRenderer>(), Is.Not.Null);

            Object.Destroy(invalidPickup);
            Object.Destroy(incompleteLibrary);
        }

        [UnityTest]
        public IEnumerator EliteMidBossBossAndShieldEnemyReuseTheBoundEnemyPrefabShell()
        {
            var known = CurrentCombatantViews();

            controller.ConfigureViewportSpawnForTests(0, .5f, .8f, true);
            controller.SpawnEnemyAtCurrentViewportForTests();
            yield return null;
            var elite = RequireNewCombatantView(known);
            AssertBoundEnemyShell(elite);
            Assert.That(elite.GetComponentsInChildren<WorldBarView>(true), Has.Length.EqualTo(1));
            var authoredEnemyShadowScale = Resources.Load<GameplayVisualPrefabLibrary>(
                "Gameplay/GameplayVisualPrefabLibrary").EnemyVisual
                .GetComponent<CombatantVisualView>().ShadowRenderer.transform.localScale;
            Assert.That(elite.ShadowRenderer.transform.localScale, Is.EqualTo(authoredEnemyShadowScale));
            known.Add(elite);
            controller.ClearViewportSpawnForTests();

            controller.SpawnEnemyForViewportClearanceTests(false, 1);
            yield return null;
            var midBoss = RequireNewCombatantView(known);
            AssertBoundEnemyShell(midBoss);
            Assert.That(midBoss.GetComponentsInChildren<WorldBarView>(true), Has.Length.EqualTo(1));
            AssertBossShadowScale(midBoss, authoredEnemyShadowScale);
            known.Add(midBoss);

            controller.SpawnEnemyForViewportClearanceTests(true, 0);
            yield return null;
            var boss = RequireNewCombatantView(known);
            AssertBoundEnemyShell(boss);
            AssertBossShadowScale(boss, authoredEnemyShadowScale);
            known.Add(boss);

            controller.SpawnSpecialEnemyForTests("shield_dokkaebi", new Vector2(4f, 0f));
            yield return null;
            var shieldEnemy = RequireNewCombatantView(known);
            AssertBoundEnemyShell(shieldEnemy);
            Assert.That(shieldEnemy.GetComponentsInChildren<WorldBarView>(true), Has.Length.EqualTo(2));
            Assert.That(CountNamedDescendants(shieldEnemy.transform, "Health Bar"), Is.EqualTo(1));
            Assert.That(CountNamedDescendants(shieldEnemy.transform, "Shield Guard Bar"), Is.EqualTo(1));
        }

        private static void AssertCombatantView(Transform root, bool isPlayer)
        {
            Assert.That(root.GetComponents<CombatantVisualView>(), Has.Length.EqualTo(1),
                $"{root.name} must have exactly one prefab view marker on its logical root.");

            AssertSingleRendererChild(root, "Soft Shadow");
            AssertSingleRendererChild(root, "Silhouette Outline");
            AssertSingleRendererChild(root, "Visual Pivot");
            Assert.That(RequireNamedDescendant(root, "Visual Pivot").GetComponent<SpriteRenderer>().sprite, Is.Not.Null,
                "Runtime sprite injection must bind to the prefab-authored body renderer.");

            if (isPlayer)
            {
                AssertSingleRendererChild(root, "Player Aura");
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true), Has.Length.EqualTo(6),
                    "The player should have body, shadow, outline, aura and one two-renderer health bar only.");
                Assert.That(CountNamedDescendants(root, "Health Bar"), Is.EqualTo(1));
            }
            else
            {
                Assert.That(CountNamedDescendants(root, "Player Aura"), Is.Zero);
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true), Has.Length.EqualTo(3),
                    "A normal enemy should have one body, one shadow and one outline renderer only.");
                Assert.That(CountNamedDescendants(root, "Health Bar"), Is.Zero);
            }
        }

        private static void AssertBoundEnemyShell(CombatantVisualView view)
        {
            Assert.That(view, Is.Not.Null);
            var root = view.transform;
            Assert.That(root.GetComponents<CombatantVisualView>(), Has.Length.EqualTo(1));
            AssertSingleRendererChild(root, "Soft Shadow");
            AssertSingleRendererChild(root, "Silhouette Outline");
            AssertSingleRendererChild(root, "Visual Pivot");
            Assert.That(CountNamedDescendants(root, "Player Aura"), Is.Zero);
            Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true).Length,
                Is.EqualTo(3 + view.GetComponentsInChildren<WorldBarView>(true).Length * 2));
        }

        private static HashSet<CombatantVisualView> CurrentCombatantViews() =>
            new HashSet<CombatantVisualView>(Object.FindObjectsByType<CombatantVisualView>(FindObjectsInactive.Include));

        private static CombatantVisualView RequireNewCombatantView(HashSet<CombatantVisualView> known)
        {
            var result = Object.FindObjectsByType<CombatantVisualView>(FindObjectsInactive.Include)
                .SingleOrDefault(candidate => !known.Contains(candidate));
            Assert.That(result, Is.Not.Null, "A newly spawned combatant must use the enemy visual prefab.");
            return result;
        }

        private void SetVisualLibrary(GameplayVisualPrefabLibrary library)
        {
            var libraryField = typeof(FirstPlayableController).GetField(
                "gameplayVisualPrefabs",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(libraryField, Is.Not.Null);
            libraryField.SetValue(controller, library);
        }

        private static void AssertBossShadowScale(CombatantVisualView view, Vector3 authoredScale)
        {
            var actual = view.ShadowRenderer.transform.localScale;
            Assert.That(actual.x, Is.EqualTo(authoredScale.x * (.90f / .72f)).Within(.001f));
            Assert.That(actual.y, Is.EqualTo(authoredScale.y * (.18f / .14f)).Within(.001f));
            Assert.That(actual.z, Is.EqualTo(authoredScale.z).Within(.001f));
        }

        private static void AssertSingleRendererChild(Transform root, string childName)
        {
            Assert.That(CountNamedDescendants(root, childName), Is.EqualTo(1),
                $"{root.name} must contain exactly one '{childName}' child.");
            Assert.That(RequireNamedDescendant(root, childName).GetComponents<SpriteRenderer>(), Has.Length.EqualTo(1));
        }

        private static int CountNamedDescendants(Transform root, string childName)
        {
            return root.GetComponentsInChildren<Transform>(true).Count(child => child.name == childName);
        }

        private static Transform RequireNamedDescendant(Transform root, string childName)
        {
            var child = root.GetComponentsInChildren<Transform>(true).SingleOrDefault(candidate => candidate.name == childName);
            Assert.That(child, Is.Not.Null, $"{root.name} must contain '{childName}'.");
            return child;
        }
    }
}
