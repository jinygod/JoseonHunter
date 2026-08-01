using System.Collections;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class StagePacingPlayModeTests
    {
        [UnityTest]
        public IEnumerator PreviewMilestonesSpawnEachMidBossOnce()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            controller.AdvanceStageForTests(0f, 60f);
            controller.AdvanceStageForTests(60f, 61f);

            Assert.That(controller.MidBossSpawnCountForTests, Is.EqualTo(1));
            Assert.That(controller.UiState.BossAlive, Is.True);
            Assert.That(controller.UiState.WaveAnnouncement, Does.Contain("중간보스"));
        }

        [UnityTest]
        public IEnumerator PrototypeDurationAndInitialEnemiesUseTheRealPortraitViewport()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            Assert.That(controller.UiState.Duration, Is.EqualTo(180f));

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
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
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
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            controller.AdvanceStageForTests(0f, 60f);
            controller.DefeatMidBossesForTests();
            Assert.That(controller.RunEndedForTests, Is.False);

            controller.AdvanceStageForTests(179f, 180.1f);
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
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            controller.AdvanceStageForTests(0f, 120f);
            controller.AdvanceStageForTests(179f, 180.1f);

            Assert.That(controller.UiState.BossMaximumHealth, Is.EqualTo(680f));
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
    }
}
