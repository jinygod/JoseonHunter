using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace JoseonHunter.Tests.PlayMode
{
    public sealed class EvolvedWeaponCombatPlayModeTests
    {
        [Test]
        public void Runtime_rejects_duplicate_weapon_registration_without_second_tick_or_dispose_slot()
        {
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            var first = new CountingExecutor();
            var second = new CountingExecutor();
            var root = new GameObject("Duplicate registration test root");
            runtime.Register(WeaponId.HwandoFlyingBlade, first);

            Assert.Throws<System.InvalidOperationException>(() => runtime.Register(WeaponId.HwandoFlyingBlade, second));
            runtime.Tick(0.1f, Vector2.zero, root.transform, null, 0);
            runtime.Dispose();
            Object.DestroyImmediate(root);

            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(second.TickCount, Is.EqualTo(0));
            Assert.That(first.DisposeCount, Is.EqualTo(1));
            Assert.That(second.DisposeCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Evolved_factory_adapts_live_telemetry_for_every_weapon()
        {
            var executors = new HashSet<IWeaponExecutor>();
            foreach (var weaponId in WeaponRoster.All)
            {
                using (var rig = EvolvedWeaponTestRig.For(weaponId))
                {
                    rig.AddTarget(new Vector2(1f, 0f));
                    yield return rig.AdvanceSeconds(0.6f);
                    var registered = rig.Runtime.ExecutorForTests(weaponId);
                    var telemetry = EvolvedExecutorFactory.ReadTelemetry(registered);

                    Assert.That(registered, Is.SameAs(rig.Executor));
                    Assert.That(rig.Runtime.IsEvolvedForTests(weaponId), Is.True);
                    Assert.That(rig.Runtime.RegistrationCountForTests(weaponId), Is.EqualTo(1));
                    Assert.That(rig.Runtime.RegisteredExecutorSlotCountForTests, Is.EqualTo(1));
                    Assert.That(telemetry.WeaponId, Is.EqualTo(weaponId));
                    Assert.That(telemetry.IsEvolved, Is.True);
                    Assert.That(telemetry.ExecutorKind, Is.Not.Empty);
                    Assert.That(telemetry.CurrentState, Is.Not.Empty);
                    Assert.That(telemetry.PrimaryObservedCount, Is.GreaterThan(0));
                    Assert.That(executors.Add(registered), Is.True);
                }
            }
        }

        [UnityTest]
        public IEnumerator Choosing_evolution_keeps_weapon_level_and_rebuilds_evolved_executor()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;
            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            controller.SetWeaponLevelForTests(WeaponId.HwandoFlyingBlade, 5);
            var preChoiceRuntime = controller.WeaponRuntime;
            var preChoiceExecutor = preChoiceRuntime.ExecutorForTests(WeaponId.HwandoFlyingBlade);
            Assert.That(preChoiceExecutor, Is.Not.Null);
            Assert.That(preChoiceRuntime.IsEvolvedForTests(WeaponId.HwandoFlyingBlade), Is.False);
            Assert.That(preChoiceRuntime.RegistrationCountForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(1));
            Assert.That(preChoiceRuntime.RegisteredExecutorSlotCountForTests, Is.EqualTo(1));
            controller.UnlockEvolutionForTests("hwando_moon_eclipse");
            controller.OpenUpgradeForTests();

            var index = controller.CurrentOffers
                .Select((offer, i) => (offer, i))
                .Single(pair => pair.offer.Id == "hwando_moon_eclipse").i;
            Assert.That(controller.TryChooseUpgrade(index), Is.True);

            Assert.That(controller.WeaponLevelForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(5));
            Assert.That(controller.AcquiredEvolutionIds, Contains.Item("hwando_moon_eclipse"));
            Assert.That(controller.WeaponRuntime, Is.Not.SameAs(preChoiceRuntime));
            Assert.That(controller.WeaponRuntime.ExecutorForTests(WeaponId.HwandoFlyingBlade), Is.Not.SameAs(preChoiceExecutor));
            Assert.That(preChoiceRuntime.IsDisposedForTests, Is.True);
            Assert.That(preChoiceRuntime.RegistrationCountForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(0));
            Assert.That(preChoiceRuntime.RegisteredExecutorSlotCountForTests, Is.EqualTo(0));
            Assert.That(controller.WeaponRuntime.IsEvolvedForTests(WeaponId.HwandoFlyingBlade), Is.True);
            Assert.That(controller.WeaponRuntime.RegistrationCountForTests(WeaponId.HwandoFlyingBlade), Is.EqualTo(1));
            Assert.That(controller.WeaponRuntime.RegisteredExecutorSlotCountForTests, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Moon_eclipse_keeps_outbound_and_return_contact_then_blasts_at_crossing()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.HwandoFlyingBlade))
            {
                rig.AddTarget(new Vector2(2f, 0f));
                rig.AddTarget(new Vector2(0.2f, 0f));
                yield return rig.AdvanceSeconds(2f);

                CollectionAssert.Contains(rig.ContactPhases, ContactPhase.Direct);
                CollectionAssert.Contains(rig.ContactPhases, ContactPhase.Inbound);
                CollectionAssert.Contains(rig.ContactPhases, ContactPhase.Blast);
            }
        }

        [UnityTest]
        public IEnumerator Sun_piercer_fires_one_high_pierce_shot_on_cadence()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.GakgungShot))
            {
                rig.AddTarget(new Vector2(3f, 0f));
                yield return rig.AdvanceCasts(4);

                Assert.That(rig.Telemetry.LastProjectileMaximumImpacts, Is.GreaterThanOrEqualTo(6));
                Assert.That(rig.Telemetry.LastProjectileScale, Is.GreaterThan(1f));
            }
        }

        [UnityTest]
        public IEnumerator Sun_piercer_keeps_normal_level_five_primary_pierce_before_fourth_cast()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.GakgungShot))
            {
                rig.AddTarget(new Vector2(3f, 0f));
                yield return rig.AdvanceCasts(3);

                Assert.That(rig.Telemetry.LastProjectileMaximumImpacts, Is.EqualTo(3));
                Assert.That(rig.Telemetry.LastProjectileScale, Is.EqualTo(1f));
            }
        }

        [UnityTest]
        public IEnumerator Heaven_chain_bursts_once_after_unique_target_chain_completes()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.TalismanThrow))
            {
                rig.AddTargets(4);
                yield return rig.AdvanceSeconds(3f);

                Assert.That(rig.Count(ContactPhase.Blast), Is.EqualTo(4));
                Assert.That(rig.UniqueDamagedTargets, Is.EqualTo(4));
                var blasts = rig.DamageEvents.Where(value => value.Phase == ContactPhase.Blast).ToArray();
                Assert.That(blasts.Select(value => value.AttackInstanceId).Distinct().Count(), Is.EqualTo(1));
                Assert.That(blasts.Select(value => value.SimulationTick).Distinct().Count(), Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator Heaven_chain_requires_three_confirmed_links_and_excludes_missing_masks()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.TalismanThrow))
            {
                rig.AddTargets(2);
                yield return rig.AdvanceSeconds(3f);
                Assert.That(rig.Count(ContactPhase.Blast), Is.EqualTo(0));
            }

            using (var rig = EvolvedWeaponTestRig.For(WeaponId.TalismanThrow))
            {
                rig.AddTarget(new Vector2(1f, 0f));
                var missingMask = rig.AddTargetWithoutMask(new Vector2(1.2f, 0f));
                rig.AddTarget(new Vector2(1.4f, 0f));
                rig.AddTarget(new Vector2(1.6f, 0f));
                yield return rig.AdvanceSeconds(3f);

                Assert.That(rig.Count(ContactPhase.Blast), Is.EqualTo(3));
                Assert.That(rig.DamageEvents.Any(value => value.TargetRuntimeId == missingMask.RuntimeId), Is.False);
            }
        }

        [Test]
        public void Normal_level_five_talisman_still_launches_three_seals()
        {
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            for (var index = 0; index < 3; index++) registry.Register(new TestTarget(index + 1, new Float2(1f + index * 0.2f, 0f), PixelHitMask.FromRows("1")));
            var executor = new TalismanExecutor(runtime, 10f, 1f, 4f, 8f, 5, 5);
            var root = new GameObject("Normal talisman preservation test root");

            executor.Tick(0.01f, new WeaponExecutionContext(default, root.transform, null, 0, 1));

            Assert.That(executor.LastLaunchCount, Is.EqualTo(3));
            executor.Dispose();
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator Thunder_prison_pulls_before_secondary_blast()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.ThunderCrashBomb))
            {
                var target = rig.AddTarget(new Vector2(2f, 0f));
                rig.Tick(0.5f);
                Assert.That(rig.DamageEvents, Is.Empty, "Lob and pull entry cannot deal damage.");
                rig.Tick(0.24f);
                Assert.That(rig.DamageEvents, Is.Empty, "Pull cannot deal damage.");
                rig.Tick(0.01f);
                Assert.That(rig.DamageEvents, Is.Empty, "Pull boundary cannot deal damage.");
                rig.Tick(0.11f);
                Assert.That(rig.DamageEvents, Is.Empty, "Compression silence cannot deal damage.");
                rig.Tick(0.01f);
                yield return null;

                Assert.That(target.Position.X, Is.LessThan(2f));
                Assert.That(rig.Telemetry.StateOrder, Is.EqualTo(new[] { "Pull", "CompressionDelay", "CompressedBlast" }));
                CollectionAssert.Contains(rig.ContactPhases, ContactPhase.Blast);
            }
        }

        [UnityTest]
        public IEnumerator Thunder_prison_moves_a_registered_first_playable_target_without_damage_during_pull()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;

            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var target = controller.SpawnEnemyForTests(new Vector2(2f, 0f));
            var executor = new ThunderBombExecutor(controller.WeaponRuntime, 10f, 10f, 4f, 0.5f, 0.15f, 1.8f, 5, true);
            var damageCount = 0;
            controller.CombatDamageService.DamageConfirmed += record => { if (record.WeaponId.Equals(WeaponId.ThunderCrashBomb)) damageCount++; };

            executor.Tick(0.5f, new WeaponExecutionContext(default, null, null, 0, 1));
            executor.Tick(0.25f, new WeaponExecutionContext(default, null, null, 0, 2));
            Assert.That(target.WorldPosition.X, Is.LessThan(2f));
            Assert.That(damageCount, Is.EqualTo(0));
            executor.Dispose();
        }

        [UnityTest]
        public IEnumerator Thunder_prison_consumes_large_tick_across_pull_and_silence_exactly()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.ThunderCrashBomb))
            {
                var target = rig.AddTarget(new Vector2(2f, 0f));
                rig.Tick(0.5f);
                rig.Tick(0.5f);
                yield return null;

                Assert.That(target.Position.X, Is.EqualTo(1f).Within(0.001f));
                Assert.That(rig.Telemetry.StateOrder, Is.EqualTo(new[] { "Pull", "CompressionDelay", "CompressedBlast" }));
                Assert.That(rig.Count(ContactPhase.Blast), Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator Thunder_prison_requires_pixel_mask_overlap_for_terminal_blast()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.ThunderCrashBomb))
            {
                rig.AddTarget(new Vector2(2f, 0f), PixelHitMask.FromRows("0"));
                rig.Tick(0.5f);
                rig.Tick(0.5f);
                yield return null;

                Assert.That(rig.Count(ContactPhase.Blast), Is.EqualTo(0));
            }
        }

        [UnityTest]
        public IEnumerator Twelve_guardians_marks_only_targets_inside_completed_ward()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.JangseungWard))
            {
                var inside = rig.AddTarget(Vector2.zero);
                var outside = rig.AddTarget(new Vector2(9f, 0f));
                yield return rig.AdvanceSeconds(1f);

                Assert.That(inside.Statuses, Contains.Item("guardian_mark"));
                Assert.That(outside.Statuses, Does.Not.Contain("guardian_mark"));
            }
        }

        [UnityTest]
        public IEnumerator Twelve_guardians_marks_and_clears_a_registered_first_playable_target()
        {
            SceneManager.LoadScene("Gameplay");
            yield return null;

            var controller = Object.FindFirstObjectByType<FirstPlayableController>();
            var target = controller.SpawnEnemyForTests(Vector2.zero);
            var executor = new JangseungWardExecutor(controller.WeaponRuntime, 10f, 10f, 4f, 4, 0, .2f, 5, true);
            executor.Tick(.4f, new WeaponExecutionContext(default, null, null, 0, 1));
            Assert.That(target is IJangseungWardStatusTarget, Is.True);
            Assert.That(controller.HasJangseungWardMark(target.RuntimeId), Is.True);
            executor.Reset();
            Assert.That(controller.HasJangseungWardMark(target.RuntimeId), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Twelve_guardians_pulses_only_a_marked_target_on_confirmed_boundary_crossing()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.JangseungWard))
            {
                var marked = rig.AddTarget(Vector2.zero);
                yield return rig.AdvanceSeconds(0.35f);
                marked.Position = new Float2(5f, 0f);
                rig.Tick(0.05f);
                yield return null;

                Assert.That(marked.Statuses, Contains.Item("guardian_mark"));
                Assert.That(rig.Count(ContactPhase.BoundaryCrossing), Is.EqualTo(1));
            }
        }

        [UnityTest]
        public IEnumerator Twelve_guardians_activates_sequentially_and_never_pulses_before_completion()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.JangseungWard))
            {
                var target = rig.AddTarget(Vector2.zero);
                rig.Tick(0.05f);
                Assert.That(rig.Telemetry.SecondaryObservedCount, Is.EqualTo(1));
                target.Position = new Float2(5f, 0f);
                rig.Tick(0.1f);
                Assert.That(rig.Telemetry.SecondaryObservedCount, Is.EqualTo(2));
                rig.Tick(0.1f);
                Assert.That(rig.Telemetry.SecondaryObservedCount, Is.EqualTo(3));
                Assert.That(rig.DamageEvents, Is.Empty);
                rig.Tick(0.1f);
                yield return null;

                Assert.That(rig.Telemetry.SecondaryObservedCount, Is.EqualTo(4));
                Assert.That(target.Statuses, Does.Not.Contain("guardian_mark"));
                Assert.That(rig.DamageEvents, Is.Empty);
            }
        }

        [UnityTest]
        public IEnumerator Twelve_guardians_excludes_unmarked_crossings_and_stationary_marked_targets()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.JangseungWard))
            {
                var marked = rig.AddTarget(Vector2.zero);
                var unmarked = rig.AddTarget(new Vector2(9f, 0f));
                yield return rig.AdvanceSeconds(0.35f);
                var stationaryCount = rig.Count(ContactPhase.BoundaryCrossing);
                yield return rig.AdvanceSeconds(0.2f);
                Assert.That(rig.Count(ContactPhase.BoundaryCrossing), Is.EqualTo(stationaryCount));
                unmarked.Position = new Float2(-5f, 0f);
                rig.Tick(0.05f);
                yield return null;

                Assert.That(marked.Statuses, Contains.Item("guardian_mark"));
                Assert.That(unmarked.Statuses, Does.Not.Contain("guardian_mark"));
                Assert.That(rig.Count(ContactPhase.BoundaryCrossing), Is.EqualTo(stationaryCount));
            }
        }

        [UnityTest]
        public IEnumerator Fire_dragon_barrage_scouts_then_focuses_marked_position()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.SingijeonVolley))
            {
                rig.AddTarget(new Vector2(2f, 0f));
                yield return rig.AdvanceSeconds(0.45f);

                Assert.That(rig.Telemetry.VolleyKinds, Is.EqualTo(new[] { "scout", "focus" }));
                Assert.That(rig.Telemetry.ScoutProjectileCount, Is.EqualTo(3));
                Assert.That(rig.Telemetry.FocusProjectileCount, Is.GreaterThanOrEqualTo(8));
            }
        }

        [UnityTest]
        public IEnumerator Fire_dragon_rockets_confirm_each_target_contact_once_per_instance()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.SingijeonVolley))
            {
                rig.AddTarget(new Vector2(0.4f, 0f));
                yield return rig.AdvanceSeconds(0.45f);

                Assert.That(rig.DamageEvents, Is.Not.Empty);
                Assert.That(rig.DamageEvents.GroupBy(value => new { value.AttackInstanceId, value.TargetRuntimeId }).All(group => group.Count() == 1), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator Fire_dragon_focus_delay_and_residual_simulation_match_split_and_large_ticks()
        {
            using (var split = EvolvedWeaponTestRig.For(WeaponId.SingijeonVolley))
            using (var large = EvolvedWeaponTestRig.For(WeaponId.SingijeonVolley))
            {
                split.AddTarget(new Vector2(0.4f, 0f));
                large.AddTarget(new Vector2(0.4f, 0f));
                split.Tick(0.35f);
                split.Tick(0.15f);
                large.Tick(0.5f);
                yield return null;

                Assert.That(split.Telemetry.VolleyKinds, Is.EqualTo(new[] { "scout", "focus" }));
                Assert.That(large.Telemetry.VolleyKinds, Is.EqualTo(split.Telemetry.VolleyKinds));
                Assert.That(large.Telemetry.ScoutProjectileCount, Is.EqualTo(3));
                Assert.That(large.Telemetry.FocusProjectileCount, Is.GreaterThanOrEqualTo(8));
                Assert.That(large.Telemetry.SecondaryObservedCount, Is.EqualTo(split.Telemetry.SecondaryObservedCount));
                Assert.That(large.DamageEvents.Count, Is.EqualTo(split.DamageEvents.Count));
            }
        }

        [UnityTest]
        public IEnumerator Fire_dragon_uses_lowest_tied_dense_bucket_centroid_as_focus_position()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.SingijeonVolley))
            {
                rig.AddTarget(new Vector2(0f, 2f));
                rig.AddTarget(new Vector2(0f, 2.2f));
                rig.AddTarget(new Vector2(2f, -0.1f));
                rig.AddTarget(new Vector2(2f, 0.1f));
                rig.Tick(0.01f);
                var executor = (SingijeonExecutor)rig.Executor;
                yield return null;

                Assert.That(executor.LastDirectionBucket, Is.EqualTo(0));
                Assert.That(executor.RecordedFocusPosition.X, Is.EqualTo(2f).Within(0.001f));
                Assert.That(executor.RecordedFocusPosition.Y, Is.EqualTo(0f).Within(0.001f));
            }
        }

        [UnityTest]
        public IEnumerator Frost_bloom_stores_only_confirmed_frozen_residents_then_shatters_each_once_on_expiry()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.FrostFlask))
            {
                rig.AddTargets(3, insideField: true);
                var missingMask = rig.AddTargetWithoutMask(new Vector2(0.7f, 0f));
                yield return rig.AdvanceSeconds(1.55f);

                var blasts = rig.DamageEvents.Where(value => value.Phase == ContactPhase.Blast).ToArray();
                Assert.That(blasts, Has.Length.EqualTo(3));
                Assert.That(blasts.Select(value => value.TargetRuntimeId), Is.Unique);
                Assert.That(blasts.Any(value => value.TargetRuntimeId == missingMask.RuntimeId), Is.False);
                Assert.That(rig.Telemetry.AllStoredTargetsResolvedOnce, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator Returning_heaven_thunder_strikes_marked_targets_by_projection_then_returns_in_reverse_with_lower_damage()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.WindThunderFan))
            {
                var near = rig.AddTarget(new Vector2(1f, 0f));
                var middle = rig.AddTarget(new Vector2(2f, 0f));
                var far = rig.AddTarget(new Vector2(3f, 0f));
                yield return rig.AdvanceSeconds(0.65f);

                var lightning = rig.DamageEvents.Where(value => value.Phase == ContactPhase.Lightning).ToArray();
                var inbound = rig.DamageEvents.Where(value => value.Phase == ContactPhase.Inbound).ToArray();
                CollectionAssert.AreEqual(new[] { near.RuntimeId, middle.RuntimeId, far.RuntimeId }, lightning.Select(value => value.TargetRuntimeId));
                CollectionAssert.AreEqual(new[] { far.RuntimeId, middle.RuntimeId, near.RuntimeId }, inbound.Select(value => value.TargetRuntimeId));
                Assert.That(lightning.Select(value => value.FinalDamage).Distinct().Single(), Is.GreaterThan(inbound.Select(value => value.FinalDamage).Distinct().Single()));
                Assert.That(lightning.Select(value => value.SimulationTick).Distinct().Count(), Is.GreaterThanOrEqualTo(2));
            }
        }

        [UnityTest]
        public IEnumerator Returning_heaven_thunder_skips_target_lost_before_inbound_without_retargeting()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.WindThunderFan))
            {
                var near = rig.AddTarget(new Vector2(1f, 0f));
                var middle = rig.AddTarget(new Vector2(2f, 0f));
                var far = rig.AddTarget(new Vector2(3f, 0f));
                rig.Tick(0.04f);
                rig.Tick(0.12f);
                rig.Tick(0.24f);
                far.ApplyResolvedDamage(1000);
                rig.Tick(0.08f);
                yield return null;

                var inbound = rig.DamageEvents.Where(value => value.Phase == ContactPhase.Inbound).ToArray();
                CollectionAssert.AreEqual(new[] { middle.RuntimeId, near.RuntimeId }, inbound.Select(value => value.TargetRuntimeId));
                Assert.That(inbound.Any(value => value.TargetRuntimeId == far.RuntimeId), Is.False);
            }
        }

        [Test]
        public void Returning_heaven_thunder_preserves_exact_outbound_cadence_for_split_and_large_ticks()
        {
            using (var split = EvolvedWeaponTestRig.For(WeaponId.WindThunderFan))
            using (var large = EvolvedWeaponTestRig.For(WeaponId.WindThunderFan))
            {
                split.AddTargets(3); large.AddTargets(3);
                for (var index = 0; index < 4; index++) { split.Tick(0.01f); large.Tick(0.01f); }
                split.Tick(0.12f);
                split.Tick(0.03f); split.Tick(0.05f); split.Tick(0.08f); split.Tick(0.08f);
                large.Tick(0.36f);

                var splitFan = (WindThunderFanExecutor)split.Executor;
                var largeFan = (WindThunderFanExecutor)large.Executor;
                Assert.That(splitFan.LastOutboundStrikeTimes[0], Is.EqualTo(0.08f).Within(0.0001f));
                Assert.That(splitFan.LastOutboundStrikeTimes[1], Is.EqualTo(0.16f).Within(0.0001f));
                Assert.That(splitFan.LastOutboundStrikeTimes[2], Is.EqualTo(0.24f).Within(0.0001f));
                CollectionAssert.AreEqual(splitFan.LastOutboundStrikeTimes, largeFan.LastOutboundStrikeTimes);
                Assert.That(split.Count(ContactPhase.Lightning), Is.EqualTo(large.Count(ContactPhase.Lightning)));
            }
        }

        [Test]
        public void Returning_heaven_thunder_waits_exactly_one_strike_interval_before_inbound_with_residual_carry()
        {
            using (var split = EvolvedWeaponTestRig.For(WeaponId.WindThunderFan))
            using (var large = EvolvedWeaponTestRig.For(WeaponId.WindThunderFan))
            {
                split.AddTargets(3); large.AddTargets(3);
                split.Tick(.04f); split.Tick(.12f); split.Tick(.24f);
                Assert.That(split.Count(ContactPhase.Lightning), Is.EqualTo(3));
                Assert.That(split.Count(ContactPhase.Inbound), Is.EqualTo(0));
                split.Tick(.079f);
                Assert.That(split.Count(ContactPhase.Inbound), Is.EqualTo(0));
                split.Tick(.001f);
                Assert.That(split.Count(ContactPhase.Inbound), Is.EqualTo(3));

                large.Tick(.04f); large.Tick(.44f);
                Assert.That(large.Count(ContactPhase.Lightning), Is.EqualTo(3));
                Assert.That(large.Count(ContactPhase.Inbound), Is.EqualTo(3));
            }
        }

        [Test]
        public void Returning_heaven_thunder_never_returns_to_a_target_that_failed_outbound_then_recontacts()
        {
            using (var rig = EvolvedWeaponTestRig.For(WeaponId.WindThunderFan))
            {
                var near = rig.AddTarget(new Vector2(1f, 0f));
                var middle = rig.AddTarget(new Vector2(2f, 0f));
                var far = rig.AddTarget(new Vector2(3f, 0f));
                for (var index = 0; index < 4; index++) rig.Tick(0.01f);
                rig.Tick(0.12f);
                Assert.That(rig.Registry.Unregister(far), Is.True);
                rig.Tick(0.24f);
                Assert.That(rig.Registry.Register(far), Is.True);
                rig.Tick(0.01f);

                CollectionAssert.AreEqual(new[] { near.RuntimeId, middle.RuntimeId }, ((WindThunderFanExecutor)rig.Executor).LastSuccessfulOutboundTargetIds);
                CollectionAssert.AreEqual(new[] { middle.RuntimeId, near.RuntimeId }, rig.DamageEvents.Where(value => value.Phase == ContactPhase.Inbound).Select(value => value.TargetRuntimeId));
            }
        }

        [Test]
        public void Normal_ward_and_singijeon_keep_their_existing_launch_and_crossing_behavior()
        {
            var registry = new CombatTargetRegistry();
            var damage = new CombatDamageService(registry);
            var runtime = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            var wardTarget = new TestTarget(1, new Float2(0f, -2f), PixelHitMask.FromRows("1"));
            var rocketTarget = new TestTarget(2, new Float2(2f, 0f), PixelHitMask.FromRows("1"));
            registry.Register(wardTarget); registry.Register(rocketTarget);
            var root = new GameObject("Normal ward and Singijeon preservation root");
            var ward = new JangseungWardExecutor(runtime, 10f, 10f, 1f, 2, 1, 0f, 1);
            var singijeon = new SingijeonExecutor(runtime, 10f, 10f, 4f, 8f, 2, 5);

            ward.Tick(0.01f, new WeaponExecutionContext(default, root.transform, null, 0, 1));
            wardTarget.Position = new Float2(0f, 2f);
            ward.Tick(0.1f, new WeaponExecutionContext(default, root.transform, null, 0, 2));
            singijeon.Tick(0.01f, new WeaponExecutionContext(default, root.transform, null, 0, 3));

            Assert.That(ward.ActivePostCount, Is.EqualTo(2));
            Assert.That(ward.IsEvolved, Is.False);
            Assert.That(wardTarget.Health, Is.LessThan(100));
            Assert.That(singijeon.LastLaunchCount, Is.EqualTo(6));
            Assert.That(singijeon.VolleyKinds, Is.Empty);
            ward.Dispose(); singijeon.Dispose(); Object.DestroyImmediate(root);
        }

        private sealed class CountingExecutor : IWeaponExecutor
        {
            public int TickCount { get; private set; }
            public int DisposeCount { get; private set; }
            public void Tick(float deltaTime, in WeaponExecutionContext context) => TickCount++;
            public void Reset() { }
            public void Dispose() => DisposeCount++;
        }
    }
}
