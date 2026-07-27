using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Presentation.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Collections;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class EightWeaponCombatPlayModeTests
    {
        [UnityTest]
        public IEnumerator GameplayStartsWithHwandoAndCanAcquireAnOfferedWeapon()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.RegisteredWeaponIds.Single().Value, Is.EqualTo("hwando_flying_blade"));
            Assert.That(controller.WeaponRuntime, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<DamageNumberPool>(), Is.Not.Null);

            var openUpgrade = typeof(FirstPlayableController).GetMethod("OpenUpgrade", BindingFlags.Instance | BindingFlags.NonPublic);
            var chooseUpgrade = typeof(FirstPlayableController).GetMethod("ChooseUpgrade", BindingFlags.Instance | BindingFlags.NonPublic);
            var offerField = typeof(FirstPlayableController).GetField("upgradeOffers", BindingFlags.Instance | BindingFlags.NonPublic);
            openUpgrade.Invoke(controller, null);
            var labels = (List<string>)offerField.GetValue(controller);
            var newWeaponIndex = labels.FindIndex(label => label.StartsWith("[신규]"));

            Assert.That(newWeaponIndex, Is.GreaterThanOrEqualTo(0));
            chooseUpgrade.Invoke(controller, new object[] { newWeaponIndex });

            Assert.That(controller.RegisteredWeaponIds.Distinct().Count(), Is.EqualTo(2));
        }
    }
}
