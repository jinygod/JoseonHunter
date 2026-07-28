using System;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.PlayMode
{
    /// <summary>Task 6 uses the checked-in Task 4 cells in real executor paths; no synthetic potential masks are permitted here.</summary>
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
        public void Every_potential_uses_the_committed_cell_mask_for_a_real_negative_pixel_overlap(WeaponPotentialId potential)
        {
            var mask = MaskFor(potential);
            var transparent = PixelHitMask.FromRows("0");
            var overlaps = PixelMaskContactService.TryFindContact(mask, PixelMaskTransform.Translation(0f, 0f), transparent, PixelMaskTransform.Translation(0f, 0f), out _);
            var rig = Drive(potential, false, targetMask: transparent);
            Assert.That(overlaps, Is.False, potential.Value);
            Assert.That(rig.Events, Is.Empty, potential.Value + " must not produce base, status, child, or delayed damage without an active target pixel.");
            rig.Dispose();
        }

        // This is deliberately a construction-and-tick matrix: each case instantiates its actual production executor twice,
        // with its exact PixelLab hit-mask used by a live target.  The events are emitted by CombatDamageService, not recreated formulas.
        [TestCaseSource(nameof(PotentialIds))]
        public void Every_potential_drives_its_specific_executor_with_the_real_cell_mask_in_normal_and_evolved_paths(WeaponPotentialId potential)
        {
            var normal = Drive(potential, false);
            var evolved = Drive(potential, true);
            Assert.That(normal.Executor.GetType(), Is.EqualTo(evolved.Executor.GetType()), potential.Value);
            Assert.That(normal.Executor, Is.TypeOf(ExecutorTypeFor(potential)), potential.Value);
            Assert.That(((IWeaponEvolutionProfile)normal.Executor).IsEvolved, Is.False, potential.Value);
            Assert.That(((IWeaponEvolutionProfile)evolved.Executor).IsEvolved, Is.True, potential.Value);
            Assert.That(normal.Events.Count + evolved.Events.Count, Is.GreaterThan(0), potential.Value + " must reach CombatDamageService through its real executor.");
            normal.Dispose(); evolved.Dispose();
        }

        [Test]
        public void Hwando_venom_refreshes_one_periodic_stream_and_afterimage_uses_new_child_attack_identity()
        {
            var rig = Drive(WeaponPotentialId.HwandoVenomFang, false, WeaponPotentialId.HwandoReturningAfterimage);
            var poisonAttack = new AttackInstance(900, RepeatHitPolicy.TimedTicks, .5f);
            var target = rig.Target;
            Assert.That(rig.Runtime.AffixStatuses.ApplyOrRefreshPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, target.RuntimeId, target.WorldPosition, 2, 3, poisonAttack, true)), Is.True);
            Assert.That(rig.Runtime.AffixStatuses.ApplyOrRefreshPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, target.RuntimeId, target.WorldPosition, 2, 3, new AttackInstance(901, RepeatHitPolicy.TimedTicks, .5f), true)), Is.True);
            rig.Runtime.AffixStatuses.Tick(.5f, 1);
            Assert.That(rig.Events.Count(e => e.Phase == ContactPhase.Poison), Is.EqualTo(1), "refresh must replace, not parallel-stack, poison ticks");
            Assert.That(rig.Events.Select(e => e.AttackInstanceId).Distinct().Count(), Is.EqualTo(rig.Events.Count), "child attacks must never reuse a root identity");
            rig.Dispose();
        }

        [Test]
        public void Gakgung_full_draw_scales_primary_endpoints_and_split_children_do_not_recurse()
        {
            var near = Drive(WeaponPotentialId.GakgungFullDraw, false, targetPosition: new Float2(.1f, 0f), advanceSeconds: 0f);
            var far = Drive(WeaponPotentialId.GakgungFullDraw, false, targetPosition: new Float2(8f, 0f), advanceSeconds: 0f);
            near.Advance(.05f); // near impact is before the 80% ramp interval
            far.Advance(.25f); // arrow has traveled 50% of its allowed range but has not reached the target
            var nearBow = (GakgungExecutor)near.Executor; var farBow = (GakgungExecutor)far.Executor;
            Assert.That(nearBow.LastProjectileScale, Is.EqualTo(1f).Within(.01f));
            Assert.That(farBow.LastProjectileScale, Is.GreaterThan(1f).And.LessThan(1.35f));
            far.Advance(.20f); // post-80% impact clamps at 1.6x damage / 1.35x visual scale
            Assert.That(near.Events.Where(e => e.Phase == ContactPhase.Direct).Select(e => e.Result.FinalDamage).First(), Is.EqualTo(10));
            Assert.That(far.Events.Where(e => e.Phase == ContactPhase.Direct).Select(e => e.Result.FinalDamage).First(), Is.EqualTo(16));
            var split = Drive(WeaponPotentialId.GakgungSplitFletching, false);
            Assert.That(split.Events.Count(e => e.Phase == ContactPhase.PotentialChain), Is.LessThanOrEqualTo(2), "split arrows are terminal child attacks and cannot recurse");
            near.Dispose(); far.Dispose(); split.Dispose();
        }

        [Test]
        public void Thunder_delays_use_exact_timing_and_evolved_path_schedules_both_strikes()
        {
            var normal = Drive(WeaponPotentialId.ThunderEarthCurrent, false, WeaponPotentialId.ThunderLightningRod);
            var evolved = Drive(WeaponPotentialId.ThunderEarthCurrent, true, WeaponPotentialId.ThunderLightningRod);
            Assert.That(normal.Events.Any(e => e.Phase == ContactPhase.PotentialBlast), Is.True, "Earth Current resolves after its 0.35s schedule");
            Assert.That(normal.Events.Any(e => e.Phase == ContactPhase.PotentialChain), Is.True, "Lightning Rod resolves after its 0.45s schedule");
            Assert.That(evolved.Events.Any(e => e.Phase == ContactPhase.PotentialBlast), Is.True, "evolved blast must schedule Earth Current");
            Assert.That(evolved.Events.Any(e => e.Phase == ContactPhase.PotentialChain), Is.True, "evolved blast must schedule Lightning Rod");
            normal.Dispose(); evolved.Dispose();
        }

        [Test]
        public void Delayed_potential_skips_dead_or_unregistered_target()
        {
            var rig = Drive(WeaponPotentialId.ThunderLightningRod, false, advanceSeconds: .35f);
            rig.Target.ApplyResolvedDamage(1000);
            rig.Advance(.5f);
            Assert.That(rig.Events.Any(e => e.Phase == ContactPhase.PotentialChain), Is.False);
            rig.Dispose();
        }

        [Test]
        public void Talisman_transfer_is_once_has_no_transfer_contact_damage_and_ghost_seeks_nearest_live_mask_contact()
        {
            var rig = Drive(WeaponPotentialId.TalismanSealTransfer, false, WeaponPotentialId.TalismanVengefulGhostBurst, advanceSeconds: 0f);
            var second = rig.AddTarget(2, new Float2(1.5f, 0f), MaskFor(WeaponPotentialId.TalismanSealTransfer));
            var third = rig.AddTarget(3, new Float2(2f, 0f), MaskFor(WeaponPotentialId.TalismanSealTransfer));
            rig.Advance(.40f); // first seal chooses the next legal target
            second.ApplyResolvedDamage(1000);
            rig.Advance(.05f); // the sealed cast transfers exactly once to target three
            var talisman = (TalismanExecutor)rig.Executor;
            Assert.That(talisman.TransferCount, Is.EqualTo(1));
            rig.Advance(.10f);
            Assert.That(rig.Events.Any(e => e.TargetRuntimeId == third.RuntimeId && (e.Phase == ContactPhase.Direct || e.Phase == ContactPhase.Attach)), Is.False,
                "transfer flight must not apply contact damage");
            third.ApplyResolvedDamage(1000);
            var ghostTarget = rig.AddTarget(4, new Float2(1.1f, 0f), MaskFor(WeaponPotentialId.TalismanVengefulGhostBurst));
            rig.Advance(.35f);
            Assert.That(talisman.TransferCount, Is.EqualTo(1), "a transferred seal cannot transfer again");
            Assert.That(talisman.LastGhostSeekTargetRuntimeId, Is.EqualTo(ghostTarget.RuntimeId));
            Assert.That(rig.Events.Any(e => e.TargetRuntimeId == ghostTarget.RuntimeId && e.Phase == ContactPhase.PotentialBlast), Is.True);
            rig.Dispose();
        }

        [Test]
        public void Talisman_frost_lasts_1_2_seconds_then_clears_once_in_normal_and_evolved_paths()
        {
            foreach (var evolved in new[] { false, true })
            {
                var rig = Drive(WeaponPotentialId.TalismanFiveElementCycle, evolved, advanceSeconds: 0f);
                for (var elapsed = 0f; elapsed < 3f && !rig.Target.Statuses.Any(value => value.StartsWith("frost:", StringComparison.Ordinal)); elapsed += .05f) rig.Advance(.05f);
                Assert.That(rig.Target.Statuses.Any(value => value.StartsWith("frost:", StringComparison.Ordinal)), Is.True);
                rig.Runtime.Targets.Unregister(rig.Target); // freeze the source sequence; the cast-owned slow record must outlive the completed cast.
                rig.Advance(1.15f);
                Assert.That(rig.Target.Statuses.Any(value => value.StartsWith("frost:", StringComparison.Ordinal)), Is.True);
                rig.Advance(.10f);
                Assert.That(rig.Target.Statuses.Any(value => value.StartsWith("frost:", StringComparison.Ordinal)), Is.False);
                rig.Dispose();
            }
        }

        [Test]
        public void Evolved_thunder_counts_only_actual_pull_motion_caps_it_and_breaks_equal_threat_by_runtime_id()
        {
            var rig = Drive(WeaponPotentialId.ThunderOverchargedCore, true, WeaponPotentialId.ThunderLightningRod, advanceSeconds: 0f);
            rig.Target.Threat = 10f;
            var immobile = rig.AddTarget(2, new Float2(1.2f, 0f), MaskFor(WeaponPotentialId.ThunderOverchargedCore));
            immobile.MovesWithKnockback = false; immobile.Threat = 10f;
            rig.Advance(1f);
            var thunder = (ThunderBombExecutor)rig.Executor;
            Assert.That(thunder.LastPulledTargetCount, Is.EqualTo(1), "movement-immune targets cannot raise Overcharged Core damage");
            Assert.That(thunder.LastLightningRodTargetRuntimeId, Is.EqualTo(rig.Target.RuntimeId), "equal threat resolves to lower runtime id");
            Assert.That(thunder.LastPulledTargetCount * .08f, Is.LessThanOrEqualTo(.80f));
            rig.Dispose();
        }

        [Test]
        public void Overcharged_core_uses_its_own_catalog_mask_for_negative_and_positive_executor_damage()
        {
            var negative = Drive(WeaponPotentialId.ThunderOverchargedCore, true, advanceSeconds: 0f, targetMask: PixelHitMask.FromRows("1"));
            negative.Advance(1f);
            var positive = Drive(WeaponPotentialId.ThunderOverchargedCore, true, advanceSeconds: 0f, targetMask: MaskFor(WeaponPotentialId.ThunderOverchargedCore));
            positive.Advance(1f);
            var negativeDamage = negative.Events.Where(e => e.Phase == ContactPhase.Blast).Select(e => e.FinalDamage).First();
            var positiveDamage = positive.Events.Where(e => e.Phase == ContactPhase.Blast).Select(e => e.FinalDamage).First();
            Assert.That(negativeDamage, Is.EqualTo(20));
            Assert.That(positiveDamage, Is.EqualTo(22));
            negative.Dispose(); positive.Dispose();
        }

        [Test]
        public void All_twelve_potentials_have_their_exact_owner_executor_mapping()
        {
            foreach (var potential in PotentialIds)
            {
                var weapon = WeaponFor(potential);
                var expected = potential.Value.StartsWith("hwando_", StringComparison.Ordinal) ? WeaponId.HwandoFlyingBlade :
                    potential.Value.StartsWith("gakgung_", StringComparison.Ordinal) ? WeaponId.GakgungShot :
                    potential.Value.StartsWith("talisman_", StringComparison.Ordinal) ? WeaponId.TalismanThrow : WeaponId.ThunderCrashBomb;
                Assert.That(weapon, Is.EqualTo(expected), potential.Value);
            }
        }

        [Test]
        public void Lightning_rod_tracks_live_position_and_skips_unregistered_target_in_normal_and_evolved_paths()
        {
            foreach (var evolved in new[] { false, true })
            {
                var rig = Drive(WeaponPotentialId.ThunderLightningRod, evolved, advanceSeconds: 0f);
                rig.Target.Threat = 9f;
                rig.Advance(.55f); // scheduled, but before the 0.45s delayed resolution for both timing paths
                rig.Target.Position = new Float2(1.25f, 0f);
                rig.Advance(.55f);
                Assert.That(rig.Events.Any(e => e.TargetRuntimeId == rig.Target.RuntimeId && e.Phase == ContactPhase.PotentialChain), Is.True);
                var skipped = Drive(WeaponPotentialId.ThunderLightningRod, evolved, advanceSeconds: .55f);
                skipped.Runtime.Targets.Unregister(skipped.Target);
                skipped.Advance(.55f);
                Assert.That(skipped.Events.Any(e => e.Phase == ContactPhase.PotentialChain), Is.False);
                rig.Dispose(); skipped.Dispose();
            }
        }

        private static DrivenExecutor Drive(WeaponPotentialId primary, bool evolved, WeaponPotentialId secondary = default, Float2? targetPosition = null, float advanceSeconds = 2f, PixelHitMask targetMask = null)
        {
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            var root = new GameObject("Task6 potential executor test");
            var all = secondary.Value == null ? new[] { primary } : new[] { primary, secondary };
            var modifiers = WeaponRuntimeModifiers.From(new WeaponRunAffixProfile(Array.Empty<WeaponAffixRoll>(), all));
            var position = targetPosition ?? new Float2(1f, 0f);
            var target = new TestTarget(1, position, targetMask ?? MaskFor(primary));
            registry.Register(target);
            var executor = CreateExecutor(primary, runtime, evolved, modifiers);
            var events = new List<ConfirmedDamageEvent>();
            damage.DamageConfirmed += events.Add;
            var result = new DrivenExecutor(runtime, root, executor, target, events, primary);
            result.Advance(advanceSeconds);
            return result;
        }

        private static IWeaponExecutor CreateExecutor(WeaponPotentialId potential, WeaponRuntimeController runtime, bool evolved, WeaponRuntimeModifiers modifiers)
        {
            var weapon = WeaponFor(potential);
            if (weapon.Equals(WeaponId.HwandoFlyingBlade)) return new FlyingBladeExecutor(runtime, 10f, 10f, 4f, 20f, 1, evolved, modifiers);
            if (weapon.Equals(WeaponId.GakgungShot)) return new GakgungExecutor(runtime, 10f, 10f, 10f, 20f, 1, evolved, modifiers);
            if (weapon.Equals(WeaponId.TalismanThrow)) return new TalismanExecutor(runtime, 10f, .05f, 4f, 20f, 3, 1, evolved, modifiers);
            return new ThunderBombExecutor(runtime, 10f, 10f, 4f, .1f, 0f, 2f, 1, evolved, modifiers);
        }

        private static Type ExecutorTypeFor(WeaponPotentialId potential)
        {
            var weapon = WeaponFor(potential);
            if (weapon.Equals(WeaponId.HwandoFlyingBlade)) return typeof(FlyingBladeExecutor);
            if (weapon.Equals(WeaponId.GakgungShot)) return typeof(GakgungExecutor);
            if (weapon.Equals(WeaponId.TalismanThrow)) return typeof(TalismanExecutor);
            return typeof(ThunderBombExecutor);
        }

        private static WeaponId WeaponFor(WeaponPotentialId potential)
        {
            var id = potential.Value;
            if (id.StartsWith("hwando_", StringComparison.Ordinal)) return WeaponId.HwandoFlyingBlade;
            if (id.StartsWith("gakgung_", StringComparison.Ordinal)) return WeaponId.GakgungShot;
            if (id.StartsWith("talisman_", StringComparison.Ordinal)) return WeaponId.TalismanThrow;
            if (id.StartsWith("thunder_", StringComparison.Ordinal)) return WeaponId.ThunderCrashBomb;
            throw new ArgumentOutOfRangeException(nameof(potential), potential.Value, "Task 6 potential has no owning weapon.");
        }

        private static PixelHitMask MaskFor(WeaponPotentialId potential)
        {
            var catalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            Assert.That(catalog, Is.Not.Null, "Task 4 catalog must be present for potential combat tests.");
            var sprite = catalog.SpriteForPotential(potential); var texture = catalog.MaskForPotential(potential);
            Assert.That(sprite, Is.Not.Null, potential.Value); Assert.That(texture, Is.Not.Null, potential.Value);
            return PixelHitMask.FromTexture(texture, sprite.pivot, sprite.pixelsPerUnit);
        }

        private sealed class DrivenExecutor : IDisposable
        {
            private int tick;
            public DrivenExecutor(WeaponRuntimeController runtime, GameObject root, IWeaponExecutor executor, TestTarget target, List<ConfirmedDamageEvent> events, WeaponPotentialId potential)
            { Runtime = runtime; Root = root; Executor = executor; Target = target; Events = events; Potential = potential; }
            public WeaponRuntimeController Runtime { get; } public GameObject Root { get; } public IWeaponExecutor Executor { get; } public TestTarget Target { get; } public List<ConfirmedDamageEvent> Events { get; } public WeaponPotentialId Potential { get; }
            public void Advance(float seconds) { for (var elapsed = 0f; elapsed < seconds; elapsed += .05f) Executor.Tick(.05f, new WeaponExecutionContext(default, Root.transform, null, 0, ++tick)); }
            public TestTarget AddTarget(int runtimeId, Float2 position, PixelHitMask mask)
            {
                var target = new TestTarget(runtimeId, position, mask); Runtime.Targets.Register(target); return target;
            }
            public void Dispose() { Executor.Dispose(); Runtime.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }
    }
}
