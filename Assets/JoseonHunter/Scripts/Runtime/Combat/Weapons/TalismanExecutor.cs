using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public enum TalismanState { Flying, Attached, Sealing, Transferring, Complete }

    /// <summary>Sequential, contact-gated talisman chain. Reservations are owned by one cast and never shared with other weapons.</summary>
    public sealed class TalismanExecutor : IWeaponExecutor
    {
        private const float ArrivalDistance = 0.04f;
        private readonly WeaponRuntimeController runtime;
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly List<TalismanCast> active = new List<TalismanCast>();
        private readonly List<ICombatTarget> bindingTargets = new List<ICombatTarget>();
        private float cooldown;
        private AttackInstance bindingAttack;

        public TalismanExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float speed, int hopCount, int level)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            BaseDamage = Mathf.Max(1f, baseDamage); CooldownSeconds = Mathf.Max(0.01f, cooldownSeconds);
            Range = Mathf.Max(0.01f, range); Speed = Mathf.Max(0.01f, speed);
            HopCount = Mathf.Max(1, hopCount); Level = Mathf.Clamp(level, 1, 5);
        }

        public float BaseDamage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public float Speed { get; }
        public int HopCount { get; }
        public int Level { get; }
        public int ActiveCastCount => active.Count;
        public TalismanState LastState { get; private set; } = TalismanState.Complete;
        public int LastFinalBurstCount { get; private set; }
        public IReadOnlyList<ContactPhase> LastContactPhases => lastContactPhases;
        private readonly List<ContactPhase> lastContactPhases = new List<ContactPhase>();

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            cooldown -= Mathf.Max(0f, deltaTime);
            if (cooldown <= 0f && TryFindNearestLegal(context.OwnerPosition, null, out var target))
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
                active.RemoveAt(index);
            }
            if (Level == 5 && active.Count == 0 && bindingTargets.Count > 0) ResolveBindingBurst(context);
        }

        public void Reset()
        {
            foreach (var cast in active) runtime.DamageService.RetireAttack(cast.Attack.InstanceId);
            active.Clear(); bindingTargets.Clear();
            if (bindingAttack != null) runtime.DamageService.RetireAttack(bindingAttack.InstanceId);
            bindingAttack = null; cooldown = 0f; LastState = TalismanState.Complete; LastFinalBurstCount = 0; lastContactPhases.Clear();
        }

        private void Launch(in WeaponExecutionContext context, ICombatTarget target)
        {
            var simultaneous = Level == 5 ? Mathf.Min(3, HopCount) : 1;
            LastFinalBurstCount = 0; lastContactPhases.Clear();
            bindingTargets.Clear();
            bindingAttack = Level == 5 ? new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f) : null;
            var launchReservations = new HashSet<int>();
            for (var index = 0; index < simultaneous; index++)
            {
                if (index > 0 && !TryFindNearestLegal(context.OwnerPosition, launchReservations, out target)) break;
                // Each talisman has its own reservations and hit memory, while IDs remain globally allocated by the runtime.
                var cast = new TalismanCast(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f), context.OwnerPosition, target, HopCount);
                cast.ReservedTargets.Add(target.RuntimeId);
                launchReservations.Add(target.RuntimeId);
                active.Add(cast);
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
            if (!PixelMaskContactService.TryFindContact(runtime.BladeMask, PixelMaskTransform.Translation(cast.Position.X, cast.Position.Y), cast.Target.HurtMask, cast.Target.HurtMaskTransform, out var contact)) return;

            if (cast.State == TalismanState.Flying)
                Apply(cast, cast.Target, contact, ContactPhase.Direct, context.SimulationTick);
            Apply(cast, cast.Target, contact, ContactPhase.Attach, context.SimulationTick);
            cast.State = TalismanState.Attached;
        }

        private void ResolveSeal(TalismanCast cast, in WeaponExecutionContext context)
        {
            if (!IsCurrentTargetValid(cast.Target)) { ResolveNoTarget(cast, context); return; }
            if (TryContact(cast.Target, out var contact)) Apply(cast, cast.Target, contact, ContactPhase.Seal, context.SimulationTick);
            cast.CompletedHops++;
            if (cast.CompletedHops >= HopCount || !TryFindNearestLegal(cast.Target.WorldPosition, cast.ReservedTargets, out var next))
            {
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
            cast.State = TalismanState.Transferring;
        }

        private void ResolveNoTarget(TalismanCast cast, in WeaponExecutionContext context)
        {
            // A terminal attached target always gets exactly one safe burst; an initial no-target cast never exists.
            if (IsCurrentTargetValid(cast.Target) && TryContact(cast.Target, out var contact))
            {
                var multiplier = Level == 5 ? 2 : 1;
                if (Apply(cast, cast.Target, contact, ContactPhase.Blast, context.SimulationTick, multiplier)) LastFinalBurstCount++;
            }
            cast.State = TalismanState.Complete;
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

        private bool TryContact(ICombatTarget target, out Float2 contact) =>
            target != null && target.HurtMask != null && PixelMaskContactService.TryFindContact(runtime.BladeMask, PixelMaskTransform.Translation(target.WorldPosition.X, target.WorldPosition.Y), target.HurtMask, target.HurtMaskTransform, out contact);

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
            public TalismanCast(AttackInstance attack, Float2 position, ICombatTarget target, int hopLimit)
            { Attack = attack; Position = position; Target = target; HopLimit = hopLimit; }
            public AttackInstance Attack { get; }
            public Float2 Position { get; set; }
            public ICombatTarget Target { get; set; }
            public int HopLimit { get; }
            public int CompletedHops { get; set; }
            public float SealRemaining { get; set; }
            public HashSet<int> ReservedTargets { get; } = new HashSet<int>();
            public TalismanState State { get; set; } = TalismanState.Flying;
        }
    }
}
