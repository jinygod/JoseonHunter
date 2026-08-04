using System.Collections;
using System.Linq;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class LobbyNavigationPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            if (MetaGameSession.Current != null) Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

        [UnityTest]
        public IEnumerator LobbyShowsExactlyThreeNavigationButtonsAndDefaultsToPatrol()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;
            var lobby = Object.FindFirstObjectByType<LobbyBootstrap>();
            Assert.That(lobby, Is.Not.Null);

            var navigation = GameObject.Find("Bottom Navigation");
            var labels = navigation.GetComponentsInChildren<Button>(true)
                .Select(button => button.GetComponentInChildren<TMPro.TMP_Text>().text).ToArray();

            Assert.That(labels, Is.EqualTo(new[] { "무기 연구", "출전", "공통 수련" }));
            Assert.That(FindIncludingInactive("Patrol Panel").activeSelf, Is.True);
            Assert.That(FindIncludingInactive("Weapon Research Panel").activeSelf, Is.False);
            Assert.That(FindIncludingInactive("Common Training Panel").activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator NavigationSwitchesOneOpaquePanelAtATime()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;
            var navigation = GameObject.Find("Bottom Navigation");
            navigation.GetComponentsInChildren<Button>(true)[0].onClick.Invoke();
            yield return null;

            Assert.That(FindIncludingInactive("Weapon Research Panel").activeSelf, Is.True);
            Assert.That(FindIncludingInactive("Patrol Panel").activeSelf, Is.False);
            Assert.That(FindIncludingInactive("Common Training Panel").activeSelf, Is.False);
        }

        private static GameObject FindIncludingInactive(string name) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(transform => transform.name == name).gameObject;
    }
}
