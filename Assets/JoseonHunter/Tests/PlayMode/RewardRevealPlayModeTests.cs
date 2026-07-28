using System.Collections;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class RewardRevealPlayModeTests
    {
        [TestCase(ProgressionRewardKind.Support, 70)]
        [TestCase(ProgressionRewardKind.WeaponLevel, 80)]
        [TestCase(ProgressionRewardKind.NewWeapon, 90)]
        [TestCase(ProgressionRewardKind.Evolution, 100)]
        public void Reward_kind_maps_to_expected_intensity(ProgressionRewardKind kind, int expected)
        {
            Assert.That(RewardRevealPresenter.IntensityFor(kind), Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator New_weapon_reveal_uses_unscaled_duration()
        {
            var presenter = new GameObject("Reward Reveal Test").AddComponent<RewardRevealPresenter>();
            Time.timeScale = 0f;
            presenter.Play(new ProgressionRewardEvent("weapon", "weapon", 1, ProgressionRewardKind.NewWeapon,
                "Weapon", "Acquired", null));

            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(presenter.IsRevealing, Is.True);
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(presenter.IsRevealing, Is.False);
            Time.timeScale = 1f;
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Pending_choice_waits_for_reward_reveal_before_opening()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            controller.AddExperienceForTests(100);
            Assert.That(controller.TryChooseUpgrade(0), Is.True);

            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(controller.IsUpgradeOpen, Is.False);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(controller.IsUpgradeOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator Rack_pulse_targets_only_requested_slot_and_cleans_up_on_reset_and_disable()
        {
            var root = new GameObject("Rack Test");
            var rack = root.AddComponent<WeaponRackPresenter>();
            rack.Render(new[]
            {
                new WeaponSlotView("one", "One", 1, null),
                new WeaponSlotView("two", "Two", 1, null)
            });
            rack.Pulse("two", 2);
            yield return null;
            Assert.That(root.transform.Find("Weapon Slot 0").localScale, Is.EqualTo(Vector3.one));
            Assert.That(root.transform.Find("Weapon Slot 1").localScale, Is.EqualTo(Vector3.one));
            Assert.That(root.transform.Find("Weapon Slot 1").Find("Accent").localScale.x, Is.GreaterThan(1f));

            rack.ResetPulses();
            Assert.That(root.transform.Find("Weapon Slot 1").Find("Accent").localScale, Is.EqualTo(Vector3.one));
            rack.Pulse("two", 2);
            yield return null;
            root.SetActive(false);
            Assert.That(root.transform.Find("Weapon Slot 1").Find("Accent").localScale, Is.EqualTo(Vector3.one));
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Repeated_slot_pulse_replaces_the_previous_animation()
        {
            var root = new GameObject("Repeated Rack Test");
            var rack = root.AddComponent<WeaponRackPresenter>();
            rack.Render(new[] { new WeaponSlotView("one", "One", 1, null) });
            rack.Pulse("one", 2);
            yield return new WaitForSecondsRealtime(.12f);
            rack.Pulse("one", 3);
            yield return new WaitForSecondsRealtime(.16f);
            Assert.That(root.transform.Find("Weapon Slot 0").localScale, Is.EqualTo(Vector3.one));
            Assert.That(root.transform.Find("Weapon Slot 0").Find("Accent").localScale.x, Is.GreaterThan(1f));
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(root.transform.Find("Weapon Slot 0").Find("Accent").localScale, Is.EqualTo(Vector3.one));
            Object.Destroy(root);
        }
    }
}
