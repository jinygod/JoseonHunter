using JoseonHunter.Editor.Scenes;
using NUnit.Framework;
using UnityEditor.SceneManagement;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class PlayModeSceneGuardTests
    {
        [Test]
        public void InteractiveEditorResolvesBootstrapAsPlayStartScene()
        {
            Assert.That(
                PlayModeSceneGuard.ResolveStartScenePath(false),
                Is.EqualTo("Assets/JoseonHunter/Scenes/Bootstrap.unity"));
        }

        [Test]
        public void BatchModeDoesNotOverridePlayStartScene()
        {
            Assert.That(PlayModeSceneGuard.ResolveStartScenePath(true), Is.Null);
        }

        [Test]
        public void ConfigureStartSceneAssignsBootstrapForInteractiveEditor()
        {
            var previous = EditorSceneManager.playModeStartScene;
            try
            {
                PlayModeSceneGuard.ConfigureStartScene(false);

                Assert.That(EditorSceneManager.playModeStartScene, Is.Not.Null);
                Assert.That(
                    EditorSceneManager.playModeStartScene.name,
                    Is.EqualTo("Bootstrap"));
            }
            finally
            {
                EditorSceneManager.playModeStartScene = previous;
            }
        }
    }
}
