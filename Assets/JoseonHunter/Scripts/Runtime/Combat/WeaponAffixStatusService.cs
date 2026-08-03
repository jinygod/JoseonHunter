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
        private readonly Dictionary<int, TargetStatusState> timedStatuses = new Dictionary<int, TargetStatusState>();
        private readonly List<int> statusTargetIds = new List<int>();
        private readonly List<ICombatTarget> targetBuffer = new List<ICombatTarget>();
        private readonly List<ICombatTarget> reactionTargets = new List<ICombatTarget>(5);
        private int nextReactionAttackId = 1500000000;

        private sealed class TargetStatusState
        {
            public readonly float[] Remaining = new float[7];
            public readonly byte[] Stacks = new byte[7];
            public readonly WeaponId[] Sources = new WeaponId[7];
            public float NextReactionTime;
            public float ReactionCooldownRemaining;
            public float SealVulnerabilityRemaining;
        }

        public WeaponAffixStatusService(CombatTargetRegistry targets, CombatDamageService damage)
        {
            this.targets = targets ?? throw new ArgumentNullException(nameof(targets));
            this.damage = damage ?? throw new ArgumentNullException(nameof(damage));
        }

        public event Action<StatusReactionEvent> ReactionTriggered;

        public bool ApplyTimedStatus(int targetId, CombatStatusKind kind, float duration, int stacks, WeaponId source)
        {
            var statusIndex = (int)kind;
            if (statusIndex < 0 || statusIndex >= 7 || !IsFinite(duration) || duration <= 0f ||
                stacks <= 0 || !TryGetLiveTarget(targetId, out _)) return false;
            if (!timedStatuses.TryGetValue(targetId, out var state))
                timedStatuses.Add(targetId, state = new TargetStatusState());
            state.Remaining[statusIndex] = Math.Max(state.Remaining[statusIndex], duration);
            state.Stacks[statusIndex] = (byte)Math.Min(byte.MaxValue, Math.Max(state.Stacks[statusIndex], stacks));
            state.Sources[statusIndex] = source;
            return true;
        }

        public bool HasStatus(int targetId, CombatStatusKind kind)
        {
            var statusIndex = (int)kind;
            return statusIndex >= 0 && statusIndex < 7 && timedStatuses.TryGetValue(targetId, out var state) &&
                   state.Remaining[statusIndex] > 0f && state.Stacks[statusIndex] > 0;
        }

        public bool ApplySealVulnerability(int targetId, float duration)
        {
            if (!IsFinite(duration) || duration <= 0f || !TryGetLiveTarget(targetId, out _))
                return false;
            if (!timedStatuses.TryGetValue(targetId, out var state))
                timedStatuses.Add(targetId, state = new TargetStatusState());
            state.SealVulnerabilityRemaining = Math.Max(state.SealVulnerabilityRemaining, duration);
            return true;
        }

        public bool ApplyPeriodic(in PeriodicEffectRequest request)
        {
            if (!request.ConfirmedContact || request.DamagePerTick <= 0 || request.RemainingTicks <= 0 || request.AttackInstance == null ||
                request.AttackInstance.RepeatHitPolicy != RepeatHitPolicy.TimedTicks || Math.Abs(request.AttackInstance.RepeatInterval - PeriodicInterval) > .0001f ||
                !IsFinite(request.ConfirmedContactPoint) || !TryGetLiveTarget(request.TargetRuntimeId, out _)) return false;
            periodicEffects.Add(new PeriodicEffect(request));
            SynchronizePeriodicStatus(request);
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
                SynchronizePeriodicStatus(request);
                return true;
            }
            periodicEffects.Add(new PeriodicEffect(request));
            SynchronizePeriodicStatus(request);
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
            TickTimedStatuses(deltaTime);
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
            timedStatuses.Remove(runtimeId);
            for (var index = periodicEffects.Count - 1; index >= 0; index--)
                if (periodicEffects[index].TargetRuntimeId == runtimeId) { Retire(periodicEffects[index]); periodicEffects.RemoveAt(index); }
        }

        public void Reset()
        {
            foreach (var effect in periodicEffects) Retire(effect);
            periodicEffects.Clear();
            vulnerabilityRemaining.Clear();
            timedStatuses.Clear();
            statusTargetIds.Clear();
            targetBuffer.Clear();
            reactionTargets.Clear();
            nextReactionAttackId = 1500000000;
        }

        internal float IncomingDamageMultiplier(int targetRuntimeId, ContactPhase phase)
        {
            if (IsPeriodicPhase(phase)) return 1f;
            var multiplier = vulnerabilityRemaining.ContainsKey(targetRuntimeId) ? 1.2f : 1f;
            if (!timedStatuses.TryGetValue(targetRuntimeId, out var state)) return multiplier;
            if (Active(state, CombatStatusKind.ArmorBreak)) multiplier = Math.Max(multiplier, 1.25f);
            if (state.SealVulnerabilityRemaining > 0f) multiplier = Math.Max(multiplier, 1.15f);
            return multiplier;
        }

        public StatusReactionResult TryResolveReaction(in WeaponDamageRequest hit,
            in ConfirmedDamageEvent confirmed)
        {
            if ((hit.Traits & WeaponHitTrait.Reaction) != 0 || hit.Target == null ||
                confirmed.TargetRuntimeId != hit.Target.RuntimeId ||
                !timedStatuses.TryGetValue(hit.Target.RuntimeId, out var state) ||
                hit.HitTime < state.NextReactionTime)
                return default;

            StatusReactionKind kind;
            int affected;
            if (Active(state, CombatStatusKind.Freeze) &&
                HasAny(hit.Traits, WeaponHitTrait.Explosion | WeaponHitTrait.Heavy))
            {
                Consume(state, CombatStatusKind.Freeze);
                affected = DamageNearest(hit, confirmed.ContactPoint, 1.4f, 5, 1.8f, ContactPhase.PotentialBlast);
                kind = StatusReactionKind.IceShatter;
            }
            else if ((Active(state, CombatStatusKind.Burn) || Active(state, CombatStatusKind.Poison)) &&
                     HasAny(hit.Traits, WeaponHitTrait.Wind | WeaponHitTrait.Pull))
            {
                affected = SpreadFireWind(hit.Target.RuntimeId, state, confirmed.ContactPoint);
                kind = StatusReactionKind.FireWind;
            }
            else if ((Active(state, CombatStatusKind.Seal) || Active(state, CombatStatusKind.ArmorBreak)) &&
                     HasAny(hit.Traits, WeaponHitTrait.Slash | WeaponHitTrait.Pierce))
            {
                if (Active(state, CombatStatusKind.Seal)) Consume(state, CombatStatusKind.Seal);
                else Consume(state, CombatStatusKind.ArmorBreak);
                affected = SubmitReactionDamage(hit.Target, hit, 1.5f, ContactPhase.PotentialBlast) ? 1 : 0;
                kind = StatusReactionKind.FormationBreak;
            }
            else if (Active(state, CombatStatusKind.Shock) &&
                     HasAny(hit.Traits, WeaponHitTrait.Barrier | WeaponHitTrait.Knockback | WeaponHitTrait.Pull))
            {
                Consume(state, CombatStatusKind.Shock);
                affected = DamageNearest(hit, confirmed.ContactPoint, 6f, 3, .8f, ContactPhase.PotentialChain,
                    applyStagger: true);
                kind = StatusReactionKind.Overload;
            }
            else return default;

            state.NextReactionTime = hit.HitTime + .6f;
            state.ReactionCooldownRemaining = .6f;
            var result = new StatusReactionResult(kind, confirmed.ContactPoint, affected);
            ReactionTriggered?.Invoke(new StatusReactionEvent(kind, confirmed.ContactPoint, affected));
            return result;
        }

#if UNITY_INCLUDE_TESTS
        public int PeriodicEffectCountForTests => periodicEffects.Count;
        public bool HasVulnerabilityForTests(int targetRuntimeId) => vulnerabilityRemaining.ContainsKey(targetRuntimeId);
        public float VulnerabilityRemainingForTests(int targetRuntimeId) => vulnerabilityRemaining.TryGetValue(targetRuntimeId, out var remaining) ? remaining : 0f;
#endif

        private int SpreadFireWind(int sourceTargetId, TargetStatusState state, Float2 center)
        {
            var kind = Active(state, CombatStatusKind.Burn) ? CombatStatusKind.Burn : CombatStatusKind.Poison;
            var index = (int)kind;
            var copiedDuration = state.Remaining[index] * .5f;
            var stacks = state.Stacks[index];
            var source = state.Sources[index];
            state.Remaining[index] = copiedDuration;
            var periodicIndex = FindPeriodicEffect(sourceTargetId, kind);
            var hasPeriodic = periodicIndex >= 0;
            var periodicTemplate = hasPeriodic ? periodicEffects[periodicIndex] : default;
            if (hasPeriodic)
            {
                periodicTemplate.RemainingTicks = Math.Max(1,
                    (int)Math.Ceiling(periodicTemplate.RemainingTicks * .5f));
                periodicEffects[periodicIndex] = periodicTemplate;
            }
            SelectNearest(center, 6f, 4, sourceTargetId);
            var affected = 0;
            foreach (var target in reactionTargets)
            {
                if (hasPeriodic)
                {
                    var ticks = Math.Max(1, (int)Math.Ceiling(copiedDuration / PeriodicInterval));
                    var request = new PeriodicEffectRequest(periodicTemplate.SourceWeapon, target.RuntimeId,
                        target.WorldPosition, periodicTemplate.DamagePerTick, ticks,
                        new AttackInstance(AllocateReactionAttackId(), RepeatHitPolicy.TimedTicks, PeriodicInterval),
                        true, periodicTemplate.Phase);
                    if (ApplyOrRefreshPeriodic(request)) affected++;
                }
                else if (ApplyTimedStatus(target.RuntimeId, kind, copiedDuration, stacks, source)) affected++;
            }
            return affected;
        }

        private int DamageNearest(in WeaponDamageRequest hit, Float2 center, float radius, int cap,
            float multiplier, ContactPhase phase, bool applyStagger = false)
        {
            SelectNearest(center, radius, cap, 0);
            var affected = 0;
            foreach (var target in reactionTargets)
            {
                if (SubmitReactionDamage(target, hit, multiplier, phase)) affected++;
                if (applyStagger && target is IControlStatusTarget control) control.ApplyStagger(.2f);
            }
            return affected;
        }

        private bool SubmitReactionDamage(ICombatTarget target, in WeaponDamageRequest sourceHit,
            float multiplier, ContactPhase phase)
        {
            if (target == null || !target.IsAlive) return false;
            var attackId = AllocateReactionAttackId();
            var attack = new AttackInstance(attackId, RepeatHitPolicy.OncePerPhase, 0f);
            var baseDamage = Math.Max(1, (int)Math.Round(sourceHit.DamageRequest.BaseDamage * multiplier,
                MidpointRounding.AwayFromZero));
            var applied = damage.TryApply(WeaponDamageRequest.Create(attack, sourceHit.WeaponId, target, baseDamage,
                false, target.WorldPosition, phase, sourceHit.SimulationTick, sourceHit.HitTime, true,
                WeaponHitTrait.Reaction, sourceHit.AttackOrigin ?? sourceHit.ContactPoint), out _);
            damage.RetireAttack(attackId);
            return applied;
        }

        private void SelectNearest(Float2 center, float radius, int cap, int excludedTargetId)
        {
            reactionTargets.Clear();
            targets.CopyTo(targetBuffer);
            var radiusSquared = radius * radius;
            foreach (var target in targetBuffer)
            {
                if (target == null || !target.IsAlive || target.Health <= 0 || target.RuntimeId == excludedTargetId)
                    continue;
                var distance = DistanceSquared(target.WorldPosition, center);
                if (distance > radiusSquared) continue;
                var insertAt = reactionTargets.Count;
                while (insertAt > 0 && DistanceSquared(reactionTargets[insertAt - 1].WorldPosition, center) > distance)
                    insertAt--;
                if (insertAt >= cap) continue;
                reactionTargets.Insert(insertAt, target);
                if (reactionTargets.Count > cap) reactionTargets.RemoveAt(cap);
            }
        }

        private void TickTimedStatuses(float deltaTime)
        {
            statusTargetIds.Clear();
            foreach (var pair in timedStatuses) statusTargetIds.Add(pair.Key);
            foreach (var targetId in statusTargetIds)
            {
                var state = timedStatuses[targetId];
                var any = false;
                state.ReactionCooldownRemaining = Math.Max(0f, state.ReactionCooldownRemaining - deltaTime);
                state.SealVulnerabilityRemaining = Math.Max(0f,
                    state.SealVulnerabilityRemaining - deltaTime);
                if (state.SealVulnerabilityRemaining > 0f) any = true;
                for (var index = 0; index < state.Remaining.Length; index++)
                {
                    if (state.Remaining[index] <= 0f) continue;
                    state.Remaining[index] = Math.Max(0f, state.Remaining[index] - deltaTime);
                    if (state.Remaining[index] <= 0f) state.Stacks[index] = 0;
                    else any = true;
                }
                if (!any && state.ReactionCooldownRemaining <= 0f) timedStatuses.Remove(targetId);
            }
        }

        private void SynchronizePeriodicStatus(in PeriodicEffectRequest request)
        {
            CombatStatusKind kind;
            switch (request.Phase)
            {
                case ContactPhase.Poison: kind = CombatStatusKind.Poison; break;
                case ContactPhase.Burn: kind = CombatStatusKind.Burn; break;
                case ContactPhase.Bleed: kind = CombatStatusKind.Bleed; break;
                default: return;
            }
            ApplyTimedStatus(request.TargetRuntimeId, kind,
                request.RemainingTicks * PeriodicInterval, 1, request.SourceWeapon);
        }

        private int FindPeriodicEffect(int targetId, CombatStatusKind kind)
        {
            var phase = kind == CombatStatusKind.Burn ? ContactPhase.Burn : ContactPhase.Poison;
            for (var index = periodicEffects.Count - 1; index >= 0; index--)
                if (periodicEffects[index].TargetRuntimeId == targetId && periodicEffects[index].Phase == phase)
                    return index;
            return -1;
        }

        private int AllocateReactionAttackId()
        {
            if (nextReactionAttackId == int.MaxValue) nextReactionAttackId = 1500000000;
            return nextReactionAttackId++;
        }

        private static bool Active(TargetStatusState state, CombatStatusKind kind) =>
            state.Remaining[(int)kind] > 0f && state.Stacks[(int)kind] > 0;

        private static void Consume(TargetStatusState state, CombatStatusKind kind)
        {
            state.Remaining[(int)kind] = 0f;
            state.Stacks[(int)kind] = 0;
        }

        private static bool HasAny(WeaponHitTrait value, WeaponHitTrait flags) => (value & flags) != 0;
        private static float DistanceSquared(Float2 left, Float2 right)
        {
            var x = left.X - right.X;
            var y = left.Y - right.Y;
            return x * x + y * y;
        }

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
