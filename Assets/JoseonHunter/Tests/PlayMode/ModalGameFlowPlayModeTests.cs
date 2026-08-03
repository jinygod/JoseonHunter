using System.Collections;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Domain.Runs;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class ModalGameFlowPlayModeTests
    {
        [TearDown]
        public void RestoreTimeScale() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator Weapon_appraisal_keeps_gameplay_paused_until_confirmation()
        {
            yield return LoadGameplay();
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var affix = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            Assert.That(Object.FindFirstObjectByType<FirstPlayableUiBootstrap>().BoundController, Is.SameAs(controller));
            var elapsedBefore = controller.UiState.Elapsed;

            controller.OpenUpgradeOffersForTests(new UpgradeOffer(
                WeaponId.HwandoFlyingBlade.Value, UpgradeKind.Weapon, 2));
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.LevelUpSelection));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(controller.TryChooseUpgrade(0), Is.True);
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.AugmentResult));

            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(controller.UiState.Elapsed, Is.EqualTo(elapsedBefore));
            affix.Skip();
            yield return new WaitForSecondsRealtime(1.4f);
            Assert.That(affix.IsAwaitingConfirmation, Is.True);
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.AugmentResult));

            affix.Confirm();
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Playing));
        }

        [UnityTest]
        public IEnumerator Confirming_appraisal_returns_to_combat_before_the_next_queued_level()
        {
            yield return LoadGameplay();
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var affix = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            Assert.That(Object.FindFirstObjectByType<FirstPlayableUiBootstrap>().BoundController, Is.SameAs(controller));
            controller.OpenUpgradeOffersForTests(new UpgradeOffer(
                WeaponId.HwandoFlyingBlade.Value, UpgradeKind.Weapon, 2));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);
            controller.AddExperienceForTests(100);

            yield return new WaitForSecondsRealtime(.6f);
            affix.Skip();
            yield return new WaitForSecondsRealtime(1.4f);
            affix.Confirm();
            yield return new WaitForSecondsRealtime(.2f);

            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(controller.IsUpgradeOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            controller.TickGameplayIfRunningForTests(.5f);
            Assert.That(controller.IsUpgradeOpen, Is.False);
            controller.TickGameplayIfRunningForTests(.51f);
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.LevelUpSelection));
            Assert.That(controller.IsUpgradeOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Weapon_detail_pauses_playing_and_restores_it_when_dismissed()
        {
            yield return LoadGameplay();
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var rack = Object.FindFirstObjectByType<WeaponRackPresenter>();
            var affix = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            var slot = rack.transform.Find("Weapon Slot 0").GetComponent<UnityEngine.UI.Button>();

            ExecuteEvents.Execute<IPointerClickHandler>(slot.gameObject,
                new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Paused));
            Assert.That(affix.IsDetailOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);

            ExecuteEvents.Execute<IPointerClickHandler>(affix.gameObject,
                new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Playing));
        }

        [UnityTest]
        public IEnumerator Gameplay_scene_reload_creates_one_bootstrap_and_binds_the_new_controller()
        {
            yield return LoadGameplay();
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;

            var bootstrap = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.BoundController, Is.SameAs(controller));
            Assert.That(Object.FindObjectsByType<FirstPlayableUiBootstrap>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Forced_offer_helper_transitions_and_publishes_like_the_upgrade_flow()
        {
            yield return LoadGameplay();
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var choice = Object.FindFirstObjectByType<UpgradeChoicePresenter>();

            controller.SetUpgradeOffersForTests(new UpgradeOffer("boots", UpgradeKind.Support, 1));
            yield return null;

            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.LevelUpSelection));
            Assert.That(controller.IsUpgradeOpen, Is.True);
            Assert.That(choice.IsOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator Disabling_bootstrap_cancels_open_levelup_and_restores_playing()
        {
            yield return LoadGameplay();
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var bootstrap = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
            controller.OpenUpgradeOffersForTests(new UpgradeOffer("boots", UpgradeKind.Support, 1));

            bootstrap.gameObject.SetActive(false);
            yield return null;

            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(controller.IsUpgradeOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Destroying_bootstrap_cancels_augment_result_and_restores_playing()
        {
            yield return LoadGameplay();
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var bootstrap = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
            controller.OpenUpgradeOffersForTests(new UpgradeOffer("boots", UpgradeKind.Support, 1));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);

            Object.Destroy(bootstrap.gameObject);
            yield return null;

            Assert.That(controller.Flow.State, Is.EqualTo(GameFlowState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        private static IEnumerator LoadGameplay()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;
            yield return null;
        }
    }
}
