using System.Collections;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class BootstrapLoadingPlayModeTests
    {
        [UnityTest]
        public IEnumerator BootstrapKeepsOpaqueOverlayUntilGameplayReadyThenRemovesIt()
        {
            GameplayReadySignal.Reset();
            SceneManager.LoadScene("Bootstrap");

            yield return WaitForScene("Gameplay", 5f);
            var loader = Object.FindFirstObjectByType<BootstrapLoadingPresenter>();
            Assert.That(loader, Is.Not.Null);
            Assert.That(loader.OpaqueForTests, Is.True);
            yield return WaitForProgress(loader, 5f);
            Assert.That(loader.ProgressForTests, Is.EqualTo(1f).Within(.001f));
            yield return WaitForReadiness(5f);
            Assert.That(GameplayReadySignal.IsReady, Is.True);

            yield return WaitForLoaderRemoval(5f);
            Assert.That(Object.FindFirstObjectByType<BootstrapLoadingPresenter>(), Is.Null);
            Assert.That(Object.FindFirstObjectByType<FirstPlayableController>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator DirectGameplayEntryPublishesReadinessWithoutLoadingOverlay()
        {
            GameplayReadySignal.Reset();
            SceneManager.LoadScene("Gameplay");
            yield return null;

            Assert.That(GameplayReadySignal.IsReady, Is.True);
            Assert.That(Object.FindFirstObjectByType<FirstPlayableController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<BootstrapLoadingPresenter>(), Is.Null);
        }

        private static IEnumerator WaitForScene(string sceneName, float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (SceneManager.GetActiveScene().name != sceneName &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName));
        }

        private static IEnumerator WaitForLoaderRemoval(float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Object.FindFirstObjectByType<BootstrapLoadingPresenter>() != null &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitForProgress(
            BootstrapLoadingPresenter loader,
            float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (loader != null && loader.ProgressForTests < .999f &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitForReadiness(float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!GameplayReadySignal.IsReady && Time.realtimeSinceStartup < deadline)
                yield return null;
        }
    }
}
