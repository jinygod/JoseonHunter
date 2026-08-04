using System.Collections;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class GameSceneRouterPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            if (MetaGameSession.Current != null)
                Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [UnityTest]
        public IEnumerator DestroyedRouteHostCannotLeaveRouterPermanentlyBusy()
        {
            var session = MetaGameSession.EnsureExists();
            SceneManager.LoadScene("Lobby");
            yield return null;
            var firstHost = new GameObject("Transient Lobby Route Host").AddComponent<RouteHost>();
            firstHost.Begin(session.Router.LoadGameplay());

            yield return WaitForScene("Gameplay", 5f);
            yield return null;
            Assert.That(session.Router.IsRouting, Is.False);

            var secondHost = new GameObject("Transient Gameplay Route Host").AddComponent<RouteHost>();
            secondHost.Begin(session.Router.LoadLobby());
            yield return WaitForScene("Lobby", 5f);
            Assert.That(session.Router.IsRouting, Is.False);
        }

        private static IEnumerator WaitForScene(string expected, float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (SceneManager.GetActiveScene().name != expected && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(expected));
        }

        private sealed class RouteHost : MonoBehaviour
        {
            public void Begin(IEnumerator routine) => StartCoroutine(routine);
        }
    }
}
