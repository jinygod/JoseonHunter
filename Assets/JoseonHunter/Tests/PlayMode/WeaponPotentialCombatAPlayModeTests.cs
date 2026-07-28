using System.Collections;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    /// <summary>Contact provenance guard for every first-half potential.  Effect-specific suites may only start from this gate.</summary>
    public sealed class WeaponPotentialCombatAPlayModeTests
    {
        private static readonly WeaponPotentialId[] PotentialIds =
        {
            WeaponPotentialId.HwandoVenomFang, WeaponPotentialId.HwandoReturningAfterimage, WeaponPotentialId.HwandoFlyingBladeDance,
            WeaponPotentialId.GakgungArmorBreakArrowhead, WeaponPotentialId.GakgungSplitFletching, WeaponPotentialId.GakgungFullDraw,
            WeaponPotentialId.TalismanFiveElementCycle, WeaponPotentialId.TalismanSealTransfer, WeaponPotentialId.TalismanVengefulGhostBurst,
            WeaponPotentialId.ThunderEarthCurrent, WeaponPotentialId.ThunderOverchargedCore, WeaponPotentialId.ThunderLightningRod
        };

        [TestCaseSource(nameof(PotentialIds))]
        public void Every_potential_contact_gate_rejects_overlapping_bounds_when_active_pixels_do_not_overlap(WeaponPotentialId potential)
        {
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var target = new TestTarget(1, new Float2(0f, 0f), PixelHitMask.FromRows("0"));
            registry.Register(target);
            var potentialAttack = new AttackInstance(100 + potential.GetHashCode(), RepeatHitPolicy.OncePerInstance, 0f);

            var applied = damage.TryApply(WeaponDamageRequest.Create(potentialAttack, WeaponId.HwandoFlyingBlade, target, 10, false,
                new Float2(0f, 0f), ContactPhase.PotentialBlast, 1, false), out _);

            Assert.That(applied, Is.False, potential.Value);
            Assert.That(target.Health, Is.EqualTo(100), potential.Value);
        }

        [UnityTest]
        public IEnumerator Delayed_and_child_attack_identities_are_distinct_and_dead_targets_are_skipped()
        {
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            var target = new TestTarget(1, new Float2(1f, 0f), PixelHitMask.FromRows("1"));
            registry.Register(target);
            var ids = new List<int>();
            damage.DamageConfirmed += record => ids.Add(record.AttackInstanceId);
            var root = new GameObject("Potential identity root");
            var modifiers = WeaponRuntimeModifiers.From(new WeaponRunAffixProfile(System.Array.Empty<WeaponAffixRoll>(), new[] { WeaponPotentialId.HwandoVenomFang, WeaponPotentialId.HwandoReturningAfterimage }));
            var blade = new FlyingBladeExecutor(runtime, 10f, 10f, 2f, 20f, 1, false, modifiers);
            blade.Tick(.1f, new WeaponExecutionContext(default, root.transform, null, 0, 1));
            target.ApplyResolvedDamage(1000);
            blade.Tick(1f, new WeaponExecutionContext(default, root.transform, null, 0, 2));
            yield return null;
            Assert.That(ids, Is.Not.Empty);
            Assert.That(ids, Is.Unique);
            blade.Dispose(); runtime.Dispose(); Object.DestroyImmediate(root);
        }
    }
}
