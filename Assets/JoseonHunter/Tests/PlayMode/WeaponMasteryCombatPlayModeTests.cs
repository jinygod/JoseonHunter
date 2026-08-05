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
    public sealed class WeaponMasteryCombatPlayModeTests
    {
        [UnityTest]
        public IEnumerator ConfirmedKillingBlowAwardsOnlyItsWeapon()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            controller.ConfigureSeparationLoadScenarioForTests();
            var target = controller.SpawnEnemyForTests(Vector2.right);
            var request = WeaponDamageRequest.Create(
                new AttackInstance(91001, RepeatHitPolicy.OncePerInstance, 0f),
                WeaponId.GakgungShot,
                target,
                int.MaxValue,
                false,
                new Float2(1f, 0f),
                ContactPhase.Direct,
                1);

            Assert.That(controller.CombatDamageService.TryApply(request, out _), Is.True);

            Assert.That(controller.RunMasterySnapshotForTests[WeaponId.GakgungShot], Is.EqualTo(1));
            Assert.That(controller.RunMasterySnapshotForTests.ContainsKey(WeaponId.HwandoFlyingBlade), Is.False);
        }

        [UnityTest]
        public IEnumerator DirectNonWeaponKillDoesNotReuseAnEarlierWeaponHit()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            controller.ConfigureSeparationLoadScenarioForTests();
            var target = controller.SpawnEnemyForTests(Vector2.right);
            var request = WeaponDamageRequest.Create(
                new AttackInstance(91002, RepeatHitPolicy.OncePerInstance, 0f),
                WeaponId.FrostFlask,
                target,
                1,
                false,
                new Float2(1f, 0f),
                ContactPhase.Direct,
                1);
            Assert.That(controller.CombatDamageService.TryApply(request, out _), Is.True);

            target.ApplyResolvedDamage(int.MaxValue);

            Assert.That(controller.RunMasterySnapshotForTests.ContainsKey(WeaponId.FrostFlask), Is.False);
        }
    }
}
