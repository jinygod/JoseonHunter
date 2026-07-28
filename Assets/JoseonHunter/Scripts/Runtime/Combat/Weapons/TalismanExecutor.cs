using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public enum TalismanState { Flying, Attached, Sealing, Transferring, Complete }

    /// <summary>Sequential, contact-gated talisman chain. Reservations are owned by one cast and never shared with other weapons.</summary>
    public sealed class TalismanExecutor : IWeaponExecutor, IWeaponEvolutionProfile
    {
        private const float ArrivalDistance = 0.04f;
        private readonly WeaponRuntimeController runtime;
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly List<TalismanCast> active = new List<TalismanCast>();
        private readonly List<ICombatTarget> bindingTargets = new List<ICombatTarget>();
        private float cooldown;
        private AttackInstance bindingAttack;

        public TalismanExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float speed, int hopCount, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            BaseDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage)); CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds));
            Range = Mathf.Max(0.01f, modifiers.ScaleArea(range)); Speed = Mathf.Max(0.01f, modifiers.ScaleSpeed(speed)); Potentials = modifiers;
            HopCount = Mathf.Max(1, hopCount); Level = Mathf.Clamp(level, 1, 5);
            IsEvolved = evolved;
        }

        public float BaseDamage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public float Speed { get; }
        public int HopCount { get; }
        public int Level { get; }
        public bool IsEvolved { get; }
        public WeaponRuntimeModifiers Potentials { get; }
        public int ActiveCastCount => active.Count;
        public int LastLaunchCount { get; private set; }
        public int TotalLaunchedTalismanCount { get; private set; }
        public TalismanState LastState { get; private set; } = TalismanState.Complete;
        public int LastFinalBurstCount { get; private set; }
        public IReadOnlyList<ContactPhase> LastContactPhases => lastContactPhases;
        private readonly List<ContactPhase> lastContactPhases = new List<ContactPhase>();

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            cooldown -= Mathf.Max(0f, deltaTime);
            // A level-five cast owns its shared five-color binding state until every seal has resolved.
            // Do not let a short cooldown clear or reuse that state underneath active talismans.
            if (cooldown <= 0f && (Level != 5 || active.Count == 0) && TryFindNearestLegal(context.OwnerPosition, null, out var target))
            {
                cooldown = CooldownSeconds;
                Launch(context, target);
            }

            for (var index = active.Count - 1; index >= 0; index--)
            {
                var cast = active[index];
                Advance(cast, Mathf.Max(0f, deltaTime), context);
                LastState = cast.State;
                if (cast.State != TalismanState.Complete) continue;
                runtime.DamageService.RetireAttack(cast.Attack.InstanceId);
                if (cast.BlastAttack != null) runtime.DamageService.RetireAttack(cast.BlastAttack.InstanceId);
                active.RemoveAt(index);
            }
            if (!IsEvolved && Level == 5 && active.Count == 0)
            {
                if (bindingTargets.Count > 0) ResolveBindingBurst(context);
                else if (bindingAttack != null)
                {
                    runtime.DamageService.RetireAttack(bindingAttack.InstanceId);
                    bindingAttack = null;
                }
            }
        }

        public void Reset()
        {
            foreach (var cast in active)
            {
                runtime.DamageService.RetireAttack(cast.Attack.InstanceId);
                if (cast.BlastAttack != null) runtime.DamageService.RetireAttack(cast.BlastAttack.InstanceId);
            }
            active.Clear(); bindingTargets.Clear();
            if (bindingAttack != null) runtime.DamageService.RetireAttack(bindingAttack.InstanceId);
            bindingAttack = null; cooldown = 0f; LastState = TalismanState.Complete; LastFinalBurstCount = 0;
            LastLaunchCount = 0; TotalLaunchedTalismanCount = 0; lastContactPhases.Clear();
        }

        public void Dispose() => Reset();

        private void Launch(in WeaponExecutionContext context, ICombatTarget target)
        {
            // Hop count is each talisman's sequential chain length; the master form always starts up to three independent seals.
            var simultaneous = !IsEvolved && Level == 5 ? 3 : 1;
            LastFinalBurstCount = 0; lastContactPhases.Clear();
            bindingTargets.Clear();
            bindingAttack = !IsEvolved && Level == 5 ? new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f) : null;
            var launchReservations = new HashSet<int>();
            LastLaunchCount = 0;
            for (var index = 0; index < simultaneous; index++)
            {
                if (index > 0 && !TryFindNearestLegal(context.OwnerPosition, launchReservations, out target)) break;
                // Each talisman has its own reservations and hit memory, while IDs remain globally allocated by the runtime.
                var cast = new TalismanCast(
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f),
                    IsEvolved ? new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f) : null,
                    context.OwnerPosition, target, HopCount);
                cast.ReservedTargets.Add(target.RuntimeId);
                cast.AttemptedTargets.Add(target.RuntimeId);
                launchReservations.Add(target.RuntimeId);
                active.Add(cast);
                LastLaunchCount++;
                TotalLaunchedTalismanCount++;
            }
        }

        private void Advance(TalismanCast cast, float deltaTime, in WeaponExecutionContext context)
        {
            switch (cast.State)
            {
                case TalismanState.Flying:
                case TalismanState.Transferring:
                    AdvanceFlight(cast, deltaTime, context);
                    break;
                case TalismanState.Attached:
                    cast.SealRemaining = SealDelay;
                    cast.State = TalismanState.Sealing;
                    break;
                case TalismanState.Sealing:
                    cast.SealRemaining -= deltaTime;
                    if (cast.SealRemaining <= 0f) ResolveSeal(cast, context);
                    break;
            }
        }

        private float SealDelay => Mathf.Max(0.03f, 0.18f / (1f + (Level - 1) * 0.2f));

        private void AdvanceFlight(TalismanCast cast, float deltaTime, in WeaponExecutionContext context)
        {
            if (!IsCurrentTargetValid(cast.Target)) { ResolveNoTarget(cast, context); return; }
            var delta = Subtract(cast.Target.WorldPosition, cast.Position);
            var distance = Length(delta);
            var step = Mathf.Min(distance, Speed * deltaTime);
            cast.Position = distance <= ArrivalDistance ? cast.Target.WorldPosition : new Float2(cast.Position.X + delta.X / distance * step, cast.Position.Y + delta.Y / distance * step);
            if (Length(Subtract(cast.Target.WorldPosition, cast.Position)) > ArrivalDistance) return;
            if (!TryContact(cast.Target, out var contact))
            {
                ResolveFailedContact(cast);
                return;
            }

            Apply(cast, cast.Target, contact, ContactPhase.Direct, context.SimulationTick);
            Apply(cast, cast.Target, contact, ContactPhase.Attach, context.SimulationTick);
            cast.State = TalismanState.Attached;
        }

        private void ResolveFailedContact(TalismanCast cast)
        {
            // A live target without overlapping active pixels is not an attached target. Do not reserve it forever or wait at its position.
            cast.ReservedTargets.Remove(cast.Target.RuntimeId);
            if (cast.CompletedHops < HopCount && TryFindNearestLegal(cast.Position, cast.AttemptedTargets, out var next))
            {
                cast.Target = next;
                cast.ReservedTargets.Add(next.RuntimeId);
                cast.AttemptedTargets.Add(next.RuntimeId);
                cast.State = TalismanState.Transferring;
                return;
            }

            cast.State = TalismanState.Complete;
        }

        private void ResolveSeal(TalismanCast cast, in WeaponExecutionContext context)
        {
            if (!IsCurrentTargetValid(cast.Target)) { ResolveNoTarget(cast, context); return; }
            // A five-color binding may include only seals that were both pixel-confirmed and accepted by the damage service.
            if (!TryContact(cast.Target, out var contact) || !Apply(cast, cast.Target, contact, ContactPhase.Seal, context.SimulationTick))
            {
                ResolveFailedContact(cast);
                return;
            }
            cast.CompletedHops++;
            if (IsEvolved) cast.RecordLinkedTarget(cast.Target);
            if (cast.CompletedHops >= HopCount || !TryFindNearestLegal(cast.Target.WorldPosition, cast.ReservedTargets, out var next))
            {
                if (IsEvolved)
                {
                    ResolveEvolvedChainBurst(cast, context);
                    cast.State = TalismanState.Complete;
                    return;
                }
                if (Level == 5)
                {
                    if (!bindingTargets.Contains(cast.Target)) bindingTargets.Add(cast.Target);
                    cast.State = TalismanState.Complete;
                    return;
                }
                ResolveNoTarget(cast, context);
                return;
            }

            cast.Target = next;
            cast.ReservedTargets.Add(next.RuntimeId);
            cast.AttemptedTargets.Add(next.RuntimeId);
            cast.State = TalismanState.Transferring;
        }

        private void ResolveNoTarget(TalismanCast cast, in WeaponExecutionContext context)
        {
            if (IsEvolved)
            {
                if (cast.LinkedTargets.Count >= 3) ResolveEvolvedChainBurst(cast, context);
                cast.State = TalismanState.Complete;
                return;
            }
            // A terminal attached target always gets exactly one safe burst; an initial no-target cast never exists.
            if (IsCurrentTargetValid(cast.Target) && TryContact(cast.Target, out var contact))
            {
                var multiplier = Level == 5 ? 2 : 1;
                if (Apply(cast, cast.Target, contact, ContactPhase.Blast, context.SimulationTick, multiplier)) LastFinalBurstCount++;
            }
            cast.State = TalismanState.Complete;
        }

        private void ResolveEvolvedChainBurst(TalismanCast cast, in WeaponExecutionContext context)
        {
            if (cast.LinkedTargets.Count < 3) return;
            var resolved = false;
            foreach (var target in cast.LinkedTargets)
            {
                if (!IsCurrentTargetValid(target) || !TryContact(target, out var contact)) continue;
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(cast.BlastAttack, WeaponId.TalismanThrow, target, Mathf.CeilToInt(BaseDamage) * 2, false, contact, ContactPhase.Blast, context.SimulationTick), out _))
                {
                    lastContactPhases.Add(ContactPhase.Blast);
                    resolved = true;
                }
            }
            if (resolved) LastFinalBurstCount = 1;
        }

        private void ResolveBindingBurst(in WeaponExecutionContext context)
        {
            var resolved = false;
            foreach (var target in bindingTargets)
            {
                if (!IsCurrentTargetValid(target) || !TryContact(target, out var contact)) continue;
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(bindingAttack, WeaponId.TalismanThrow, target, Mathf.CeilToInt(BaseDamage) * 2, false, contact, ContactPhase.Blast, context.SimulationTick), out _))
                {
                    lastContactPhases.Add(ContactPhase.Blast); resolved = true;
                }
            }
            if (resolved) LastFinalBurstCount = 1;
            runtime.DamageService.RetireAttack(bindingAttack.InstanceId);
            bindingAttack = null; bindingTargets.Clear();
        }

        private bool TryFindNearestLegal(Float2 origin, HashSet<int> reservations, out ICombatTarget selected)
        {
            selected = null; var bestDistance = Range * Range;
            runtime.Targets.CopyTo(targets);
            foreach (var candidate in targets)
            {
                if (!IsTargetAvailable(candidate, reservations)) continue;
                var offset = Subtract(candidate.WorldPosition, origin); var distance = offset.X * offset.X + offset.Y * offset.Y;
                if (distance > bestDistance || selected != null && (distance > bestDistance || distance == bestDistance && candidate.RuntimeId >= selected.RuntimeId)) continue;
                selected = candidate; bestDistance = distance;
            }
            return selected != null;
        }

        private bool TryContact(ICombatTarget target, out Float2 contact)
        {
            contact = default;
            return target != null && target.HurtMask != null &&
                PixelMaskContactService.TryFindContact(runtime.BladeMask, PixelMaskTransform.Translation(target.WorldPosition.X, target.WorldPosition.Y), target.HurtMask, target.HurtMaskTransform, out contact);
        }

        private bool Apply(TalismanCast cast, ICombatTarget target, Float2 contact, ContactPhase phase, int tick, int multiplier = 1)
        {
            var applied = runtime.DamageService.TryApply(WeaponDamageRequest.Create(cast.Attack, WeaponId.TalismanThrow, target, Mathf.CeilToInt(BaseDamage) * multiplier, false, contact, phase, tick), out _);
            if (applied) lastContactPhases.Add(phase);
            return applied;
        }

        private static bool IsCurrentTargetValid(ICombatTarget target) => target != null && target.IsAlive;
        private static bool IsTargetAvailable(ICombatTarget target, HashSet<int> reservations) => IsCurrentTargetValid(target) && (reservations == null || !reservations.Contains(target.RuntimeId));
        private static Float2 Subtract(Float2 left, Float2 right) => new Float2(left.X - right.X, left.Y - right.Y);
        private static float Length(Float2 value) => Mathf.Sqrt(value.X * value.X + value.Y * value.Y);

        private sealed class TalismanCast
        {
            public TalismanCast(AttackInstance attack, AttackInstance blastAttack, Float2 position, ICombatTarget target, int hopLimit)
            { Attack = attack; BlastAttack = blastAttack; Position = position; Target = target; HopLimit = hopLimit; }
            public AttackInstance Attack { get; }
            public AttackInstance BlastAttack { get; }
            public Float2 Position { get; set; }
            public ICombatTarget Target { get; set; }
            public int HopLimit { get; }
            public int CompletedHops { get; set; }
            public float SealRemaining { get; set; }
            public HashSet<int> ReservedTargets { get; } = new HashSet<int>();
            public HashSet<int> AttemptedTargets { get; } = new HashSet<int>();
            public List<ICombatTarget> LinkedTargets { get; } = new List<ICombatTarget>();
            public HashSet<int> LinkedTargetIds { get; } = new HashSet<int>();
            public TalismanState State { get; set; } = TalismanState.Flying;

            public void RecordLinkedTarget(ICombatTarget target)
            {
                if (target != null && LinkedTargetIds.Add(target.RuntimeId)) LinkedTargets.Add(target);
            }
        }
    }
}
