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
            var registry = new CombatTargetRegistry();
            registry.Register(target);
            var service = new CombatDamageService(registry);
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
        public void ConfirmedFatalBossHitPreservesEventTimeBossClassification()
        {
            var target = new FakeCombatTarget(7, 9, isBoss: true);
            var registry = new CombatTargetRegistry();
            registry.Register(target);
            var service = new CombatDamageService(registry);

            Assert.That(service.TryApply(WeaponDamageRequest.Create(12, WeaponId.HwandoFlyingBlade, target, 9, false,
                new Float2(3f, 4f), ContactPhase.Outbound, 44), out var confirmed), Is.True);
            Assert.That(target.IsAlive, Is.False);
            Assert.That(confirmed.IsBossTarget, Is.True);
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
            var request = WeaponDamageRequest.Create(new AttackInstance(12, RepeatHitPolicy.OncePerPhase, 0f), WeaponId.HwandoFlyingBlade, target, 9, false, new Float2(3f, 4f), ContactPhase.Outbound, 44);

            Assert.That(service.TryApply(request, out _), Is.False);
            Assert.That(target.Health, Is.EqualTo(40));

            registry.Register(target);
            var unconfirmed = WeaponDamageRequest.Create(new AttackInstance(13, RepeatHitPolicy.OncePerPhase, 0f), WeaponId.HwandoFlyingBlade, target, 9, false, new Float2(3f, 4f), ContactPhase.Outbound, 44, false);
            Assert.That(service.TryApply(unconfirmed, out _), Is.False);
            Assert.That(target.Health, Is.EqualTo(40));
        }

        [Test]
        public void Nonfinite_contact_cannot_create_or_track_an_attack()
        {
            var registry = new CombatTargetRegistry();
            var target = new FakeCombatTarget(9, 40);
            registry.Register(target);
            var service = new CombatDamageService(registry);

            Assert.That(service.TryApply(WeaponDamageRequest.Create(19, WeaponId.HwandoFlyingBlade, target, 9, false,
                new Float2(float.PositiveInfinity, 4f), ContactPhase.Outbound, 44), out _), Is.False);
            Assert.That(target.Health, Is.EqualTo(40));
            Assert.That(service.TrackedAttackCount, Is.EqualTo(0));
        }

        [Test]
        public void ServiceRequiresARegistryAndDamageResolutionRejectsInvalidNumbers()
        {
            Assert.That(() => new CombatDamageService(null), Throws.ArgumentNullException);
            Assert.That(DamageResolver.TryResolve(new DamageRequest(1, 0, false, -1f), out _), Is.False);
            Assert.That(DamageResolver.TryResolve(new DamageRequest(int.MaxValue, 0, false, 2f), out _), Is.False);
            Assert.That(DamageResolver.TryResolve(new DamageRequest(0, 0, false, 0f), out var zeroMultiplier), Is.True);
            Assert.That(zeroMultiplier.FinalDamage, Is.EqualTo(1));
        }

        [Test]
        public void Status_vulnerability_and_directional_resistance_combine_once()
        {
            var registry = new CombatTargetRegistry();
            var target = new ResistantTarget(17, 200);
            registry.Register(target);
            var damage = new CombatDamageService(registry);
            var statuses = new WeaponAffixStatusService(registry, damage);
            damage.AttachAffixStatuses(statuses);
            Assert.That(statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.ArmorBreak, 2f, 1,
                WeaponId.GakgungShot), Is.True);
            Assert.That(statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Seal, 2f, 1,
                WeaponId.TalismanThrow), Is.True);
            var request = WeaponDamageRequest.Create(new AttackInstance(91, RepeatHitPolicy.OncePerPhase, 0f),
                WeaponId.GakgungShot, target, 100, false, new Float2(2f, 0f), ContactPhase.Direct, 1,
                1f, true, WeaponHitTrait.Pierce, new Float2(0f, 0f));

            Assert.That(damage.TryApply(request, out var confirmed), Is.True);
            Assert.That(confirmed.FinalDamage, Is.EqualTo(81));
            damage.DetachAffixStatuses(statuses);
        }

        private sealed class FakeCombatTarget : ICombatTarget
        {
            private readonly bool isBoss;
            public FakeCombatTarget(int runtimeId, int health, bool isBoss = false) { RuntimeId = runtimeId; Health = health; this.isBoss = isBoss; }
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

        private sealed class ResistantTarget : ICombatTarget, IIncomingDamageResistanceTarget
        {
            public ResistantTarget(int runtimeId, int health) { RuntimeId = runtimeId; Health = health; }
            public int RuntimeId { get; }
            public bool IsAlive => Health > 0;
            public int Health { get; private set; }
            public bool IsBoss => false;
            public bool IsElite => false;
            public float ThreatScore => 0f;
            public Float2 WorldPosition => new(2f, 0f);
            public PixelHitMask HurtMask => null;
            public PixelMaskTransform HurtMaskTransform => PixelMaskTransform.Identity;
            public void ApplyResolvedDamage(int damage) => Health -= damage;
            public void ApplyKnockback(Float2 direction, float force) { }
            public float IncomingDamageMultiplier(Float2 attackOrigin, WeaponHitTrait traits) =>
                (traits & WeaponHitTrait.Pierce) != 0 ? .65f : 1f;
        }
    }
}
