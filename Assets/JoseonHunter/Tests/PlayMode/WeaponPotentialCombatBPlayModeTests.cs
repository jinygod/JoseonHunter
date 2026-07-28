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

        [TestCase("jangseung_ghost_face", false)] [TestCase("jangseung_ghost_face", true)]
        [TestCase("jangseung_four_direction_barrier", false)] [TestCase("jangseung_four_direction_barrier", true)]
        [TestCase("jangseung_guardian_descent", false)] [TestCase("jangseung_guardian_descent", true)]
        public void Each_jangseung_potential_uses_a_moving_pixel_confirmed_crossing(string potentialValue, bool evolved)
        {
            var potential = new WeaponPotentialId(potentialValue);
            var rig = Drive(potential, evolved, MaskFor(potential));
            rig.Target.Position = new Float2(-3f, 0f); rig.TickExact(.01f);
            if (evolved) rig.Advance(.4f); // all four posts must be active before a crossing is legal.
            rig.Target.Position = new Float2(0f, 0f); rig.Advance(1.25f);
            Assert.That(rig.Events.Any(value => value.Phase == ContactPhase.BoundaryCrossing), Is.True, potential.Value);
            if (potential.Equals(WeaponPotentialId.JangseungGhostFace)) Assert.That(((JangseungWardExecutor)rig.Executor).GhostFaceApplicationsForTests, Is.EqualTo(1));
            if (potential.Equals(WeaponPotentialId.JangseungFourDirectionBarrier)) Assert.That(rig.Events.Any(value => value.Phase == ContactPhase.PotentialBlast && value.FinalDamage == 7), Is.True);
            if (potential.Equals(WeaponPotentialId.JangseungGuardianDescent))
            {
                Assert.That(rig.Events.Count(value => value.Phase == ContactPhase.PotentialChain), Is.EqualTo(1));
                var ward = (JangseungWardExecutor)rig.Executor;
                Assert.That(ward.GuardianSpawnsForTests, Is.EqualTo(1));
                Assert.That(ward.ActiveGuardianCountForTests, Is.Zero, "one visible guardian retires after its 1.2s lifetime");
            }
            rig.Dispose();
        }

        [TestCase("singijeon_powder_trail", false)] [TestCase("singijeon_powder_trail", true)]
        [TestCase("singijeon_submunition_split", false)] [TestCase("singijeon_submunition_split", true)]
        [TestCase("singijeon_chain_ignition", false)] [TestCase("singijeon_chain_ignition", true)]
        public void Each_singijeon_potential_has_an_explicit_reachable_or_focus_only_contract(string potentialValue, bool evolved)
        {
            var potential = new WeaponPotentialId(potentialValue);
            var rig = Drive(potential, evolved, MaskFor(potential));
            var executor = (SingijeonExecutor)rig.Executor;
            if (potential.Equals(WeaponPotentialId.SingijeonPowderTrail))
            {
                rig.Target.Position = new Float2(3f, 0f); rig.TickExact(.1f);
                rig.Target.Position = new Float2(.7f, 0f); rig.Advance(.45f);
                Assert.That(executor.ActiveTrailCountForTests, Is.GreaterThan(0));
                Assert.That(rig.Events.Count(value => value.Phase == ContactPhase.Burn), Is.GreaterThan(0));
            }
            else if (!evolved)
            {
                // Split and retarget are focus-rocket-only; normal volleys must own the
                // profile safely without inventing a scout/normal child branch.
                rig.Advance(1f);
                Assert.That(executor.FocusProjectileCount, Is.Zero);
                Assert.That(executor.UnlaunchedFocusCountForTests, Is.Zero);
            }
            else
            {
                if (potential.Equals(WeaponPotentialId.SingijeonChainIgnition))
                {
                    executor = (SingijeonExecutor)rig.Executor;
                    rig.AddTarget(2, new Float2(1.5f, 0f), MaskFor(potential));
                    executor.BeforeFocusPotentialCheckForTests = target => { target.ApplyResolvedDamage(1000); return true; };
                }
                rig.Advance(1.2f);
                Assert.That(executor.FocusProjectileCount, Is.EqualTo(8));
                if (potential.Equals(WeaponPotentialId.SingijeonSubmunitionSplit))
                {
                    Assert.That(executor.SplitChildAttackIdsForTests.Count, Is.EqualTo(3));
                    Assert.That(executor.SplitChildAttackIdsForTests.Distinct().Count(), Is.EqualTo(3));
                    var childHits = rig.Events.Where(value => executor.SplitChildAttackIdsForTests.Contains(value.AttackInstanceId)).ToArray();
                    Assert.That(childHits.Length, Is.EqualTo(3));
                    Assert.That(childHits.All(value => value.FinalDamage == 4), Is.True);
                    Assert.That(childHits.Select(value => value.AttackInstanceId).Distinct().Count(), Is.EqualTo(3));
                }
                if (potential.Equals(WeaponPotentialId.SingijeonChainIgnition)) Assert.That(((SingijeonExecutor)rig.Executor).FocusRetargetCountForTests, Is.EqualTo(1));
            }
            rig.Dispose();
        }

        [TestCase("frost_crack_mark", false)] [TestCase("frost_crack_mark", true)]
        [TestCase("frost_spread", false)] [TestCase("frost_spread", true)]
        [TestCase("frost_mist", false)] [TestCase("frost_mist", true)]
        public void Each_frost_potential_has_one_isolated_natural_expiry(string potentialValue, bool evolved)
        {
            var potential = new WeaponPotentialId(potentialValue);
            var rig = Drive(potential, evolved, MaskFor(potential));
            var nearby = rig.AddTarget(2, new Float2(1.1f, 0f), MaskFor(potential));
            rig.Advance(.86f);
            var frost = (FrostFlaskExecutor)rig.Executor;
            Assert.That(frost.ExpiredFieldCount, Is.EqualTo(1));
            if (potential.Equals(WeaponPotentialId.FrostCrackMark)) Assert.That(rig.Events.Any(value => value.Phase == ContactPhase.Blast && value.FinalDamage == 13), Is.True);
            if (potential.Equals(WeaponPotentialId.FrostSpread))
            {
                Assert.That(nearby.Statuses.Any(value => value.StartsWith("frost:", StringComparison.Ordinal)), Is.True);
                rig.Advance(.239f);
                Assert.That(nearby.Statuses.Any(value => value.StartsWith("frost:", StringComparison.Ordinal)), Is.True);
                rig.Advance(.001f);
                Assert.That(nearby.Statuses.Any(value => value.StartsWith("frost:", StringComparison.Ordinal)), Is.False);
            }
            if (potential.Equals(WeaponPotentialId.FrostMist)) Assert.That(frost.LastFieldVisualScale, Is.EqualTo(1.5f).Within(.01f));
            rig.Dispose();
        }

        [TestCase("fan_vacuum_edge", false)] [TestCase("fan_vacuum_edge", true)]
        [TestCase("fan_distant_thunder", false)] [TestCase("fan_distant_thunder", true)]
        [TestCase("fan_returning_chain", false)] [TestCase("fan_returning_chain", true)]
        public void Each_fan_potential_uses_its_reachable_phase_contract(string potentialValue, bool evolved)
        {
            var potential = new WeaponPotentialId(potentialValue);
            var rig = Drive(potential, evolved, MaskFor(potential));
            var next = rig.AddTarget(2, new Float2(1.25f, 0f), MaskFor(potential));
            rig.TickExact(.01f); // confirmed gust schedules the only possible bleed source.
            var fan = (WindThunderFanExecutor)rig.Executor;
            if (potential.Equals(WeaponPotentialId.FanVacuumEdge))
            {
                Assert.That(fan.ActiveBleedCountForTests, Is.EqualTo(1));
                rig.Advance(.4f);
                Assert.That(rig.Events.Count(value => value.Phase == ContactPhase.Bleed && value.FinalDamage == 2), Is.EqualTo(1));
            }
            else if (potential.Equals(WeaponPotentialId.FanDistantThunder))
            {
                rig.Advance(evolved ? .25f : .15f); if (!evolved) rig.TickExact(.01f);
                Assert.That(rig.Events.Any(value => value.Phase == ContactPhase.Lightning && value.FinalDamage == 18), Is.True,
                    "cast-relative projection 1 / range 5 gives 1.15x over the level-five 15-damage lightning base");
            }
            else if (!evolved)
            {
                rig.Advance(.3f);
                Assert.That(rig.Events.Any(value => value.Phase == ContactPhase.Inbound), Is.False, "normal fan has no inbound return leg to schedule a chain");
            }
            else
            {
                // Gust and the first outbound strike leave the primary alive.  Lower it only
                // after that outbound event so the inbound hit itself is the killing event.
                rig.Advance(.21f); rig.Target.ApplyResolvedDamage(74); rig.Advance(.08f); rig.Advance(.08f);
                Assert.That(fan.PendingChainForTests, Is.True);
                rig.Advance(.079f);
                Assert.That(rig.Events.Any(value => value.Phase == ContactPhase.PotentialChain), Is.False);
                rig.TickExact(.001f);
                var chains = rig.Events.Where(value => value.Phase == ContactPhase.PotentialChain).ToArray();
                Assert.That(chains.Length, Is.EqualTo(1));
                Assert.That(chains[0].TargetRuntimeId, Is.EqualTo(next.RuntimeId));
                Assert.That(chains[0].FinalDamage, Is.EqualTo(5));
            }
            rig.Dispose();
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
        public void Frost_spread_capacity_eviction_consumes_the_current_frame_residual_once()
        {
            var one = Drive(WeaponPotentialId.FrostSpread, false, MaskFor(WeaponPotentialId.FrostSpread), frostCooldown: .1f);
            var oneNear = one.AddTarget(2, new Float2(1.1f, 0f), MaskFor(WeaponPotentialId.FrostSpread));
            one.Advance(.16f); // launch, then capacity-evict at the next cooldown boundary
            Assert.That(((FrostFlaskExecutor)one.Executor).ActiveSpreadResidenceCountForTests, Is.GreaterThan(0));
            ((FrostFlaskExecutor)one.Executor).SuppressNewCastsForTests = true;
            one.TickExact(.25f);
            Assert.That(oneNear.Statuses.Any(value => value.StartsWith("frost:", StringComparison.Ordinal)), Is.False);

            var split = Drive(WeaponPotentialId.FrostSpread, false, MaskFor(WeaponPotentialId.FrostSpread), frostCooldown: .1f);
            var splitNear = split.AddTarget(2, new Float2(1.1f, 0f), MaskFor(WeaponPotentialId.FrostSpread));
            split.Advance(.16f); ((FrostFlaskExecutor)split.Executor).SuppressNewCastsForTests = true; split.TickExact(.1f); split.TickExact(.15f);
            Assert.That(splitNear.Statuses.Any(value => value.StartsWith("frost:", StringComparison.Ordinal)), Is.False);
            Assert.That(EventSignature(one.Events), Is.EqualTo(EventSignature(split.Events)));
            one.Dispose(); split.Dispose();
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
            singijeon.Target.Position = new Float2(.7f, 0f); // keep it contact-eligible for a broken stale trail.
            ((SingijeonExecutor)singijeon.Executor).SuppressNewCastsForTests = true;
            var before = singijeon.Events.Count; singijeon.TickExact(.8f);
            Assert.That(singijeon.Events.Count, Is.EqualTo(before), "reset retires live trail attacks");
            Assert.That(singijeon.Runtime.DamageService.TrackedAttackCount, Is.Zero);
            singijeon.Runtime.Targets.Unregister(singijeon.Target);
            var reused = singijeon.AddTarget(1, new Float2(3f, 0f), MaskFor(WeaponPotentialId.SingijeonPowderTrail));
            singijeon.Executor.Reset(); singijeon.TickExact(.1f); // fresh ID's first observation cannot be a crossing.
            Assert.That(reused.RuntimeId, Is.EqualTo(1));
            var freshStart = singijeon.Events.Count;
            reused.Position = new Float2(.7f, 0f); singijeon.Advance(.4f);
            Assert.That(singijeon.Events.Skip(freshStart).Count(value => value.Phase == ContactPhase.Burn), Is.EqualTo(1), "fresh observation followed by out-to-in movement crosses exactly once");
            singijeon.Dispose();

            var fan = Drive(WeaponPotentialId.FanVacuumEdge, true, MaskFor(WeaponPotentialId.FanVacuumEdge), WeaponPotentialId.FanReturningChain);
            fan.Advance(.5f);
            Assert.That(((WindThunderFanExecutor)fan.Executor).ActiveBleedCountForTests, Is.GreaterThan(0));
            fan.Executor.Dispose();
            fan.Target.Position = new Float2(1f, 0f); // retain the original bleed contact target.
            ((WindThunderFanExecutor)fan.Executor).SuppressNewCastsForTests = true;
            before = fan.Events.Count; fan.TickExact(.8f);
            Assert.That(fan.Events.Count, Is.EqualTo(before), "dispose retires bleed and pending chain attacks");
            Assert.That(fan.Runtime.DamageService.TrackedAttackCount, Is.Zero);
            fan.Runtime.Dispose(); UnityEngine.Object.DestroyImmediate(fan.Root);
        }

        private static void AssertFrameSplit(WeaponPotentialId potential, float boundary)
        {
            var one = Drive(potential, true, MaskFor(potential));
            PrepareDelayedWork(potential, one);
            var oneStart = one.Events.Count;
            one.TickExact(boundary + .02f);
            var split = Drive(potential, true, MaskFor(potential));
            PrepareDelayedWork(potential, split);
            var splitStart = split.Events.Count;
            split.TickExact(boundary); split.TickExact(.02f);
            var oneDelayed = NamedDelayedEvents(potential, one.Events.Skip(oneStart)).ToArray();
            var splitDelayed = NamedDelayedEvents(potential, split.Events.Skip(splitStart)).ToArray();
            Assert.That(oneDelayed, Is.Not.Empty, potential.Value + " must create and then advance its named delayed work");
            Assert.That(EventSignature(oneDelayed), Is.EqualTo(EventSignature(splitDelayed)), potential.Value + " at " + boundary);
            one.Dispose(); split.Dispose();
        }

        private static IEnumerable<ConfirmedDamageEvent> NamedDelayedEvents(WeaponPotentialId potential, IEnumerable<ConfirmedDamageEvent> events)
        {
            if (potential.Equals(WeaponPotentialId.JangseungFourDirectionBarrier)) return events.Where(value => value.Phase == ContactPhase.PotentialBlast);
            if (potential.Equals(WeaponPotentialId.FanReturningChain)) return events.Where(value => value.Phase == ContactPhase.PotentialChain);
            if (potential.Equals(WeaponPotentialId.FanVacuumEdge)) return events.Where(value => value.Phase == ContactPhase.Bleed);
            if (potential.Equals(WeaponPotentialId.SingijeonPowderTrail)) return events.Where(value => value.Phase == ContactPhase.Burn);
            return events.Where(value => value.Phase == ContactPhase.Blast);
        }

        private static void PrepareDelayedWork(WeaponPotentialId potential, DrivenExecutor rig)
        {
            if (potential.Equals(WeaponPotentialId.SingijeonPowderTrail))
            {
                rig.Target.Position = new Float2(3f, 0f); rig.TickExact(.1f);
                rig.Target.Position = new Float2(.7f, 0f); rig.TickExact(.05f);
                Assert.That(((SingijeonExecutor)rig.Executor).ActiveTrailCountForTests, Is.GreaterThan(0));
                return;
            }
            if (potential.Equals(WeaponPotentialId.FanVacuumEdge))
            {
                rig.TickExact(.01f);
                Assert.That(((WindThunderFanExecutor)rig.Executor).ActiveBleedCountForTests, Is.EqualTo(1));
                return;
            }
            if (potential.Equals(WeaponPotentialId.FrostCrackMark))
            {
                rig.TickExact(.05f);
                Assert.That(((FrostFlaskExecutor)rig.Executor).ActiveFieldCount, Is.EqualTo(1));
                return;
            }
            if (potential.Equals(WeaponPotentialId.JangseungFourDirectionBarrier))
            {
                rig.Target.Position = new Float2(-3f, 0f); rig.TickExact(.01f);
                rig.Target.Position = new Float2(0f, 0f); rig.TickExact(.4f);
                Assert.That(((JangseungWardExecutor)rig.Executor).ActiveBarrierCountForTests, Is.EqualTo(1));
                return;
            }
            // Evolved fan only: make the inbound strike the kill and leave its .08 chain
            // pending before the split comparison begins.
            rig.AddTarget(2, new Float2(1.25f, 0f), MaskFor(WeaponPotentialId.FanReturningChain));
            rig.TickExact(.01f); rig.Advance(.21f); rig.Target.ApplyResolvedDamage(74); rig.TickExact(.08f); rig.TickExact(.08f);
            Assert.That(((WindThunderFanExecutor)rig.Executor).PendingChainForTests, Is.True);
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

        private static DrivenExecutor Drive(WeaponPotentialId primary, bool evolved, PixelHitMask targetMask, WeaponPotentialId secondary = default, WeaponPotentialId tertiary = default, Action<TestTarget, ConfirmedDamageEvent> beforeExecutorDamage = null, float frostCooldown = 99f)
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
            if (beforeExecutorDamage != null) damage.DamageConfirmed += value => beforeExecutorDamage(target, value);
            var executor = CreateExecutor(primary, runtime, evolved, modifiers, frostCooldown);
            var events = new List<ConfirmedDamageEvent>(); damage.DamageConfirmed += events.Add;
            return new DrivenExecutor(runtime, root, executor, target, events);
        }

        private static IWeaponExecutor CreateExecutor(WeaponPotentialId potential, WeaponRuntimeController runtime, bool evolved, WeaponRuntimeModifiers modifiers, float frostCooldown)
        {
            var value = potential.Value;
            if (value.StartsWith("jangseung_", StringComparison.Ordinal)) return new JangseungWardExecutor(runtime, PixelHitMask.FromRows("111", "111", "111"), 10f, .2f, 2f, 4, 1, 0f, 5, evolved, modifiers);
            if (value.StartsWith("singijeon_", StringComparison.Ordinal)) return new SingijeonExecutor(runtime, 10f, .2f, 5f, 15f, 1, 5, evolved, modifiers);
            // One field only: expiry assertions must never be satisfied by a cooldown
            // relaunch or capacity eviction from the generic rig.
            if (value.StartsWith("frost_", StringComparison.Ordinal)) return new FrostFlaskExecutor(runtime, 10f, frostCooldown, 3f, .05f, .8f, 2f, 1, 5, evolved, modifiers);
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
