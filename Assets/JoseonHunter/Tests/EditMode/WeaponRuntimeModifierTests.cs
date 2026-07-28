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
            public Target(int runtimeId, int health) { RuntimeId = runtimeId; Health = health; }
            public int RuntimeId { get; }
            public bool IsAlive => Health > 0;
            public int Health { get; private set; }
            public bool IsBoss => false;
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
