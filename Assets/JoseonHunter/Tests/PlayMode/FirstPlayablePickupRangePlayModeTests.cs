using System.Collections;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class FirstPlayablePickupRangePlayModeTests
    {
        [UnityTest]
        public IEnumerator StartingExperienceAtOneWorldUnitDoesNotMagnetize()
        {
            var setup = LoadPickupAt(new Vector2(1f, 0f));
            while (setup.MoveNext()) yield return setup.Current;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var pickup = GameObject.Find("Experience Flame");
            Assert.That(controller, Is.Not.Null);
            Assert.That(pickup, Is.Not.Null);
            var before = pickup.transform.position;

            Assert.That(controller.TickGameplayIfRunningForTests(.05f), Is.True);

            Assert.That(pickup.transform.position, Is.EqualTo(before),
                "Starting pickup attraction must require near-contact distance.");
        }

        [UnityTest]
        public IEnumerator ExperienceFlameIsReadableWithoutIncreasingItsPickupRange()
        {
            var setup = LoadPickupAt(new Vector2(1f, 0f));
            while (setup.MoveNext()) yield return setup.Current;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var pickup = GameObject.Find("Experience Flame");
            Assert.That(controller, Is.Not.Null);
            Assert.That(pickup, Is.Not.Null);
            Assert.That(pickup.transform.localScale.x, Is.InRange(.29f, .31f));
            var before = pickup.transform.position;

            Assert.That(controller.TickGameplayIfRunningForTests(.05f), Is.True);

            Assert.That(pickup.transform.position, Is.EqualTo(before),
                "Visual enlargement must not enlarge the starting attraction radius.");
        }

        [UnityTest]
        public IEnumerator StartingExperienceAtHalfAWorldUnitMovesTowardThePlayer()
        {
            var setup = LoadPickupAt(new Vector2(.5f, 0f));
            while (setup.MoveNext()) yield return setup.Current;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var pickup = GameObject.Find("Experience Flame");
            Assert.That(controller, Is.Not.Null);
            Assert.That(pickup, Is.Not.Null);
            var beforeDistance = pickup.transform.position.magnitude;

            Assert.That(controller.TickGameplayIfRunningForTests(.02f), Is.True);

            Assert.That(pickup.transform.position.magnitude, Is.LessThan(beforeDistance));
        }

        private static IEnumerator LoadPickupAt(Vector2 position)
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(controller, Is.Not.Null);
            controller.ConfigureSeparationLoadScenarioForTests();
            var target = controller.SpawnEnemyForTests(position);
            Assert.That(target, Is.Not.Null);
            target.ApplyResolvedDamage(int.MaxValue);
        }
    }
}
