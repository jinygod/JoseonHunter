using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class EvolvedWeaponCombatPlayModeTests
    {
        [Test]
        public void Evolved_factory_registers_unique_evolved_executor_for_every_weapon()
        {
            var executors = new HashSet<IWeaponExecutor>();
            foreach (var weaponId in WeaponRoster.All)
            {
                using (var rig = EvolvedWeaponTestRig.For(weaponId))
                {
                    var registered = rig.Runtime.ExecutorForTests(weaponId);

                    Assert.That(registered, Is.SameAs(rig.Executor));
                    Assert.That(rig.Runtime.IsEvolvedForTests(weaponId), Is.True);
                    Assert.That(rig.Runtime.RegistrationCountForTests(weaponId), Is.EqualTo(1));
                    Assert.That(executors.Add(registered), Is.True);
                }
            }
        }

        [UnityTest]
        public IEnumerator Choosing_evolution_keeps_weapon_level_and_rebuilds_evolved_executor()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 5);
            var preChoiceRuntime = controller.WeaponRuntime;
            var preChoiceExecutor = preChoiceRuntime.ExecutorForTests(WeaponId.HwandoFlyingBlade);
            Assert.That(preChoiceExecutor, Is.Not.Null);
            Assert.That(preChoiceRuntime.IsEvolvedForTests(WeaponId.HwandoFlyingBlade), Is.False);
            Assert.That(preChoiceRuntime.RegistrationCountForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(1));
            controller.UnlockEvolutionForTests("hwando_moon_eclipse");
            controller.OpenUpgradeForTests();

            var index = controller.CurrentOffers
                .Select((offer, i) => (offer, i))
                .Single(pair => pair.offer.Id == "hwando_moon_eclipse").i;
            Assert.That(controller.TryChooseUpgrade(index), Is.True);

            Assert.That(controller.WeaponLevelForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(5));
            Assert.That(controller.AcquiredEvolutionIds, Contains.Item("hwando_moon_eclipse"));
            Assert.That(controller.WeaponRuntime, Is.Not.SameAs(preChoiceRuntime));
            Assert.That(controller.WeaponRuntime.ExecutorForTests(WeaponId.HwandoFlyingBlade), Is.Not.SameAs(preChoiceExecutor));
            Assert.That(preChoiceRuntime.IsDisposedForTests, Is.True);
            Assert.That(preChoiceRuntime.RegistrationCountForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(0));
            Assert.That(controller.WeaponRuntime.IsEvolvedForTests(WeaponId.HwandoFlyingBlade), Is.True);
            Assert.That(controller.WeaponRuntime.RegistrationCountForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(1));
        }
    }
}
