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
            var composition = Create("Composition").AddComponent<GameplaySceneComposition>();
            var battlefield = Create("Battlefield", composition.transform);
            var runtimeObjects = Create("Runtime Objects", composition.transform);
            var runtimeSystems = Create("Runtime Systems", composition.transform);
            var spawnGuides = Create("Spawn Guides", composition.transform);
            var cameraObject = Create("Gameplay Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(3f, -7f, -10f), Quaternion.Euler(12f, 0f, 31f));
            var playerObject = Create("Authored Player", runtimeObjects.transform);
            playerObject.transform.SetLocalPositionAndRotation(new Vector3(1.25f, -2.5f, 0.5f), Quaternion.Euler(0f, 0f, 15f));
            playerObject.transform.localScale = new Vector3(1.5f, 1.25f, 1f);
            var player = playerObject.AddComponent<CombatantVisualView>();
            var uiRoot = Create("UI Root");
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

        [Test]
        public void OverlappingRunScopedRootsAreRejectedAndCannotDeleteAuthoredPlayer()
        {
            var battlefield = Create("Battlefield");
            var runtimeObjects = Create("Runtime Objects", battlefield.transform);
            var spawnGuides = Create("Spawn Guides", battlefield.transform);
            var camera = Create("Gameplay Camera").AddComponent<Camera>();
            var playerObject = Create("Authored Player", runtimeObjects.transform);
            var player = playerObject.AddComponent<CombatantVisualView>();
            var composition = Create("Composition").AddComponent<GameplaySceneComposition>();

            composition.Configure(
                camera,
                battlefield.transform,
                runtimeObjects.transform,
                runtimeObjects.transform,
                spawnGuides.transform,
                player,
                Create("UI Root"));

            Assert.That(composition.IsComplete, Is.False);
            composition.ClearRunScopedChildren();
            Assert.That(playerObject == null, Is.False);
            Assert.That(playerObject.transform.parent, Is.SameAs(runtimeObjects.transform));
        }

        [Test]
        public void AncestorOrDescendantRunScopedRootsAreRejected()
        {
            var battlefield = Create("Battlefield");
            var runtimeObjects = Create("Runtime Objects", battlefield.transform);
            var runtimeSystems = Create("Runtime Systems", runtimeObjects.transform);
            var spawnGuides = Create("Spawn Guides", battlefield.transform);
            var camera = Create("Gameplay Camera").AddComponent<Camera>();
            var player = Create("Authored Player", runtimeObjects.transform).AddComponent<CombatantVisualView>();
            var composition = Create("Composition").AddComponent<GameplaySceneComposition>();

            composition.Configure(
                camera,
                battlefield.transform,
                runtimeObjects.transform,
                runtimeSystems.transform,
                spawnGuides.transform,
                player,
                Create("UI Root"));

            Assert.That(composition.IsComplete, Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void StableCameraOrBattlefieldInsideRuntimeSystemsIsRejectedAndNotCleared(bool placeCameraInRuntimeSystems)
        {
            var owner = Create("FirstPlayable");
            var battlefield = Create("Battlefield", owner.transform);
            var runtimeObjects = Create("Runtime Objects", owner.transform);
            var runtimeSystems = Create("Runtime Systems", owner.transform);
            var spawnGuides = Create("Spawn Guides", owner.transform);
            var camera = Create("Gameplay Camera", placeCameraInRuntimeSystems
                    ? runtimeSystems.transform
                    : owner.transform)
                .AddComponent<Camera>();
            if (!placeCameraInRuntimeSystems)
                battlefield.transform.SetParent(runtimeSystems.transform, false);
            var playerObject = Create("Authored Player", runtimeObjects.transform);
            var player = playerObject.AddComponent<CombatantVisualView>();
            var composition = owner.AddComponent<GameplaySceneComposition>();

            composition.Configure(
                camera,
                battlefield.transform,
                runtimeObjects.transform,
                runtimeSystems.transform,
                spawnGuides.transform,
                player,
                Create("UI Root", owner.transform));

            Assert.That(composition.IsComplete, Is.False);
            composition.ClearRunScopedChildren();
            Assert.That(camera, Is.Not.Null);
            Assert.That(battlefield, Is.Not.Null);
            Assert.That(playerObject, Is.Not.Null);
        }

        [Test]
        public void CaptureAuthoredStateOnlyOnceKeepsTheFirstCameraAndPlayerPose()
        {
            var composition = Create("Composition").AddComponent<GameplaySceneComposition>();
            var battlefield = Create("Battlefield", composition.transform);
            var runtimeObjects = Create("Runtime Objects", composition.transform);
            var runtimeSystems = Create("Runtime Systems", composition.transform);
            var spawnGuides = Create("Spawn Guides", composition.transform);
            var camera = Create("Gameplay Camera").AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(-3f, 4f, -10f), Quaternion.Euler(0f, 0f, -20f));
            var playerObject = Create("Authored Player", runtimeObjects.transform);
            playerObject.transform.SetLocalPositionAndRotation(new Vector3(-1.5f, 2.25f, 0f), Quaternion.Euler(0f, 0f, 35f));
            playerObject.transform.localScale = new Vector3(.8f, 1.1f, 1f);
            var player = playerObject.AddComponent<CombatantVisualView>();

            composition.Configure(
                camera,
                battlefield.transform,
                runtimeObjects.transform,
                runtimeSystems.transform,
                spawnGuides.transform,
                player,
                Create("UI Root"));
            composition.CaptureAuthoredState();
            camera.transform.SetPositionAndRotation(new Vector3(9f, 8f, 7f), Quaternion.identity);
            playerObject.transform.SetLocalPositionAndRotation(Vector3.one, Quaternion.identity);
            playerObject.transform.localScale = Vector3.one;
            composition.CaptureAuthoredState();
            composition.RestoreAuthoredState();

            Assert.That(camera.transform.position, Is.EqualTo(new Vector3(-3f, 4f, -10f)));
            Assert.That(Quaternion.Angle(camera.transform.rotation, Quaternion.Euler(0f, 0f, -20f)), Is.LessThan(.001f));
            Assert.That(playerObject.transform.localPosition, Is.EqualTo(new Vector3(-1.5f, 2.25f, 0f)));
            Assert.That(Quaternion.Angle(playerObject.transform.localRotation, Quaternion.Euler(0f, 0f, 35f)), Is.LessThan(.001f));
            Assert.That(playerObject.transform.localScale, Is.EqualTo(new Vector3(.8f, 1.1f, 1f)));
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
