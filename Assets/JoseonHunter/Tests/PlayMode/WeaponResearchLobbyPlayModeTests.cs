using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
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
    public sealed class WeaponResearchLobbyPlayModeTests
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
        public IEnumerator ResearchShowsEightWeaponsThreeStylesAndPurchasesReadyStyleOnce()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 800;
            data.WeaponMasteryPoints[WeaponId.GakgungShot.Value] = 2000;
            var repository = new MemoryRepository(data);
            MetaGameSession.EnsureExists(repository);
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindAnyObjectByType<WeaponResearchPresenter>(FindObjectsInactive.Include);
            presenter.SelectWeaponForTests(1);

            Assert.That(presenter.WeaponCountForTests, Is.EqualTo(8));
            Assert.That(presenter.StyleCountForTests, Is.EqualTo(3));
            Assert.That(presenter.SelectedStyleStateForTests(1), Is.EqualTo("해금 가능"));
            Canvas.ForceUpdateCanvases();
            var styleButtons = presenter.GetComponentsInChildren<Button>(true)
                .Where(button => button.name.StartsWith("Style Card ")).ToArray();
            Assert.That(styleButtons, Has.Length.EqualTo(3));
            Assert.That(styleButtons.Min(button => button.GetComponent<RectTransform>().rect.height),
                Is.GreaterThanOrEqualTo(64f));
            Assert.That(styleButtons.Min(button => button.GetComponentInChildren<TMPro.TMP_Text>().fontSize),
                Is.GreaterThanOrEqualTo(18f));
            var title = presenter.transform.Find("Research Title").GetComponent<TMPro.TMP_Text>();
            Assert.That(title.color.r, Is.GreaterThan(title.color.b));
            Assert.That(title.color.g, Is.GreaterThan(.35f));

            presenter.ActivateStyleForTests(1);
            presenter.ActivateStyleForTests(1);

            Assert.That(MetaGameSession.Current.Data.Coins, Is.Zero);
            Assert.That(MetaGameSession.Current.Data.UnlockedWeaponStyles,
                Contains.Item(WeaponLegacyPathId.GakgungSunPiercer.Value));
            Assert.That(presenter.SelectedStyleStateForTests(1), Is.EqualTo("장착 중"));
        }

        [UnityTest]
        public IEnumerator ResearchShowsSelectedIconProgressAndExactSequentialLockMessage()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 9999;
            data.WeaponMasteryPoints[WeaponId.GakgungShot.Value] = 564;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindAnyObjectByType<WeaponResearchPresenter>(FindObjectsInactive.Include);
            presenter.SelectWeaponForTests(1);

            var icon = presenter.transform.Find("Selected Weapon Icon").GetComponent<Image>();
            Assert.That(icon.sprite, Is.Not.Null);
            var fill = presenter.transform.Find("Research Progress Backplate/Mastery Progress/Mastery Progress Fill")
                .GetComponent<RectTransform>();
            Assert.That(fill.anchorMax.x, Is.EqualTo(564f / 2000f).Within(.002f));
            var mastery = presenter.transform.Find("Research Progress Backplate/Mastery Summary")
                .GetComponent<TMPro.TMP_Text>();
            Assert.That(mastery.text, Does.Contain("564 / 2,000"));

            presenter.ActivateStyleForTests(2);

            var feedback = presenter.transform.Find("Research Feedback").GetComponent<TMPro.TMP_Text>();
            Assert.That(feedback.text, Is.EqualTo("2단계 연구 완료 시 해금"));
        }

        [UnityTest]
        public IEnumerator ResearchUsesCompactBackplatesAndSeparatedPortraitRows()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;
            Object.FindAnyObjectByType<WeaponResearchPresenter>(FindObjectsInactive.Include).gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();

            Assert.That(ImageNamed("Research Progress Backplate").sprite.name, Is.EqualTo("content_backplate"));
            Assert.That(ButtonsUnder("Weapon Grid"), Has.Length.EqualTo(8));
            Assert.That(ImageNamed("Style Card 0").sprite.name, Is.EqualTo("content_backplate"));
            Assert.That(ImageNamed("Style Card 1").sprite.name, Is.EqualTo("content_backplate"));
            Assert.That(ImageNamed("Style Card 2").sprite.name, Is.EqualTo("content_backplate"));
            AssertNoOverlap("Weapon Grid", "Style Card 0", "Style Card 1", "Style Card 2");
            foreach (var index in Enumerable.Range(0, 3))
                Assert.That(TextUnder("Style Card " + index).text.Split('\n').Length, Is.LessThanOrEqualTo(3));
        }

        [UnityTest]
        public IEnumerator HwandoStarterPathsSwitchWithoutSpendingCoins()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 155;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindAnyObjectByType<WeaponResearchPresenter>(
                FindObjectsInactive.Include);

            Assert.That(presenter.SelectedStyleStateForTests(1), Is.EqualTo("장착 중"));
            Assert.That(presenter.SelectedStyleStateForTests(2), Is.EqualTo("처음부터 해금"));

            presenter.ActivateStyleForTests(2);

            Assert.That(MetaGameSession.Current.Data.Coins, Is.EqualTo(155));
            Assert.That(MetaGameSession.Current.ActiveLoadout.StyleFor(
                    WeaponId.HwandoFlyingBlade),
                Is.EqualTo(WeaponLegacyPathId.HwandoMoonEclipse));
        }

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data) { stored = data.Copy(); return new SaveResult(true, SaveError.None); }
        }

        private static Image ImageNamed(string name) => GameObject.Find(name).GetComponent<Image>();

        private static Button[] ButtonsUnder(string name) =>
            GameObject.Find(name).GetComponentsInChildren<Button>(true);

        private static TMPro.TMP_Text TextUnder(string name) =>
            GameObject.Find(name).GetComponentInChildren<TMPro.TMP_Text>(true);

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void AssertNoOverlap(params string[] names)
        {
            var rects = names.Select(name => (name, rect: WorldRect(GameObject.Find(name).GetComponent<RectTransform>())))
                .ToArray();
            for (var left = 0; left < rects.Length; left++)
            for (var right = left + 1; right < rects.Length; right++)
                Assert.That(rects[left].rect.Overlaps(rects[right].rect), Is.False,
                    rects[left].name + " overlaps " + rects[right].name);
        }
    }
}
