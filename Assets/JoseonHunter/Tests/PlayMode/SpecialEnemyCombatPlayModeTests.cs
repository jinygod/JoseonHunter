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
        public IEnumerator ShieldKeepsItsSpriteAndBreaksAfterSixUniqueConfirmedFrontHits()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var target = controller.SpawnSpecialEnemyForTests("shield_dokkaebi", Vector2.right * .5f);
            var originalSprite = controller.EnemySpriteForTests(target);
            var resistance = (IIncomingDamageResistanceTarget)target;
            Assert.That(resistance.IncomingDamageMultiplier(new Float2(0f, 0f), WeaponHitTrait.Slash),
                Is.EqualTo(.15f));
            Assert.That(controller.LastSpecialEnemyGuideForTests,
                Is.EqualTo("방패 도깨비 · 정면 직접 공격 6회로 방패 파괴"));
            Assert.That(controller.ShieldChargesForTests(target), Is.EqualTo(6));
            Assert.That(controller.ShieldBarFillForTests(target), Is.EqualTo(1f).Within(.001f));

            controller.UpdateEnemiesForTests(.32f);
            Assert.That(controller.EnemySpriteForTests(target), Is.SameAs(originalSprite));

            for (var hit = 0; hit < 6; hit++)
            {
                var request = WeaponDamageRequest.Create(
                    new AttackInstance(800 + hit, RepeatHitPolicy.OncePerInstance, 0f),
                    WeaponId.HwandoFlyingBlade, target, 1, false, new Float2(.5f, 0f),
                    ContactPhase.Direct, hit, hit, true, WeaponHitTrait.Slash, new Float2(0f, 0f));
                Assert.That(controller.CombatDamageService.TryApply(request, out _), Is.True);
                Assert.That(controller.ShieldChargesForTests(target), Is.EqualTo(5 - hit));
            }

            Assert.That(controller.HasShieldBarForTests(target), Is.False);
            Assert.That(resistance.IncomingDamageMultiplier(new Float2(0f, 0f), WeaponHitTrait.Slash),
                Is.EqualTo(1f));
            controller.SpawnSpecialEnemyForTests("shield_dokkaebi", Vector2.left);
            Assert.That(controller.SpecialEnemyGuideCountForTests, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator WaveSpecialPopulationNeverExceedsOneEighthOfNormalPopulation()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            controller.SetElapsedForTests(150f);
            for (var index = 0; index < 300; index++) controller.TickSpawningForTests(.1f);
            Assert.That(controller.LivingSpecialEnemyCountForTests * 8,
                Is.LessThanOrEqualTo(controller.LivingNormalOnlyEnemyCountForTests));
        }
    }
}
