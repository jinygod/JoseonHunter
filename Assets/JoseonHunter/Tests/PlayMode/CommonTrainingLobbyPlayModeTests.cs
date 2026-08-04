using System.Collections;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Save;
using JoseonHunter.Presentation.UI.Lobby;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
            var presenter = Object.FindFirstObjectByType<CommonTrainingPresenter>(FindObjectsInactive.Include);

            presenter.SelectForTests(CommonTrainingId.Vitality);
            Assert.That(presenter.CurrentTextForTests, Is.EqualTo("현재 최대 체력 +0%"));
            Assert.That(presenter.NextTextForTests, Is.EqualTo("강화 후 최대 체력 +2%"));
            Assert.That(presenter.CostTextForTests, Is.EqualTo("필요 엽전 100"));

            presenter.PurchaseForTests();
            Assert.That(MetaGameSession.Current.Data.Coins, Is.EqualTo(400));
            presenter.ResetForTests();
            Assert.That(MetaGameSession.Current.Data.Coins, Is.EqualTo(500));
            Assert.That(MetaGameSession.Current.Data.CommonTrainingRanks[CommonTrainingId.Vitality.ToString()], Is.Zero);
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
