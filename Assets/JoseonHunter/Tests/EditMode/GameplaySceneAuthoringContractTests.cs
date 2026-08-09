using System;
using System.Linq;
using JoseonHunter.Editor.Scenes;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class GameplaySceneAuthoringContractTests
    {
        private const string GameplayScenePath = "Assets/JoseonHunter/Scenes/Gameplay.unity";

        [Test]
        public void GameplaySceneContainsOneCompleteAuthoredComposition()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var cameraRoot = FindSingleRoot(scene, "Main Camera");
                var firstPlayable = FindSingleRoot(scene, "FirstPlayable");
                var uiRoot = FindSingleRoot(scene, "First Playable UI");
                var eventSystemRoot = FindSingleRoot(scene, "EventSystem");
                Assert.That(cameraRoot, Is.Not.Null);
                Assert.That(firstPlayable, Is.Not.Null);
                Assert.That(uiRoot, Is.Not.Null);
                Assert.That(eventSystemRoot, Is.Not.Null);

                var composition = firstPlayable.GetComponents<GameplaySceneComposition>().SingleOrDefault();
                Assert.That(composition, Is.Not.Null);
                Assert.That(composition.IsComplete, Is.True);
                Assert.That(firstPlayable.transform.Find("FlatField"), Is.Not.Null);
                Assert.That(firstPlayable.transform.Find("RuntimeObjects"), Is.Not.Null);
                Assert.That(firstPlayable.transform.Find("RuntimeSystems"), Is.Not.Null);
                Assert.That(firstPlayable.transform.Find("Spawn Guides"), Is.Not.Null);

                var player = firstPlayable.transform.Find("RuntimeObjects/Han Yeonhwa");
                Assert.That(player, Is.Not.Null);
                Assert.That(PrefabUtility.GetCorrespondingObjectFromOriginalSource(player.gameObject), Is.Not.Null);
                var playerView = player.GetComponent<CombatantVisualView>();
                Assert.That(playerView, Is.Not.Null);
                Assert.That(playerView.HealthBarAnchor, Is.Not.Null);
                Assert.That(playerView.HealthBarAnchor.GetComponentsInChildren<WorldBarView>(true), Has.Length.EqualTo(1));
                Assert.That(firstPlayable.GetComponentsInChildren<GameplayBattlefieldHost>(true), Has.Length.EqualTo(1));
                Assert.That(uiRoot.GetComponents<FirstPlayableUiBootstrap>(), Has.Length.EqualTo(1));
                Assert.That(eventSystemRoot.GetComponents<EventSystem>(), Has.Length.EqualTo(1));
                Assert.That(eventSystemRoot.GetComponents<BaseInputModule>(), Has.Length.EqualTo(1));
                Assert.That(firstPlayable.GetComponents<FirstPlayableController>(), Has.Length.EqualTo(1));
                Assert.That(firstPlayable.GetComponents<GameFlowCoordinator>(), Has.Length.EqualTo(1));
                Assert.That(AssetDatabase.LoadAssetAtPath<GameplayVisualPrefabLibrary>(
                    GameplayVisualPrefabBuilder.LibraryAssetPath), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GeneratorPreservesExistingAuthoringOnRepeatedValidation()
        {
            FirstPlayableSceneGenerator.Generate();
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var player = scene.GetRootGameObjects().Single(root => root.name == "FirstPlayable")
                    .transform.Find("RuntimeObjects/Han Yeonhwa");
                var position = player.localPosition;
                var scale = player.localScale;
                var prefabPath = AssetDatabase.GetAssetPath(
                    PrefabUtility.GetCorrespondingObjectFromOriginalSource(player.gameObject));
                var dependencyHash = AssetDatabase.GetAssetDependencyHash(prefabPath);

                FirstPlayableSceneGenerator.Generate();

                Assert.That(player, Is.Not.Null);
                Assert.That(player.localPosition, Is.EqualTo(position));
                Assert.That(player.localScale, Is.EqualTo(scale));
                Assert.That(AssetDatabase.GetAssetDependencyHash(prefabPath), Is.EqualTo(dependencyHash));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GeneratorRefusesDirtyLoadedGameplayBeforeMutation()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);
            try
            {
                var dummy = new GameObject("Unsaved Gameplay Authoring Change");
                SceneManager.MoveGameObjectToScene(dummy, scene);
                EditorSceneManager.MarkSceneDirty(scene);

                Assert.That(() => FirstPlayableSceneGenerator.Generate(), Throws.TypeOf<InvalidOperationException>());
                Assert.That(scene.isDirty, Is.True);
                Assert.That(scene.GetRootGameObjects().Any(root => root == dummy), Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject FindSingleRoot(Scene scene, string name)
        {
            var matches = scene.GetRootGameObjects().Where(root => root.name == name).ToArray();
            Assert.That(matches, Has.Length.EqualTo(1), $"Expected exactly one root named '{name}'.");
            return matches.SingleOrDefault();
        }
    }
}
