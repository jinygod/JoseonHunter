using System.Collections;
using System.Linq;
using System.Reflection;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class GameplayHybridSceneOwnershipPlayModeTests
    {
        [UnityTest]
        public IEnumerator ResetRunPreservesAuthoredCameraFieldRuntimeRootsPlayerAndUiIdentity()
        {
            yield return LoadGameplay();

            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var composition = controller.GetComponent<GameplaySceneComposition>();
            Assert.That(composition, Is.Not.Null);
            Assert.That(composition.IsComplete, Is.True);

            var camera = composition.GameplayCamera;
            var field = composition.BattlefieldRoot;
            var runtimeObjects = composition.RuntimeObjectsRoot;
            var runtimeSystems = composition.RuntimeSystemsRoot;
            var player = composition.AuthoredPlayer;
            var ui = composition.UiRoot;
            var healthBar = player.GetComponentInChildren<WorldBarView>(true);
            var cameraPosition = camera.transform.position;
            var cameraRotation = camera.transform.rotation;
            var playerPosition = player.transform.localPosition;
            var playerRotation = player.transform.localRotation;
            var playerScale = player.transform.localScale;
            var canvasIds = ui.GetComponentsInChildren<Canvas>(true)
                .Select(canvas => canvas.GetEntityId())
                .OrderBy(id => id)
                .ToArray();
            Assert.That(ui.GetComponents<FirstPlayableUiBootstrap>(), Has.Length.EqualTo(1));

            camera.transform.position = new Vector3(17f, -9f, -10f);
            player.transform.localPosition = new Vector3(5f, 4f, 0f);
            controller.ResetRunForTests();
            controller.ResetRunForTests();
            yield return null;
            yield return null;

            Assert.That(composition.GameplayCamera.GetEntityId(), Is.EqualTo(camera.GetEntityId()));
            Assert.That(composition.BattlefieldRoot.GetEntityId(), Is.EqualTo(field.GetEntityId()));
            Assert.That(composition.RuntimeObjectsRoot.GetEntityId(), Is.EqualTo(runtimeObjects.GetEntityId()));
            Assert.That(composition.RuntimeSystemsRoot.GetEntityId(), Is.EqualTo(runtimeSystems.GetEntityId()));
            Assert.That(composition.AuthoredPlayer.GetEntityId(), Is.EqualTo(player.GetEntityId()));
            Assert.That(composition.UiRoot.GetEntityId(), Is.EqualTo(ui.GetEntityId()));
            Assert.That(camera.transform.position, Is.EqualTo(cameraPosition));
            Assert.That(camera.transform.rotation, Is.EqualTo(cameraRotation));
            Assert.That(player.transform.localPosition, Is.EqualTo(playerPosition));
            Assert.That(player.transform.localRotation, Is.EqualTo(playerRotation));
            Assert.That(player.transform.localScale, Is.EqualTo(playerScale));
            Assert.That(player.GetComponentInChildren<WorldBarView>(true).GetEntityId(), Is.EqualTo(healthBar.GetEntityId()));
            Assert.That(Object.FindObjectsByType<FirstPlayableController>(), Has.Length.EqualTo(1));
            var resetCanvasIds = composition.UiRoot.GetComponentsInChildren<Canvas>(true)
                .Select(canvas => canvas.GetEntityId())
                .OrderBy(id => id)
                .ToArray();
            Assert.That(resetCanvasIds, Has.Length.EqualTo(canvasIds.Length));
            CollectionAssert.AreEqual(canvasIds, resetCanvasIds);
            Assert.That(composition.UiRoot.GetComponents<FirstPlayableUiBootstrap>(), Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RuntimeEnemiesAndPickupsRemainTransientWhileAuthoredPlayerAndPickupPoolContractsSurvive()
        {
            yield return LoadGameplay();

            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var composition = controller.GetComponent<GameplaySceneComposition>();
            var runtimeObjects = composition.RuntimeObjectsRoot;
            var player = composition.AuthoredPlayer;
            var playerId = player.GetEntityId();

            var enemy = controller.SpawnEnemyForTests(new Vector2(4f, 0f));
            controller.SpawnExperiencePickupForTests(new Vector2(3f, 0f), 1);
            var enemyObject = runtimeObjects.GetComponentsInChildren<CombatantVisualView>(true)
                .Single(view => view != player).gameObject;
            var pickup = GameObject.Find("Experience Flame");
            Assert.That(enemy, Is.Not.Null);
            Assert.That(pickup, Is.Not.Null);
            Assert.That(enemyObject.transform.IsChildOf(runtimeObjects), Is.True);
            Assert.That(pickup.transform.IsChildOf(runtimeObjects), Is.True);
            Assert.That(player.transform.IsChildOf(runtimeObjects), Is.True);
            Assert.That(enemyObject.transform, Is.Not.EqualTo(player.transform));
            Assert.That(pickup.GetComponent<TrailRenderer>(), Is.Not.Null);

            controller.ResetRunForTests();
            yield return null;
            yield return null;

            Assert.That(enemyObject == null, Is.True);
            Assert.That(pickup == null, Is.True);
            Assert.That(composition.AuthoredPlayer.GetEntityId(), Is.EqualTo(playerId));
            Assert.That(runtimeObjects.childCount, Is.EqualTo(1));
            Assert.That(runtimeObjects.GetChild(0), Is.EqualTo(player.transform));
            Assert.That(player.GetComponentInChildren<WorldBarView>(true), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator MissingCompositionUsesLegacyRuntimeFallbackWithoutBreakingDirectControllerCreation()
        {
            SceneManager.LoadScene("Bootstrap");
            yield return null;

            var root = new GameObject("Standalone FirstPlayable");
            var controller = root.AddComponent<FirstPlayableController>();
            yield return null;

            Assert.That(controller, Is.Not.Null);
            Assert.That(root.transform.Find("RuntimeObjects/Han Yeonhwa"), Is.Not.Null);
            Assert.That(root.transform.Find("FlatField"), Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator ResetRunKeepsAuthoredBattlefieldPreviewInactiveAndUsesOneGeneratedPresentation()
        {
            yield return LoadGameplay();

            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var composition = controller.GetComponent<GameplaySceneComposition>();
            var host = composition.BattlefieldRoot.GetComponent<GameplayBattlefieldHost>();
            var preview = composition.BattlefieldRoot.Find("Authoring Preview");
            Assert.That(host, Is.Not.Null);
            Assert.That(preview, Is.Not.Null);

            Assert.That(preview.gameObject.activeSelf, Is.False);
            Assert.That(ActiveGeneratedPresentationCount(host), Is.EqualTo(1));

            controller.ResetRunForTests();
            controller.ResetRunForTests();
            yield return null;
            yield return null;

            Assert.That(preview.gameObject.activeSelf, Is.False);
            Assert.That(ActiveGeneratedPresentationCount(host), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ResetRunRepairsInvalidAuthoredPlayerBindingWithoutReplacingStableComposition()
        {
            yield return LoadGameplay();

            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var composition = controller.GetComponent<GameplaySceneComposition>();
            var player = composition.AuthoredPlayer;
            var bodyRendererField = typeof(CombatantVisualView).GetField(
                "bodyRenderer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bodyRendererField, Is.Not.Null);

            var originalBodyRenderer = bodyRendererField.GetValue(player);
            var cameraId = composition.GameplayCamera.GetEntityId();
            var fieldId = composition.BattlefieldRoot.GetEntityId();
            var runtimeObjectsId = composition.RuntimeObjectsRoot.GetEntityId();
            var runtimeSystemsId = composition.RuntimeSystemsRoot.GetEntityId();
            var uiId = composition.UiRoot.GetEntityId();
            var playerId = player.GetEntityId();

            bodyRendererField.SetValue(player, null);
            LogAssert.Expect(
                LogType.Warning,
                "Authored combatant visual 'Han Yeonhwa' has invalid CombatantVisualView bindings for role 'Player'. Replacing it with a runtime fallback while preserving the authored player root.");
            try
            {
                Assert.DoesNotThrow(() => controller.ResetRunForTests());
                controller.ResetRunForTests();
                yield return null;
                yield return null;

                Assert.That(composition.IsComplete, Is.True);
                Assert.That(composition.GameplayCamera.GetEntityId(), Is.EqualTo(cameraId));
                Assert.That(composition.BattlefieldRoot.GetEntityId(), Is.EqualTo(fieldId));
                Assert.That(composition.RuntimeObjectsRoot.GetEntityId(), Is.EqualTo(runtimeObjectsId));
                Assert.That(composition.RuntimeSystemsRoot.GetEntityId(), Is.EqualTo(runtimeSystemsId));
                Assert.That(composition.UiRoot.GetEntityId(), Is.EqualTo(uiId));
                Assert.That(composition.AuthoredPlayer.GetEntityId(), Is.EqualTo(playerId));
                Assert.That(player.gameObject.activeInHierarchy, Is.True);
                Assert.That(ActiveUsablePlayerVisualCount(player.transform), Is.EqualTo(1));
                Assert.That(ActiveUsableHealthBarCount(player.transform), Is.EqualTo(1));
            }
            finally
            {
                bodyRendererField.SetValue(player, originalBodyRenderer);
                controller.ResetRunForTests();
            }
        }

        [UnityTest]
        public IEnumerator ResetRunPreservesInactiveAuthoredChildThroughInvalidPlayerRecovery()
        {
            yield return LoadGameplay();

            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var player = controller.GetComponent<GameplaySceneComposition>().AuthoredPlayer;
            var inactiveChild = new GameObject("Authored Inactive Child");
            inactiveChild.transform.SetParent(player.transform, false);
            inactiveChild.SetActive(false);
            var bodyRendererField = typeof(CombatantVisualView).GetField(
                "bodyRenderer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(bodyRendererField, Is.Not.Null);
            var originalBodyRenderer = bodyRendererField.GetValue(player);

            try
            {
                controller.ResetRunForTests();
                Assert.That(inactiveChild.activeSelf, Is.False);

                bodyRendererField.SetValue(player, null);
                LogAssert.Expect(
                    LogType.Warning,
                    "Authored combatant visual 'Han Yeonhwa' has invalid CombatantVisualView bindings for role 'Player'. Replacing it with a runtime fallback while preserving the authored player root.");
                controller.ResetRunForTests();
                bodyRendererField.SetValue(player, originalBodyRenderer);
                controller.ResetRunForTests();

                Assert.That(inactiveChild.activeSelf, Is.False);
            }
            finally
            {
                bodyRendererField.SetValue(player, originalBodyRenderer);
                Object.Destroy(inactiveChild);
                controller.ResetRunForTests();
            }
        }

        [UnityTest]
        public IEnumerator ResetRunDisablesInvalidAuthoredHealthBarBeforeCreatingOneUsableReplacement()
        {
            yield return LoadGameplay();

            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var composition = controller.GetComponent<GameplaySceneComposition>();
            var player = composition.AuthoredPlayer;
            var authoredBar = player.GetComponentInChildren<WorldBarView>(true);
            var fillRendererField = typeof(WorldBarView).GetField(
                "fillRenderer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(authoredBar, Is.Not.Null);
            Assert.That(fillRendererField, Is.Not.Null);
            var originalFillRenderer = fillRendererField.GetValue(authoredBar);
            var originalBarActive = authoredBar.gameObject.activeSelf;
            var playerId = player.GetEntityId();

            fillRendererField.SetValue(authoredBar, null);
            LogAssert.Expect(
                LogType.Warning,
                "Authored WorldBar 'Health Bar' has invalid bindings. Disabling it before using the fallback for 'Health Bar'.");
            try
            {
                Assert.DoesNotThrow(() => controller.ResetRunForTests());
                controller.ResetRunForTests();
                yield return null;

                Assert.That(composition.AuthoredPlayer.GetEntityId(), Is.EqualTo(playerId));
                Assert.That(authoredBar.gameObject.activeSelf, Is.False);
                Assert.That(ActiveUsableHealthBarCount(player.transform), Is.EqualTo(1));
            }
            finally
            {
                fillRendererField.SetValue(authoredBar, originalFillRenderer);
                authoredBar.gameObject.SetActive(originalBarActive);
                controller.ResetRunForTests();
            }
        }

        [UnityTest]
        public IEnumerator ResetRunReusesOneLegacyHealthBarWhenAuthoredAndLibraryBarsAreInvalid()
        {
            yield return LoadGameplay();

            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var composition = controller.GetComponent<GameplaySceneComposition>();
            var player = composition.AuthoredPlayer;
            var authoredBar = player.GetComponentInChildren<WorldBarView>(true);
            var fillRendererField = typeof(WorldBarView).GetField(
                "fillRenderer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var libraryField = typeof(FirstPlayableController).GetField(
                "gameplayVisualPrefabs",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(authoredBar, Is.Not.Null);
            Assert.That(fillRendererField, Is.Not.Null);
            Assert.That(libraryField, Is.Not.Null);

            var originalFillRenderer = fillRendererField.GetValue(authoredBar);
            var originalBarActive = authoredBar.gameObject.activeSelf;
            var originalLibrary = libraryField.GetValue(controller);
            var production = Resources.Load<GameplayVisualPrefabLibrary>("Gameplay/GameplayVisualPrefabLibrary");
            var missingHealthBarLibrary = ScriptableObject.CreateInstance<GameplayVisualPrefabLibrary>();
            missingHealthBarLibrary.Configure(
                production.PlayerVisual,
                production.EnemyVisual,
                null,
                production.WorldShieldBar,
                production.ExperiencePickup,
                production.YeopjeonPickup,
                production.MagnetPickup);
            var cameraId = composition.GameplayCamera.GetEntityId();
            var fieldId = composition.BattlefieldRoot.GetEntityId();
            var runtimeObjectsId = composition.RuntimeObjectsRoot.GetEntityId();
            var runtimeSystemsId = composition.RuntimeSystemsRoot.GetEntityId();
            var uiId = composition.UiRoot.GetEntityId();
            var playerId = player.GetEntityId();

            fillRendererField.SetValue(authoredBar, null);
            libraryField.SetValue(controller, missingHealthBarLibrary);
            LogAssert.Expect(
                LogType.Warning,
                "Authored WorldBar 'Health Bar' has invalid bindings. Disabling it before using the fallback for 'Health Bar'.");
            LogAssert.Expect(
                LogType.Warning,
                "Gameplay visual prefab is missing for 'Health Bar'. Using the legacy visual fallback.");
            try
            {
                controller.ResetRunForTests();
                controller.ResetRunForTests();
                yield return null;

                Assert.That(composition.GameplayCamera.GetEntityId(), Is.EqualTo(cameraId));
                Assert.That(composition.BattlefieldRoot.GetEntityId(), Is.EqualTo(fieldId));
                Assert.That(composition.RuntimeObjectsRoot.GetEntityId(), Is.EqualTo(runtimeObjectsId));
                Assert.That(composition.RuntimeSystemsRoot.GetEntityId(), Is.EqualTo(runtimeSystemsId));
                Assert.That(composition.UiRoot.GetEntityId(), Is.EqualTo(uiId));
                Assert.That(composition.AuthoredPlayer.GetEntityId(), Is.EqualTo(playerId));

                var legacyBars = player.HealthBarAnchor.GetComponentsInChildren<Transform>(true)
                    .Where(candidate => candidate.parent == player.HealthBarAnchor &&
                                        candidate.name == "Health Bar" && candidate.gameObject.activeInHierarchy)
                    .ToArray();
                Assert.That(legacyBars, Has.Length.EqualTo(1));
                Assert.That(legacyBars[0].GetComponent<WorldBarView>(), Is.Null);
                Assert.That(legacyBars[0].Find("Background")?.GetComponent<SpriteRenderer>(), Is.Not.Null);
                Assert.That(legacyBars[0].Find("Fill")?.GetComponent<SpriteRenderer>(), Is.Not.Null);

                fillRendererField.SetValue(authoredBar, originalFillRenderer);
                authoredBar.gameObject.SetActive(originalBarActive);
                libraryField.SetValue(controller, originalLibrary);
                controller.ResetRunForTests();
                yield return null;

                Assert.That(ActiveLegacyBarCount(player.HealthBarAnchor, "Health Bar"), Is.Zero);
                Assert.That(ActiveUsableHealthBarCount(player.transform), Is.EqualTo(1));
            }
            finally
            {
                fillRendererField.SetValue(authoredBar, originalFillRenderer);
                authoredBar.gameObject.SetActive(originalBarActive);
                libraryField.SetValue(controller, originalLibrary);
                Object.Destroy(missingHealthBarLibrary);
                controller.ResetRunForTests();
            }
        }

        [UnityTest]
        public IEnumerator ResetRunDoesNotReplaceAnIntentionallyInactiveValidAuthoredHealthBar()
        {
            yield return LoadGameplay();

            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var player = controller.GetComponent<GameplaySceneComposition>().AuthoredPlayer;
            var authoredBar = player.GetComponentInChildren<WorldBarView>(true);
            Assert.That(authoredBar, Is.Not.Null);
            authoredBar.gameObject.SetActive(false);
            try
            {
                controller.ResetRunForTests();

                Assert.That(authoredBar.gameObject.activeSelf, Is.False);
                Assert.That(player.HealthBarAnchor.GetComponentsInChildren<WorldBarView>(true)
                    .Count(bar => bar.transform.parent == player.HealthBarAnchor), Is.EqualTo(1));
            }
            finally
            {
                authoredBar.gameObject.SetActive(true);
                controller.ResetRunForTests();
            }
        }

        private static IEnumerator LoadGameplay()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
        }

        private static int ActiveGeneratedPresentationCount(GameplayBattlefieldHost host)
        {
            var runtimeRoot = host.RuntimeRoot;
            var count = 0;
            for (var index = 0; index < runtimeRoot.childCount; index++)
                if (runtimeRoot.GetChild(index).name == "Generated Battlefield Presentation" &&
                    runtimeRoot.GetChild(index).gameObject.activeSelf)
                    count++;
            return count;
        }

        private static int ActiveUsablePlayerVisualCount(Transform root) =>
            root.GetComponentsInChildren<CombatantVisualView>(true).Count(view =>
                view.gameObject.activeInHierarchy && view.HasRequiredBindings(CombatantVisualRole.Player));

        private static int ActiveUsableHealthBarCount(Transform root) =>
            root.GetComponentsInChildren<WorldBarView>(true).Count(bar =>
                bar.gameObject.activeInHierarchy && bar.HasRequiredBindings);

        private static int ActiveLegacyBarCount(Transform anchor, string name) =>
            anchor.GetComponentsInChildren<Transform>(true).Count(candidate =>
                candidate.parent == anchor && candidate.name == name && candidate.gameObject.activeInHierarchy &&
                candidate.GetComponent<WorldBarView>() == null &&
                candidate.Find("Background")?.GetComponent<SpriteRenderer>() != null &&
                candidate.Find("Fill")?.GetComponent<SpriteRenderer>() != null);
    }
}
