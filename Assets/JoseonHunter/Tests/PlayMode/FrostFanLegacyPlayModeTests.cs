using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public sealed class FrostFanLegacyPlayModeTests
    {
        [Test]
        public void Chosen_paths_apply_approved_costs()
        {
            using var rig = new Rig();
            var mist = rig.Frost(WeaponLegacyPathId.FrostMist, WeaponLegacyStage.Chosen);
            var shatter = rig.Frost(WeaponLegacyPathId.FrostShatter, WeaponLegacyStage.Chosen);
            var vacuum = rig.Fan(WeaponLegacyPathId.FanVacuum, WeaponLegacyStage.Chosen);
            var thunder = rig.Fan(WeaponLegacyPathId.FanHeavenThunder, WeaponLegacyStage.Chosen);

            Assert.That(mist.BaseDamage, Is.EqualTo(65f).Within(.001f));
            Assert.That(mist.Radius, Is.EqualTo(2.025f).Within(.001f));
            Assert.That(mist.SlowFraction, Is.EqualTo(.45f).Within(.001f));
            Assert.That(shatter.Duration, Is.EqualTo(1.5f).Within(.001f));
            Assert.That(shatter.LegacyLandingDamageForTests, Is.EqualTo(150f).Within(.001f));
            Assert.That(vacuum.LegacyLightningDamageMultiplierForTests, Is.EqualTo(.7f));
            Assert.That(vacuum.LegacyPullMultiplierForTests, Is.EqualTo(1.5f));
            Assert.That(thunder.LegacyPullMultiplierForTests, Is.Zero);
            mist.Dispose(); shatter.Dispose(); vacuum.Dispose(); thunder.Dispose();
        }

        [UnityTest]
        public IEnumerator Completed_mist_freezes_on_third_hit_and_emits_three_sixty_percent_blooms()
        {
            using var rig = new Rig(health: 10000);
            var executor = rig.Frost(WeaponLegacyPathId.FrostMist,
                WeaponLegacyStage.Completed);
            var events = new List<ConfirmedDamageEvent>();
            rig.Damage.DamageConfirmed += events.Add;
            rig.Drive(executor, 1.2f);

            Assert.That(rig.Runtime.AffixStatuses.HasStatus(rig.Target.RuntimeId,
                CombatStatusKind.Freeze), Is.True);
            Assert.That(executor.CompletedBloomCountForTests, Is.EqualTo(3));
            Assert.That(events.Count(value => value.Phase == ContactPhase.PotentialBlast &&
                (value.FinalDamage == 60 || value.FinalDamage == 66)), Is.EqualTo(3));
            Assert.That(events.Where(value => value.Phase == ContactPhase.PotentialBlast &&
                    (value.FinalDamage == 60 || value.FinalDamage == 66))
                .Select(value => value.FinalDamage), Is.EqualTo(new[] { 60, 66, 66 }));
            Assert.That(rig.Target.LastSlowStrength, Is.EqualTo(.45f).Within(.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Frost_shatter_consumes_freeze_and_chains_three_then_five_targets()
        {
            using var reinforcedRig = new Rig(health: 10000);
            var reinforcedTargets = reinforcedRig.AddCluster(5);
            foreach (var target in reinforcedTargets)
                reinforcedRig.Runtime.AffixStatuses.ApplyTimedStatus(target.RuntimeId,
                    CombatStatusKind.Freeze, 2f, 1, WeaponId.FrostFlask);
            var reinforced = reinforcedRig.Frost(WeaponLegacyPathId.FrostShatter,
                WeaponLegacyStage.Reinforced);
            reinforcedRig.Drive(reinforced, .5f);
            Assert.That(reinforced.LastLegacyShatterTargetCountForTests, Is.EqualTo(3));

            using var completedRig = new Rig(health: 10000);
            var completedTargets = completedRig.AddCluster(7);
            foreach (var target in completedTargets)
                completedRig.Runtime.AffixStatuses.ApplyTimedStatus(target.RuntimeId,
                    CombatStatusKind.Freeze, 2f, 1, WeaponId.FrostFlask);
            var completed = completedRig.Frost(WeaponLegacyPathId.FrostShatter,
                WeaponLegacyStage.Completed);
            completedRig.Drive(completed, .5f);
            Assert.That(completed.LastLegacyShatterTargetCountForTests, Is.EqualTo(5));
            Assert.That(completedTargets.Count(target => completedRig.Runtime.AffixStatuses.HasStatus(
                target.RuntimeId, CombatStatusKind.Freeze)), Is.EqualTo(2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vacuum_builds_three_bleed_stacks_then_reinforced_ruptures_and_cleans_up()
        {
            using var rig = new Rig(health: 10000);
            var executor = rig.Fan(WeaponLegacyPathId.FanVacuum,
                WeaponLegacyStage.Completed);
            var events = new List<ConfirmedDamageEvent>();
            rig.Damage.DamageConfirmed += events.Add;
            rig.Drive(executor, 1.2f);

            Assert.That(executor.MaximumBleedStacksForTests, Is.EqualTo(3));
            Assert.That(events.Any(value => value.Phase == ContactPhase.PotentialBlast &&
                value.FinalDamage == 100), Is.True);
            Assert.That(executor.MaximumVacuumTargetsQueriedForTests,
                Is.LessThanOrEqualTo(8));
            executor.Reset();
            Assert.That(executor.ActiveBleedCountForTests, Is.Zero);
            Assert.That(rig.Damage.TrackedAttackCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Heaven_thunder_bounces_four_then_completed_seven_and_explodes_marked_center()
        {
            using var chosenRig = new Rig(health: 10000);
            chosenRig.AddCluster(6);
            var chosen = chosenRig.Fan(WeaponLegacyPathId.FanHeavenThunder,
                WeaponLegacyStage.Chosen);
            chosenRig.Drive(chosen, .8f);
            Assert.That(chosen.LastHeavenThunderBounceCountForTests, Is.EqualTo(4));

            using var completedRig = new Rig(health: 10000);
            completedRig.AddCluster(8);
            var completed = completedRig.Fan(WeaponLegacyPathId.FanHeavenThunder,
                WeaponLegacyStage.Completed);
            var events = new List<ConfirmedDamageEvent>();
            completedRig.Damage.DamageConfirmed += events.Add;
            completedRig.Drive(completed, 1.2f);
            Assert.That(completed.LastHeavenThunderBounceCountForTests, Is.EqualTo(7));
            Assert.That(events.Count(value => value.Phase == ContactPhase.PotentialChain &&
                value.FinalDamage == 70), Is.EqualTo(7));
            Assert.That(events.Any(value => value.Phase == ContactPhase.Blast &&
                value.FinalDamage == 200), Is.True);
            yield return null;
        }

        private static WeaponRuntimeModifiers Modifiers(WeaponLegacyPathId path,
            WeaponLegacyStage stage) => WeaponRuntimeModifiers.From(null,
            new WeaponLegacySnapshot(path, stage));

        private sealed class Rig : System.IDisposable
        {
            private readonly GameObject root = new("Frost Fan Legacy Root");
            private readonly PixelHitMask mask = PixelHitMask.FromRows("111", "111", "111");
            private int tick;

            public Rig(int health = 1000)
            {
                Registry = new CombatTargetRegistry();
                Damage = new CombatDamageService(Registry);
                Runtime = new WeaponRuntimeController(Registry, Damage, mask);
                Target = new Target(1, health, new Float2(.45f, 0f), mask);
                Registry.Register(Target);
            }

            public CombatTargetRegistry Registry { get; }
            public CombatDamageService Damage { get; }
            public WeaponRuntimeController Runtime { get; }
            public Target Target { get; }

            public FrostFlaskExecutor Frost(WeaponLegacyPathId path, WeaponLegacyStage stage) =>
                new(Runtime, 100f, 10f, 5f, .1f, 3f, 1.5f, 1,
                    stage == WeaponLegacyStage.Completed ? 5 : stage == WeaponLegacyStage.Reinforced ? 4 : 3,
                    false, Modifiers(path, stage), .5f);

            public WindThunderFanExecutor Fan(WeaponLegacyPathId path, WeaponLegacyStage stage) =>
                new(Runtime, 100f, 10f, 5f, .4f, 8,
                    stage == WeaponLegacyStage.Completed ? 5 : stage == WeaponLegacyStage.Reinforced ? 4 : 3,
                    false, Modifiers(path, stage));

            public List<Target> AddCluster(int total)
            {
                var result = new List<Target> { Target };
                for (var id = 2; id <= total; id++)
                {
                    var target = new Target(id, 10000, new Float2(.35f + id * .12f, 0f), mask);
                    Registry.Register(target); result.Add(target);
                }
                return result;
            }

            public void Drive(IWeaponExecutor executor, float seconds)
            {
                var remaining = seconds;
                while (remaining > .0001f)
                {
                    var step = Mathf.Min(.05f, remaining);
                    executor.Tick(step, new WeaponExecutionContext(default, root.transform, null,
                        _ => null, _ => mask, 0, ++tick));
                    Runtime.AffixStatuses.Tick(step, tick);
                    remaining -= step;
                }
            }

            public void Dispose()
            {
                Runtime.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        private sealed class Target : ICombatTarget, IFrostStatusTarget, IControlStatusTarget
        {
            public Target(int id, int health, Float2 position, PixelHitMask mask)
            { RuntimeId = id; Health = health; WorldPosition = position; HurtMask = mask; }
            public int RuntimeId { get; }
            public bool IsAlive => Health > 0;
            public int Health { get; private set; }
            public bool IsBoss => false;
            public bool IsElite => false;
            public float ThreatScore => RuntimeId;
            public Float2 WorldPosition { get; private set; }
            public PixelHitMask HurtMask { get; }
            public PixelMaskTransform HurtMaskTransform =>
                PixelMaskTransform.Translation(WorldPosition.X, WorldPosition.Y);
            public float LastSlowStrength { get; private set; }
            public void ApplyResolvedDamage(int damage) => Health -= damage;
            public void ApplyKnockback(Float2 direction, float force) => WorldPosition =
                new Float2(WorldPosition.X + direction.X * force,
                    WorldPosition.Y + direction.Y * force);
            public void ApplyFrostSlow(int sourceId, float strength) => LastSlowStrength = strength;
            public void RemoveFrostSlow(int sourceId, float decaySeconds) { }
            public void ApplyFreeze(int sourceId, float durationSeconds) { }
            public void ApplyStagger(float durationSeconds) { }
        }
    }
}
