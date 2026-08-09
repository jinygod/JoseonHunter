using System.Linq;
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using JoseonHunter.Editor.Scenes;
using JoseonHunter.Presentation.UI;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class SceneScaffoldTests
    {
        private static readonly string[] ExpectedScenePaths =
        {
            "Assets/JoseonHunter/Scenes/Bootstrap.unity",
            "Assets/JoseonHunter/Scenes/Lobby.unity",
            "Assets/JoseonHunter/Scenes/Gameplay.unity"
        };

        [Test]
        public void EnabledBuildScenesAreTheFoundationScenesInNavigationOrder()
        {
            CollectionAssert.AreEqual(
                ExpectedScenePaths,
                EditorBuildSettings.scenes.Where(scene => scene.enabled)
                    .Select(scene => scene.path).ToArray());
        }

        [TestCase("Assets/JoseonHunter/Scenes/Bootstrap.unity")]
        [TestCase("Assets/JoseonHunter/Scenes/Lobby.unity")]
        [TestCase("Assets/JoseonHunter/Scenes/Gameplay.unity")]
        public void EachFoundationSceneHasExpectedRootObjects(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var expected = scenePath.EndsWith("Gameplay.unity", StringComparison.Ordinal)
                    ? new[] { "Main Camera", "FirstPlayable", "First Playable UI", "EventSystem" }
                    : scenePath.EndsWith("Bootstrap.unity", StringComparison.Ordinal)
                        ? new[] { "Bootstrap Loading" }
                        : new[] { "Lobby Camera", "Lobby Canvas", "EventSystem" };
                CollectionAssert.AreEquivalent(expected, roots.Select(root => root.name));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GameplaySceneContainsExpectedPortraitRuntimeRoots()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/JoseonHunter/Scenes/Gameplay.unity",
                OpenSceneMode.Additive);
            try
            {
                CollectionAssert.AreEquivalent(
                    new[] { "Main Camera", "FirstPlayable", "First Playable UI", "EventSystem" },
                    scene.GetRootGameObjects().Select(root => root.name));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void BootstrapContainsLoadingPresenter()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/JoseonHunter/Scenes/Bootstrap.unity",
                OpenSceneMode.Additive);
            try
            {
                var presenter = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<BootstrapLoadingPresenter>(true))
                    .SingleOrDefault();
                Assert.That(presenter, Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GenerateRefusesToOverwriteAnOpenDirtyFoundationScene()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/JoseonHunter/Scenes/Bootstrap.unity",
                OpenSceneMode.Additive);
            try
            {
                var unsavedChange = new GameObject("UnsavedChange");
                SceneManager.MoveGameObjectToScene(unsavedChange, scene);
                EditorSceneManager.MarkSceneDirty(scene);

                Assert.That(
                    () => SceneScaffoldGenerator.Generate(),
                    Throws.TypeOf<InvalidOperationException>());
                Assert.That(scene.isDirty, Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GenerateRefusesToReplaceAnOpenDirtyNonFoundationScene()
        {
            EditorSceneManager.OpenScene(
                "Assets/JoseonHunter/Scenes/Bootstrap.unity",
                OpenSceneMode.Single);
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            try
            {
                var unsavedChange = new GameObject("UnsavedNonFoundationChange");
                SceneManager.MoveGameObjectToScene(unsavedChange, scene);
                EditorSceneManager.MarkSceneDirty(scene);

                Assert.That(
                    () => SceneScaffoldGenerator.Generate(),
                    Throws.TypeOf<InvalidOperationException>());
                Assert.That(scene.isLoaded, Is.True);
                Assert.That(scene.isDirty, Is.True);
            }
            finally
            {
                if (scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
