using System;
using System.Reflection;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat;
using JoseonHunter.Runtime.Combat.Weapons;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class WeaponRuntimeModifierTests
    {
        [Test]
        public void Repeated_damage_rolls_stack_additively_before_scaling()
        {
            var profile = new WeaponRunAffixProfile(new[]
            {
                new WeaponAffixRoll(WeaponAffixStat.Damage, WeaponAffixTier.Standard, 10d),
                new WeaponAffixRoll(WeaponAffixStat.Damage, WeaponAffixTier.Perfect, 30d),
                new WeaponAffixRoll(WeaponAffixStat.Cooldown, WeaponAffixTier.High, -12d)
            });
            var modifiers = WeaponRuntimeModifiers.From(profile);
            Assert.That(modifiers.ScaleDamage(100f), Is.EqualTo(140f).Within(.001f));
            Assert.That(modifiers.ScaleCooldown(2f), Is.EqualTo(1.76f).Within(.001f));
        }

        [Test]
        public void Default_modifiers_are_identity_safe()
        {
            var modifiers = default(WeaponRuntimeModifiers);
            Assert.That(modifiers.ScaleDamage(13f), Is.EqualTo(13f));
            Assert.That(modifiers.ScaleCooldown(2f), Is.EqualTo(2f));
            Assert.That(modifiers.ScaleArea(3f), Is.EqualTo(3f));
            Assert.That(modifiers.HasPotential(WeaponPotentialId.HwandoVenomFang), Is.False);
        }

        [Test]
        public void Periodic_damage_only_ticks_at_half_second_boundaries_from_confirmed_contact()
        {
            var registry = new CombatTargetRegistry();
            var target = new Target(17, 40);
            registry.Register(target);
            var damage = new CombatDamageService(registry);
            var statuses = new WeaponAffixStatusService(registry, damage);
            var request = new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, target.RuntimeId, new Float2(2f, 3f), 7, 2,
                new AttackInstance(91, RepeatHitPolicy.TimedTicks, .5f), true);
            Assert.That(statuses.ApplyPeriodic(request), Is.True);
            statuses.Tick(.49f, 1);
            Assert.That(target.Health, Is.EqualTo(40));
            statuses.Tick(.01f, 2);
            Assert.That(target.Health, Is.EqualTo(33));
        }

        [Test]
        public void Periodic_damage_crosses_multiple_boundaries_and_preserves_residual_time()
        {
            var registry = new CombatTargetRegistry();
            var target = new Target(19, 100);
            registry.Register(target);
            var damage = new CombatDamageService(registry);
            var statuses = new WeaponAffixStatusService(registry, damage);
            Assert.That(statuses.ApplyPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, target.RuntimeId,
                new Float2(4f, 5f), 10, 3, new AttackInstance(92, RepeatHitPolicy.TimedTicks, .5f), true)), Is.True);

            statuses.Tick(1.2f, 7);
            Assert.That(target.Health, Is.EqualTo(80));
            statuses.Tick(.3f, 8);
            Assert.That(target.Health, Is.EqualTo(70));
            Assert.That(damage.TrackedAttackCount, Is.EqualTo(0));
        }

        [Test]
        public void Periodic_rejects_unconfirmed_nonfinite_dead_and_unregistered_inputs()
        {
            var registry = new CombatTargetRegistry();
            var target = new Target(20, 10);
            var dead = new Target(21, 0);
            registry.Register(target);
            registry.Register(dead);
            var statuses = new WeaponAffixStatusService(registry, new CombatDamageService(registry));
            var attack = new AttackInstance(93, RepeatHitPolicy.TimedTicks, .5f);

            Assert.That(statuses.ApplyPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, target.RuntimeId,
                new Float2(0f, 0f), 1, 1, attack, false)), Is.False);
            Assert.That(statuses.ApplyPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, target.RuntimeId,
                new Float2(float.NaN, 0f), 1, 1, attack, true)), Is.False);
            Assert.That(statuses.ApplyPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, target.RuntimeId,
                new Float2(0f, 0f), 1, 1, new AttackInstance(98, RepeatHitPolicy.OncePerPhase, 0f), true)), Is.False);
            Assert.That(statuses.ApplyPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, dead.RuntimeId,
                new Float2(0f, 0f), 1, 1, attack, true)), Is.False);
            Assert.That(registry.Unregister(target), Is.True);
            Assert.That(statuses.ApplyPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, target.RuntimeId,
                new Float2(0f, 0f), 1, 1, attack, true)), Is.False);
            Assert.That(statuses.ApplyVulnerability(dead.RuntimeId, new Float2(0f, 0f), float.NaN, true), Is.False);
            Assert.That(statuses.ApplyVulnerability(dead.RuntimeId, new Float2(0f, 0f), float.PositiveInfinity, true), Is.False);
        }

        [Test]
        public void Periodic_event_preserves_weapon_contact_boss_and_attack_identity_without_vulnerability_recursion()
        {
            var registry = new CombatTargetRegistry();
            var target = new Target(22, 50, true);
            registry.Register(target);
            var damage = new CombatDamageService(registry);
            var controller = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            var attack = new AttackInstance(94, RepeatHitPolicy.TimedTicks, .5f);
            ConfirmedDamageEvent confirmed = default;
            damage.DamageConfirmed += value => confirmed = value;
            Assert.That(controller.AffixStatuses.ApplyVulnerability(target.RuntimeId, new Float2(8f, 9f), 2f, true), Is.True);
            Assert.That(controller.AffixStatuses.ApplyPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, target.RuntimeId,
                new Float2(8f, 9f), 10, 1, attack, true)), Is.True);

            controller.AffixStatuses.Tick(.5f, 3);
            Assert.That(confirmed.WeaponId, Is.EqualTo(WeaponId.HwandoFlyingBlade));
            Assert.That(confirmed.AttackInstanceId, Is.EqualTo(94));
            Assert.That(confirmed.ContactPoint, Is.EqualTo(new Float2(8f, 9f)));
            Assert.That(confirmed.IsBossTarget, Is.True);
            Assert.That(confirmed.FinalDamage, Is.EqualTo(10));
            controller.Dispose();
        }

        [Test]
        public void Target_unregistration_clears_statuses_before_runtime_id_reuse_and_retires_attack()
        {
            var registry = new CombatTargetRegistry();
            var oldTarget = new Target(23, 100);
            registry.Register(oldTarget);
            var damage = new CombatDamageService(registry);
            var controller = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            Assert.That(controller.AffixStatuses.ApplyVulnerability(23, new Float2(1f, 1f), 2f, true), Is.True);
            Assert.That(controller.AffixStatuses.ApplyPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, 23,
                new Float2(1f, 1f), 5, 2, new AttackInstance(95, RepeatHitPolicy.TimedTicks, .5f), true)), Is.True);
            controller.AffixStatuses.Tick(.5f, 1);
            Assert.That(damage.TrackedAttackCount, Is.EqualTo(1));
            Assert.That(registry.Unregister(oldTarget), Is.True);
            Assert.That(damage.TrackedAttackCount, Is.EqualTo(0));
            var replacement = new Target(23, 100);
            registry.Register(replacement);
            Assert.That(damage.TryApply(WeaponDamageRequest.Create(96, WeaponId.GakgungShot, replacement, 10, false,
                new Float2(1f, 1f), ContactPhase.Direct, 2), out var normal), Is.True);
            Assert.That(normal.FinalDamage, Is.EqualTo(10));
            controller.Dispose();
        }

        [Test]
        public void Reset_and_dispose_retire_active_periodic_attacks()
        {
            var registry = new CombatTargetRegistry();
            var target = new Target(24, 100);
            registry.Register(target);
            var damage = new CombatDamageService(registry);
            var controller = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            Assert.That(controller.AffixStatuses.ApplyPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, 24,
                new Float2(0f, 0f), 5, 2, new AttackInstance(97, RepeatHitPolicy.TimedTicks, .5f), true)), Is.True);
            controller.AffixStatuses.Tick(.5f, 1);
            Assert.That(damage.TrackedAttackCount, Is.EqualTo(1));
            controller.Reset();
            Assert.That(damage.TrackedAttackCount, Is.EqualTo(0));
            controller.Dispose();
            Assert.That(damage.TrackedAttackCount, Is.EqualTo(0));
        }

        [Test]
        public void Dispose_while_periodic_effect_is_live_retires_its_attack()
        {
            var registry = new CombatTargetRegistry();
            var target = new Target(25, 100);
            registry.Register(target);
            var damage = new CombatDamageService(registry);
            var controller = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            Assert.That(controller.AffixStatuses.ApplyPeriodic(new PeriodicEffectRequest(WeaponId.HwandoFlyingBlade, 25,
                new Float2(0f, 0f), 5, 2, new AttackInstance(99, RepeatHitPolicy.TimedTicks, .5f), true)), Is.True);
            controller.AffixStatuses.Tick(.5f, 1);
            Assert.That(damage.TrackedAttackCount, Is.EqualTo(1));

            controller.Dispose();
            Assert.That(damage.TrackedAttackCount, Is.EqualTo(0));
        }

        [Test]
        public void Dispose_unsubscribes_target_removal_before_runtime_id_reuse()
        {
            var registry = new CombatTargetRegistry();
            var target = new Target(26, 100);
            registry.Register(target);
            var damage = new CombatDamageService(registry);
            var controller = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            Assert.That(TargetUnregisteredSubscriberCount(registry), Is.EqualTo(1));

            controller.Dispose();
            Assert.That(TargetUnregisteredSubscriberCount(registry), Is.EqualTo(0));
            Assert.That(registry.Unregister(target), Is.True);
            var replacement = new Target(26, 100);
            registry.Register(replacement);
            Assert.That(damage.TryApply(WeaponDamageRequest.Create(100, WeaponId.GakgungShot, replacement, 10, false,
                new Float2(0f, 0f), ContactPhase.Direct, 1), out var normal), Is.True);
            Assert.That(normal.FinalDamage, Is.EqualTo(10));
        }

        [Test]
        public void Shared_damage_service_rejects_second_controller_until_first_disposes_without_side_effects()
        {
            var registry = new CombatTargetRegistry();
            var target = new Target(27, 100);
            registry.Register(target);
            var damage = new CombatDamageService(registry);
            var first = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            Assert.That(TargetUnregisteredSubscriberCount(registry), Is.EqualTo(1));
            Assert.That(() => new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1")), Throws.TypeOf<InvalidOperationException>());
            Assert.That(TargetUnregisteredSubscriberCount(registry), Is.EqualTo(1));
            Assert.That(first.AffixStatuses.ApplyVulnerability(27, new Float2(0f, 0f), 2f, true), Is.True);
            Assert.That(damage.TryApply(WeaponDamageRequest.Create(101, WeaponId.GakgungShot, target, 10, false,
                new Float2(0f, 0f), ContactPhase.Direct, 1), out var firstBoosted), Is.True);
            Assert.That(firstBoosted.FinalDamage, Is.EqualTo(12));

            first.Dispose();
            var replacement = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            Assert.That(replacement.AffixStatuses.ApplyVulnerability(27, new Float2(0f, 0f), 2f, true), Is.True);
            Assert.That(damage.TryApply(WeaponDamageRequest.Create(102, WeaponId.GakgungShot, target, 10, false,
                new Float2(0f, 0f), ContactPhase.Direct, 2), out var replacementBoosted), Is.True);
            Assert.That(replacementBoosted.FinalDamage, Is.EqualTo(12));
            replacement.Dispose();
        }

        private static int TargetUnregisteredSubscriberCount(CombatTargetRegistry registry)
        {
            var field = typeof(CombatTargetRegistry).GetField("TargetUnregistered", BindingFlags.Instance | BindingFlags.NonPublic);
            return (field?.GetValue(registry) as Delegate)?.GetInvocationList().Length ?? 0;
        }

        [Test]
        public void Vulnerability_multiplies_unrelated_later_damage_for_two_seconds_then_expires()
        {
            var registry = new CombatTargetRegistry();
            var target = new Target(18, 100);
            registry.Register(target);
            var damage = new CombatDamageService(registry);
            var controller = new WeaponRuntimeController(registry, damage, PixelHitMask.FromRows("1"));
            Assert.That(controller.AffixStatuses.ApplyVulnerability(target.RuntimeId, new Float2(1f, 1f), 2f, true), Is.True);
            Assert.That(damage.TryApply(WeaponDamageRequest.Create(301, WeaponId.GakgungShot, target, 10, false,
                new Float2(1f, 1f), ContactPhase.Direct, 1), out var boosted), Is.True);
            Assert.That(boosted.FinalDamage, Is.EqualTo(12));
            controller.AffixStatuses.Tick(2f, 2);
            Assert.That(damage.TryApply(WeaponDamageRequest.Create(302, WeaponId.GakgungShot, target, 10, false,
                new Float2(1f, 1f), ContactPhase.Direct, 3), out var normal), Is.True);
            Assert.That(normal.FinalDamage, Is.EqualTo(10));
            controller.Dispose();
        }

        private sealed class Target : ICombatTarget
        {
            private readonly bool isBoss;
            public Target(int runtimeId, int health, bool isBoss = false) { RuntimeId = runtimeId; Health = health; this.isBoss = isBoss; }
            public int RuntimeId { get; }
            public bool IsAlive => Health > 0;
            public int Health { get; private set; }
            public bool IsBoss => isBoss;
            public bool IsElite => false;
            public float ThreatScore => 0f;
            public Float2 WorldPosition => new Float2(0f, 0f);
            public PixelHitMask HurtMask => null;
            public PixelMaskTransform HurtMaskTransform => PixelMaskTransform.Identity;
            public void ApplyResolvedDamage(int damage) { Health -= damage; }
            public void ApplyKnockback(Float2 direction, float force) { }
        }
    }
}
