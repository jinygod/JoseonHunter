using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class CombatDamageServiceTests
    {
        [Test]
        public void ConfirmedHitMutatesHealthOnceAndPublishesExactResolvedDamage()
        {
            var target = new FakeCombatTarget(7, 40);
            var service = new CombatDamageService();
            ConfirmedDamageEvent published = default;
            service.DamageConfirmed += value => published = value;
            var request = WeaponDamageRequest.Create(12, WeaponId.HwandoFlyingBlade, target, 9, false, new Float2(3f, 4f), ContactPhase.Outbound, 44);

            Assert.That(service.TryApply(request, out var confirmed), Is.True);
            Assert.That(target.Health, Is.EqualTo(31));
            Assert.That(confirmed, Is.EqualTo(published));
            Assert.That(confirmed.FinalDamage, Is.EqualTo(9));
            Assert.That(confirmed.ContactPoint, Is.EqualTo(new Float2(3f, 4f)));
        }

        [Test]
        public void RegisteredTargetRejectsDuplicateContactBeforeASecondMutation()
        {
            var registry = new CombatTargetRegistry();
            var target = new FakeCombatTarget(7, 40);
            registry.Register(target);
            var service = new CombatDamageService(registry);
            var attack = new AttackInstance(12, RepeatHitPolicy.OncePerInstance, 0f);
            var request = WeaponDamageRequest.Create(attack, WeaponId.HwandoFlyingBlade, target, 9, false, new Float2(3f, 4f), ContactPhase.Outbound, 44);

            Assert.That(service.TryApply(request, out _), Is.True);
            Assert.That(service.TryApply(request, out _), Is.False);
            Assert.That(target.Health, Is.EqualTo(31));
        }

        [Test]
        public void UnregisteredOrUnconfirmedRequestCannotMutateHealth()
        {
            var registry = new CombatTargetRegistry();
            var target = new FakeCombatTarget(7, 40);
            var service = new CombatDamageService(registry);
            var request = WeaponDamageRequest.Create(new AttackInstance(12, RepeatHitPolicy.OncePerPhase, 0f), WeaponId.HwandoFlyingBlade, target, 9, false, new Float2(3f, 4f), ContactPhase.Outbound, 44, false);

            Assert.That(service.TryApply(request, out _), Is.False);
            Assert.That(target.Health, Is.EqualTo(40));
        }

        private sealed class FakeCombatTarget : ICombatTarget
        {
            public FakeCombatTarget(int runtimeId, int health) { RuntimeId = runtimeId; Health = health; }
            public int RuntimeId { get; }
            public bool IsAlive => Health > 0;
            public int Health { get; private set; }
            public Float2 WorldPosition => new Float2(0f, 0f);
            public PixelHitMask HurtMask => null;
            public PixelMaskTransform HurtMaskTransform => PixelMaskTransform.Identity;
            public void ApplyResolvedDamage(int damage) { Health -= damage; }
            public void ApplyKnockback(Float2 direction, float force) { }
        }
    }
}
