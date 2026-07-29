using System.Collections;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using JoseonHunter.Runtime.Gameplay;
using JoseonHunter.Domain.Combat;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class WeaponAffixRevealPlayModeTests
    {
        [TearDown]
        public void RestoreTimeScale() => Time.timeScale = 1f;
        [TestCase(WeaponAffixTier.Standard, 0, 2.36f)]
        [TestCase(WeaponAffixTier.High, 0, 2.54f)]
        [TestCase(WeaponAffixTier.Perfect, 0, 2.72f)]
        [TestCase(WeaponAffixTier.Standard, 1, 2.82f)]
        [TestCase(WeaponAffixTier.Standard, 2, 3.06f)]
        [TestCase(WeaponAffixTier.Standard, 3, 3.32f)]
        public void Duration_uses_the_exact_affix_and_jackpot_caps(WeaponAffixTier tier, int potentialCount, float expected)
        {
            Assert.That(WeaponAffixRevealPresenter.DurationFor(Result(tier, potentialCount)), Is.EqualTo(expected));
        }

        [Test]
        public void ResultValueUsesAReadableMobileFontSize()
        {
            var presenter = new GameObject("Affix Readability Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(Result(WeaponAffixTier.High, 0));

            Assert.That(presenter.DetailFontSize, Is.GreaterThanOrEqualTo(26f));
            Object.DestroyImmediate(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Skip_is_idempotent_and_does_not_change_the_roll_result()
        {
            var presenter = new GameObject("Affix Reveal Test").AddComponent<WeaponAffixRevealPresenter>();
            var result = Result(WeaponAffixTier.Perfect, 3);
            var completions = 0;
            presenter.RevealCompleted += () => completions++;
            Time.timeScale = 0f;
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(result);
            presenter.Skip(); presenter.Skip();
            yield return new WaitForSecondsRealtime(1.16f);
            Assert.That(presenter.IsAwaitingConfirmation, Is.True);
            presenter.Confirm();
            yield return new WaitForSecondsRealtime(.16f);
            Assert.That(presenter.IsRevealing, Is.False);
            Assert.That(presenter.LastCompletedResult, Is.SameAs(result));
            Assert.That(completions, Is.EqualTo(1));
            Time.timeScale = 1f;
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Pointer_dispatch_skip_is_idempotent()
        {
            var eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            var presenter = new GameObject("Pointer Skip Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            var result = Result(WeaponAffixTier.Standard, 0);
            presenter.Play(result);
            var pointer = new PointerEventData(eventSystem);
            ExecuteEvents.Execute<IPointerClickHandler>(presenter.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            ExecuteEvents.Execute<IPointerClickHandler>(presenter.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.86f);
            ExecuteEvents.Execute<IPointerClickHandler>(presenter.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.16f);
            Assert.That(presenter.LastCompletedResult, Is.SameAs(result));
            Object.Destroy(presenter.gameObject); Object.Destroy(eventSystem.gameObject);
        }

        [UnityTest]
        public IEnumerator Weapon_reveal_waits_for_choice_close_then_opens_one_queued_choice_after_skip()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null; yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var choice = Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            var generic = Object.FindFirstObjectByType<RewardRevealPresenter>();
            var affix = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            affix.SetCatalogForTests(TestCatalog());
            controller.OpenUpgradeForTests();
            controller.SetUpgradeOffersForTests(new UpgradeOffer(WeaponId.GakgungShot.Value, UpgradeKind.Weapon, 1));
            yield return new WaitForSecondsRealtime(.35f);
            var completions = 0;
            var queuedOpens = 0;
            affix.RevealCompleted += () => completions++;
            controller.UpgradeOpened += _ => queuedOpens++;
            var card = choice.GetComponentInChildren<Button>(true);
            ExecuteEvents.Execute<IPointerClickHandler>(card.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            controller.AddExperienceForTests(100);
            yield return new WaitForSecondsRealtime(.05f);
            Assert.That(choice.IsOpen, Is.True);
            Assert.That(affix.IsRevealing, Is.False);
            Assert.That(generic.IsRevealing, Is.False);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(affix.IsRevealing, Is.True);
            affix.Skip();
            affix.Skip();
            yield return new WaitForSecondsRealtime(1.16f);
            Assert.That(affix.IsAwaitingConfirmation, Is.True);
            affix.Confirm();
            yield return new WaitForSecondsRealtime(.16f);
            Assert.That(controller.IsUpgradeOpen, Is.True);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(queuedOpens, Is.EqualTo(1));
            yield return null;
            Assert.That(controller.IsUpgradeOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator Support_and_evolution_keep_generic_reward_reveal()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null; yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var choice = Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            var generic = Object.FindFirstObjectByType<RewardRevealPresenter>();
            var affix = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            affix.SetCatalogForTests(TestCatalog());

            yield return ChooseThroughVisibleCard(controller, choice, new UpgradeOffer("boots", UpgradeKind.Support, 1));
            Assert.That(generic.IsRevealing, Is.True);
            Assert.That(affix.IsRevealing, Is.False);
            yield return new WaitForSecondsRealtime(.5f);

            yield return ChooseThroughVisibleCard(controller, choice, new UpgradeOffer("gakgung_sun_piercer", UpgradeKind.Evolution, 5));
            Assert.That(generic.IsRevealing, Is.True);
            Assert.That(affix.IsRevealing, Is.False);
        }

        [UnityTest]
        public IEnumerator Hide_cancels_without_a_completion_notification()
        {
            var presenter = new GameObject("Affix Reveal Cancel Test").AddComponent<WeaponAffixRevealPresenter>();
            var completions = 0;
            presenter.RevealCompleted += () => completions++;
            presenter.Play(Result(WeaponAffixTier.Standard, 0));
            presenter.HideImmediately();
            yield return null;
            Assert.That(completions, Is.Zero);
            Assert.That(presenter.IsRevealing, Is.False);
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Every_result_holds_until_explicit_confirmation()
        {
            var presenter = new GameObject("Boundary Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            Time.timeScale = 0f;
            foreach (var result in new[] { Result(WeaponAffixTier.Standard, 0), Result(WeaponAffixTier.High, 0), Result(WeaponAffixTier.Perfect, 0), Result(WeaponAffixTier.Standard, 1), Result(WeaponAffixTier.Standard, 2), Result(WeaponAffixTier.Standard, 3) })
            {
                presenter.Play(result);
                yield return new WaitForSecondsRealtime(WeaponAffixRevealPresenter.DurationFor(result) + .04f);
                Assert.That(presenter.IsAwaitingConfirmation, Is.True);
                Assert.That(presenter.LastCompletedResult, Is.Not.SameAs(result));
                presenter.Confirm();
                yield return new WaitForSecondsRealtime(.16f);
                Assert.That(presenter.IsRevealing, Is.False);
                Assert.That(presenter.LastCompletedResult, Is.SameAs(result));
            }
            Object.Destroy(presenter.gameObject);
        }

        [TestCase(WeaponAffixTier.Standard, 0, false)]
        [TestCase(WeaponAffixTier.High, 0, true)]
        [TestCase(WeaponAffixTier.Perfect, 0, true)]
        [TestCase(WeaponAffixTier.Standard, 1, true)]
        public void Tension_is_reserved_for_high_perfect_or_potential(WeaponAffixTier tier, int potentialCount, bool expected)
        {
            var presenter = new GameObject("Tension Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.Play(Result(tier, potentialCount));
            Assert.That(presenter.IsTensionActive, Is.EqualTo(expected));
            Object.DestroyImmediate(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Tension_changes_the_reel_transform_but_standard_does_not()
        {
            var presenter = new GameObject("Tension Motion Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(Result(WeaponAffixTier.Standard, 0));
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(presenter.TensionScale, Is.EqualTo(1f));
            presenter.Play(Result(WeaponAffixTier.High, 0));
            yield return new WaitForSecondsRealtime(1.9f);
            Assert.That(presenter.TensionScale, Is.Not.EqualTo(1f));
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Final_affix_is_hidden_while_spinning_then_locks_in()
        {
            var presenter = new GameObject("Slot Phase Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(Result(WeaponAffixTier.Standard, 0));
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(presenter.Phase, Is.EqualTo(WeaponAffixRevealPresenter.RevealPhase.Spinning));
            Assert.That(presenter.IsFinalAffixVisible, Is.False);
            yield return new WaitForSecondsRealtime(1.0f);
            Assert.That(presenter.IsFinalAffixVisible, Is.True);
            Object.Destroy(presenter.gameObject);
        }

        [UnityTest]
        public IEnumerator Jackpot_lines_unlock_in_order()
        {
            var presenter = new GameObject("Slot Lines Test").AddComponent<WeaponAffixRevealPresenter>();
            presenter.SetCatalogForTests(TestCatalog());
            presenter.Play(Result(WeaponAffixTier.Perfect, 3));
            yield return new WaitForSecondsRealtime(1.98f);
            Assert.That(presenter.VisiblePotentialCount, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(.26f);
            Assert.That(presenter.VisiblePotentialCount, Is.EqualTo(2));
            yield return new WaitForSecondsRealtime(.26f);
            Assert.That(presenter.VisiblePotentialCount, Is.EqualTo(3));
            Object.Destroy(presenter.gameObject);
        }

        private static WeaponAffixRollResult Result(WeaponAffixTier tier, int potentialCount)
        {
            var potentials = new WeaponPotentialId[potentialCount];
            for (var index = 0; index < potentialCount; index++)
                potentials[index] = new WeaponPotentialId("test_potential_" + index);
            return new WeaponAffixRollResult(new WeaponAffixRoll(WeaponAffixStat.Damage, tier, .2d), potentials);
        }

        private static JoseonHunter.Content.Weapons.WeaponAffixPresentationCatalogAsset TestCatalog()
        {
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f));
            var catalog = ScriptableObject.CreateInstance<JoseonHunter.Content.Weapons.WeaponAffixPresentationCatalogAsset>();
            catalog.SetSlotKitForTests(sprite, sprite, sprite, sprite, sprite);
            return catalog;
        }

        private static IEnumerator ChooseThroughVisibleCard(FirstPlayableController controller, UpgradeChoicePresenter choice, UpgradeOffer offer)
        {
            controller.OpenUpgradeForTests();
            controller.SetUpgradeOffersForTests(offer);
            yield return new WaitForSecondsRealtime(.35f);
            var card = choice.GetComponentInChildren<Button>(true);
            ExecuteEvents.Execute<IPointerClickHandler>(card.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.2f);
        }
    }
}
