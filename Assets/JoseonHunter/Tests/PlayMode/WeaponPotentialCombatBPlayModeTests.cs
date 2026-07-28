using System;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Content.Weapons;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace JoseonHunter.Tests.PlayMode
{
    /// <summary>
    /// Task 7 regression matrix.  This deliberately drives the committed executors: mask
    /// tests alone are not evidence that a potential branch is reachable in combat.
    /// </summary>
    public sealed class WeaponPotentialCombatBPlayModeTests
    {
        private static readonly WeaponPotentialId[] Potentials =
        {
            WeaponPotentialId.JangseungGhostFace, WeaponPotentialId.JangseungFourDirectionBarrier, WeaponPotentialId.JangseungGuardianDescent,
            WeaponPotentialId.SingijeonPowderTrail, WeaponPotentialId.SingijeonSubmunitionSplit, WeaponPotentialId.SingijeonChainIgnition,
            WeaponPotentialId.FrostCrackMark, WeaponPotentialId.FrostSpread, WeaponPotentialId.FrostMist,
            WeaponPotentialId.FanVacuumEdge, WeaponPotentialId.FanDistantThunder, WeaponPotentialId.FanReturningChain
        };

        [TestCaseSource(nameof(Potentials))]
        public void Every_task7_potential_has_committed_sprite_and_mask(WeaponPotentialId potential)
        {
            var catalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var sprite = catalog.SpriteForPotential(potential);
            var texture = catalog.MaskForPotential(potential);
            Assert.That(sprite, Is.Not.Null, potential.Value);
            Assert.That(texture, Is.Not.Null, potential.Value);
            Assert.That(PixelHitMask.FromTexture(texture, sprite.pivot, sprite.pixelsPerUnit), Is.Not.Null, potential.Value);
        }

        // The negative fixture intentionally keeps the base weapon pixel live at the same
        // location.  A potential must not silently borrow that base contact.
        [TestCaseSource(nameof(Potentials))]
        public void Potential_cell_is_not_the_base_weapon_pixel(WeaponPotentialId potential)
        {
            AssertBaseOnlyFixture(potential, PixelHitMask.FromRows("1"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Jangseung_ghost_barrier_and_guardian_require_their_own_cells_and_keep_distinct_attacks(bool evolved)
        {
            var negative = Drive(WeaponPotentialId.JangseungFourDirectionBarrier, evolved, PixelHitMask.FromRows("1"));
            negative.Target.Position = new Float2(-3f, 0f); negative.TickExact(.01f); negative.Target.Position = new Float2(0f, 0f); negative.Advance(.9f);
            Assert.That(negative.Events.Any(e => e.Phase == ContactPhase.PotentialBlast), Is.False);
            negative.Dispose();

            var rig = Drive(WeaponPotentialId.JangseungGhostFace, evolved, MaskFor(WeaponPotentialId.JangseungGhostFace), WeaponPotentialId.JangseungFourDirectionBarrier, WeaponPotentialId.JangseungGuardianDescent);
            rig.Target.Position = new Float2(-3f, 0f); rig.TickExact(.01f); rig.Target.Position = new Float2(0f, 0f); rig.Advance(1.25f);
            var wardEvents = rig.Events.Where(e => e.WeaponId.Equals(WeaponId.JangseungWard)).ToArray();
            Assert.That(wardEvents.Any(e => e.Phase == ContactPhase.BoundaryCrossing), Is.True);
            Assert.That(wardEvents.Any(e => e.Phase == ContactPhase.PotentialBlast), Is.True, "barrier is a finite, cell-confirmed 70% attack");
            Assert.That(wardEvents.Any(e => e.Phase == ContactPhase.PotentialChain), Is.True, "guardian selects a marked live target once");
            AssertDistinctAttackIds(wardEvents.Where(value => value.Phase == ContactPhase.BoundaryCrossing || value.Phase == ContactPhase.PotentialBlast || value.Phase == ContactPhase.PotentialChain));
            rig.Dispose();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Singijeon_trail_split_and_retarget_are_mask_gated_terminal_and_skip_dead_targets(bool evolved)
        {
            var negative = Drive(WeaponPotentialId.SingijeonPowderTrail, evolved, PixelHitMask.FromRows("1"));
            negative.Advance(1.4f);
            Assert.That(negative.Events.Any(e => e.Phase == ContactPhase.Burn), Is.False);
            negative.Dispose();

            var rig = Drive(WeaponPotentialId.SingijeonPowderTrail, evolved, MaskFor(WeaponPotentialId.SingijeonPowderTrail), WeaponPotentialId.SingijeonSubmunitionSplit, WeaponPotentialId.SingijeonChainIgnition);
            var second = rig.AddTarget(2, new Float2(1.4f, 0f), MaskFor(WeaponPotentialId.SingijeonSubmunitionSplit));
            rig.Advance(1.8f);
            var burn = rig.Events.Where(e => e.Phase == ContactPhase.Burn).ToArray();
            Assert.That(burn, Is.Not.Empty, "a real trail-cell crossing must start its own burn stream");
            Assert.That(burn.Length, Is.LessThanOrEqualTo(2), "one crossing can burn at most twice per finite trail cell");
            Assert.That(rig.Events.Any(e => e.Phase == ContactPhase.Direct), Is.True);
            Assert.That(((SingijeonExecutor)rig.Executor).ActiveTrailCountForTests, Is.GreaterThan(0));
            second.ApplyResolvedDamage(1000); rig.Advance(.8f);
            Assert.That(rig.Events.Where(e => e.TargetRuntimeId == second.RuntimeId && e.Phase == ContactPhase.Burn).All(e => e.Result.FinalDamage > 0), Is.True);
            rig.Dispose();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Frost_crack_spread_and_mist_use_cell_contact_and_exact_expiry_lifecycle(bool evolved)
        {
            var negative = Drive(WeaponPotentialId.FrostCrackMark, evolved, PixelHitMask.FromRows("1"), WeaponPotentialId.FrostSpread, WeaponPotentialId.FrostMist);
            negative.Advance(1.4f);
            Assert.That(negative.Events.Where(e => e.Phase == ContactPhase.Blast).Select(e => e.FinalDamage).All(value => value == 10), Is.True);
            negative.Dispose();

            var rig = Drive(WeaponPotentialId.FrostCrackMark, evolved, MaskFor(WeaponPotentialId.FrostCrackMark), WeaponPotentialId.FrostSpread, WeaponPotentialId.FrostMist);
            var nearby = rig.AddTarget(2, new Float2(1.1f, 0f), MaskFor(WeaponPotentialId.FrostSpread));
            rig.Advance(.86f);
            var frost = (FrostFlaskExecutor)rig.Executor;
            Assert.That(frost.LastFieldVisualScale, Is.GreaterThan(1f).And.LessThanOrEqualTo(1.5f));
            Assert.That(rig.Events.Any(e => e.Phase == ContactPhase.Blast && e.FinalDamage >= 10), Is.True);
            Assert.That(nearby.Statuses.Any(value => value.StartsWith("frost:", StringComparison.Ordinal)), Is.True, "expiry spread starts a real 0.25s residence");
            rig.Advance(.3f);
            Assert.That(nearby.Statuses.Any(value => value.StartsWith("frost:", StringComparison.Ordinal)), Is.False);
            rig.Dispose();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Fan_bleed_distance_and_return_chain_are_cell_gated_with_one_terminal_chain(bool evolved)
        {
            var negative = Drive(WeaponPotentialId.FanVacuumEdge, evolved, PixelHitMask.FromRows("1"), WeaponPotentialId.FanDistantThunder, WeaponPotentialId.FanReturningChain);
            negative.Advance(1.6f);
            Assert.That(negative.Events.Any(e => e.Phase == ContactPhase.Bleed || e.Phase == ContactPhase.PotentialChain), Is.False);
            negative.Dispose();

            var rig = Drive(WeaponPotentialId.FanVacuumEdge, evolved, MaskFor(WeaponPotentialId.FanVacuumEdge), WeaponPotentialId.FanDistantThunder, WeaponPotentialId.FanReturningChain);
            var next = rig.AddTarget(2, new Float2(1.25f, 0f), MaskFor(WeaponPotentialId.FanReturningChain));
            rig.Advance(1.8f);
            var bleeds = rig.Events.Where(e => e.Phase == ContactPhase.Bleed).ToArray();
            Assert.That(bleeds, Is.Not.Empty, "the aligned vacuum cell schedules the bleed stream");
            Assert.That(bleeds.Length, Is.LessThanOrEqualTo(4));
            Assert.That(bleeds.All(e => e.FinalDamage == 2), Is.True, "15% of modified ten damage rounds once in the combat service");
            Assert.That(rig.Events.Any(e => e.Phase == ContactPhase.Lightning), Is.True);
            Assert.That(rig.Events.Where(e => e.Phase == ContactPhase.Lightning).All(e => e.FinalDamage >= 11), Is.True);
            rig.Target.ApplyResolvedDamage(1000); rig.Advance(.5f);
            Assert.That(rig.Events.Count(e => e.Phase == ContactPhase.PotentialChain && e.TargetRuntimeId == next.RuntimeId), Is.LessThanOrEqualTo(1));
            rig.Dispose();
        }

        [Test]
        public void Task7_delayed_boundaries_are_frame_independent_and_consume_residual_once()
        {
            AssertFrameSplit(WeaponPotentialId.SingijeonPowderTrail, .3f);
            AssertFrameSplit(WeaponPotentialId.FanVacuumEdge, .4f);
            AssertFrameSplit(WeaponPotentialId.FrostCrackMark, .5f);
            AssertFrameSplit(WeaponPotentialId.SingijeonPowderTrail, .6f);
            AssertFrameSplit(WeaponPotentialId.JangseungFourDirectionBarrier, .8f);
            AssertFrameSplit(WeaponPotentialId.FanReturningChain, .08f);
        }

        [Test]
        public void Reset_and_dispose_retire_delayed_attacks_and_clear_reused_target_transform_state()
        {
            var singijeon = Drive(WeaponPotentialId.SingijeonPowderTrail, false, MaskFor(WeaponPotentialId.SingijeonPowderTrail));
            singijeon.Advance(.4f);
            Assert.That(((SingijeonExecutor)singijeon.Executor).ActiveTrailCountForTests, Is.GreaterThan(0));
            singijeon.Executor.Reset();
            Assert.That(((SingijeonExecutor)singijeon.Executor).ActiveTrailCountForTests, Is.Zero);
            singijeon.Runtime.Targets.Unregister(singijeon.Target);
            var before = singijeon.Events.Count; singijeon.TickExact(.8f);
            Assert.That(singijeon.Events.Count, Is.EqualTo(before), "reset retires live trail attacks");
            var reused = singijeon.AddTarget(1, new Float2(1f, 0f), PixelHitMask.FromRows("1"));
            singijeon.Executor.Reset(); singijeon.TickExact(.2f);
            Assert.That(reused.RuntimeId, Is.EqualTo(1));
            singijeon.Dispose();

            var fan = Drive(WeaponPotentialId.FanVacuumEdge, true, MaskFor(WeaponPotentialId.FanVacuumEdge), WeaponPotentialId.FanReturningChain);
            fan.Advance(.5f);
            Assert.That(((WindThunderFanExecutor)fan.Executor).ActiveBleedCountForTests, Is.GreaterThan(0));
            fan.Executor.Dispose();
            fan.Runtime.Targets.Unregister(fan.Target);
            before = fan.Events.Count; fan.TickExact(.8f);
            Assert.That(fan.Events.Count, Is.EqualTo(before), "dispose retires bleed and pending chain attacks");
            fan.Runtime.Dispose(); UnityEngine.Object.DestroyImmediate(fan.Root);
        }

        private static void AssertFrameSplit(WeaponPotentialId potential, float boundary)
        {
            var one = Drive(potential, true, MaskFor(potential));
            one.TickExact(boundary + .02f);
            var split = Drive(potential, true, MaskFor(potential));
            split.TickExact(boundary); split.TickExact(.02f);
            Assert.That(one.Events, Is.Not.Empty, potential.Value + " must exercise a real executor path before comparing frame splits");
            Assert.That(EventSignature(one.Events), Is.EqualTo(EventSignature(split.Events)), potential.Value + " at " + boundary);
            one.Dispose(); split.Dispose();
        }

        private static string EventSignature(IEnumerable<ConfirmedDamageEvent> events)
        {
            return string.Join("|", events.Select(value => value.Phase + ":" + value.TargetRuntimeId + ":" + value.FinalDamage + ":" + value.AttackInstanceId));
        }

        private static void AssertDistinctAttackIds(IEnumerable<ConfirmedDamageEvent> events)
        {
            var materialized = events.ToArray();
            Assert.That(materialized.Length, Is.GreaterThan(1), "fixture must actually reach multiple potential attack paths");
            Assert.That(materialized.Select(value => value.AttackInstanceId), Is.All.GreaterThan(0), "every damage path must carry an explicit attack identity");
            Assert.That(materialized.Select(value => value.AttackInstanceId).Distinct().Count(), Is.EqualTo(materialized.Length), "each child potential attack must use a fresh terminal identity");
        }

        private static DrivenExecutor Drive(WeaponPotentialId primary, bool evolved, PixelHitMask targetMask, WeaponPotentialId secondary = default, WeaponPotentialId tertiary = default)
        {
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("111", "111", "111"));
            var root = new GameObject("Task7 potential executor test");
            var ids = new List<WeaponPotentialId> { primary };
            if (!string.IsNullOrEmpty(secondary.Value)) ids.Add(secondary);
            if (!string.IsNullOrEmpty(tertiary.Value)) ids.Add(tertiary);
            var modifiers = WeaponRuntimeModifiers.From(new WeaponRunAffixProfile(Array.Empty<WeaponAffixRoll>(), ids));
            var target = new TestTarget(1, new Float2(1f, 0f), targetMask);
            registry.Register(target);
            var executor = CreateExecutor(primary, runtime, evolved, modifiers);
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;
            return new DrivenExecutor(runtime, root, executor, target, events);
        }

        private static IWeaponExecutor CreateExecutor(WeaponPotentialId potential, WeaponRuntimeController runtime, bool evolved, WeaponRuntimeModifiers modifiers)
        {
            var value = potential.Value;
            if (value.StartsWith("jangseung_", StringComparison.Ordinal)) return new JangseungWardExecutor(runtime, PixelHitMask.FromRows("111", "111", "111"), 10f, .2f, 2f, 4, 1, 0f, 5, evolved, modifiers);
            if (value.StartsWith("singijeon_", StringComparison.Ordinal)) return new SingijeonExecutor(runtime, 10f, .2f, 5f, 15f, 1, 5, evolved, modifiers);
            // One field only: expiry assertions must never be satisfied by a cooldown
            // relaunch or capacity eviction from the generic rig.
            if (value.StartsWith("frost_", StringComparison.Ordinal)) return new FrostFlaskExecutor(runtime, 10f, 99f, 3f, .05f, .8f, 2f, 1, 5, evolved, modifiers);
            return new WindThunderFanExecutor(runtime, 10f, .2f, 5f, 0f, 8, 5, evolved, modifiers);
        }

        private static PixelHitMask MaskFor(WeaponPotentialId potential)
        {
            var catalog = Resources.Load<WeaponAffixPresentationCatalogAsset>("WeaponAffixPresentationCatalog");
            Assert.That(catalog, Is.Not.Null);
            var sprite = catalog.SpriteForPotential(potential); var texture = catalog.MaskForPotential(potential);
            Assert.That(sprite, Is.Not.Null, potential.Value); Assert.That(texture, Is.Not.Null, potential.Value);
            return PixelHitMask.FromTexture(texture, sprite.pivot, sprite.pixelsPerUnit);
        }

        private static void AssertBaseOnlyFixture(WeaponPotentialId potential, PixelHitMask targetMask)
        {
            var baseMask = PixelHitMask.FromRows("111", "111", "111");
            Assert.That(PixelMaskContactService.TryFindContact(baseMask, PixelMaskTransform.Translation(1f, 0f), targetMask, PixelMaskTransform.Translation(1f, 0f), out _), Is.True, potential.Value + " base contact");
            Assert.That(PixelMaskContactService.TryFindContact(MaskFor(potential), PixelMaskTransform.Translation(1f, 0f), targetMask, PixelMaskTransform.Translation(1f, 0f), out _), Is.False, potential.Value + " potential cell non-overlap");
        }

        private sealed class DrivenExecutor : IDisposable
        {
            private int tick;
            public DrivenExecutor(WeaponRuntimeController runtime, GameObject root, IWeaponExecutor executor, TestTarget target, List<ConfirmedDamageEvent> events) { Runtime = runtime; Root = root; Executor = executor; Target = target; Events = events; }
            public WeaponRuntimeController Runtime { get; } public GameObject Root { get; } public IWeaponExecutor Executor { get; } public TestTarget Target { get; } public List<ConfirmedDamageEvent> Events { get; }
            public void TickExact(float delta) { Executor.Tick(delta, new WeaponExecutionContext(default, Root.transform, null, 0, ++tick)); Runtime.AffixStatuses.Tick(delta, tick); }
            public void Advance(float seconds) { while (seconds > .00001f) { var delta = Mathf.Min(.05f, seconds); TickExact(delta); seconds -= delta; } }
            public void AdvanceStatuses(float seconds) { while (seconds > .00001f) { var delta = Mathf.Min(.05f, seconds); Runtime.AffixStatuses.Tick(delta, ++tick); seconds -= delta; } }
            public TestTarget AddTarget(int id, Float2 position, PixelHitMask mask) { var target = new TestTarget(id, position, mask); Runtime.Targets.Register(target); return target; }
            public void Dispose() { Executor.Dispose(); Runtime.Dispose(); UnityEngine.Object.DestroyImmediate(Root); }
        }
    }
}
