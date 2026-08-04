using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Save;
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
        [SetUp]
        public void SetUp()
        {
            if (MetaGameSession.Current != null) Object.DestroyImmediate(MetaGameSession.Current.gameObject);
        }

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

            Assert.That(labels, Is.EqualTo(new[] { "무기 연구", "출전", "수련" }));
            Assert.That(FindIncludingInactive("Patrol Panel").activeSelf, Is.True);
            Assert.That(FindIncludingInactive("Weapon Research Panel").activeSelf, Is.False);
            Assert.That(FindIncludingInactive("Common Training Panel").activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator HeaderShowsCoinSpriteBesideBareNumber()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 155;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var coinIcon = GameObject.Find("Coin Icon")?.GetComponent<Image>();
            var coinText = GameObject.Find("Coin Text")?.GetComponent<TMPro.TMP_Text>();

            Assert.That(coinIcon, Is.Not.Null);
            Assert.That(coinIcon.sprite, Is.Not.Null);
            Assert.That(coinIcon.preserveAspect, Is.True);
            Assert.That(coinText, Is.Not.Null);
            Assert.That(coinText.text, Is.EqualTo("155"));
            Assert.That(coinText.text, Does.Not.Contain("엽전"));
            Assert.That(coinText.text, Does.Not.Contain("냥"));
        }

        [UnityTest]
        public IEnumerator NavigationSwitchesOneOpaquePanelAtATime()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;
            var navigation = GameObject.Find("Bottom Navigation");
            var buttons = navigation.GetComponentsInChildren<Button>(true);
            buttons[0].onClick.Invoke();
            yield return null;

            Assert.That(FindIncludingInactive("Weapon Research Panel").activeSelf, Is.True);
            Assert.That(FindIncludingInactive("Patrol Panel").activeSelf, Is.False);
            Assert.That(FindIncludingInactive("Common Training Panel").activeSelf, Is.False);
            Assert.That(buttons[0].colors.normalColor,
                Is.EqualTo(new Color(.34f, .10f, .075f, 1f)));
            Assert.That(buttons[1].colors.normalColor,
                Is.EqualTo(new Color(.035f, .043f, .065f, 1f)));
        }

        private static GameObject FindIncludingInactive(string name) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(transform => transform.name == name).gameObject;

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data)
            {
                stored = data.Copy();
                return new SaveResult(true, SaveError.None);
            }
        }
    }
}
