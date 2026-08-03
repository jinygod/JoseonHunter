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
    public sealed class TalismanThunderLegacyPlayModeTests
    {
        [Test]
        public void Chosen_paths_apply_their_approved_costs()
        {
            using var rig = new Rig();
            var heaven = rig.Talisman(WeaponLegacyPathId.TalismanHeavenSeal, WeaponLegacyStage.Chosen);
            var ghost = rig.Talisman(WeaponLegacyPathId.TalismanGhostBurst, WeaponLegacyStage.Chosen);
            var prison = rig.Thunder(WeaponLegacyPathId.ThunderPrison, WeaponLegacyStage.Chosen);
            var current = rig.Thunder(WeaponLegacyPathId.ThunderEarthCurrent, WeaponLegacyStage.Chosen);

            Assert.That(heaven.BaseDamage, Is.EqualTo(7.5f).Within(.001f));
            Assert.That(ghost.BaseDamage, Is.EqualTo(10f).Within(.001f));
            Assert.That(prison.CooldownSeconds, Is.EqualTo(12.5f).Within(.001f));
            Assert.That(current.BaseDamage, Is.EqualTo(7f).Within(.001f));
            heaven.Dispose(); ghost.Dispose(); prison.Dispose(); current.Dispose();
        }

        [Test]
        public void Only_reinforced_heaven_seal_adds_fifteen_percent_incoming_damage()
        {
            using var chosenRig = new Rig(health: 10000);
            var chosen = chosenRig.Talisman(WeaponLegacyPathId.TalismanHeavenSeal,
                WeaponLegacyStage.Chosen);
            chosenRig.Drive(chosen, .25f);
            Assert.That(chosenRig.Hit(chosenRig.Target, 100).FinalDamage, Is.EqualTo(100));
            chosen.Dispose();

            using var reinforcedRig = new Rig(health: 10000);
            var reinforced = reinforcedRig.Talisman(WeaponLegacyPathId.TalismanHeavenSeal,
                WeaponLegacyStage.Reinforced);
            reinforcedRig.Drive(reinforced, .25f);
            Assert.That(reinforcedRig.Hit(reinforcedRig.Target, 100).FinalDamage, Is.EqualTo(115));
            reinforced.Dispose();
        }

        [UnityTest]
        public IEnumerator Heaven_seal_lasts_two_seconds_and_completed_death_chain_caps_at_four()
        {
            using var rig = new Rig(health: 10000);
            var executor = rig.Talisman(WeaponLegacyPathId.TalismanHeavenSeal,
                WeaponLegacyStage.Completed);
            for (var id = 2; id <= 7; id++)
                rig.AddTarget(id, new Float2(.6f + id * .12f, 0f), health: 10000);
            rig.Drive(executor, .7f);

            Assert.That(rig.Runtime.AffixStatuses.HasStatus(rig.Target.RuntimeId,
                CombatStatusKind.Seal), Is.True);
            rig.DriveStatusesOnly(1.2f);
            Assert.That(rig.Runtime.AffixStatuses.HasStatus(rig.Target.RuntimeId,
                CombatStatusKind.Seal), Is.True);

            var events = new List<ConfirmedDamageEvent>();
            rig.Damage.DamageConfirmed += events.Add;
            rig.Kill(rig.Target);
            Assert.That(events.Count(value => value.WeaponId.Equals(WeaponId.TalismanThrow) &&
                value.Phase == ContactPhase.PotentialChain), Is.LessThanOrEqualTo(4));
            Assert.That(events.Where(value => value.WeaponId.Equals(WeaponId.TalismanThrow) &&
                value.Phase == ContactPhase.PotentialChain).All(value => value.FinalDamage == 14), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ghost_burst_delays_then_reinforces_and_completed_chain_caps_at_three()
        {
            using var rig = new Rig(health: 10000);
            for (var id = 2; id <= 7; id++)
                rig.AddTarget(id, new Float2(.7f + id * .08f, 0f), health: 10000);
            var executor = rig.Talisman(WeaponLegacyPathId.TalismanGhostBurst,
                WeaponLegacyStage.Completed);
            var events = new List<ConfirmedDamageEvent>();
            rig.Damage.DamageConfirmed += events.Add;

            rig.Drive(executor, .7f);
            Assert.That(events.Any(value => value.Phase == ContactPhase.PotentialBlast), Is.False,
                "원귀 폭발은 0.6초 예고가 끝나기 전에 피해를 주면 안 됩니다.");
            rig.Drive(executor, .45f);

            Assert.That(events.Any(value => value.Phase == ContactPhase.PotentialBlast &&
                value.FinalDamage == 20), Is.True);
            Assert.That(rig.Runtime.AffixStatuses.HasStatus(rig.Target.RuntimeId,
                CombatStatusKind.Seal), Is.False);
            Assert.That(events.Any(value => value.Phase == ContactPhase.PotentialChain &&
                value.FinalDamage == 10), Is.True);
            Assert.That(events.Count(value => value.Phase == ContactPhase.PotentialChain &&
                value.FinalDamage == 12), Is.LessThanOrEqualTo(3));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Thunder_prison_pulls_for_one_second_and_completed_core_deals_three_hundred_percent()
        {
            using var rig = new Rig(health: 10000);
            var outer = rig.AddTarget(2, new Float2(1.2f, 0f), health: 10000);
            var executor = rig.Thunder(WeaponLegacyPathId.ThunderPrison,
                WeaponLegacyStage.Completed);
            var events = new List<ConfirmedDamageEvent>();
            rig.Damage.DamageConfirmed += events.Add;
            var before = outer.WorldPosition;

            rig.Drive(executor, 1.6f);

            Assert.That(outer.WorldPosition.X, Is.LessThan(before.X));
            Assert.That(executor.LastPullDurationForTests, Is.EqualTo(1f).Within(.001f));
            Assert.That(events.Any(value => value.Phase == ContactPhase.Blast &&
                value.FinalDamage == 30), Is.True);
            Assert.That(rig.Runtime.AffixStatuses.HasStatus(rig.Target.RuntimeId,
                CombatStatusKind.Shock), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Earth_current_ticks_every_half_second_caps_queries_and_retires_attacks()
        {
            using var rig = new Rig(health: 10000);
            for (var id = 2; id <= 8; id++)
                rig.AddTarget(id, new Float2(.45f + id * .08f, 0f), health: 10000);
            var executor = rig.Thunder(WeaponLegacyPathId.ThunderEarthCurrent,
                WeaponLegacyStage.Reinforced, cooldown: 50f);
            var events = new List<ConfirmedDamageEvent>();
            rig.Damage.DamageConfirmed += events.Add;

            rig.Drive(executor, 2f);
            var firstTick = events.Where(value => value.Phase == ContactPhase.PotentialBlast)
                .Take(3).ToArray();
            Assert.That(firstTick.Length, Is.EqualTo(3));
            Assert.That(firstTick.All(value => value.FinalDamage == 3), Is.True);
            Assert.That(executor.MaximumCurrentTargetsQueriedForTests, Is.EqualTo(3));

            rig.Drive(executor, 4f);
            Assert.That(executor.ActiveGroundCurrentCountForTests, Is.Zero);
            Assert.That(rig.Damage.TrackedAttackCount, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Completed_earth_current_death_propagates_to_at_most_five_targets()
        {
            using var rig = new Rig(health: 10000);
            for (var id = 2; id <= 8; id++)
                rig.AddTarget(id, new Float2(.5f + id * .08f, 0f), health: 10000);
            var executor = rig.Thunder(WeaponLegacyPathId.ThunderEarthCurrent,
                WeaponLegacyStage.Completed, cooldown: 50f);

            rig.Drive(executor, 1f);
            rig.Kill(rig.TargetFor(executor.LastEarthCurrentTargetRuntimeIdForTests));

            Assert.That(executor.LastEarthPropagationCountForTests, Is.EqualTo(5));
            Assert.That(executor.ActiveGroundCurrentCountForTests, Is.LessThanOrEqualTo(6));
            rig.Drive(executor, 5f);
            Assert.That(executor.ActiveGroundCurrentCountForTests, Is.Zero);
            Assert.That(rig.Damage.TrackedAttackCount, Is.Zero);
            yield return null;
        }

        private static WeaponRuntimeModifiers Modifiers(WeaponLegacyPathId path,
            WeaponLegacyStage stage) => WeaponRuntimeModifiers.From(null,
            new WeaponLegacySnapshot(path, stage));

        private sealed class Rig : System.IDisposable
        {
            private readonly GameObject root = new("Talisman Thunder Legacy Root");
            private readonly PixelHitMask mask = PixelHitMask.FromRows("1");
            private readonly Dictionary<int, Target> targets = new();
            private int tick;

            public Rig(int health = 1000)
            {
                Registry = new CombatTargetRegistry();
                Damage = new CombatDamageService(Registry);
                Runtime = new WeaponRuntimeController(Registry, Damage, mask);
                Target = new Target(1, health, new Float2(.6f, 0f), mask);
                Registry.Register(Target);
                targets.Add(Target.RuntimeId, Target);
            }

            public CombatTargetRegistry Registry { get; }
            public CombatDamageService Damage { get; }
            public WeaponRuntimeController Runtime { get; }
            public Target Target { get; }

            public TalismanExecutor Talisman(WeaponLegacyPathId path, WeaponLegacyStage stage) =>
                new(Runtime, 10f, 10f, 4f, 20f, 1,
                    stage == WeaponLegacyStage.Completed ? 5 : stage == WeaponLegacyStage.Reinforced ? 4 : 3,
                    false, Modifiers(path, stage));

            public ThunderBombExecutor Thunder(WeaponLegacyPathId path, WeaponLegacyStage stage,
                float cooldown = 10f) => new(Runtime, 10f, cooldown, 4f, .1f, 0f, 2f,
                stage == WeaponLegacyStage.Completed ? 5 : stage == WeaponLegacyStage.Reinforced ? 4 : 3,
                false, Modifiers(path, stage));

            public Target AddTarget(int id, Float2 position, int health)
            {
                var target = new Target(id, health, position, mask);
                Registry.Register(target);
                targets.Add(id, target);
                return target;
            }

            public Target TargetFor(int runtimeId) => targets[runtimeId];

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

            public void DriveStatusesOnly(float seconds)
            {
                var remaining = seconds;
                while (remaining > .0001f)
                {
                    var step = Mathf.Min(.05f, remaining);
                    Runtime.AffixStatuses.Tick(step, ++tick);
                    remaining -= step;
                }
            }

            public void Kill(Target target)
            {
                const int attackId = 999999;
                Damage.TryApply(WeaponDamageRequest.Create(new AttackInstance(attackId,
                        RepeatHitPolicy.OncePerInstance, 0f), WeaponId.HwandoFlyingBlade, target,
                    100000, false, target.WorldPosition, ContactPhase.Direct, ++tick), out _);
                Damage.RetireAttack(attackId);
            }

            public ConfirmedDamageEvent Hit(Target target, int damage)
            {
                Damage.TryApply(WeaponDamageRequest.Create(new AttackInstance(900000 + tick,
                        RepeatHitPolicy.OncePerInstance, 0f), WeaponId.HwandoFlyingBlade, target,
                    damage, false, target.WorldPosition, ContactPhase.Direct, ++tick), out var confirmed);
                return confirmed;
            }

            public void Dispose()
            {
                Runtime.Dispose();
                Object.DestroyImmediate(root);
            }
        }

        private sealed class Target : ICombatTarget
        {
            public Target(int id, int health, Float2 position, PixelHitMask mask)
            { RuntimeId = id; Health = health; WorldPosition = position; HurtMask = mask; }
            public int RuntimeId { get; }
            public bool IsAlive => Health > 0;
            public int Health { get; private set; }
            public bool IsBoss => false;
            public bool IsElite => false;
            public float ThreatScore => 0f;
            public Float2 WorldPosition { get; private set; }
            public PixelHitMask HurtMask { get; }
            public PixelMaskTransform HurtMaskTransform =>
                PixelMaskTransform.Translation(WorldPosition.X, WorldPosition.Y);
            public void ApplyResolvedDamage(int damage) => Health -= damage;
            public void ApplyKnockback(Float2 direction, float force) => WorldPosition =
                new Float2(WorldPosition.X + direction.X * force,
                    WorldPosition.Y + direction.Y * force);
        }
    }
}
