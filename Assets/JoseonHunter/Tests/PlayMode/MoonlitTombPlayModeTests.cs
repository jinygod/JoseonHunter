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
    public sealed class MoonlitTombPlayModeTests
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
        public IEnumerator BossMilestonesUseMoonlitIdsAndOnlyTheQueenEndsTheRun()
        {
            LoadMoonlitTomb();
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            controller.AdvanceStageForTests(0f, 300f);
            Assert.That(controller.FeaturedBossContentIdForTests, Is.EqualTo("royal_guard_wraith"));
            controller.DefeatMidBossesForTests();
            Assert.That(controller.RunEndedForTests, Is.False);

            controller.AdvanceStageForTests(300f, 600f);
            Assert.That(controller.FeaturedBossContentIdForTests, Is.EqualTo("eclipse_priest"));
            controller.DefeatMidBossesForTests();
            Assert.That(controller.RunEndedForTests, Is.False);

            controller.AdvanceStageForTests(600f, 900f);
            Assert.That(controller.FeaturedBossContentIdForTests, Is.EqualTo("eclipse_queen"));
            Assert.That(controller.FeaturedBossScaleMultiplierForTests, Is.EqualTo(2.8f).Within(.001f));
            controller.DefeatFinalBossForTests();
            Assert.That(controller.RunEndedForTests, Is.True);
            Assert.That(controller.VictoryForTests, Is.True);
        }

        [UnityTest]
        public IEnumerator OpeningWindowSpawnsOnlyTombAttendants()
        {
            LoadMoonlitTomb();
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            controller.SetElapsedForTests(20f);
            for (var tick = 0; tick < 120; tick++) controller.TickSpawningForTests(.1f);

            Assert.That(controller.LivingNormalEnemyIdsForTests, Is.Not.Empty);
            Assert.That(controller.LivingNormalEnemyIdsForTests, Is.All.EqualTo("tomb_attendant"));
        }

        [UnityTest]
        public IEnumerator RangedEnemyCannotAimOffscreenButFiresWhenVisible()
        {
            LoadMoonlitTomb();
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);
            controller.ConfigureSeparationLoadScenarioForTests();

            controller.SpawnSpecialEnemyForTests("tomb_archer_ghost", new Vector2(50f, 50f));
            for (var tick = 0; tick < 35; tick++) controller.UpdateEnemiesForTests(.1f);
            Assert.That(controller.ActiveStageProjectileCountForTests, Is.Zero);

            controller.SpawnSpecialEnemyForTests("tomb_archer_ghost", new Vector2(2f, 0f));
            for (var tick = 0; tick < 35; tick++) controller.UpdateEnemiesForTests(.1f);
            Assert.That(controller.ActiveStageProjectileCountForTests, Is.GreaterThan(0));
            Assert.That(controller.StageProjectileCapacityForTests, Is.EqualTo(48));
        }

        [UnityTest]
        public IEnumerator CurseFieldExpiresAndResetClearsEveryStageAttack()
        {
            LoadMoonlitTomb();
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);
            controller.ConfigureSeparationLoadScenarioForTests();
            controller.SpawnSpecialEnemyForTests("curse_shaman", new Vector2(2f, 0f));

            for (var tick = 0; tick < 45; tick++) controller.UpdateEnemiesForTests(.1f);
            Assert.That(controller.ActiveStageHazardCountForTests, Is.GreaterThan(0));
            Assert.That(controller.StageHazardCapacityForTests, Is.EqualTo(24));

            controller.ResetRunForTests();
            Assert.That(controller.ActiveStageHazardCountForTests, Is.Zero);
            Assert.That(controller.ActiveStageProjectileCountForTests, Is.Zero);
        }

        private static void LoadMoonlitTomb()
        {
            var data = SaveDataV1.CreateDefaults();
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 400, 35)));
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.DokkaebiPass, StageDifficulty.Normal), 900f, 500, 35)));
            data.SelectedStageId = StageId.MoonlitTomb.Value;
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
