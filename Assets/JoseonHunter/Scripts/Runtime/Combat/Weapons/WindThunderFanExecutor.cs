using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public enum WindThunderFanState { WindActive, EchoDelay, LightningResolve, Complete }

    /// <summary>Contact-gated gusts mark targets first; the later echo resolves all marks in one simulation tick.</summary>
    public sealed class WindThunderFanExecutor : IWeaponExecutor
    {
        private readonly WeaponRuntimeController runtime;
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly List<ICombatTarget> marked = new List<ICombatTarget>();
        private float cooldown;
        private AttackInstance attack;
        private int gustIndex;
        private float echoRemaining;

        public WindThunderFanExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float knockback, int markedTargetCap, int level)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            BaseDamage = Mathf.Max(1f, baseDamage); CooldownSeconds = Mathf.Max(0.01f, cooldownSeconds); Range = Mathf.Max(0.01f, range);
            Knockback = Mathf.Max(0f, knockback); MarkedTargetCap = Mathf.Max(1, markedTargetCap); Level = Mathf.Clamp(level, 1, 5);
            State = WindThunderFanState.Complete;
        }

        public float BaseDamage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public float Knockback { get; }
        public int MarkedTargetCap { get; }
        public int Level { get; }
        public WindThunderFanState State { get; private set; }
        public int LastWindContactCount { get; private set; }
        public int LastLightningContactCount { get; private set; }
        public int LastLightningSimulationTick { get; private set; } = -1;

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            cooldown -= Mathf.Max(0f, deltaTime);
            if (State == WindThunderFanState.Complete && cooldown <= 0f && HasLegalTarget()) StartCast();
            switch (State)
            {
                case WindThunderFanState.WindActive:
                    ResolveGust(context);
                    break;
                case WindThunderFanState.EchoDelay:
                    echoRemaining -= Mathf.Max(0f, deltaTime);
                    if (echoRemaining <= 0f) State = WindThunderFanState.LightningResolve;
                    break;
                case WindThunderFanState.LightningResolve:
                    ResolveLightning(context);
                    break;
            }
        }

        public void Reset()
        {
            if (attack != null) runtime.DamageService.RetireAttack(attack.InstanceId);
            attack = null; marked.Clear(); cooldown = 0f; State = WindThunderFanState.Complete;
            LastWindContactCount = 0; LastLightningContactCount = 0; LastLightningSimulationTick = -1;
        }

        private void StartCast()
        {
            cooldown = CooldownSeconds; marked.Clear(); gustIndex = 0; LastWindContactCount = 0; LastLightningContactCount = 0;
            attack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f);
            State = WindThunderFanState.WindActive;
        }

        private void ResolveGust(in WeaponExecutionContext context)
        {
            var direction = Level == 5 ? CardinalDirections[gustIndex] : DangerousDirection(context.OwnerPosition);
            runtime.Targets.CopyTo(targets);
            targets.Sort((left, right) => CompareDanger(context.OwnerPosition, left, right));
            foreach (var target in targets)
            {
                if (marked.Count >= MarkedTargetCap || target == null || !target.IsAlive || marked.Contains(target)) continue;
                if (!IsInsideCone(context.OwnerPosition, direction, target.WorldPosition) || !TryGustContact(target, out var contact)) continue;
                // Push is intentionally issued before the confirmed wind damage, so an echo cannot precede the visible gust response.
                target.ApplyKnockback(direction, Knockback);
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(attack, WeaponId.WindThunderFan, target, Mathf.CeilToInt(BaseDamage), false, contact, ContactPhase.Wind, context.SimulationTick), out _))
                {
                    marked.Add(target); LastWindContactCount++;
                }
            }
            gustIndex++;
            if (gustIndex < (Level == 5 ? 4 : 1)) return;
            echoRemaining = 0.12f;
            State = WindThunderFanState.EchoDelay;
        }

        private void ResolveLightning(in WeaponExecutionContext context)
        {
            // Resolve from the fixed marked list; every confirmed lightning event receives this exact tick.
            foreach (var target in marked)
            {
                if (target == null || !target.IsAlive || !TryGustContact(target, out var contact)) continue;
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(attack, WeaponId.WindThunderFan, target, Mathf.CeilToInt(BaseDamage * (1f + Level * 0.1f)), false, contact, ContactPhase.Lightning, context.SimulationTick), out _)) LastLightningContactCount++;
            }
            LastLightningSimulationTick = context.SimulationTick;
            runtime.DamageService.RetireAttack(attack.InstanceId);
            attack = null; marked.Clear(); State = WindThunderFanState.Complete;
        }

        private bool HasLegalTarget()
        {
            runtime.Targets.CopyTo(targets);
            foreach (var target in targets) if (target != null && target.IsAlive) return true;
            return false;
        }

        private bool TryGustContact(ICombatTarget target, out Float2 contact) => target.HurtMask != null &&
            PixelMaskContactService.TryFindContact(runtime.BladeMask, PixelMaskTransform.Translation(target.WorldPosition.X, target.WorldPosition.Y), target.HurtMask, target.HurtMaskTransform, out contact);

        private bool IsInsideCone(Float2 origin, Float2 direction, Float2 position)
        {
            var offset = new Float2(position.X - origin.X, position.Y - origin.Y); var distance = Mathf.Sqrt(offset.X * offset.X + offset.Y * offset.Y);
            if (distance > Range || distance < 0.0001f) return distance < 0.0001f;
            return (offset.X * direction.X + offset.Y * direction.Y) / distance >= Mathf.Cos(50f * Mathf.Deg2Rad);
        }

        private Float2 DangerousDirection(Float2 origin)
        {
            runtime.Targets.CopyTo(targets); ICombatTarget best = null;
            foreach (var target in targets) if (target != null && target.IsAlive && (best == null || CompareDanger(origin, target, best) < 0)) best = target;
            if (best == null) return new Float2(1f, 0f);
            var x = best.WorldPosition.X - origin.X; var y = best.WorldPosition.Y - origin.Y; var length = Mathf.Sqrt(x * x + y * y);
            return length > 0.0001f ? new Float2(x / length, y / length) : new Float2(1f, 0f);
        }

        private static int CompareDanger(Float2 origin, ICombatTarget left, ICombatTarget right)
        {
            if (left == null) return 1; if (right == null) return -1;
            var leftScore = (left.ThreatScore + (left.IsElite ? 25f : 0f) + (left.IsBoss ? 100f : 0f)) / (1f + DistanceSquared(origin, left.WorldPosition));
            var rightScore = (right.ThreatScore + (right.IsElite ? 25f : 0f) + (right.IsBoss ? 100f : 0f)) / (1f + DistanceSquared(origin, right.WorldPosition));
            var compared = rightScore.CompareTo(leftScore);
            return compared != 0 ? compared : left.RuntimeId.CompareTo(right.RuntimeId);
        }

        private static float DistanceSquared(Float2 left, Float2 right) { var x = left.X - right.X; var y = left.Y - right.Y; return x * x + y * y; }
        private static readonly Float2[] CardinalDirections = { new Float2(1f, 0f), new Float2(0f, 1f), new Float2(-1f, 0f), new Float2(0f, -1f) };
    }
}
