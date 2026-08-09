using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
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
        private static readonly WeaponId[] ExpectedWeaponOrder =
        {
            WeaponId.HwandoFlyingBlade, WeaponId.GakgungShot, WeaponId.TalismanThrow, WeaponId.ThunderCrashBomb,
            WeaponId.JangseungWard, WeaponId.SingijeonVolley, WeaponId.FrostFlask, WeaponId.WindThunderFan
        };
        private static readonly string[] ExpectedWeaponNames =
        {
            "환도 비검", "각궁", "주술 부적", "벽력탄", "장승진", "신기전", "서리병", "풍뢰선"
        };
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
            var page = presenter.GetComponent<ResearchPageView>();

            Assert.That(presenter.WeaponCountForTests, Is.EqualTo(8));
            Assert.That(presenter.StyleCountForTests, Is.EqualTo(3));
            Assert.That(presenter.SelectedStyleStateForTests(1), Is.EqualTo("해금 가능"));
            Canvas.ForceUpdateCanvases();
            Assert.That(page, Is.Not.Null);
            Assert.That(page.WeaponSelectors, Has.Length.EqualTo(8));
            var styleButtons = page.Rows.Select(row => row.ActionButton).ToArray();
            Assert.That(styleButtons, Has.Length.EqualTo(3));
            Assert.That(styleButtons.Min(button => button.GetComponent<RectTransform>().rect.height),
                Is.GreaterThanOrEqualTo(64f));
            Assert.That(styleButtons.Min(button => button.GetComponentInChildren<TMPro.TMP_Text>().fontSize),
                Is.GreaterThanOrEqualTo(18f));
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

            var page = presenter.GetComponent<ResearchPageView>();
            Assert.That(page, Is.Not.Null);
            Assert.That(page.HasRequiredBindings, Is.True);
            Assert.That(page.SelectedWeaponIcon.sprite, Is.Not.Null);
            var fill = page.MasteryProgress.GetComponentsInChildren<Image>(true)
                .Single(image => image.fillAmount < 1f);
            Assert.That(fill.fillAmount, Is.EqualTo(564f / 2000f).Within(.002f));
            var mastery = page.MasteryProgress.GetComponentInChildren<TMPro.TMP_Text>(true);
            Assert.That(mastery.text, Does.Contain("564 / 2,000"));

            presenter.ActivateStyleForTests(2);

            var feedback = page.FeedbackText;
            Assert.That(feedback.text, Is.EqualTo("2단계 연구 완료 시 해금"));
        }

        [UnityTest]
        public IEnumerator ResearchUsesCompactBackplatesAndSeparatedPortraitRows()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindAnyObjectByType<WeaponResearchPresenter>(FindObjectsInactive.Include);
            presenter.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            var page = presenter.GetComponent<ResearchPageView>();

            Assert.That(page, Is.Not.Null);
            Assert.That(page.WeaponSelectors, Has.Length.EqualTo(8));
            Assert.That(page.Rows, Has.Length.EqualTo(3));
            var hierarchyBeforeSelection = page.GetComponentsInChildren<Transform>(true);
            presenter.SelectWeaponForTests(1);
            Canvas.ForceUpdateCanvases();
            Assert.That(page.WeaponSelectors.All(selector => selector.Background.sprite.name == "weapon_selector_frame"), Is.True,
                string.Join(", ", page.WeaponSelectors.Select(selector => selector.Background.sprite.name)));
            Assert.That(page.WeaponSelectors[1].Button.colors.normalColor,
                Is.Not.EqualTo(page.WeaponSelectors[0].Button.colors.normalColor));
            CollectionAssert.AreEqual(hierarchyBeforeSelection, page.GetComponentsInChildren<Transform>(true));
            Assert.That(page.Rows.All(row => row.GetComponent<Image>().sprite.name == "content_backplate"), Is.True);
            var selectorCards = page.WeaponSelectors.Select(selector => selector.GetComponent<RectTransform>()).ToArray();
            var pageBounds = page.GetComponent<RectTransform>();
            Assert.That(selectorCards.All(card => card.rect.width <= pageBounds.rect.width), Is.True);
            Assert.That(page.Rows.All(row => row.GetComponent<RectTransform>().rect.height >= 64f), Is.True);
            AssertNoOverlap(selectorCards.Concat(page.Rows.Select(row => row.GetComponent<RectTransform>())).ToArray());
            foreach (var row in page.Rows)
            {
                Assert.That(row.EffectText.text.Split('\n').Length, Is.LessThanOrEqualTo(3));
                Assert.That(row.EffectText.fontSize, Is.EqualTo(18f));
                Assert.That(row.EffectText.rectTransform.anchorMin, Is.EqualTo(new Vector2(.28f, .51f)));
                Assert.That(row.EffectText.rectTransform.anchorMax, Is.EqualTo(new Vector2(.28f, .51f)));
                Assert.That(row.EffectText.rectTransform.sizeDelta, Is.EqualTo(new Vector2(500f, 32f)));
                var rowBounds = WorldRect(row.GetComponent<RectTransform>());
                var effectBounds = WorldRect(row.EffectText.rectTransform);
                Assert.That(rowBounds.Contains(effectBounds.min) && rowBounds.Contains(effectBounds.max), Is.True,
                    $"{row.name} effect bounds {effectBounds} overflow row bounds {rowBounds}");
            }
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

        [UnityTest]
        public IEnumerator AuthoredResearchPageBindsFixedWeaponOrderRowsAndExternalListeners()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 800;
            data.WeaponMasteryPoints[WeaponId.GakgungShot.Value] = 2000;
            var session = MetaGameSession.EnsureExists(new MemoryRepository(data));
            var root = new GameObject("Authored Research Page");
            var presenter = root.AddComponent<WeaponResearchPresenter>();
            var page = root.AddComponent<ResearchPageView>();
            var selectors = WeaponRoster.All.Select((weapon, index) =>
                CreateSelector(root.transform, weapon, index)).ToArray();
            var rows = Enumerable.Range(0, 3).Select(index => CreateResearchRow(root.transform, index)).ToArray();
            var actionClicks = 0;
            rows[1].ActionButton.onClick.AddListener(() => actionClicks++);
            page.Configure(selectors, CreateImage("Selected Icon", root.transform),
                CreateText("Selected Name", root.transform), CreateProgress("Mastery", root.transform), rows,
                CreateText("Feedback", root.transform));
            presenter.ConfigureView(page);
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponCatalogAsset>(
                "Assets/JoseonHunter/Content/Weapons/WeaponCatalog.asset");
            presenter.ConfigureCatalog(catalog);

            var selectorIds = selectors.Select(selector => selector.GetEntityId()).ToArray();
            var rowIds = rows.Select(row => row.GetEntityId()).ToArray();
            presenter.InitializeAuthored(session, null);
            presenter.InitializeAuthored(session, null);
            presenter.SelectWeaponForTests(1);

            Assert.That(page.HasRequiredBindings, Is.True);
            Assert.That(page.WeaponSelectors, Has.Length.EqualTo(8));
            Assert.That(page.Rows, Has.Length.EqualTo(3));
            CollectionAssert.AreEqual(ExpectedWeaponOrder.Select(weapon => weapon.Value), WeaponRoster.All.Select(weapon => weapon.Value));
            CollectionAssert.AreEqual(ExpectedWeaponOrder.Select(weapon => weapon.Value),
                page.WeaponSelectors.Select(selector => selector.name.Replace("Selector ", string.Empty)));
            CollectionAssert.AreEqual(ExpectedWeaponNames, page.WeaponSelectors.Select(selector => selector.WeaponName.text));
            for (var index = 0; index < ExpectedWeaponOrder.Length; index++)
            {
                Assert.That(catalog.TryGet(ExpectedWeaponOrder[index], out var definition), Is.True);
                var expectedIcon = definition.UiIcon ?? definition.PresentationSprites.FirstOrDefault();
                Assert.That(page.WeaponSelectors[index].Icon.sprite, Is.SameAs(expectedIcon));
            }
            CollectionAssert.AreEqual(selectorIds, page.WeaponSelectors.Select(selector => selector.GetEntityId()));
            CollectionAssert.AreEqual(rowIds, page.Rows.Select(row => row.GetEntityId()));
            Assert.That(page.SelectedWeaponName.text, Is.Not.Empty);
            Assert.That(page.SelectedWeaponIcon.sprite, Is.Not.Null);
            Assert.That(page.MasteryProgress.GetComponentInChildren<TMPro.TMP_Text>().text, Does.Contain("2,000"));
            foreach (var row in page.Rows)
            {
                Assert.That(row.StageNameText.text, Is.Not.Empty);
                Assert.That(row.StatusText.text, Is.Not.Empty);
                Assert.That(row.EffectText.text, Is.Not.Empty);
                Assert.That(row.RequirementText.text, Is.Not.Empty);
                Assert.That(row.ActionText.text, Is.Not.Empty);
                Assert.That(row.LockOverlay, Is.Not.Null);
            }

            rows[1].ActionButton.onClick.Invoke();
            Assert.That(actionClicks, Is.EqualTo(1));
            Assert.That(session.Data.UnlockedWeaponStyles, Contains.Item(WeaponLegacyPathId.GakgungSunPiercer.Value));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator IncompleteAuthoredResearchBindingsThrowBeforeOwnedListenerUnbind()
        {
            var session = MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            var root = new GameObject("Incomplete Research Page");
            var presenter = root.AddComponent<WeaponResearchPresenter>();
            var page = root.AddComponent<ResearchPageView>();
            var selectors = WeaponRoster.All.Select((weapon, index) =>
                CreateSelector(root.transform, weapon, index)).ToArray();
            var rows = Enumerable.Range(0, 3).Select(index => CreateResearchRow(root.transform, index)).ToArray();
            page.Configure(selectors, CreateImage("Selected Icon", root.transform),
                CreateText("Selected Name", root.transform), CreateProgress("Mastery", root.transform), rows,
                CreateText("Feedback", root.transform));
            presenter.ConfigureView(page);
            presenter.InitializeAuthored(session, null);
            var externalClicks = 0;
            rows[0].ActionButton.onClick.AddListener(() => externalClicks++);
            rows[1] = rows[0];
            page.Configure(selectors, page.SelectedWeaponIcon, page.SelectedWeaponName, page.MasteryProgress, rows,
                page.FeedbackText);

            Assert.That(() => presenter.InitializeAuthored(session, null), Throws.TypeOf<System.InvalidOperationException>());
            rows[0].ActionButton.onClick.Invoke();
            Assert.That(externalClicks, Is.EqualTo(1));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuthoredEquippedBaseStyleCannotSaveAgainWhenInvoked()
        {
            var repository = new MemoryRepository(SaveDataV1.CreateDefaults());
            var session = MetaGameSession.EnsureExists(repository);
            var root = new GameObject("Equipped Base Research Page");
            var presenter = root.AddComponent<WeaponResearchPresenter>();
            var page = CreateAuthoredPage(root.transform, out var selectors, out var rows);
            presenter.ConfigureView(page);
            presenter.InitializeAuthored(session, null);
            presenter.SelectWeaponForTests(1);

            Assert.That(rows[0].ActionButton.interactable, Is.False);
            rows[0].ActionButton.onClick.Invoke();
            Assert.That(repository.SaveCount, Is.Zero);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CrossAliasedAuthoredButtonFailsBeforePriorListenersOrSessionChange()
        {
            var repository = new MemoryRepository(SaveDataV1.CreateDefaults());
            var session = MetaGameSession.EnsureExists(repository);
            var root = new GameObject("Cross Aliased Research Page");
            var presenter = root.AddComponent<WeaponResearchPresenter>();
            var page = CreateAuthoredPage(root.transform, out var selectors, out var rows);
            presenter.ConfigureView(page);
            presenter.InitializeAuthored(session, null);
            var ids = selectors.Select(selector => selector.GetEntityId()).Concat(rows.Select(row => row.GetEntityId())).ToArray();
            var externalClicks = 0;
            selectors[0].Button.onClick.AddListener(() => externalClicks++);
            rows[0].Configure(rows[0].StageNameText, rows[0].StatusText, rows[0].EffectText, rows[0].RequirementText,
                selectors[0].Button, rows[0].ActionText, rows[0].LockOverlay);
            page.Configure(selectors, page.SelectedWeaponIcon, page.SelectedWeaponName, page.MasteryProgress, rows, page.FeedbackText);

            Assert.That(() => presenter.InitializeAuthored(session, null), Throws.TypeOf<System.InvalidOperationException>());
            selectors[0].Button.onClick.Invoke();
            Assert.That(externalClicks, Is.EqualTo(1));
            CollectionAssert.AreEqual(ids, selectors.Select(selector => selector.GetEntityId()).Concat(rows.Select(row => row.GetEntityId())));
            Assert.That(repository.SaveCount, Is.Zero);
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReinitializeAndSequentialResearchSaveExactlyPurchaseThenLoadout()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 99999;
            data.WeaponMasteryPoints[WeaponId.GakgungShot.Value] = 10000;
            var repository = new MemoryRepository(data);
            var session = MetaGameSession.EnsureExists(repository);
            var root = new GameObject("Research Save Order");
            var presenter = root.AddComponent<WeaponResearchPresenter>();
            var page = CreateAuthoredPage(root.transform, out _, out _);
            presenter.ConfigureView(page);
            presenter.InitializeAuthored(session, null);
            presenter.InitializeAuthored(session, null);
            presenter.SelectWeaponForTests(1);

            presenter.ActivateStyleForTests(2);
            Assert.That(repository.SaveCount, Is.Zero, "Path 2 cannot purchase before path 1.");
            presenter.ActivateStyleForTests(1);
            Assert.That(repository.SaveCount, Is.EqualTo(2));
            presenter.ActivateStyleForTests(1);
            Assert.That(repository.SaveCount, Is.EqualTo(2), "Equipped path 1 must not save again.");
            presenter.ActivateStyleForTests(2);
            Assert.That(repository.SaveCount, Is.EqualTo(4));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RepeatedAuthoredInitializeBindsOneRowActionListener()
        {
            var data = SaveDataV1.CreateDefaults();
            data.UnlockedWeaponStyles.Add(WeaponLegacyPathId.GakgungSunPiercer.Value);
            var repository = new MemoryRepository(data, failSaves: true);
            var session = MetaGameSession.EnsureExists(repository);
            var root = new GameObject("Research Owned Listener Count");
            var presenter = root.AddComponent<WeaponResearchPresenter>();
            var page = CreateAuthoredPage(root.transform, out _, out var rows);
            presenter.ConfigureView(page);
            var headerRefreshes = 0;
            presenter.InitializeAuthored(session, () => headerRefreshes++);
            presenter.InitializeAuthored(session, () => headerRefreshes++);
            presenter.SelectWeaponForTests(1);

            rows[1].ActionButton.onClick.Invoke();

            Assert.That(repository.SaveCount, Is.EqualTo(1));
            Assert.That(headerRefreshes, Is.EqualTo(1));
            Object.Destroy(root);
            yield return null;
        }

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            private readonly bool failSaves;
            public int SaveCount { get; private set; }
            public MemoryRepository(SaveDataV1 data, bool failSaves = false)
            {
                stored = data.Copy();
                this.failSaves = failSaves;
            }
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data)
            {
                SaveCount++;
                if (failSaves) return new SaveResult(false, SaveError.IoFailure);
                stored = data.Copy();
                return new SaveResult(true, SaveError.None);
            }
        }

        private static ResearchPageView CreateAuthoredPage(Transform parent, out LobbyWeaponSelectorCardView[] selectors,
            out LobbyResearchRowView[] rows)
        {
            var page = parent.gameObject.AddComponent<ResearchPageView>();
            selectors = ExpectedWeaponOrder.Select((weapon, index) => CreateSelector(parent, weapon, index)).ToArray();
            rows = Enumerable.Range(0, 3).Select(index => CreateResearchRow(parent, index)).ToArray();
            page.Configure(selectors, CreateImage("Selected Icon", parent), CreateText("Selected Name", parent),
                CreateProgress("Mastery", parent), rows, CreateText("Feedback", parent));
            return page;
        }

        private static LobbyWeaponSelectorCardView CreateSelector(Transform parent, WeaponId weaponId, int index)
        {
            var root = new GameObject("Selector " + weaponId.Value, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var selector = root.AddComponent<LobbyWeaponSelectorCardView>();
            selector.Configure(CreateButton("Button", root.transform), CreateImage("Icon", root.transform),
                CreateText("Caption", root.transform), CreateText("Name", root.transform),
                CreateText("Chevron", root.transform));
            return selector;
        }

        private static LobbyResearchRowView CreateResearchRow(Transform parent, int index)
        {
            var root = new GameObject("Research Row " + index, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var overlay = new GameObject("Lock Overlay", typeof(RectTransform));
            overlay.transform.SetParent(root.transform, false);
            var row = root.AddComponent<LobbyResearchRowView>();
            row.Configure(CreateText("Stage", root.transform), CreateText("Status", root.transform),
                CreateText("Effect", root.transform), CreateText("Requirement", root.transform),
                CreateButton("Action", root.transform), CreateText("Action Text", root.transform), overlay);
            return row;
        }

        private static LobbyProgressBarView CreateProgress(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var progress = root.AddComponent<LobbyProgressBarView>();
            progress.Configure(CreateImage("Fill", root.transform), CreateText("Value", root.transform));
            return progress;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            return image;
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var image = CreateImage(name, parent);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static TMPro.TMP_Text CreateText(string name, Transform parent)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI))
                .GetComponent<TMPro.TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            return text;
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void AssertNoOverlap(params RectTransform[] rectTransforms)
        {
            var rects = rectTransforms.Select(rectTransform => (rectTransform.name, rect: WorldRect(rectTransform)))
                .ToArray();
            for (var left = 0; left < rects.Length; left++)
            for (var right = left + 1; right < rects.Length; right++)
                Assert.That(rects[left].rect.Overlaps(rects[right].rect), Is.False,
                    rects[left].name + " overlaps " + rects[right].name);
        }
    }
}
