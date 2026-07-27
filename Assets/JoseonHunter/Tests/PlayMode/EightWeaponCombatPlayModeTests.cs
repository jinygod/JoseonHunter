using System.Linq;
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
        public IEnumerator GameplayRegistersEachLaunchWeaponOnceThroughItsCatalog()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.RegisteredWeaponIds.Distinct().Count(), Is.EqualTo(8));
            Assert.That(controller.RegisteredWeaponIds.Count, Is.EqualTo(8));
            Assert.That(controller.WeaponRuntime, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<DamageNumberPool>(), Is.Not.Null);
        }
    }
}
