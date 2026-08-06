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
            var purchase = presenter.transform.Find("Purchase Training").GetComponent<Button>();
            Assert.That(purchase.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(64f));
            Assert.That(purchase.GetComponentInChildren<TMPro.TMP_Text>().fontSize, Is.GreaterThanOrEqualTo(18f));
            var detail = presenter.transform.Find("Training Detail").GetComponent<Image>();
            Assert.That(detail.color.maxColorComponent, Is.LessThan(.5f));
            Assert.That(detail.color.a, Is.GreaterThan(.95f));

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

        private sealed class MemoryRepository : ISaveRepository
        {
            private SaveDataV1 stored;
            public MemoryRepository(SaveDataV1 data) => stored = data.Copy();
            public LoadResult Load() => new LoadResult(stored.Copy(), LoadSource.Current, SaveError.None);
            public SaveResult Save(SaveDataV1 data) { stored = data.Copy(); return new SaveResult(true, SaveError.None); }
        }
    }
}
