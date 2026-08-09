using System.Collections;
using System.Linq;
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
            var canvasCount = Object.FindObjectsByType<Canvas>().Length;

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
            Assert.That(Object.FindObjectsByType<Canvas>(), Has.Length.EqualTo(canvasCount));
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

        private static IEnumerator LoadGameplay()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
        }
    }
}
