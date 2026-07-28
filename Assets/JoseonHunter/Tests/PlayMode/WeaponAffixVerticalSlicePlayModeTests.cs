using System.Collections;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Presentation.UI;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
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

            controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 5);
            var beforeEvolution = controller.WeaponRuntime;
            controller.OpenUpgradeOffersForTests(new UpgradeOffer("hwando_moon_eclipse", UpgradeKind.Evolution, 5));
            Assert.That(controller.TryChooseUpgrade(0), Is.True);
            Assert.That(beforeEvolution.IsDisposedForTests, Is.True);
            Assert.That(controller.WeaponRuntime.IsEvolvedForTests(WeaponId.HwandoFlyingBlade), Is.True);
            Assert.That(controller.WeaponRuntime.RegistrationCountForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(1));
            Assert.That(controller.AffixProfileForTests(WeaponId.HwandoFlyingBlade).PotentialIds, Is.EqualTo(profile.PotentialIds));

            var catalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            var venomMask = MaskFor(catalog, WeaponPotentialId.HwandoVenomFang);
            var shadowMask = MaskFor(catalog, WeaponPotentialId.HwandoReturningAfterimage);
            var danceMask = MaskFor(catalog, WeaponPotentialId.HwandoFlyingBladeDance);
            Assert.That(controller.RegisterCombatTargetForTests(new TestTarget(8101, new Float2(1f, 0f), venomMask)), Is.True);
            Assert.That(controller.RegisterCombatTargetForTests(new TestTarget(8102, new Float2(1.04f, 0f), shadowMask)), Is.True);
            for (var targetId = 8103; targetId < 8109; targetId++)
                Assert.That(controller.RegisterCombatTargetForTests(new TestTarget(targetId, new Float2(1f + (targetId - 8103) * .05f, 0f), danceMask)), Is.True);

            var combatEvents = new System.Collections.Generic.List<ConfirmedDamageEvent>();
            controller.CombatDamageService.DamageConfirmed += combatEvents.Add;
            var combatRoot = new GameObject("Task8 Hwando confirmed-contact root");
            var hwando = (FlyingBladeExecutor)controller.WeaponRuntime.ExecutorForTests(WeaponId.HwandoFlyingBlade);
            for (var tick = 0; tick < 120 && (!combatEvents.Any(e => e.Phase == ContactPhase.Poison) || !combatEvents.Any(e => e.Phase == ContactPhase.PotentialChain)); tick++)
                controller.WeaponRuntime.Tick(.05f, Vector2.zero, combatRoot.transform, null, 0);

            var poison = combatEvents.Where(e => e.Phase == ContactPhase.Poison).ToArray();
            var shadow = combatEvents.Where(e => e.Phase == ContactPhase.PotentialChain).ToArray();
            var direct = combatEvents.Where(e => e.Phase == ContactPhase.Outbound || e.Phase == ContactPhase.Inbound).ToArray();
            Assert.That(poison, Is.Not.Empty, "Venom Fang uses the committed potential mask before creating poison ticks.");
            Assert.That(shadow, Is.Not.Empty, "Returning Afterimage uses the committed potential mask before its delayed child hit.");
            Assert.That(direct.Select(e => e.TargetRuntimeId).Distinct().Count(), Is.GreaterThanOrEqualTo(5));
            Assert.That(direct.Max(e => e.FinalDamage), Is.EqualTo(Mathf.CeilToInt(hwando.BaseDamage * 1.6f)), "Blade Dance caps its distinct-target ramp at 60%.");
            Assert.That(poison.Concat(shadow).Select(e => e.AttackInstanceId).Distinct().Count(), Is.EqualTo(poison.Concat(shadow).Count()));

            // Continue only until a new inbound shadow and an active poison stream coexist, then reset the real run.
            for (var tick = 0; tick < 80 && (hwando.PendingAfterimageCountForTests == 0 || controller.WeaponRuntime.AffixStatuses.PeriodicEffectCountForTests == 0); tick++)
                controller.WeaponRuntime.Tick(.01f, Vector2.zero, combatRoot.transform, null, 0);
            Assert.That(hwando.PendingAfterimageCountForTests, Is.GreaterThan(0));
            Assert.That(controller.WeaponRuntime.AffixStatuses.PeriodicEffectCountForTests, Is.GreaterThan(0));
            Assert.That(controller.CombatDamageService.TrackedAttackCount, Is.GreaterThan(0));
            rack.Pulse(WeaponId.HwandoFlyingBlade.Value, 5, 3);
            var liveRuntime = controller.WeaponRuntime;
            var liveDamage = controller.CombatDamageService;

            controller.ResetRunForTests();
            yield return null;
            Assert.That(controller.AffixProfileForTests(WeaponId.HwandoFlyingBlade), Is.Null);
            Assert.That(beforeEvolution.IsDisposedForTests, Is.True);
            Assert.That(liveRuntime.IsDisposedForTests, Is.True);
            Assert.That(liveDamage.TrackedAttackCount, Is.Zero);
            Assert.That(controller.CombatDamageService.TrackedAttackCount, Is.Zero);
            Assert.That(controller.WeaponRuntime.AffixStatuses.PeriodicEffectCountForTests, Is.Zero);
            Assert.That(reveal.IsRevealing, Is.False);
            Assert.That(rack.GetComponentsInChildren<Image>(true).Where(image => image.name.StartsWith("Potential Cell")).All(cell => cell.transform.localScale == Vector3.one), Is.True);
            Object.Destroy(combatRoot);
        }

        private static PixelHitMask MaskFor(WeaponAffixPresentationCatalogAsset catalog, WeaponPotentialId potential)
        {
            Assert.That(catalog, Is.Not.Null);
            var sprite = catalog.SpriteForPotential(potential);
            var texture = catalog.MaskForPotential(potential);
            Assert.That(sprite, Is.Not.Null, potential.Value);
            Assert.That(texture, Is.Not.Null, potential.Value);
            return PixelHitMask.FromTexture(texture, sprite.pivot, sprite.pixelsPerUnit);
        }

        private sealed class PerfectThreeLineRandom : IAffixRandom
        {
            private int unitCall;
            public int NextIndex(int exclusiveMax) => 0;
            public double NextUnit() => unitCall++ == 0 ? .99d : 0d;
        }
    }
}
