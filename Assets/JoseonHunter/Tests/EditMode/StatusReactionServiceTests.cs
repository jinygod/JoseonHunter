using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Runtime.Combat;
using NUnit.Framework;

namespace JoseonHunter.Tests.EditMode
{
    public sealed class StatusReactionServiceTests
    {
        [Test]
        public void One_hit_consumes_only_highest_priority_eligible_reaction()
        {
            using var rig = new Rig(8);
            var target = rig.Targets[0];
            rig.Statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Freeze, 2f, 1, WeaponId.FrostFlask);
            rig.Statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Shock, 2f, 1, WeaponId.ThunderCrashBomb);
            var hit = Hit(target, WeaponHitTrait.Explosion | WeaponHitTrait.Pull, 1f);

            var result = rig.Statuses.TryResolveReaction(hit, Confirmed(hit));

            Assert.That(result.Kind, Is.EqualTo(StatusReactionKind.IceShatter));
            Assert.That(result.AffectedCount, Is.LessThanOrEqualTo(5));
            Assert.That(rig.Damage.TrackedAttackCount, Is.Zero);
            Assert.That(rig.Statuses.HasStatus(target.RuntimeId, CombatStatusKind.Freeze), Is.False);
            Assert.That(rig.Statuses.HasStatus(target.RuntimeId, CombatStatusKind.Shock), Is.True);
        }

        [Test]
        public void Per_target_cooldown_is_exact_and_reaction_hits_cannot_recurse()
        {
            using var rig = new Rig(8);
            var target = rig.Targets[0];
            rig.Statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Freeze, 2f, 1, WeaponId.FrostFlask);
            var first = Hit(target, WeaponHitTrait.Explosion, 1f);
            Assert.That(rig.Statuses.TryResolveReaction(first, Confirmed(first)).Kind,
                Is.EqualTo(StatusReactionKind.IceShatter));

            rig.Statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Freeze, 2f, 1, WeaponId.FrostFlask);
            var early = Hit(target, WeaponHitTrait.Heavy, 1.59f);
            Assert.That(rig.Statuses.TryResolveReaction(early, Confirmed(early)).Kind,
                Is.EqualTo(StatusReactionKind.None));
            var ready = Hit(target, WeaponHitTrait.Heavy, 1.6f);
            Assert.That(rig.Statuses.TryResolveReaction(ready, Confirmed(ready)).Kind,
                Is.EqualTo(StatusReactionKind.IceShatter));

            rig.Statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Freeze, 2f, 1, WeaponId.FrostFlask);
            var recursive = Hit(target, WeaponHitTrait.Explosion | WeaponHitTrait.Reaction, 3f);
            Assert.That(rig.Statuses.TryResolveReaction(recursive, Confirmed(recursive)).Kind,
                Is.EqualTo(StatusReactionKind.None));
        }

        [Test]
        public void Reaction_caps_are_five_four_one_and_three()
        {
            using var ice = new Rig(9);
            ice.Statuses.ApplyTimedStatus(ice.Targets[0].RuntimeId, CombatStatusKind.Freeze, 2f, 1, WeaponId.FrostFlask);
            var iceHit = Hit(ice.Targets[0], WeaponHitTrait.Explosion, 1f);
            Assert.That(ice.Statuses.TryResolveReaction(iceHit, Confirmed(iceHit)).AffectedCount, Is.EqualTo(5));

            using var wind = new Rig(9);
            wind.Statuses.ApplyPeriodic(new PeriodicEffectRequest(WeaponId.SingijeonVolley,
                wind.Targets[0].RuntimeId, wind.Targets[0].WorldPosition, 2, 8,
                new AttackInstance(901, RepeatHitPolicy.TimedTicks, .5f), true, ContactPhase.Burn));
            var windHit = Hit(wind.Targets[0], WeaponHitTrait.Wind, 1f);
            var windResult = wind.Statuses.TryResolveReaction(windHit, Confirmed(windHit));
            Assert.That(windResult.Kind, Is.EqualTo(StatusReactionKind.FireWind));
            Assert.That(windResult.AffectedCount, Is.EqualTo(4));
            Assert.That(wind.Statuses.PeriodicEffectCountForTests, Is.EqualTo(5));

            using var formation = new Rig(3);
            formation.Statuses.ApplyTimedStatus(formation.Targets[0].RuntimeId, CombatStatusKind.Seal, 2f, 1, WeaponId.TalismanThrow);
            var formationHit = Hit(formation.Targets[0], WeaponHitTrait.Slash, 1f);
            Assert.That(formation.Statuses.TryResolveReaction(formationHit, Confirmed(formationHit)).AffectedCount,
                Is.EqualTo(1));

            using var overload = new Rig(9);
            overload.Statuses.ApplyTimedStatus(overload.Targets[0].RuntimeId, CombatStatusKind.Shock, 2f, 1, WeaponId.ThunderCrashBomb);
            var overloadHit = Hit(overload.Targets[0], WeaponHitTrait.Barrier, 1f);
            var overloadResult = overload.Statuses.TryResolveReaction(overloadHit, Confirmed(overloadHit));
            Assert.That(overloadResult.Kind, Is.EqualTo(StatusReactionKind.Overload));
            Assert.That(overloadResult.AffectedCount, Is.EqualTo(3));
            Assert.That(overload.Targets[0].StaggerSeconds, Is.EqualTo(.2f).Within(.001f));
        }

        [Test]
        public void Invalid_statuses_are_rejected_and_unregister_clears_state()
        {
            using var rig = new Rig(1);
            var target = rig.Targets[0];
            Assert.That(rig.Statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Poison, 0f, 1,
                WeaponId.HwandoFlyingBlade), Is.False);
            Assert.That(rig.Statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Poison, float.NaN, 1,
                WeaponId.HwandoFlyingBlade), Is.False);
            Assert.That(rig.Statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Poison, 1f, 0,
                WeaponId.HwandoFlyingBlade), Is.False);
            Assert.That(rig.Statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Poison, 1f, 1,
                WeaponId.HwandoFlyingBlade), Is.True);
            rig.Registry.Unregister(target);
            Assert.That(rig.Statuses.HasStatus(target.RuntimeId, CombatStatusKind.Poison), Is.False);
        }

        [Test]
        public void Damage_service_resolves_reaction_only_after_confirming_the_original_hit()
        {
            using var rig = new Rig(3);
            var target = rig.Targets[0];
            var events = 0;
            rig.Statuses.ReactionTriggered += _ => events++;
            rig.Statuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Freeze, 2f, 1,
                WeaponId.FrostFlask);
            var unconfirmed = WeaponDamageRequest.Create(new AttackInstance(810, RepeatHitPolicy.OncePerPhase, 0f),
                WeaponId.ThunderCrashBomb, target, 10, false, target.WorldPosition, ContactPhase.Direct, 1,
                1f, false, WeaponHitTrait.Explosion);
            Assert.That(rig.Damage.TryApply(unconfirmed, out _), Is.False);
            Assert.That(events, Is.Zero);
            Assert.That(rig.Statuses.HasStatus(target.RuntimeId, CombatStatusKind.Freeze), Is.True);

            var confirmed = WeaponDamageRequest.Create(new AttackInstance(811, RepeatHitPolicy.OncePerPhase, 0f),
                WeaponId.ThunderCrashBomb, target, 10, false, target.WorldPosition, ContactPhase.Direct, 2,
                1f, true, WeaponHitTrait.Explosion);
            Assert.That(rig.Damage.TryApply(confirmed, out _), Is.True);
            Assert.That(events, Is.EqualTo(1));
            Assert.That(rig.Statuses.HasStatus(target.RuntimeId, CombatStatusKind.Freeze), Is.False);
        }

        private static WeaponDamageRequest Hit(Target target, WeaponHitTrait traits, float hitTime) =>
            WeaponDamageRequest.Create(new AttackInstance(700 + target.RuntimeId, RepeatHitPolicy.OncePerPhase, 0f),
                WeaponId.ThunderCrashBomb, target, 10, false, target.WorldPosition, ContactPhase.Direct,
                (int)(hitTime * 100f), hitTime, true, traits, new Float2(0f, 0f));

        private static ConfirmedDamageEvent Confirmed(in WeaponDamageRequest hit) =>
            new(hit.AttackInstanceId, hit.WeaponId, hit.Target.RuntimeId, new DamageResult(10, false),
                hit.ContactPoint, hit.Phase, hit.SimulationTick);

        private sealed class Rig : System.IDisposable
        {
            public Rig(int count)
            {
                Registry = new CombatTargetRegistry();
                Damage = new CombatDamageService(Registry);
                Runtime = new WeaponRuntimeController(Registry, Damage, PixelHitMask.FromRows("1"));
                Targets = new Target[count];
                for (var index = 0; index < count; index++)
                {
                    Targets[index] = new Target(index + 1, new Float2(index * .15f, 0f));
                    Registry.Register(Targets[index]);
                }
            }

            public CombatTargetRegistry Registry { get; }
            public CombatDamageService Damage { get; }
            public WeaponRuntimeController Runtime { get; }
            public WeaponAffixStatusService Statuses => Runtime.AffixStatuses;
            public Target[] Targets { get; }
            public void Dispose() => Runtime.Dispose();
        }

        private sealed class Target : ICombatTarget, IControlStatusTarget
        {
            public Target(int id, Float2 position) { RuntimeId = id; WorldPosition = position; }
            public int RuntimeId { get; }
            public bool IsAlive => Health > 0;
            public int Health { get; private set; } = 1000;
            public bool IsBoss => false;
            public bool IsElite => false;
            public float ThreatScore => 0f;
            public Float2 WorldPosition { get; }
            public PixelHitMask HurtMask => null;
            public PixelMaskTransform HurtMaskTransform => PixelMaskTransform.Identity;
            public float StaggerSeconds { get; private set; }
            public void ApplyResolvedDamage(int damage) => Health -= damage;
            public void ApplyKnockback(Float2 direction, float force) { }
            public void ApplyStagger(float durationSeconds) => StaggerSeconds = durationSeconds;
        }
    }
}
