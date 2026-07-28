using System.Collections;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class CombatHudPlayModeTests
    {
        [TearDown]
        public void RestoreTimeScale()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Level_up_opens_cards_accepts_one_choice_restores_combat_and_sequences_the_queue()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var bootstrap = Object.FindFirstObjectByType<FirstPlayableUiBootstrap>();
            var choice = Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            var rewardReveal = Object.FindFirstObjectByType<RewardRevealPresenter>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(choice, Is.Not.Null);
            Assert.That(rewardReveal, Is.Not.Null);
            Assert.That(bootstrap.BoundController, Is.EqualTo(controller));

            controller.OpenUpgradeForTests();
            controller.AddExperienceForTests(100);
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(choice.IsOpen, Is.True);

            var cards = choice.GetComponentsInChildren<Button>(true);
            Assert.That(cards, Has.Length.EqualTo(3));
            Assert.That(EventSystem.current, Is.Not.Null);
            Assert.That(EventSystem.current.GetComponent<BaseInputModule>(), Is.Not.Null);
            ExecuteEvents.Execute<IPointerClickHandler>(cards[0].gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute<IPointerClickHandler>(cards[1].gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.25f);

            Assert.That(controller.AppliedUpgradeCount, Is.EqualTo(1));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(rewardReveal.IsRevealing, Is.True);
            Assert.That(controller.IsUpgradeOpen, Is.False,
                "The queued choice must wait until the unscaled reward reveal completes.");

            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(rewardReveal.IsRevealing, Is.False);
            Assert.That(controller.IsUpgradeOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator Run_reset_closes_the_bootstrap_owned_upgrade_presentation()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var choice = Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(choice, Is.Not.Null);

            controller.OpenUpgradeForTests();
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(choice.IsOpen, Is.True);

            controller.ResetRunForTests();
            Assert.That(choice.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator Bootstrap_creates_one_portrait_hud_with_a_safe_area_container()
        {
            yield return DestroyBootstraps();
            var root = new GameObject("UI Test");
            var bootstrap = root.AddComponent<FirstPlayableUiBootstrap>();
            yield return null;

            var canvas = root.GetComponentInChildren<Canvas>(true);
            var scaler = root.GetComponentInChildren<CanvasScaler>(true);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1080f, 1920f)));
            Assert.That(root.GetComponentInChildren<CombatHudPresenter>(true), Is.Not.Null);
            Assert.That(root.GetComponentInChildren<WeaponRackPresenter>(true), Is.Not.Null);
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(EventSystem.current.GetComponent<BaseInputModule>(), Is.Not.Null);

            bootstrap.ApplySafeArea(new Rect(0f, 120f, 1000f, 1760f), new Vector2(1000f, 2000f));
            Assert.That(bootstrap.SafeAreaContainer.anchorMin, Is.EqualTo(new Vector2(0f, .06f)));
            Assert.That(bootstrap.SafeAreaContainer.anchorMax, Is.EqualTo(new Vector2(1f, .94f)));

            new GameObject("Duplicate UI Test").AddComponent<FirstPlayableUiBootstrap>();
            yield return null;
            Assert.That(Object.FindObjectsByType<FirstPlayableUiBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                Is.EqualTo(1));
            Assert.That(bootstrap.BoundController, Is.Null);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Weapon_rack_hides_a_null_icon_without_creating_extra_slots()
        {
            var root = new GameObject("Rack Test");
            var rack = root.AddComponent<WeaponRackPresenter>();
            rack.Render(new[] { new WeaponSlotView("hwando_flying_blade", "Hwando", 1, null) });
            yield return null;

            var icon = System.Array.Find(root.GetComponentsInChildren<Image>(true), image => image.name == "Icon");
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.enabled, Is.False);
            rack.Render(new[] { new WeaponSlotView("hwando_flying_blade", "Hwando", 1, null) });
            Assert.That(root.GetComponentsInChildren<Image>(true).Length, Is.GreaterThanOrEqualTo(3));
            Object.Destroy(root);
        }

        private static IEnumerator DestroyBootstraps()
        {
            foreach (var bootstrap in Object.FindObjectsByType<FirstPlayableUiBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(bootstrap.gameObject);
            yield return null;
        }
    }
}
