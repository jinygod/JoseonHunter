using System.Collections.Generic;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GameplaySceneCompositionTests
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";
        private readonly List<GameObject> cleanupObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in cleanupObjects)
            {
                if (gameObject != null)
                    Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CompleteCompositionPreservesStableRootsAndRestoresAuthoredCameraAndPlayerPose()
        {
            var battlefield = Create("Battlefield");
            var runtimeObjects = Create("Runtime Objects", battlefield.transform);
            var runtimeSystems = Create("Runtime Systems", battlefield.transform);
            var spawnGuides = Create("Spawn Guides", battlefield.transform);
            var cameraObject = Create("Gameplay Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(3f, -7f, -10f), Quaternion.Euler(12f, 0f, 31f));
            var playerObject = Create("Authored Player", runtimeObjects.transform);
            playerObject.transform.SetLocalPositionAndRotation(new Vector3(1.25f, -2.5f, 0.5f), Quaternion.Euler(0f, 0f, 15f));
            playerObject.transform.localScale = new Vector3(1.5f, 1.25f, 1f);
            var player = playerObject.AddComponent<CombatantVisualView>();
            var uiRoot = Create("UI Root");
            var composition = Create("Composition").AddComponent<GameplaySceneComposition>();
            var battlefieldId = battlefield.GetEntityId();
            var runtimeObjectsId = runtimeObjects.GetEntityId();
            var runtimeSystemsId = runtimeSystems.GetEntityId();
            var spawnGuidesId = spawnGuides.GetEntityId();
            var playerId = playerObject.GetEntityId();

            composition.Configure(
                camera,
                battlefield.transform,
                runtimeObjects.transform,
                runtimeSystems.transform,
                spawnGuides.transform,
                player,
                uiRoot);
            composition.CaptureAuthoredState();

            var transientObject = Create("Transient Object", runtimeObjects.transform);
            var transientSystem = Create("Transient System", runtimeSystems.transform);
            camera.transform.SetPositionAndRotation(new Vector3(99f, 98f, 97f), Quaternion.identity);
            playerObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            playerObject.transform.localScale = Vector3.one;
            playerObject.SetActive(false);
            composition.ClearRunScopedChildren();
            composition.RestoreAuthoredState();

            Assert.That(composition.IsComplete, Is.True);
            Assert.That(battlefield.GetEntityId(), Is.EqualTo(battlefieldId));
            Assert.That(runtimeObjects.GetEntityId(), Is.EqualTo(runtimeObjectsId));
            Assert.That(runtimeSystems.GetEntityId(), Is.EqualTo(runtimeSystemsId));
            Assert.That(spawnGuides.GetEntityId(), Is.EqualTo(spawnGuidesId));
            Assert.That(playerObject.GetEntityId(), Is.EqualTo(playerId));
            Assert.That(transientObject == null, Is.True);
            Assert.That(transientSystem == null, Is.True);
            Assert.That(camera.transform.position, Is.EqualTo(new Vector3(3f, -7f, -10f)));
            Assert.That(camera.transform.rotation, Is.EqualTo(Quaternion.Euler(12f, 0f, 31f)));
            Assert.That(playerObject.transform.localPosition, Is.EqualTo(new Vector3(1.25f, -2.5f, 0.5f)));
            Assert.That(playerObject.transform.localRotation, Is.EqualTo(Quaternion.Euler(0f, 0f, 15f)));
            Assert.That(playerObject.transform.localScale, Is.EqualTo(new Vector3(1.5f, 1.25f, 1f)));
            Assert.That(playerObject.activeSelf, Is.True);
        }

        [Test]
        public void IncompleteOrCrossHierarchyReferencesAreRejected()
        {
            var battlefield = Create("Battlefield");
            var runtimeObjects = Create("Runtime Objects", battlefield.transform);
            var runtimeSystems = Create("Runtime Systems", battlefield.transform);
            var spawnGuides = Create("Spawn Guides", battlefield.transform);
            var camera = Create("Gameplay Camera").AddComponent<Camera>();
            var playerOutsideRuntimeObjects = Create("Player Outside Runtime Objects").AddComponent<CombatantVisualView>();
            var uiRoot = Create("UI Root");
            var composition = Create("Composition").AddComponent<GameplaySceneComposition>();

            composition.Configure(
                camera,
                battlefield.transform,
                runtimeObjects.transform,
                runtimeSystems.transform,
                spawnGuides.transform,
                playerOutsideRuntimeObjects,
                uiRoot);
            Assert.That(composition.IsComplete, Is.False);

            var foreignScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            var foreignBattlefield = new GameObject("Foreign Battlefield");
            SceneManager.MoveGameObjectToScene(foreignBattlefield, foreignScene);
            try
            {
                var playerInsideRuntimeObjects = Create("Player Inside Runtime Objects", runtimeObjects.transform)
                    .AddComponent<CombatantVisualView>();
                composition.Configure(
                    camera,
                    foreignBattlefield.transform,
                    runtimeObjects.transform,
                    runtimeSystems.transform,
                    spawnGuides.transform,
                    playerInsideRuntimeObjects,
                    uiRoot);

                Assert.That(composition.IsComplete, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(foreignBattlefield);
                EditorSceneManager.CloseScene(foreignScene, true);
            }
        }

        private GameObject Create(string name, Transform parent = null)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            cleanupObjects.Add(gameObject);
            return gameObject;
        }
    }
}
