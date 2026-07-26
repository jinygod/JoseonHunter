using System.Linq;
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using JoseonHunter.Editor.Scenes;

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
        public void EachFoundationSceneHasOnlyTheSceneRoot(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.That(roots, Has.Length.EqualTo(1));
                Assert.That(roots[0].name, Is.EqualTo("SceneRoot"));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void GameplaySceneRootContainsWorldAndUi()
        {
            var scene = EditorSceneManager.OpenScene(
                "Assets/JoseonHunter/Scenes/Gameplay.unity",
                OpenSceneMode.Additive);
            try
            {
                var sceneRoot = scene.GetRootGameObjects().Single();
                var childNames = sceneRoot.transform.Cast<UnityEngine.Transform>()
                    .Select(child => child.name)
                    .ToArray();

                CollectionAssert.AreEquivalent(new[] { "World", "UI" }, childNames);
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
    }
}
