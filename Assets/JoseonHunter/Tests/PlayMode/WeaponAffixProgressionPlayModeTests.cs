using System.Collections;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class WeaponAffixProgressionPlayModeTests
    {
        [Test]
        public void Stable_seed_is_repeatable_and_includes_all_progression_inputs()
        {
            var seed = WeaponAffixRoller.StableSeed(WeaponId.GakgungShot, 2, 17, 4);
            Assert.That(WeaponAffixRoller.StableSeed(WeaponId.GakgungShot, 2, 17, 4), Is.EqualTo(seed));
            Assert.That(WeaponAffixRoller.StableSeed(WeaponId.GakgungShot, 3, 17, 4), Is.Not.EqualTo(seed));
            Assert.That(WeaponAffixRoller.StableSeed(WeaponId.TalismanThrow, 2, 17, 4), Is.Not.EqualTo(seed));
        }

        [UnityTest]
        public IEnumerator Weapon_upgrade_rolls_once_and_evolution_preserves_the_profile()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            controller.SetAffixRandomFactoryForTests((_, _, _, _) => new FixedAffixRandom());
            controller.SetWeaponLevelForTests(WeaponId.GakgungShot, 1);

            Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot).GeneralRolls.Count, Is.EqualTo(0));
            controller.RollWeaponAffixForTests(WeaponId.GakgungShot);
            Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot).GeneralRolls.Count, Is.EqualTo(1));

            controller.SetWeaponLevelForTests(WeaponId.GakgungShot, 5);
            controller.AcquireEvolutionForTests("gakgung_sun_piercer");
            Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot).GeneralRolls.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Run_reset_clears_weapon_affix_profiles()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            controller.SetAffixRandomFactoryForTests((_, _, _, _) => new JackpotAffixRandom());
            controller.RollWeaponAffixForTests(WeaponId.GakgungShot);
            Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot).PotentialIds.Count, Is.EqualTo(3));

            controller.ResetRunForTests();
            Assert.That(controller.AffixProfileForTests(WeaponId.GakgungShot), Is.Null);
        }

        private sealed class FixedAffixRandom : IAffixRandom
        {
            public double NextUnit() => .5d;
            public int NextIndex(int exclusiveMax) => 0;
        }

        private sealed class JackpotAffixRandom : IAffixRandom
        {
            private int calls;
            public double NextUnit() => calls++ == 0 ? .5d : 0d;
            public int NextIndex(int exclusiveMax) => 0;
        }
    }
}
