using System.Collections;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Domain.Save;
using JoseonHunter.Runtime.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class StagePacingPlayModeTests
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
        public IEnumerator FifteenMinuteMilestonesSpawnEachMidBossOnce()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            controller.AdvanceStageForTests(0f, 300f);
            controller.AdvanceStageForTests(300f, 301f);

            Assert.That(controller.MidBossSpawnCountForTests, Is.EqualTo(1));
            Assert.That(controller.UiState.BossAlive, Is.True);
            Assert.That(controller.UiState.WaveAnnouncement, Does.Contain("중간보스"));
        }

        [UnityTest]
        public IEnumerator PrototypeDurationAndInitialEnemiesUseTheRealPortraitViewport()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            Assert.That(controller.UiState.Duration, Is.EqualTo(900f));

            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            Assert.That(camera.orthographicSize, Is.EqualTo(18f));
            var originalAspect = camera.aspect;
            var randomState = Random.state;
            try
            {
                foreach (var aspect in new[] { 9f / 16f, 9f / 19.5f, 1170f / 2532f })
                {
                    camera.aspect = aspect;
                    controller.SpawnEnemyAtCurrentViewportForTests();
                    Assert.That(ViewportBounds(camera).Contains(controller.LastSpawnPositionForTests), Is.False);
                }
            }
            finally
            {
                camera.aspect = originalAspect;
                Random.state = randomState;
            }
        }

        [UnityTest]
        public IEnumerator SpawnedRendererBoundsStayOutsideActualViewportForEverySideAndRank()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var camera = Camera.main;
            Assert.That(controller, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);

            var randomState = Random.state;
            try
            {
                foreach (var side in new[] { 0, 1, 2, 3 })
                {
                    foreach (var scenario in new[]
                             {
                                 new SpawnScenario(false, 0, false, .75f),
                                 new SpawnScenario(false, 0, true, 1.5f),
                                 new SpawnScenario(false, 1, false, .75f),
                                 new SpawnScenario(false, 2, false, 1.5f),
                                 new SpawnScenario(true, 0, false, .75f)
                             })
                    {
                        controller.ConfigureViewportSpawnForTests(side, .5f, scenario.Margin, scenario.ForceElite);
                        controller.SpawnEnemyForViewportClearanceTests(scenario.IsBoss, scenario.MidBossTier);

                        var view = ViewportBounds(camera);
                        AssertRendererIsOutside(view, side, controller.LastSpawnRendererBoundsForTests);
                        AssertRootRemainsOutside(view, side, scenario.Margin, controller.LastSpawnRootPositionForTests);
                        if (scenario.IsBoss)
                            Assert.That(controller.LastSpawnScaleForTests, Is.EqualTo(.78f * 2.3f).Within(.001f));
                        else if (scenario.MidBossTier == 1)
                            Assert.That(controller.LastSpawnScaleForTests, Is.EqualTo(.78f * 1.7f).Within(.001f));
                        else if (scenario.MidBossTier == 2)
                            Assert.That(controller.LastSpawnScaleForTests, Is.EqualTo(.78f * 1.9f).Within(.001f));
                    }
                }
            }
            finally
            {
                controller.ClearViewportSpawnForTests();
                Random.state = randomState;
            }
        }

        [UnityTest]
        public IEnumerator MidBossDeathDoesNotEndRunButFinalBossDeathWins()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            controller.AdvanceStageForTests(0f, 300f);
            controller.DefeatMidBossesForTests();
            Assert.That(controller.RunEndedForTests, Is.False);

            controller.AdvanceStageForTests(899f, 900f);
            Assert.That(controller.FinalBossSpawnCountForTests, Is.EqualTo(1));
            Assert.That(controller.RunEndedForTests, Is.False);

            controller.DefeatFinalBossForTests();
            Assert.That(controller.RunEndedForTests, Is.True);
            Assert.That(controller.VictoryForTests, Is.True);
        }

        [UnityTest]
        public IEnumerator FinalBossHealthTakesHudPriorityOverSurvivingMidBosses()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            controller.AdvanceStageForTests(0f, 600f);
            controller.AdvanceStageForTests(899f, 900f);

            Assert.That(controller.UiState.BossMaximumHealth, Is.EqualTo(6000f));
        }

        [UnityTest]
        public IEnumerator OmenSelectionScalesSpawnedEnemyAndDensityWithoutBreakingTheMobileCap()
        {
            var data = SaveDataV1.CreateDefaults();
            data.StageClearRecords.Add(StageClearRecordData.From(StageClearRecord.Victory(
                new StageSelection(StageId.GwigokField, StageDifficulty.Normal), 900f, 500, 35)));
            data.SelectedStageId = StageId.GwigokField.Value;
            data.SelectedStageDifficulty = "omen";
            MetaGameSession.EnsureExists(new MemoryRepository(data));
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();

            controller.SpawnEnemyForLifecycleTests();

            Assert.That(controller.ActiveStageDifficultyForTests, Is.EqualTo(StageDifficulty.Omen));
            Assert.That(controller.LastSpawnHealthForTests, Is.EqualTo(18f * 1.35f).Within(.01f));
            Assert.That(controller.LastSpawnContactDamageForTests, Is.EqualTo(10f * 1.15f).Within(.01f));
            Assert.That(controller.ActiveEnemyCapForTests, Is.LessThanOrEqualTo(140));
            Assert.That(controller.NextSpawnIntervalForTests, Is.LessThan(.22f));
        }

        private static Rect ViewportBounds(Camera camera)
        {
            var bottomLeft = camera.ViewportToWorldPoint(Vector3.zero);
            var topRight = camera.ViewportToWorldPoint(Vector3.one);
            return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
        }

        private static void AssertRendererIsOutside(Rect view, int side, Bounds bounds)
        {
            if (side == 0) Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(view.yMax));
            if (side == 1) Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(view.xMax));
            if (side == 2) Assert.That(bounds.max.y, Is.LessThanOrEqualTo(view.yMin));
            if (side == 3) Assert.That(bounds.max.x, Is.LessThanOrEqualTo(view.xMin));
        }

        private static void AssertRootRemainsOutside(Rect view, int side, float margin, Vector2 root)
        {
            if (side == 0) Assert.That(root.y, Is.GreaterThanOrEqualTo(view.yMax + margin));
            if (side == 1) Assert.That(root.x, Is.GreaterThanOrEqualTo(view.xMax + margin));
            if (side == 2) Assert.That(root.y, Is.LessThanOrEqualTo(view.yMin - margin));
            if (side == 3) Assert.That(root.x, Is.LessThanOrEqualTo(view.xMin - margin));
        }

        private readonly struct SpawnScenario
        {
            public SpawnScenario(bool isBoss, int midBossTier, bool forceElite, float margin)
            {
                IsBoss = isBoss;
                MidBossTier = midBossTier;
                ForceElite = forceElite;
                Margin = margin;
            }

            public bool IsBoss { get; }
            public int MidBossTier { get; }
            public bool ForceElite { get; }
            public float Margin { get; }
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
