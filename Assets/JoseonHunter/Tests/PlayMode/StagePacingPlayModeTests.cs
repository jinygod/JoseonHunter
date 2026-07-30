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

            controller.AdvanceStageForTests(0f, 17f);
            controller.AdvanceStageForTests(17f, 18f);

            Assert.That(controller.MidBossSpawnCountForTests, Is.EqualTo(1));
            Assert.That(controller.UiState.BossAlive, Is.True);
            Assert.That(controller.UiState.WaveAnnouncement, Does.Contain("중간보스"));
        }

        [UnityTest]
        public IEnumerator MidBossDeathDoesNotEndRunButFinalBossDeathWins()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            controller.AdvanceStageForTests(0f, 17f);
            controller.DefeatMidBossesForTests();
            Assert.That(controller.RunEndedForTests, Is.False);

            controller.AdvanceStageForTests(49f, 50.1f);
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

            controller.AdvanceStageForTests(0f, 34f);
            controller.AdvanceStageForTests(49f, 50.1f);

            Assert.That(controller.UiState.BossMaximumHealth, Is.EqualTo(680f));
        }
    }
}
