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
        public IEnumerator PrototypeDurationAndInitialEnemiesRespectPortraitViewport()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            Assert.That(controller.UiState.Duration, Is.EqualTo(180f));

            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
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
    }
}
