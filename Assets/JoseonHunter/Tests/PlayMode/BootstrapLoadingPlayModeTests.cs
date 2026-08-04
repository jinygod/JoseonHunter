using System.Collections;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class BootstrapLoadingPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            if (MetaGameSession.Current != null)
                Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [UnityTest]
        public IEnumerator BootstrapLoadsLobbyAfterSaveInitializationThenRemovesOverlay()
        {
            GameplayReadySignal.Reset();
            SceneManager.LoadScene("Bootstrap");

            yield return WaitForScene("Lobby", 5f);
            var loader = Object.FindFirstObjectByType<BootstrapLoadingPresenter>();
            Assert.That(loader, Is.Not.Null);
            Assert.That(loader.OpaqueForTests, Is.True);
            var hero = loader.transform.Find("Han Yeonhwa Loading Art").GetComponent<Image>();
            Assert.That(hero.sprite, Is.Not.Null);
            Assert.That(hero.color.a, Is.GreaterThan(.9f));
            yield return WaitForProgress(loader, 5f);
            Assert.That(loader.ProgressForTests, Is.EqualTo(1f).Within(.001f));
            yield return WaitForLoaderRemoval(5f);
            Assert.That(Object.FindFirstObjectByType<BootstrapLoadingPresenter>(), Is.Null);
            Assert.That(MetaGameSession.Current, Is.Not.Null);
            Assert.That(MetaGameSession.Current.Data.SchemaVersion, Is.EqualTo(2));
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

        [UnityTest]
        public IEnumerator LobbySortieUsesBootstrapAndKeepsLoadingArtVisibleBeforeGameplay()
        {
            MetaGameSession.EnsureExists();
            SceneManager.LoadScene("Lobby");
            yield return null;
            var start = GameObject.Find("Start Patrol").GetComponent<Button>();
            start.onClick.Invoke();

            yield return WaitForScene("Bootstrap", 5f);
            var startedAt = Time.realtimeSinceStartup;
            var loader = Object.FindFirstObjectByType<BootstrapLoadingPresenter>();
            Assert.That(loader, Is.Not.Null);
            Assert.That(loader.OpaqueForTests, Is.True);
            yield return WaitForScene("Gameplay", 5f);
            yield return WaitForLoaderRemoval(5f);
            Assert.That(Time.realtimeSinceStartup - startedAt, Is.GreaterThanOrEqualTo(1.4f));
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
