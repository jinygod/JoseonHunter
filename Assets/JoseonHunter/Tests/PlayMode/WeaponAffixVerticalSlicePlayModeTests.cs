using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace JoseonHunter.Tests.PlayMode
{
    /// <summary>
    /// This is intentionally a production-path slice.  The deterministic random factory is the only injected seam:
    /// card input, choice close, affix reel, executor rebuild, evolution, and reset are all driven by live components.
    /// Pixel-mask contact behavior for these same three committed cells is covered in WeaponPotentialCombatAPlayModeTests.
    /// </summary>
    public sealed class WeaponAffixVerticalSlicePlayModeTests
    {
        [TearDown]
        public void RestoreTimeScale() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator Perfect_general_affix_flows_from_pointer_choice_to_appraisal_and_run_reset()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var choice = Object.FindFirstObjectByType<UpgradeChoicePresenter>();
            var reveal = Object.FindFirstObjectByType<WeaponAffixRevealPresenter>();
            var rack = Object.FindFirstObjectByType<WeaponRackPresenter>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(choice, Is.Not.Null);
            Assert.That(reveal, Is.Not.Null);
            Assert.That(rack, Is.Not.Null);
            Assert.That(EventSystem.current, Is.Not.Null);

            controller.SetAffixRandomFactoryForTests((_, _, _, _) => new PerfectThreeLineRandom());
            var forcedHwando = new UpgradeOffer(WeaponId.HwandoFlyingBlade.Value, UpgradeKind.Weapon, 2);
            UpgradeChoiceState displayed = null;
            controller.UpgradeOpened += state => displayed = state;
            controller.OpenUpgradeOffersForTests(forcedHwando);
            controller.AddExperienceForTests(100); // queues exactly one or more later choices; it must not open during the reel.
            yield return new WaitForSecondsRealtime(.35f);

            var card = choice.GetComponentsInChildren<Button>(true).First(button => button.gameObject.activeInHierarchy);
            Assert.That(controller.CurrentOffers, Is.EqualTo(new[] { forcedHwando }));
            Assert.That(displayed, Is.Not.Null);
            Assert.That(displayed.Choices, Has.Count.EqualTo(1));
            Assert.That(displayed.Choices[0].Id, Is.EqualTo(forcedHwando.Id));
            Assert.That(displayed.Choices[0].Kind, Is.EqualTo(forcedHwando.Kind));
            ExecuteEvents.Execute<IPointerClickHandler>(card.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.2f); // card close completes before the reel begins

            var profile = controller.AffixProfileForTests(WeaponId.HwandoFlyingBlade);
            Assert.That(profile.GeneralRolls, Has.Count.EqualTo(1));
            Assert.That(profile.GeneralRolls[0].Stat, Is.EqualTo(WeaponAffixStat.Damage));
            Assert.That(profile.GeneralRolls[0].Tier, Is.EqualTo(WeaponAffixTier.Perfect));
            Assert.That(profile.PotentialIds, Is.Empty);
            Assert.That(reveal.IsRevealing, Is.True);
            Assert.That(controller.IsUpgradeOpen, Is.False, "queued upgrade is gated by reveal completion");

            ExecuteEvents.Execute<IPointerClickHandler>(reveal.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return WaitUntil(() => reveal.IsAwaitingConfirmation, "the skipped jackpot reel to await explicit confirmation");
            reveal.Confirm();
            yield return WaitUntil(() => !reveal.IsRevealing, "the confirmed jackpot reel to complete");
            Assert.That(reveal.LastCompletedResult.NewPotentials, Is.Empty);
            Assert.That(controller.IsUpgradeOpen, Is.False,
                "queued upgrade must leave a playable combat beat after appraisal");
            controller.TickGameplayIfRunningForTests(1.01f);
            Assert.That(controller.IsUpgradeOpen, Is.True,
                "queued upgrade opens after the post-appraisal combat beat");

            var potentialCells = rack.GetComponentsInChildren<Image>(true)
                .Where(image => image.name.StartsWith("Potential Cell")).ToArray();
            Assert.That(potentialCells, Has.Length.EqualTo(3));
            Assert.That(potentialCells.All(cell => !cell.gameObject.activeSelf), Is.True,
                "random potential cells are retired from the rack");
            var legacyLabel = rack.GetComponentsInChildren<Component>(true).First(component =>
                component.name == "Legacy Path" && component.GetType().Name == "TextMeshProUGUI");
            Assert.That(legacyLabel.GetType().GetProperty("text").GetValue(legacyLabel), Is.EqualTo("전승 미선택"));

            rack.Pulse(WeaponId.HwandoFlyingBlade.Value, 2);
            yield return null;

            controller.ResetRunForTests();
            yield return null;
            Assert.That(controller.AffixProfileForTests(WeaponId.HwandoFlyingBlade), Is.Null);
            Assert.That(reveal.IsRevealing, Is.False);
        }

        private static IEnumerator WaitUntil(System.Func<bool> predicate, string condition)
        {
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!predicate())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                    $"Timed out waiting for {condition}.");
                yield return null;
            }
        }

        private sealed class PerfectThreeLineRandom : IAffixRandom
        {
            private int unitCall;
            public int NextIndex(int exclusiveMax) => 0;
            public double NextUnit() => unitCall++ == 0 ? .99d : 0d;
        }
    }
}
