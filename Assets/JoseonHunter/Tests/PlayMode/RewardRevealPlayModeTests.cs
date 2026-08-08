using System.Collections;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
            Assert.That(presenter.IsAwaitingConfirmation, Is.True);
            presenter.Confirm();
            yield return null;
            Assert.That(presenter.IsRevealing, Is.False);
            Time.timeScale = 1f;
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Support_reward_finishes_on_an_opaque_readable_hanji_panel()
        {
            var presenter = new GameObject("Support Reward Test").AddComponent<RewardRevealPresenter>();
            presenter.Play(new ProgressionRewardEvent("warding_bell", null, 1, ProgressionRewardKind.Support,
                "수호 방울", "경험치 획득 범위 +0.7", null));

            yield return new WaitForSecondsRealtime(.5f);

            var reveal = presenter.transform.Find("Reward Reveal");
            var panel = reveal.Find("Reward Panel").GetComponent<Image>();
            var title = panel.transform.Find("Title").GetComponent("TextMeshProUGUI");
            var detail = panel.transform.Find("Detail").GetComponent("TextMeshProUGUI");
            var confirm = panel.transform.Find("Confirm Reward").GetComponent<Button>();
            var confirmLabel = confirm.transform.Find("Confirm Label").GetComponent("TextMeshProUGUI");
            Assert.That(reveal.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(.001f));
            Assert.That(panel.color.a, Is.EqualTo(1f).Within(.001f));
            Assert.That(panel.color, Is.EqualTo(JoseonUiPalette.Hanji));
            Assert.That(TextColor(title), Is.EqualTo(JoseonUiPalette.HanjiInk));
            Assert.That(TextColor(detail), Is.EqualTo(JoseonUiPalette.HanjiMutedInk));
            Assert.That(TextValue(detail), Is.EqualTo("경험치 획득 범위 +0.7"));
            Assert.That(confirm.image.sprite, Is.Not.Null);
            Assert.That(confirm.image.sprite.name, Is.EqualTo("primary_red_button"));
            Assert.That(confirm.image.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(TextValue(confirmLabel), Is.EqualTo("확인"));
            Assert.That(TextColor(confirmLabel), Is.EqualTo(JoseonUiPalette.Hanji));
            presenter.Confirm();
            yield return null;
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Support_choice_applies_and_returns_without_reward_confirmation()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var reward = Object.FindAnyObjectByType<RewardRevealPresenter>();
            var startingSpeed = controller.StartingMoveSpeedForTests;
            controller.OpenUpgradeOffersForTests(new UpgradeOffer("boots", UpgradeKind.Support, 1));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);

            for (var frame = 0; frame < 60 && controller.Flow.State != GameFlowState.Playing; frame++)
                yield return null;

            Assert.That(reward.IsRevealing, Is.False);
            Assert.That(reward.IsAwaitingConfirmation, Is.False);
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(controller.StartingMoveSpeedForTests,
                Is.EqualTo(startingSpeed * 1.12f).Within(.001f));
        }

        private static string TextValue(Component text) =>
            (string)text.GetType().GetProperty("text").GetValue(text);

        private static Color TextColor(Component text) =>
            (Color)text.GetType().GetProperty("color").GetValue(text);

        [UnityTest]
        public IEnumerator Pending_choice_keeps_gameplay_grace_after_immediate_support()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            yield return null;
            var controller = Object.FindAnyObjectByType<FirstPlayableController>();
            var reward = Object.FindAnyObjectByType<RewardRevealPresenter>();
            controller.OpenUpgradeOffersForTests(new UpgradeOffer("boots", UpgradeKind.Support, 1));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);
            controller.AddExperienceForTests(100);

            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(controller.IsUpgradeOpen, Is.False);
            Assert.That(reward.IsRevealing, Is.False);
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(controller.IsUpgradeOpen, Is.False);
            controller.TickGameplayIfRunningForTests(1.01f);
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
            Assert.That(root.transform.Find("Weapon Slot 1").Find("Quality Border").localScale.x, Is.GreaterThan(1f));

            rack.ResetPulses();
            Assert.That(root.transform.Find("Weapon Slot 1").Find("Quality Border").localScale, Is.EqualTo(Vector3.one));
            rack.Pulse("two", 2);
            yield return null;
            root.SetActive(false);
            Assert.That(root.transform.Find("Weapon Slot 1").Find("Quality Border").localScale, Is.EqualTo(Vector3.one));
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
            Assert.That(root.transform.Find("Weapon Slot 0").Find("Quality Border").localScale.x, Is.GreaterThan(1f));
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(root.transform.Find("Weapon Slot 0").Find("Quality Border").localScale, Is.EqualTo(Vector3.one));
            Object.Destroy(root);
        }
    }
}
