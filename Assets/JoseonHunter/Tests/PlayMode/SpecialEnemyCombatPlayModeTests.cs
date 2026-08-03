using System.Collections;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class SpecialEnemyCombatPlayModeTests
    {
        [UnityTest]
        public IEnumerator SpawnedShieldDelegatesDirectionalResistanceAndGuideAppearsOnce()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var target = controller.SpawnSpecialEnemyForTests("shield_dokkaebi", Vector2.right);
            var resistance = (IIncomingDamageResistanceTarget)target;
            Assert.That(resistance.IncomingDamageMultiplier(new Float2(0f, 0f), WeaponHitTrait.Slash), Is.EqualTo(.65f));
            Assert.That(controller.LastSpecialEnemyGuideForTests, Does.Contain("방패 도깨비"));
            controller.SpawnSpecialEnemyForTests("shield_dokkaebi", Vector2.left);
            Assert.That(controller.SpecialEnemyGuideCountForTests, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator WaveSpecialPopulationNeverExceedsOneQuarterOfNormalPopulation()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            controller.SetElapsedForTests(60f);
            for (var index = 0; index < 300; index++) controller.TickSpawningForTests(.1f);
            Assert.That(controller.LivingSpecialEnemyCountForTests * 4,
                Is.LessThanOrEqualTo(controller.LivingNormalOnlyEnemyCountForTests));
        }
    }
}
