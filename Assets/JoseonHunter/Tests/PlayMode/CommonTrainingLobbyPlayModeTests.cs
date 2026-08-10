using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Presentation.UI.Lobby.Views;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEditor;

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
            var page = presenter.GetComponent<TrainingPageView>();

            Assert.That(page, Is.Not.Null);
            var pageText = page.GetComponentsInChildren<TMPro.TMP_Text>(true);
            Assert.That(pageText.Count(text => text.text == "수련"), Is.EqualTo(1),
                "The authored page header is the sole training title.");
            Assert.That(page.GetComponentsInChildren<Transform>(true)
                .Any(item => item.name is "Training Title" or "Training Description"), Is.False,
                "Legacy title and description controls must not overlap the modular page header.");

            presenter.SelectForTests(CommonTrainingId.Vitality);
            Assert.That(presenter.CurrentTextForTests, Is.EqualTo("현재 최대 체력 +0%"));
            Assert.That(presenter.NextTextForTests, Is.EqualTo("강화 후 최대 체력 +2%"));
            Assert.That(presenter.CostTextForTests, Is.EqualTo("필요 엽전 100 · 강화 후 400"));
            Canvas.ForceUpdateCanvases();
            var purchase = page.PurchaseButton;
            Assert.That(purchase.GetComponentInChildren<TMPro.TMP_Text>().fontSize, Is.GreaterThanOrEqualTo(18f));

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
            Assert.That(presenter.ButtonTextForTests(CommonTrainingId.Vitality), Is.EqualTo("8 / 20"));
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
            var originalWidth = Screen.width;
            var originalHeight = Screen.height;
            Screen.SetResolution(720, 1280, false);
            try
            {
                MetaGameSession.EnsureExists(new MemoryRepository(SaveDataV1.CreateDefaults()));
                SceneManager.LoadScene("Lobby");
                yield return null;
                var presenter = Object.FindAnyObjectByType<CommonTrainingPresenter>(FindObjectsInactive.Include);
                presenter.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                var page = presenter.GetComponent<TrainingPageView>();

                Assert.That(page, Is.Not.Null);
                Assert.That(page.HasRequiredBindings, Is.True);
                Assert.That(page.Rows, Has.Length.EqualTo(6));
                presenter.SelectForTests(CommonTrainingId.Power);
                for (var index = 0; index < page.Rows.Length; index++)
                {
                    var row = page.Rows[index];
                    Assert.That(row, Is.Not.Null);
                    Assert.That(row.TrainingId, Is.EqualTo((CommonTrainingId)index));
                    Assert.That(row.Progress.HasRequiredBindings, Is.True);
                    var rowImage = row.Button.GetComponent<Image>();
                    Assert.That(rowImage.sprite, Is.Not.Null, row.name);
                    Assert.That(rowImage.sprite.name, Is.EqualTo("small_item_frame"));
                    Assert.That(row.GetComponent<RectTransform>().rect.height,
                        Is.GreaterThanOrEqualTo(64f), row.name);
                }
                Assert.That(page.Rows[(int)CommonTrainingId.Power].Button.colors.normalColor,
                    Is.Not.EqualTo(page.Rows[(int)CommonTrainingId.Vitality].Button.colors.normalColor),
                    "The selected training row needs a tint highlight without changing its semantic frame.");

                var purchase = page.PurchaseButton;
                var reset = page.ResetButton;
                Assert.That(purchase.GetComponent<Image>().sprite.name, Is.EqualTo("primary_red_button"));
                Assert.That(reset.GetComponent<Image>().sprite.name, Is.EqualTo("secondary_dark_button"));
                Assert.That(purchase.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(64f));
                Assert.That(reset.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(64f));

                var touchTargets = new RectTransform[page.Rows.Length + 2];
                for (var index = 0; index < page.Rows.Length; index++)
                    touchTargets[index] = page.Rows[index].GetComponent<RectTransform>();
                touchTargets[^2] = purchase.GetComponent<RectTransform>();
                touchTargets[^1] = reset.GetComponent<RectTransform>();
                AssertNoOverlap(touchTargets);
            }
            finally
            {
                Screen.SetResolution(originalWidth, originalHeight, false);
            }
        }

        [UnityTest]
        public IEnumerator AuthoredTrainingPageBindsSixOrderedRowsWithoutReplacingExternalListeners()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 500;
            var session = MetaGameSession.EnsureExists(new MemoryRepository(data));
            var root = new GameObject("Authored Training Page");
            var presenter = root.AddComponent<CommonTrainingPresenter>();
            var page = root.AddComponent<TrainingPageView>();
            var rows = new LobbyTrainingRowView[6];
            for (var index = 0; index < rows.Length; index++)
            {
                rows[index] = CreateAuthoredRow(root.transform, (CommonTrainingId)index);
            }
            var current = CreateText("Current Effect", root.transform);
            var next = CreateText("Next Effect", root.transform);
            var cost = CreateText("Cost", root.transform);
            var capacity = CreateText("Capacity", root.transform);
            var feedback = CreateText("Feedback", root.transform);
            var purchase = CreateButton("Purchase", root.transform);
            var reset = CreateButton("Reset", root.transform);
            var externalPurchaseClicks = 0;
            purchase.onClick.AddListener(() => externalPurchaseClicks++);
            var iconSet = AssetDatabase.LoadAssetAtPath<LobbyTrainingIconSet>(
                "Assets/JoseonHunter/Prefabs/UI/Lobby/TrainingIconSet.asset");
            page.Configure(rows, iconSet, current, next, cost, capacity, purchase, reset, feedback);
            presenter.ConfigureView(page);

            var rowIds = new int[rows.Length];
            for (var index = 0; index < rows.Length; index++) rowIds[index] = rows[index].GetEntityId().GetHashCode();
            presenter.InitializeAuthored(session, null);
            presenter.InitializeAuthored(session, null);
            presenter.SelectForTests(CommonTrainingId.Vitality);

            Assert.That(page.HasRequiredBindings, Is.True);
            Assert.That(page.Rows, Has.Length.EqualTo(6));
            Assert.That(page.Rows[0].TrainingId, Is.EqualTo(CommonTrainingId.Vitality));
            Assert.That(page.Rows[1].TrainingId, Is.EqualTo(CommonTrainingId.Power));
            Assert.That(page.Rows[2].TrainingId, Is.EqualTo(CommonTrainingId.Footwork));
            Assert.That(page.Rows[3].TrainingId, Is.EqualTo(CommonTrainingId.Learning));
            Assert.That(page.Rows[4].TrainingId, Is.EqualTo(CommonTrainingId.Guard));
            Assert.That(page.Rows[5].TrainingId, Is.EqualTo(CommonTrainingId.Resonance));
            for (var index = 0; index < rows.Length; index++)
            {
                Assert.That(rows[index].GetEntityId().GetHashCode(), Is.EqualTo(rowIds[index]));
                Assert.That(rows[index].IconImage.sprite, Is.SameAs(iconSet.Icons[index]));
                Assert.That(rows[index].NameText.text, Is.Not.Empty);
                Assert.That(rows[index].RankText.text, Is.EqualTo("0 / 20"));
            }

            purchase.onClick.Invoke();
            Assert.That(externalPurchaseClicks, Is.EqualTo(1));
            Assert.That(session.Data.Coins, Is.EqualTo(400));
            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InvalidDuplicateAuthoredBindingsThrowBeforeUnbindingExistingListeners()
        {
            var data = SaveDataV1.CreateDefaults(); data.Coins = 500;
            var session = MetaGameSession.EnsureExists(new MemoryRepository(data));
            var root = new GameObject("Duplicate Training Page");
            var presenter = root.AddComponent<CommonTrainingPresenter>();
            var page = root.AddComponent<TrainingPageView>();
            var rows = new LobbyTrainingRowView[6];
            for (var index = 0; index < rows.Length; index++) rows[index] = CreateAuthoredRow(root.transform, (CommonTrainingId)index);
            var iconSet = AssetDatabase.LoadAssetAtPath<LobbyTrainingIconSet>("Assets/JoseonHunter/Prefabs/UI/Lobby/TrainingIconSet.asset");
            var purchase = CreateButton("Purchase", root.transform); var reset = CreateButton("Reset", root.transform);
            page.Configure(rows, iconSet, CreateText("Current", root.transform), CreateText("Next", root.transform),
                CreateText("Cost", root.transform), CreateText("Capacity", root.transform), purchase, reset, CreateText("Feedback", root.transform));
            presenter.ConfigureView(page); presenter.InitializeAuthored(session, null);
            var id = rows[0].GetEntityId(); var external = 0; purchase.onClick.AddListener(() => external++);
            rows[1] = rows[0];
            page.Configure(rows, iconSet, page.CurrentEffectText, page.NextEffectText, page.CostText, page.CapacityText, purchase, reset, page.FeedbackText);
            Assert.That(() => presenter.InitializeAuthored(session, null), Throws.TypeOf<System.InvalidOperationException>());
            Assert.That(rows[0].GetEntityId(), Is.EqualTo(id));
            purchase.onClick.Invoke();
            Assert.That(external, Is.EqualTo(1));
            Assert.That(session.Data.Coins, Is.EqualTo(400));
            Object.Destroy(root); yield return null;
        }

        [UnityTest]
        public IEnumerator RepeatedAuthoredInitializePreservesBoundRowsAndListeners()
        {
            var data = SaveDataV1.CreateDefaults();
            data.Coins = 500;
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Lobby");
            yield return null;
            var presenter = Object.FindAnyObjectByType<CommonTrainingPresenter>(FindObjectsInactive.Include);
            var page = presenter.GetComponent<TrainingPageView>();
            Assert.That(page, Is.Not.Null);
            Assert.That(page.HasRequiredBindings, Is.True);
            var rows = page.Rows;
            var purchase = page.PurchaseButton;
            var reset = page.ResetButton;

            presenter.ConfigureView(page);
            presenter.InitializeAuthored(MetaGameSession.Current, null);
            presenter.InitializeAuthored(MetaGameSession.Current, null);

            Assert.That(page.Rows, Is.SameAs(rows));
            Assert.That(page.PurchaseButton, Is.SameAs(purchase));
            Assert.That(page.ResetButton, Is.SameAs(reset));

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

        private static LobbyTrainingRowView CreateAuthoredRow(Transform parent, CommonTrainingId id)
        {
            var root = new GameObject("Training Row " + id, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var button = CreateButton("Button", root.transform);
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(root.transform, false);
            var name = CreateText("Name", root.transform);
            var rank = CreateText("Rank", root.transform);
            var progressRoot = new GameObject("Progress", typeof(RectTransform));
            progressRoot.transform.SetParent(root.transform, false);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(progressRoot.transform, false);
            var value = CreateText("Value", progressRoot.transform);
            var progress = progressRoot.AddComponent<LobbyProgressBarView>();
            progress.Configure(fill, value);
            var row = root.AddComponent<LobbyTrainingRowView>();
            row.Configure(id, button, name, icon, rank, progress);
            return row;
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
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

        private static void AssertNoOverlap(params RectTransform[] rects)
        {
            for (var index = 0; index < rects.Length; index++)
            {
                for (var other = index + 1; other < rects.Length; other++)
                    Assert.That(WorldRect(rects[index]).Overlaps(WorldRect(rects[other])), Is.False,
                        $"{rects[index].name} overlaps {rects[other].name}.");
            }
        }
    }
}
