using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;

namespace JoseonHunter.Runtime.Combat
{
    public readonly struct PeriodicEffectRequest
    {
        public const float IntervalSeconds = .5f;
        public PeriodicEffectRequest(WeaponId sourceWeapon, int targetRuntimeId, Float2 confirmedContactPoint, int damagePerTick, int remainingTicks, AttackInstance attackInstance, bool confirmedContact, ContactPhase phase = ContactPhase.Poison)
        {
            SourceWeapon = sourceWeapon;
            TargetRuntimeId = targetRuntimeId;
            ConfirmedContactPoint = confirmedContactPoint;
            DamagePerTick = damagePerTick;
            RemainingTicks = remainingTicks;
            AttackInstance = attackInstance;
            ConfirmedContact = confirmedContact;
            Phase = phase;
        }

        public WeaponId SourceWeapon { get; }
        public int TargetRuntimeId { get; }
        public Float2 ConfirmedContactPoint { get; }
        public int DamagePerTick { get; }
        public int RemainingTicks { get; }
        public AttackInstance AttackInstance { get; }
        public bool ConfirmedContact { get; }
        public ContactPhase Phase { get; }
    }

    /// <summary>Run-owned periodic damage and vulnerability state, kept separate from weapon executors.</summary>
    public sealed class WeaponAffixStatusService
    {
        private const float PeriodicInterval = PeriodicEffectRequest.IntervalSeconds;
        private readonly CombatTargetRegistry targets;
        private readonly CombatDamageService damage;
        private readonly List<PeriodicEffect> periodicEffects = new List<PeriodicEffect>();
        private readonly Dictionary<int, float> vulnerabilityRemaining = new Dictionary<int, float>();
        private readonly List<int> expiredVulnerabilityTargets = new List<int>();
        private readonly List<int> vulnerabilityTargets = new List<int>();

        public WeaponAffixStatusService(CombatTargetRegistry targets, CombatDamageService damage)
        {
            this.targets = targets ?? throw new ArgumentNullException(nameof(targets));
            this.damage = damage ?? throw new ArgumentNullException(nameof(damage));
        }

        public bool ApplyPeriodic(in PeriodicEffectRequest request)
        {
            if (!request.ConfirmedContact || request.DamagePerTick <= 0 || request.RemainingTicks <= 0 || request.AttackInstance == null ||
                request.AttackInstance.RepeatHitPolicy != RepeatHitPolicy.TimedTicks || Math.Abs(request.AttackInstance.RepeatInterval - PeriodicInterval) > .0001f ||
                !IsFinite(request.ConfirmedContactPoint) || !TryGetLiveTarget(request.TargetRuntimeId, out _)) return false;
            periodicEffects.Add(new PeriodicEffect(request));
            return true;
        }

        /// <summary>Refreshes the same source/phase on one target instead of creating parallel poison, burn, or bleed stacks.</summary>
        public bool ApplyOrRefreshPeriodic(in PeriodicEffectRequest request)
        {
            if (!request.ConfirmedContact || request.DamagePerTick <= 0 || request.RemainingTicks <= 0 || request.AttackInstance == null ||
                request.AttackInstance.RepeatHitPolicy != RepeatHitPolicy.TimedTicks || Math.Abs(request.AttackInstance.RepeatInterval - PeriodicInterval) > .0001f ||
                !IsFinite(request.ConfirmedContactPoint) || !TryGetLiveTarget(request.TargetRuntimeId, out _)) return false;
            for (var index = 0; index < periodicEffects.Count; index++)
            {
                var effect = periodicEffects[index];
                if (!effect.SourceWeapon.Equals(request.SourceWeapon) || effect.TargetRuntimeId != request.TargetRuntimeId || effect.Phase != request.Phase) continue;
                Retire(effect);
                periodicEffects[index] = new PeriodicEffect(request);
                return true;
            }
            periodicEffects.Add(new PeriodicEffect(request));
            return true;
        }

        public bool ApplyVulnerability(int targetRuntimeId, Float2 confirmedContact, float durationSeconds, bool confirmedContactHit)
        {
            if (!confirmedContactHit || !IsFinite(durationSeconds) || durationSeconds <= 0f || !IsFinite(confirmedContact) || !TryGetLiveTarget(targetRuntimeId, out _)) return false;
            vulnerabilityRemaining[targetRuntimeId] = Math.Max(durationSeconds, vulnerabilityRemaining.TryGetValue(targetRuntimeId, out var current) ? current : 0f);
            return true;
        }

        public void Tick(float deltaTime, int simulationTick)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            TickVulnerability(deltaTime);
            for (var index = periodicEffects.Count - 1; index >= 0; index--)
            {
                var effect = periodicEffects[index];
                if (!TryGetLiveTarget(effect.TargetRuntimeId, out var target)) { Retire(effect); periodicEffects.RemoveAt(index); continue; }
                effect.Elapsed += deltaTime;
                while (effect.Elapsed >= PeriodicInterval && effect.RemainingTicks > 0)
                {
                    if (!damage.TryApply(WeaponDamageRequest.Create(effect.AttackInstance, effect.SourceWeapon, target, effect.DamagePerTick, false,
                        effect.ContactPoint, effect.Phase, simulationTick, effect.NextTickTime, true), out _)) break;
                    effect.Elapsed -= PeriodicInterval;
                    effect.NextTickTime += PeriodicInterval;
                    effect.RemainingTicks--;
                }
                if (effect.RemainingTicks <= 0) { Retire(effect); periodicEffects.RemoveAt(index); } else periodicEffects[index] = effect;
            }
        }

        public void ClearTarget(int runtimeId)
        {
            vulnerabilityRemaining.Remove(runtimeId);
            for (var index = periodicEffects.Count - 1; index >= 0; index--)
                if (periodicEffects[index].TargetRuntimeId == runtimeId) { Retire(periodicEffects[index]); periodicEffects.RemoveAt(index); }
        }

        public void Reset()
        {
            foreach (var effect in periodicEffects) Retire(effect);
            periodicEffects.Clear();
            vulnerabilityRemaining.Clear();
        }

        internal float IncomingDamageMultiplier(int targetRuntimeId, ContactPhase phase) =>
            IsPeriodicPhase(phase) || !vulnerabilityRemaining.ContainsKey(targetRuntimeId) ? 1f : 1.2f;

        private void TickVulnerability(float deltaTime)
        {
            expiredVulnerabilityTargets.Clear();
            vulnerabilityTargets.Clear();
            foreach (var entry in vulnerabilityRemaining) vulnerabilityTargets.Add(entry.Key);
            foreach (var runtimeId in vulnerabilityTargets)
            {
                var remaining = vulnerabilityRemaining[runtimeId] - deltaTime;
                if (remaining <= 0f) expiredVulnerabilityTargets.Add(runtimeId); else vulnerabilityRemaining[runtimeId] = remaining;
            }
            foreach (var runtimeId in expiredVulnerabilityTargets) vulnerabilityRemaining.Remove(runtimeId);
        }

        private bool TryGetLiveTarget(int runtimeId, out ICombatTarget target) =>
            targets.TryGet(runtimeId, out target) && target != null && target.IsAlive && target.Health > 0 && targets.Contains(target);

        private void Retire(PeriodicEffect effect) => damage.RetireAttack(effect.AttackInstance.InstanceId);

        private static bool IsPeriodicPhase(ContactPhase phase) => phase == ContactPhase.Poison || phase == ContactPhase.Burn || phase == ContactPhase.Bleed;
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool IsFinite(Float2 point) => !float.IsNaN(point.X) && !float.IsInfinity(point.X) && !float.IsNaN(point.Y) && !float.IsInfinity(point.Y);

        private struct PeriodicEffect
        {
            public PeriodicEffect(PeriodicEffectRequest request)
            {
                SourceWeapon = request.SourceWeapon; TargetRuntimeId = request.TargetRuntimeId; ContactPoint = request.ConfirmedContactPoint;
                DamagePerTick = request.DamagePerTick; RemainingTicks = request.RemainingTicks; AttackInstance = request.AttackInstance;
                Phase = request.Phase; Elapsed = 0f; NextTickTime = PeriodicInterval;
            }
            public WeaponId SourceWeapon; public int TargetRuntimeId; public Float2 ContactPoint; public int DamagePerTick;
            public int RemainingTicks; public AttackInstance AttackInstance; public ContactPhase Phase; public float Elapsed; public float NextTickTime;
        }
    }
}
