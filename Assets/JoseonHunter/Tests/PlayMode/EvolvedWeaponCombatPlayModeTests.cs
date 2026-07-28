using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class EvolvedWeaponCombatPlayModeTests
    {
        [UnityTest]
        public IEnumerator Choosing_evolution_keeps_weapon_level_and_rebuilds_evolved_executor()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 5);
            controller.UnlockEvolutionForTests("hwando_moon_eclipse");
            controller.OpenUpgradeForTests();

            var index = controller.CurrentOffers
                .Select((offer, i) => (offer, i))
                .Single(pair => pair.offer.Id == "hwando_moon_eclipse").i;
            Assert.That(controller.TryChooseUpgrade(index), Is.True);

            Assert.That(controller.WeaponLevelForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(5));
            Assert.That(controller.AcquiredEvolutionIds, Contains.Item("hwando_moon_eclipse"));
            Assert.That(controller.WeaponRuntime.IsEvolvedForTests(WeaponId.HwandoFlyingBlade), Is.True);
        }
    }
}
