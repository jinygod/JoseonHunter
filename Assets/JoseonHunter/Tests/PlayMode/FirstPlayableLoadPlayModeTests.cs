using System.Collections;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class FirstPlayableLoadPlayModeTests
    {
        [UnityTest]
        public IEnumerator SeparationKeepsThirtyAgentsDistinctAndChasing()
        {
            yield return VerifyCrowd(30);
        }

        [UnityTest]
        public IEnumerator SeparationKeepsFiftyAgentsDistinctAndChasing()
        {
            yield return VerifyCrowd(50);
        }

        [UnityTest]
        public IEnumerator SeparationKeepsOneHundredAgentsDistinctAndChasing()
        {
            yield return VerifyCrowd(100);
        }

        [UnityTest]
        public IEnumerator FullGameplayTickRunsOnlyWhileFlowIsPlaying()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            var randomState = Random.state;
            try
            {
                controller.ConfigureSeparationLoadScenarioForTests();
                controller.SpawnEnemyForSeparationTests(new Vector2(10f, 0f));
                controller.SetContactInvulnerabilityForTests(.5f);
                var beforePosition = controller.LivingEnemyPositionsForTests[0];
                var beforeInvulnerability = controller.ContactInvulnerabilityForTests;

                Assert.That(controller.Flow.TryTransition(GameFlowState.Paused), Is.True);
                Assert.That(controller.TickGameplayIfRunningForTests(.05f), Is.False);
                Assert.That(controller.LivingEnemyPositionsForTests[0], Is.EqualTo(beforePosition));
                Assert.That(controller.ContactInvulnerabilityForTests, Is.EqualTo(beforeInvulnerability));

                Assert.That(controller.Flow.TryTransition(GameFlowState.Playing), Is.True);
                Assert.That(controller.TickGameplayIfRunningForTests(.05f), Is.True);
                Assert.That(controller.LastSeparationAgentCountForTests, Is.EqualTo(1));
                Assert.That(controller.ContactInvulnerabilityForTests, Is.LessThan(beforeInvulnerability));
            }
            finally
            {
                Random.state = randomState;
                controller.Flow.ResetToPlaying();
                Time.timeScale = 1f;
            }
        }

        private static IEnumerator VerifyCrowd(int count)
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            var randomState = Random.state;
            try
            {
                controller.ConfigureSeparationLoadScenarioForTests();
                for (var index = 0; index < count; index++) controller.SpawnEnemyForSeparationTests(new Vector2(10f, 0f));
                controller.SpawnTreasureForSeparationTests(new Vector2(10f, 0f));
                var before = controller.AverageLivingEnemyDistanceToPlayerForTests;
                for (var tick = 0; tick < 80; tick++)
                    Assert.That(controller.TickGameplayIfRunningForTests(.05f), Is.True);
                var positions = controller.LivingEnemyPositionsForTests;

                Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Playing));
                Assert.That(controller.RunEndedForTests, Is.False);
                Assert.That(controller.LastSeparationAgentCountForTests, Is.EqualTo(count));
                Assert.That(controller.AverageLivingEnemyDistanceToPlayerForTests, Is.LessThan(before));
                Assert.That(positions.Count, Is.EqualTo(count));
                for (var first = 0; first < positions.Count; first++)
                for (var second = first + 1; second < positions.Count; second++)
                    Assert.That((positions[first] - positions[second]).sqrMagnitude, Is.GreaterThan(0f));
            }
            finally
            {
                Random.state = randomState;
                controller.Flow.ResetToPlaying();
                Time.timeScale = 1f;
            }
        }
    }
}
