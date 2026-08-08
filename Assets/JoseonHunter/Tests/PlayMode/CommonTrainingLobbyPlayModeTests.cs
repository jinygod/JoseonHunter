using System.Collections;
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
    public sealed class CommonTrainingLobbyPlayModeTests
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
        public IEnumerator TrainingShowsPreviewPurchasesAndFullyRefunds()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 500;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindAnyObjectByType<CommonTrainingPresenter>(FindObjectsInactive.Include);

            Assert.That(presenter.transform.Find("Training Title").GetComponent<TMPro.TMP_Text>().text,
                Is.EqualTo("수련"));
            Assert.That(presenter.transform.Find("Training Description").GetComponent<TMPro.TMP_Text>().text,
                Is.EqualTo("수련 효과는 모든 출전에 적용되며, 항목별 최대치는 15%입니다."));

            presenter.SelectForTests(CommonTrainingId.Vitality);
            Assert.That(presenter.CurrentTextForTests, Is.EqualTo("현재 최대 체력 +0%"));
            Assert.That(presenter.NextTextForTests, Is.EqualTo("강화 후 최대 체력 +2%"));
            Assert.That(presenter.CostTextForTests, Is.EqualTo("필요 엽전 100 · 강화 후 400"));
            Canvas.ForceUpdateCanvases();
            var purchase = presenter.transform.Find("Training Content Panel/Purchase Training").GetComponent<Button>();
            Assert.That(purchase.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(64f));
            Assert.That(purchase.GetComponentInChildren<TMPro.TMP_Text>().fontSize, Is.GreaterThanOrEqualTo(18f));
            var detail = presenter.transform.Find("Training Content Panel/Training Summary Backplate")
                .GetComponent<Image>();
            Assert.That(detail.sprite.name, Is.EqualTo("content_backplate"));

            presenter.PurchaseForTests();
            Assert.That(MetaGameSession.Current.Data.Coins, Is.EqualTo(400));
            presenter.ResetForTests();
            Assert.That(MetaGameSession.Current.Data.Coins, Is.EqualTo(500));
            Assert.That(MetaGameSession.Current.Data.CommonTrainingRanks[CommonTrainingId.Vitality.ToString()], Is.Zero);
        }

        [UnityTest]
        public IEnumerator TrainingShowsTotalCapacityRankTwentyAndDiminishingPreview()
        {
            var data = SaveDataV1.CreateDefaults();
            data.AccountExperience = AccountProgression.TotalExperienceForLevel(7);
            data.Coins = 5000;
            data.CommonTrainingRanks[CommonTrainingId.Vitality.ToString()] = 8;
            data.CommonTrainingRanks[CommonTrainingId.Power.ToString()] = 4;
            data.CommonTrainingRanks[CommonTrainingId.Footwork.ToString()] = 4;
            data.CommonTrainingRanks[CommonTrainingId.Learning.ToString()] = 4;
            data.CommonTrainingRanks[CommonTrainingId.Guard.ToString()] = 2;
            data.CommonTrainingRanks[CommonTrainingId.Resonance.ToString()] = 2;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindAnyObjectByType<CommonTrainingPresenter>(FindObjectsInactive.Include);

            presenter.SelectForTests(CommonTrainingId.Vitality);

            Assert.That(presenter.CapacityTextForTests, Is.EqualTo("총 수련 24/35 · 계정 7레벨 한도"));
            Assert.That(presenter.CurrentTextForTests, Is.EqualTo("현재 최대 체력 +11.8%"));
            Assert.That(presenter.NextTextForTests, Is.EqualTo("강화 후 최대 체력 +12.4%"));
            Assert.That(presenter.CostTextForTests, Is.EqualTo("필요 엽전 868 · 강화 후 4,132"));
            Assert.That(presenter.ButtonTextForTests(CommonTrainingId.Vitality), Is.EqualTo("활력\n8/20"));
        }

        [UnityTest]
        public IEnumerator TrainingDisablesPurchaseAtAccountCapacityAndNamesNextUnlockLevel()
        {
            var data = SaveDataV1.CreateDefaults();
            data.AccountExperience = AccountProgression.TotalExperienceForLevel(7);
            data.Coins = 10000;
            data.CommonTrainingRanks[CommonTrainingId.Vitality.ToString()] = 20;
            data.CommonTrainingRanks[CommonTrainingId.Power.ToString()] = 15;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindAnyObjectByType<CommonTrainingPresenter>(FindObjectsInactive.Include);

            presenter.SelectForTests(CommonTrainingId.Guard);

            Assert.That(presenter.PurchaseInteractableForTests, Is.False);
            Assert.That(presenter.FeedbackTextForTests, Is.EqualTo("계정 레벨 8에서 추가 수련이 열립니다."));
        }

        [UnityTest]
        public IEnumerator TrainingNamesThePermanentMaximumAtOneHundredTotalRanks()
        {
            var data = SaveDataV1.CreateDefaults();
            data.AccountExperience = AccountProgression.TotalExperienceForLevel(20);
            data.Coins = 10000;
            data.CommonTrainingRanks[CommonTrainingId.Vitality.ToString()] = 20;
            data.CommonTrainingRanks[CommonTrainingId.Power.ToString()] = 20;
            data.CommonTrainingRanks[CommonTrainingId.Footwork.ToString()] = 20;
            data.CommonTrainingRanks[CommonTrainingId.Learning.ToString()] = 20;
            data.CommonTrainingRanks[CommonTrainingId.Guard.ToString()] = 20;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindAnyObjectByType<CommonTrainingPresenter>(FindObjectsInactive.Include);

            presenter.SelectForTests(CommonTrainingId.Resonance);

            Assert.That(presenter.PurchaseInteractableForTests, Is.False);
            Assert.That(presenter.FeedbackTextForTests, Is.EqualTo("총 수련 최대치에 도달했습니다."));
        }

        [UnityTest]
        public IEnumerator TrainingUsesSixSmallStatCardsAndSeparateSummaryActions()
        {
            MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
            SceneManager.LoadScene("Lobby");
            yield return null;
            Object.FindAnyObjectByType<CommonTrainingPresenter>(FindObjectsInactive.Include).gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();

            var cards = GameObject.Find("Training Grid").GetComponentsInChildren<Button>(true);
            Assert.That(cards, Has.Length.EqualTo(6));
            Assert.That(cards, Has.All.Matches<Button>(button =>
                button.GetComponent<Image>().sprite.name == "small_item_frame"));
            Assert.That(GameObject.Find("Training Summary Backplate").GetComponent<Image>().sprite.name,
                Is.EqualTo("content_backplate"));
            var purchase = GameObject.Find("Purchase Training").GetComponent<Button>();
            var reset = GameObject.Find("Reset Training").GetComponent<Button>();
            Assert.That(purchase.GetComponent<Image>().sprite.name, Is.EqualTo("primary_red_button"));
            Assert.That(reset.GetComponent<Image>().sprite.name, Is.EqualTo("secondary_dark_button"));
            var contentPanel = GameObject.Find("Training Content Panel").GetComponent<RectTransform>();
            Assert.That(GameObject.Find("Training Grid").transform.IsChildOf(contentPanel), Is.True);
            Assert.That(GameObject.Find("Training Summary Backplate").transform.parent, Is.SameAs(contentPanel));
            Assert.That(purchase.transform.parent, Is.SameAs(contentPanel));
            Assert.That(reset.transform.parent, Is.SameAs(contentPanel));
            Assert.That(GameObject.Find("Training Feedback").transform.parent, Is.SameAs(contentPanel));
            AssertInside(contentPanel,
                purchase.GetComponent<RectTransform>(), reset.GetComponent<RectTransform>());
            Assert.That(WorldRect(purchase.GetComponent<RectTransform>()).Overlaps(
                WorldRect(reset.GetComponent<RectTransform>())), Is.False);

            var purchaseBefore = purchase.transform.position;
            var resetBefore = reset.transform.position;
            var panelBefore = contentPanel.position;
            contentPanel.anchoredPosition += new Vector2(37f, -19f);
            Canvas.ForceUpdateCanvases();
            var panelDelta = (Vector2)contentPanel.position - (Vector2)panelBefore;
            Assert.That(Vector2.Distance(purchase.transform.position, (Vector2)purchaseBefore + panelDelta),
                Is.LessThan(.01f));
            Assert.That(Vector2.Distance(reset.transform.position, (Vector2)resetBefore + panelDelta),
                Is.LessThan(.01f));
        }

        [UnityTest]
        public IEnumerator RepeatedInitializePreservesOneActiveTrainingContentHierarchyAndListeners()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 500;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindAnyObjectByType<CommonTrainingPresenter>(FindObjectsInactive.Include);
            var panel = presenter.transform.Find("Training Content Panel");
            var grid = panel.Find("Training Grid");
            var summary = panel.Find("Training Summary Backplate");
            var purchase = panel.Find("Purchase Training").GetComponent<Button>();
            var reset = panel.Find("Reset Training").GetComponent<Button>();

            presenter.Initialize(MetaGameSession.Current, null);
            presenter.Initialize(MetaGameSession.Current, null);

            Assert.That(presenter.transform.Find("Training Content Panel"), Is.SameAs(panel));
            Assert.That(panel.Find("Training Grid"), Is.SameAs(grid));
            Assert.That(panel.Find("Training Summary Backplate"), Is.SameAs(summary));
            Assert.That(panel.Find("Purchase Training").GetComponent<Button>(), Is.SameAs(purchase));
            Assert.That(panel.Find("Reset Training").GetComponent<Button>(), Is.SameAs(reset));
            Assert.That(DirectChildCount(presenter.transform, "Training Content Panel"), Is.EqualTo(1));
            Assert.That(DirectChildCount(panel, "Training Grid"), Is.EqualTo(1));
            Assert.That(DirectChildCount(panel, "Training Summary Backplate"), Is.EqualTo(1));
            Assert.That(DirectChildCount(panel, "Purchase Training"), Is.EqualTo(1));
            Assert.That(DirectChildCount(panel, "Reset Training"), Is.EqualTo(1));

            purchase.onClick.Invoke();
            Assert.That(MetaGameSession.Current.Data.Coins, Is.EqualTo(400));
        }

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data) { stored = data.Copy(); return new SaveResult(true, SaveError.None); }
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static int DirectChildCount(Transform parent, string name)
        {
            var count = 0;
            foreach (Transform child in parent)
                if (child.name == name) count++;
            return count;
        }

        private static void AssertInside(RectTransform container, params RectTransform[] children)
        {
            var bounds = WorldRect(container);
            foreach (var child in children)
            {
                var rect = WorldRect(child);
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(bounds.xMin));
                Assert.That(rect.xMax, Is.LessThanOrEqualTo(bounds.xMax));
                Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(bounds.yMin));
                Assert.That(rect.yMax, Is.LessThanOrEqualTo(bounds.yMax));
            }
        }
    }
}
