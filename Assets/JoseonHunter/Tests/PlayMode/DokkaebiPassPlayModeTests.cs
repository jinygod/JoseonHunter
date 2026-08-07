using System.Collections;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Domain.Save;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class DokkaebiPassPlayModeTests
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
        public IEnumerator BossMilestonesUseStageSpecificIdsAndOnlyTheKingEndsTheRun()
        {
            LoadDokkaebiPass();
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            controller.AdvanceStageForTests(0f, 300f);
            Assert.That(controller.FeaturedBossContentIdForTests, Is.EqualTo("one_horn_captain"));
            Assert.That(controller.UiState.WaveAnnouncement, Does.Contain("외뿔 대장"));
            controller.DefeatMidBossesForTests();
            Assert.That(controller.RunEndedForTests, Is.False);

            controller.AdvanceStageForTests(300f, 600f);
            Assert.That(controller.FeaturedBossContentIdForTests, Is.EqualTo("iron_shield_general"));
            controller.DefeatMidBossesForTests();
            Assert.That(controller.RunEndedForTests, Is.False);

            controller.AdvanceStageForTests(600f, 900f);
            Assert.That(controller.FeaturedBossContentIdForTests, Is.EqualTo("dokkaebi_king"));
            Assert.That(controller.FeaturedBossScaleMultiplierForTests, Is.EqualTo(2.8f).Within(.001f));
            controller.DefeatFinalBossForTests();
            Assert.That(controller.RunEndedForTests, Is.True);
            Assert.That(controller.VictoryForTests, Is.True);
        }

        [UnityTest]
        public IEnumerator OpeningWindowSpawnsOnlyClubDokkaebi()
        {
            LoadDokkaebiPass();
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            controller.SetElapsedForTests(20f);
            for (var tick = 0; tick < 120; tick++) controller.TickSpawningForTests(.1f);

            Assert.That(controller.LivingNormalEnemyIdsForTests, Is.Not.Empty);
            Assert.That(controller.LivingNormalEnemyIdsForTests, Is.All.EqualTo("club_dokkaebi"));
        }

        private static void LoadDokkaebiPass()
        {
            var data = SaveDataV1.CreateDefaults();
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 400, 35)));
            data.SelectedStageId = StageId.DokkaebiPass.Value;
            data.SelectedStageDifficulty = "normal";
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Gameplay");
        }

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
