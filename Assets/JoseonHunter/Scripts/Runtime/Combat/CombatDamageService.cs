using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;

namespace JoseonHunter.Runtime.Combat
{
    public readonly struct WeaponDamageRequest
    {
        private WeaponDamageRequest(AttackInstance attackInstance, WeaponId weaponId, ICombatTarget target, DamageRequest damageRequest, Float2 contactPoint, bool hasConfirmedContact, ContactPhase phase, int simulationTick, float hitTime)
        {
            AttackInstance = attackInstance;
            WeaponId = weaponId;
            Target = target;
            DamageRequest = damageRequest;
            ContactPoint = contactPoint;
            HasConfirmedContact = hasConfirmedContact;
            Phase = phase;
            SimulationTick = simulationTick;
            HitTime = hitTime;
        }

        public AttackInstance AttackInstance { get; }
        public int AttackInstanceId => AttackInstance == null ? 0 : AttackInstance.InstanceId;
        public WeaponId WeaponId { get; }
        public ICombatTarget Target { get; }
        public DamageRequest DamageRequest { get; }
        public Float2 ContactPoint { get; }
        public bool HasConfirmedContact { get; }
        public ContactPhase Phase { get; }
        public int SimulationTick { get; }
        /// <summary>Monotonic attack timing. Existing callers use their simulation tick; real-time mechanics can supply seconds.</summary>
        public float HitTime { get; }

        public static WeaponDamageRequest Create(int attackInstanceId, WeaponId weaponId, ICombatTarget target, int baseDamage, bool critical, Float2 contactPoint, ContactPhase phase, int simulationTick) =>
            Create(new AttackInstance(attackInstanceId, RepeatHitPolicy.OncePerPhase, 0f), weaponId, target, baseDamage, critical, contactPoint, phase, simulationTick);

        public static WeaponDamageRequest Create(AttackInstance attackInstance, WeaponId weaponId, ICombatTarget target, int baseDamage, bool critical, Float2 contactPoint, ContactPhase phase, int simulationTick, bool hasConfirmedContact = true) =>
            new WeaponDamageRequest(attackInstance, weaponId, target, new DamageRequest(baseDamage, 0, critical, 1f), contactPoint, hasConfirmedContact, phase, simulationTick, simulationTick);

        public static WeaponDamageRequest Create(AttackInstance attackInstance, WeaponId weaponId, ICombatTarget target, int baseDamage, bool critical, Float2 contactPoint, ContactPhase phase, int simulationTick, float hitTime, bool hasConfirmedContact = true) =>
            new WeaponDamageRequest(attackInstance, weaponId, target, new DamageRequest(baseDamage, 0, critical, 1f), contactPoint, hasConfirmedContact, phase, simulationTick, hitTime);
    }

    public sealed class CombatDamageService
    {
        private readonly CombatTargetRegistry targetRegistry;
        private readonly Dictionary<int, AttackInstance> attacks = new Dictionary<int, AttackInstance>();
        private WeaponAffixStatusService affixStatuses;

        public CombatDamageService(CombatTargetRegistry targetRegistry)
        {
            this.targetRegistry = targetRegistry ?? throw new ArgumentNullException(nameof(targetRegistry));
        }

        public event Action<ConfirmedDamageEvent> DamageConfirmed;
        public int TrackedAttackCount => attacks.Count;
#if UNITY_INCLUDE_TESTS
        public WeaponAffixStatusService AttachedAffixStatusesForTests => affixStatuses;
#endif

        /// <summary>Attaches the sole live affix-status owner for this shared damage service.</summary>
        public void AttachAffixStatuses(WeaponAffixStatusService provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (ReferenceEquals(affixStatuses, provider)) return;
            if (affixStatuses != null) throw new InvalidOperationException("A different weapon runtime already owns affix statuses for this damage service.");
            affixStatuses = provider;
        }

        /// <summary>Detaches only the matching owner, so stale runtime disposal cannot affect a replacement.</summary>
        public void DetachAffixStatuses(WeaponAffixStatusService provider)
        {
            if (provider != null && ReferenceEquals(affixStatuses, provider)) affixStatuses = null;
        }

        /// <summary>Forgets a completed attack after its executor has permanently stopped producing contacts.</summary>
        public bool RetireAttack(int attackInstanceId) => attacks.Remove(attackInstanceId);

        public void ClearAttacks() => attacks.Clear();

        public bool TryApply(in WeaponDamageRequest request, out ConfirmedDamageEvent confirmed)
        {
            confirmed = default;
            if (!HasValidTarget(request.Target) || !request.HasConfirmedContact || !IsFinite(request.ContactPoint) || request.AttackInstance == null) return false;
            var incomingMultiplier = affixStatuses == null ? 1f : affixStatuses.IncomingDamageMultiplier(request.Target.RuntimeId, request.Phase);
            var damageRequest = new DamageRequest(request.DamageRequest.BaseDamage, request.DamageRequest.FlatBonus,
                request.DamageRequest.IsCritical, request.DamageRequest.Multiplier * incomingMultiplier);
            if (!DamageResolver.TryResolve(damageRequest, out var result)) return false;

            var attack = GetAttack(request.AttackInstance);
            if (!attack.TryRecordHit(request.Target.RuntimeId, request.Phase, request.HitTime)) return false;

            request.Target.ApplyResolvedDamage(result.FinalDamage);
            confirmed = new ConfirmedDamageEvent(attack.InstanceId, request.WeaponId, request.Target.RuntimeId, result, request.ContactPoint, request.Phase, request.SimulationTick, request.Target.IsBoss);
            DamageConfirmed?.Invoke(confirmed);
            return true;
        }

        private bool HasValidTarget(ICombatTarget target) =>
            target != null && target.IsAlive && target.Health > 0 && targetRegistry.Contains(target);

        private AttackInstance GetAttack(AttackInstance requestAttack)
        {
            if (attacks.TryGetValue(requestAttack.InstanceId, out var attack)) return attack;
            attacks.Add(requestAttack.InstanceId, requestAttack);
            return requestAttack;
        }

        private static bool IsFinite(Float2 point) =>
            !float.IsNaN(point.X) && !float.IsInfinity(point.X) && !float.IsNaN(point.Y) && !float.IsInfinity(point.Y);
    }
}
