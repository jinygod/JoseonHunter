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
            controller.SetElapsedForTests(20f);
            yield return null;

            TickSpawning(controller, 10f);

            Assert.That(controller.LivingNormalEnemyIdsForTests, Is.Not.Empty);
            Assert.That(controller.LivingNormalEnemyIdsForTests, Is.All.EqualTo("plague_rat"));
            Assert.That(controller.LivingNormalEnemyIdsForTests.Count, Is.InRange(60, 72));
        }

        [UnityTest]
        public IEnumerator SecondWaveContainsRatsSpiritsAndASpiritPack()
        {
            var controller = LoadGameplay();
            controller.SetElapsedForTests(60f);
            yield return null;

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
            controller.SetElapsedForTests(145f);
            yield return null;

            TickSpawning(controller, 30f);

            Assert.That(controller.LivingNormalEnemyIdsForTests.Count, Is.InRange(130, 140));
            Assert.That(controller.LivingNormalEnemyIdsForTests.Count, Is.LessThanOrEqualTo(140));
            Assert.That(controller.LivingNormalEnemyIdsForTests.Distinct().Count(), Is.GreaterThanOrEqualTo(2));
        }

        [UnityTest]
        public IEnumerator NormalRoleIntroductionsExplainSpiritAndDokkaebiAtTheirRosterWindows()
        {
            var controller = LoadGameplay();
            controller.SetElapsedForTests(45f);
            yield return null;
            controller.TickSpawningForTests(.1f);
            Assert.That(controller.UiState.WaveAnnouncement,
                Is.EqualTo("원한 처녀귀신 출현 · 매우 빠르지만 약합니다"));

            SceneManager.LoadScene("Gameplay");
            controller = Object.FindAnyObjectByType<FirstPlayableController>();
            controller.SetElapsedForTests(90f);
            yield return null;
            controller.TickSpawningForTests(.1f);
            Assert.That(controller.UiState.WaveAnnouncement,
                Is.EqualTo("도깨비 출현 · 느리지만 매우 단단합니다"));
        }

        [UnityTest]
        public IEnumerator AuthoredSpecialEnemiesEnterAtReadableTimesAndNormalRosterStaysClean()
        {
            var controller = LoadGameplay();
            controller.SetElapsedForTests(101.9f);
            yield return null;
            Assert.That(controller.LivingSpecialEnemyIdsForTests, Is.Empty);

            controller.RestoreElapsedForTests(102f);
            controller.TickSpawningForTests(.1f);
            Assert.That(controller.LivingSpecialEnemyIdsForTests, Does.Contain("shield_dokkaebi"));

            controller.RestoreElapsedForTests(120f);
            controller.TickSpawningForTests(.1f);
            Assert.That(controller.LivingSpecialEnemyIdsForTests, Does.Contain("charging_horn_ghost"));

            controller.RestoreElapsedForTests(138f);
            controller.TickSpawningForTests(.1f);
            Assert.That(controller.LivingSpecialEnemyIdsForTests, Does.Contain("spirit_shaman"));

            controller.RestoreElapsedForTests(150f);
            controller.TickSpawningForTests(.1f);
            Assert.That(controller.LivingSpecialEnemyIdsForTests, Does.Contain("splitting_rat"));
            Assert.That(controller.LivingNormalEnemyIdsForTests.Distinct().ToArray(),
                Is.SubsetOf(new[] { "plague_rat", "vengeful_spirit", "dokkaebi" }));
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
