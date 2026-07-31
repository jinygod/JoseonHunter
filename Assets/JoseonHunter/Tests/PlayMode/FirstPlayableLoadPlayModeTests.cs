using System.Collections;
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

        private static IEnumerator VerifyCrowd(int count)
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);

            for (var index = 0; index < count; index++) controller.SpawnEnemyForSeparationTests(new Vector2(4f, 0f));
            controller.SpawnTreasureForSeparationTests(new Vector2(4f, 0f));
            var before = controller.AverageLivingEnemyDistanceToPlayerForTests;
            for (var tick = 0; tick < 80; tick++) controller.UpdateEnemiesForSeparationTests(.05f);
            var positions = controller.LivingEnemyPositionsForTests;

            Assert.That(controller.LastSeparationAgentCountForTests, Is.EqualTo(count));
            Assert.That(controller.AverageLivingEnemyDistanceToPlayerForTests, Is.LessThan(before));
            Assert.That(positions.Count, Is.EqualTo(count));
            for (var first = 0; first < positions.Count; first++)
            for (var second = first + 1; second < positions.Count; second++)
                Assert.That((positions[first] - positions[second]).sqrMagnitude, Is.GreaterThan(0f));
        }
    }
}
