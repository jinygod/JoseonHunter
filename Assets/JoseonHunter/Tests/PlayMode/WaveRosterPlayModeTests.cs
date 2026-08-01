using System.Collections;
using System.Linq;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class WaveRosterPlayModeTests
    {
        [UnityTest]
        public IEnumerator FirstWaveSpawnsOnlyRatsAndBuildsVisiblePressure()
        {
            var controller = LoadGameplay();
            yield return null;
            controller.SetElapsedForTests(20f);

            TickSpawning(controller, 10f);

            Assert.That(controller.LivingNormalEnemyIdsForTests, Is.Not.Empty);
            Assert.That(controller.LivingNormalEnemyIdsForTests, Is.All.EqualTo("plague_rat"));
            Assert.That(controller.LivingNormalEnemyIdsForTests.Count, Is.InRange(60, 72));
        }

        [UnityTest]
        public IEnumerator SecondWaveContainsRatsSpiritsAndASpiritPack()
        {
            var controller = LoadGameplay();
            yield return null;
            controller.SetElapsedForTests(60f);

            TickSpawning(controller, 20f);

            Assert.That(controller.LivingNormalEnemyIdsForTests, Does.Contain("plague_rat"));
            Assert.That(controller.LivingNormalEnemyIdsForTests, Does.Contain("vengeful_spirit"));
            Assert.That(controller.PackSpawnCountForTests, Is.GreaterThan(0));
            Assert.That(controller.LivingNormalEnemyIdsForTests.Count, Is.LessThanOrEqualTo(104));
        }

        [UnityTest]
        public IEnumerator PeakContinuousAndPackSpawnsShareTheOneHundredFortyEnemyLimit()
        {
            var controller = LoadGameplay();
            yield return null;
            controller.SetElapsedForTests(145f);

            TickSpawning(controller, 30f);

            Assert.That(controller.LivingNormalEnemyIdsForTests.Count, Is.InRange(130, 140));
            Assert.That(controller.LivingNormalEnemyIdsForTests.Count, Is.LessThanOrEqualTo(140));
            Assert.That(controller.LivingNormalEnemyIdsForTests.Distinct().Count(), Is.GreaterThanOrEqualTo(2));
        }

        private static FirstPlayableController LoadGameplay()
        {
            SceneManager.LoadScene("Gameplay");
            return Object.FindAnyObjectByType<FirstPlayableController>();
        }

        private static void TickSpawning(FirstPlayableController controller, float seconds)
        {
            const float step = .1f;
            var ticks = Mathf.CeilToInt(seconds / step);
            for (var index = 0; index < ticks; index++) controller.TickSpawningForTests(step);
        }
    }
}
