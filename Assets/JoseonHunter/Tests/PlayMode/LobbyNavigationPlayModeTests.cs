using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Save;
using JoseonHunter.Domain.Progression;
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
            var lobby = Object.FindAnyObjectByType<LobbyBootstrap>();
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
        public IEnumerator HeaderShowsAccountLevelNameAndCurrentExperienceProgress()
        {
            var data = SaveDataV1.CreateDefaults();
            data.AccountExperience = 250;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var level = GameObject.Find("Account Level")?.GetComponent<TMPro.TMP_Text>();
            var accountName = GameObject.Find("Account Name")?.GetComponent<TMPro.TMP_Text>();
            var experienceText = GameObject.Find("Account Experience Text")?.GetComponent<TMPro.TMP_Text>();
            var experienceFill = GameObject.Find("Account Experience Fill")?.GetComponent<Image>();

            Assert.That(level, Is.Not.Null);
            Assert.That(level.text, Is.EqualTo("3"));
            Assert.That(accountName.text, Is.EqualTo("요괴 사냥꾼"));
            Assert.That(experienceText.text, Is.EqualTo("8 / 188"));
            Assert.That(experienceFill.fillAmount, Is.EqualTo(8f / 188f).Within(.001f));
            Assert.That(GameObject.Find("Lobby Title"), Is.Null);
        }

        [UnityTest]
        public IEnumerator HeaderSeparatesAccountProfileCurrencyAndSettingsControls()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 155;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var header = GameObject.Find("Header")?.transform;
            Assert.That(header, Is.Not.Null);
            var profile = header.Find("Account Profile")?.GetComponent<RectTransform>();
            var currency = header.Find("Currency Capsule")?.GetComponent<RectTransform>();
            var settings = header.Find("Settings Button")?.GetComponent<RectTransform>();

            Assert.That(profile, Is.Not.Null);
            Assert.That(currency, Is.Not.Null);
            Assert.That(settings, Is.Not.Null);
            Assert.That(profile.anchorMax.x, Is.LessThanOrEqualTo(currency.anchorMin.x));
            Assert.That(currency.anchorMax.x, Is.LessThanOrEqualTo(settings.anchorMin.x));
            Assert.That(header.Find("Currency Capsule/Coin Icon"), Is.Not.Null);
            Assert.That(header.Find("Currency Capsule/Coin Text"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator LobbySettingsUsesAReadableSpriteInsteadOfComposedGearBlocks()
        {
            SceneManager.LoadScene("Lobby");
            yield return null;

            var settings = GameObject.Find("Settings Button")?.transform;
            Assert.That(settings, Is.Not.Null);
            var icon = settings.Find("Settings Icon")?.GetComponent<Image>();
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.sprite, Is.Not.Null);
            Assert.That(settings.Find("Gear Tooth 0"), Is.Null);
            Assert.That(settings.Find("Gear Hub"), Is.Null);
        }

        [UnityTest]
        public IEnumerator LobbyGearOpensPersistentAudioControls()
        {
            var data = SaveDataV1.CreateDefaults();
            data.MusicVolume = .4f;
            data.SoundEffectVolume = .7f;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var settingsButton = GameObject.Find("Settings Button")?.GetComponent<Button>();
            Assert.That(settingsButton, Is.Not.Null);
            settingsButton.onClick.Invoke();
            yield return null;

            var sliders = Object.FindObjectsByType<Slider>(FindObjectsInactive.Include);
            var music = sliders.Single(slider => slider.name == "Music Volume Slider");
            var effects = sliders.Single(slider => slider.name == "Sound Effect Volume Slider");
            Assert.That(music.gameObject.activeInHierarchy, Is.True);
            Assert.That(music.value, Is.EqualTo(.4f).Within(.001f));
            Assert.That(effects.value, Is.EqualTo(.7f).Within(.001f));
        }

        [UnityTest]
        public IEnumerator LobbyAudioSettingsCloseButtonHidesSerializedOverlay()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var settingsButton = GameObject.Find("Settings Button")?.GetComponent<Button>();
            Assert.That(settingsButton, Is.Not.Null);
            settingsButton.onClick.Invoke();
            yield return null;

            var overlay = GameObject.Find("Audio Settings Overlay");
            var closeButton = GameObject.Find("Close Audio Settings")?.GetComponent<Button>();
            Assert.That(overlay, Is.Not.Null);
            Assert.That(closeButton, Is.Not.Null);
            Assert.That(overlay.activeInHierarchy, Is.True);

            closeButton.onClick.Invoke();
            yield return null;

            Assert.That(overlay.activeSelf, Is.False,
                "The serialized lobby audio overlay must subscribe its close request to LobbyBootstrap.");
        }

        [UnityTest]
        public IEnumerator LobbyAudioSliderHandlesStayCompactAndInsideTheirTracks()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;

            var settingsButton = GameObject.Find("Settings Button")?.GetComponent<Button>();
            Assert.That(settingsButton, Is.Not.Null);
            settingsButton.onClick.Invoke();
            yield return null;

            foreach (var slider in Object.FindObjectsByType<Slider>(FindObjectsInactive.Include))
            {
                var track = slider.GetComponent<RectTransform>();
                var handle = slider.handleRect;
                Assert.That(handle.sizeDelta.x, Is.LessThanOrEqualTo(24f), slider.name);
                Assert.That(handle.sizeDelta.y, Is.LessThanOrEqualTo(28f), slider.name);
                Assert.That(handle.parent.GetComponent<RectTransform>().offsetMin.x,
                    Is.GreaterThanOrEqualTo(12f), slider.name);
                Assert.That(handle.parent.GetComponent<RectTransform>().offsetMax.x,
                    Is.LessThanOrEqualTo(-12f), slider.name);
                Assert.That(track.sizeDelta.x, Is.GreaterThan(handle.sizeDelta.x), slider.name);
            }
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
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
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
