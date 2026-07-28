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
        public IEnumerator Perfect_hwando_jackpot_flows_from_pointer_choice_to_evolution_and_run_reset()
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
            controller.OpenUpgradeForTests();
            controller.SetUpgradeOffersForTests(new UpgradeOffer(WeaponId.HwandoFlyingBlade.Value, UpgradeKind.Weapon, 2));
            controller.AddExperienceForTests(100); // queues exactly one or more later choices; it must not open during the reel.
            yield return new WaitForSecondsRealtime(.35f);

            var card = choice.GetComponentsInChildren<Button>(true).First(button => button.gameObject.activeInHierarchy);
            ExecuteEvents.Execute<IPointerClickHandler>(card.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.2f); // card close completes before the reel begins

            var profile = controller.AffixProfileForTests(WeaponId.HwandoFlyingBlade);
            Assert.That(profile.GeneralRolls, Has.Count.EqualTo(1));
            Assert.That(profile.GeneralRolls[0].Stat, Is.EqualTo(WeaponAffixStat.Damage));
            Assert.That(profile.GeneralRolls[0].Tier, Is.EqualTo(WeaponAffixTier.Perfect));
            Assert.That(profile.PotentialIds, Is.EqualTo(new[]
            {
                WeaponPotentialId.HwandoVenomFang,
                WeaponPotentialId.HwandoReturningAfterimage,
                WeaponPotentialId.HwandoFlyingBladeDance
            }));
            Assert.That(reveal.IsRevealing, Is.True);
            Assert.That(controller.IsUpgradeOpen, Is.False, "queued upgrade is gated by reveal completion");

            ExecuteEvents.Execute<IPointerClickHandler>(reveal.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.72f);
            Assert.That(reveal.LastCompletedResult.NewPotentials, Is.EqualTo(profile.PotentialIds));
            Assert.That(controller.IsUpgradeOpen, Is.True, "queued upgrade opens only after the skipped jackpot reel completes");

            var potentialCells = rack.GetComponentsInChildren<Image>(true)
                .Where(image => image.name.StartsWith("Potential Cell")).ToArray();
            Assert.That(potentialCells, Has.Length.EqualTo(3));
            Assert.That(potentialCells.All(cell => cell.enabled && cell.sprite != null), Is.True, "the rack exposes all three committed potential cells");

            var beforeEvolution = controller.WeaponRuntime;
            controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 5);
            controller.SetUpgradeOffersForTests(new UpgradeOffer("hwando_moon_eclipse", UpgradeKind.Evolution, 5));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);
            Assert.That(beforeEvolution.IsDisposedForTests, Is.True);
            Assert.That(controller.WeaponRuntime.IsEvolvedForTests(WeaponId.HwandoFlyingBlade), Is.True);
            Assert.That(controller.WeaponRuntime.RegistrationCountForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(1));
            Assert.That(controller.AffixProfileForTests(WeaponId.HwandoFlyingBlade).PotentialIds, Is.EqualTo(profile.PotentialIds));

            controller.ResetRunForTests();
            yield return null;
            Assert.That(controller.AffixProfileForTests(WeaponId.HwandoFlyingBlade), Is.Null);
            Assert.That(reveal.IsRevealing, Is.False);
            Assert.That(rack.GetComponentsInChildren<Image>(true).Where(image => image.name.StartsWith("Potential Cell")).All(cell => cell.transform.localScale == Vector3.one), Is.True);
        }

        private sealed class PerfectThreeLineRandom : IAffixRandom
        {
            private int unitCall;
            public int NextIndex(int exclusiveMax) => 0;
            public double NextUnit() => unitCall++ == 0 ? .99d : 0d;
        }
    }
}
