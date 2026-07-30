using JoseonHunter.Editor.Scenes;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class PlayModeSceneGuardTests
    {
        [Test]
        public void EmptyPathInHumanEditorRedirectsToGameplay()
        {
            Assert.That(
                PlayModeSceneGuard.ShouldRedirectToGameplay(string.Empty, isBatchMode: false, isPlayModeTestRunner: false),
                Is.True);
        }

        [TestCase("Assets/JoseonHunter/Scenes/Gameplay.unity")]
        [TestCase("Assets/JoseonHunter/Scenes/Lobby.unity")]
        public void SavedFoundationSceneDoesNotRedirect(string activeScenePath)
        {
            Assert.That(
                PlayModeSceneGuard.ShouldRedirectToGameplay(activeScenePath, isBatchMode: false, isPlayModeTestRunner: false),
                Is.False);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void AutomatedPlayModeExecutionDoesNotRedirect(bool isBatchMode, bool isPlayModeTestRunner)
        {
            Assert.That(
                PlayModeSceneGuard.ShouldRedirectToGameplay(string.Empty, isBatchMode, isPlayModeTestRunner),
                Is.False);
        }
    }
}
