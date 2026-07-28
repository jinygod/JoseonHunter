using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public enum WindThunderFanState { WindActive, EchoDelay, LightningResolve, InboundResolve, Complete }

    /// <summary>Contact-gated gusts mark targets first; the later echo resolves all marks in one simulation tick.</summary>
    public sealed class WindThunderFanExecutor : IWeaponExecutor, IWeaponEvolutionProfile
    {
        private const float LightningStrikeInterval = 0.08f;
        private readonly WeaponRuntimeController runtime;
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly List<ICombatTarget> marked = new List<ICombatTarget>();
        private readonly List<int> successfulOutboundTargetIds = new List<int>();
        private readonly HashSet<int> successfulOutboundTargetIdSet = new HashSet<int>();
        private readonly List<float> outboundStrikeTimes = new List<float>();
        private float cooldown;
        private AttackInstance attack;
        private int gustIndex;
        private float echoRemaining;
        private int lightningIndex;
        private float strikeDueIn;
        private float outboundElapsed;
        private float inboundPauseRemaining;
        private Float2 lightningDirection;
        private readonly List<Bleed> bleeds = new List<Bleed>();
        private PendingChain pendingChain;
        private Float2 castOrigin;

        public WindThunderFanExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float knockback, int markedTargetCap, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            BaseDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage)); CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds)); Range = Mathf.Max(0.01f, modifiers.ScaleArea(range)); Potentials = modifiers;
            Knockback = Mathf.Max(0f, knockback); MarkedTargetCap = Mathf.Max(1, markedTargetCap); Level = Mathf.Clamp(level, 1, 5);
            IsEvolved = evolved;
            State = WindThunderFanState.Complete;
        }

        public float BaseDamage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public float Knockback { get; }
        public int MarkedTargetCap { get; }
        public int Level { get; }
        public bool IsEvolved { get; }
        public WeaponRuntimeModifiers Potentials { get; }
        public WindThunderFanState State { get; private set; }
        public int LastWindContactCount { get; private set; }
        public int LastLightningContactCount { get; private set; }
        public int LastInboundContactCount { get; private set; }
        public int LastLightningSimulationTick { get; private set; } = -1;
        public IReadOnlyList<int> LastSuccessfulOutboundTargetIds => successfulOutboundTargetIds;
        public IReadOnlyList<float> LastOutboundStrikeTimes => outboundStrikeTimes;

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            var step = Mathf.Max(0f, deltaTime);
            AdvanceBleeds(step, context);
            AdvancePotentialChain(step, context);
            cooldown -= step;
            if (State == WindThunderFanState.Complete && cooldown <= 0f && HasLegalTarget()) StartCast(context.OwnerPosition);
            switch (State)
            {
                case WindThunderFanState.WindActive:
                    ResolveGust(context);
                    break;
                case WindThunderFanState.EchoDelay:
                    AdvanceEchoDelay(step, context);
                    break;
                case WindThunderFanState.LightningResolve:
                    if (IsEvolved) ResolveEvolvedLightning(step, context);
                    else ResolveLightning(context);
                    break;
                case WindThunderFanState.InboundResolve:
                    AdvanceInboundPause(step, context);
                    break;
            }
        }

        public void Reset()
        {
            if (attack != null) runtime.DamageService.RetireAttack(attack.InstanceId);
            foreach (var bleed in bleeds) runtime.DamageService.RetireAttack(bleed.Attack.InstanceId);
            if (pendingChain.Attack != null) runtime.DamageService.RetireAttack(pendingChain.Attack.InstanceId);
            attack = null; marked.Clear(); successfulOutboundTargetIds.Clear(); successfulOutboundTargetIdSet.Clear(); outboundStrikeTimes.Clear(); bleeds.Clear(); pendingChain = default; outboundElapsed = 0f; inboundPauseRemaining = 0f; strikeDueIn = LightningStrikeInterval; cooldown = 0f; State = WindThunderFanState.Complete;
            LastWindContactCount = 0; LastLightningContactCount = 0; LastInboundContactCount = 0; LastLightningSimulationTick = -1;
        }

        public void Dispose() => Reset();

        private void StartCast(Float2 origin)
        {
            castOrigin = origin;
            cooldown = CooldownSeconds; marked.Clear(); successfulOutboundTargetIds.Clear(); successfulOutboundTargetIdSet.Clear(); outboundStrikeTimes.Clear();
            gustIndex = 0; lightningIndex = 0; strikeDueIn = LightningStrikeInterval; outboundElapsed = 0f; inboundPauseRemaining = 0f;
            LastWindContactCount = 0; LastLightningContactCount = 0; LastInboundContactCount = 0;
            attack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f);
            State = WindThunderFanState.WindActive;
        }

        private void ResolveGust(in WeaponExecutionContext context)
        {
            var ownerPosition = context.OwnerPosition;
            var direction = Level == 5 ? CardinalDirections[gustIndex] : DangerousDirection(ownerPosition);
            if (gustIndex == 0) lightningDirection = direction;
            runtime.Targets.CopyTo(targets);
            targets.Sort((left, right) => CompareDanger(ownerPosition, left, right));
            foreach (var target in targets)
            {
                if (marked.Count >= MarkedTargetCap || target == null || !target.IsAlive || marked.Contains(target)) continue;
                if (!IsInsideCone(context.OwnerPosition, direction, target.WorldPosition) || !TryGustContact(target, out var contact)) continue;
                // Push is intentionally issued before the confirmed wind damage, so an echo cannot precede the visible gust response.
                target.ApplyKnockback(direction, Knockback);
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(attack, WeaponId.WindThunderFan, target, Mathf.CeilToInt(BaseDamage), false, contact, ContactPhase.Wind, context.SimulationTick), out _))
                {
                    marked.Add(target); LastWindContactCount++;
                    if (Potentials.HasPotential(WeaponPotentialId.FanVacuumEdge) && TryPotentialContact(WeaponPotentialId.FanVacuumEdge, target, contact)) RefreshBleed(target, contact);
                }
            }
            gustIndex++;
            if (gustIndex < (Level == 5 ? 4 : 1)) return;
            if (IsEvolved)
                marked.Sort((left, right) => CompareProjection(lightningDirection, left, right));
            echoRemaining = 0.12f;
            State = WindThunderFanState.EchoDelay;
        }

        private void ResolveLightning(in WeaponExecutionContext context)
        {
            // Resolve from the fixed marked list; every confirmed lightning event receives this exact tick.
            foreach (var target in marked)
            {
                if (target == null || !target.IsAlive || !TryGustContact(target, out var contact)) continue;
                var multiplier = LightningMultiplier(target, contact, false);
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(attack, WeaponId.WindThunderFan, target, Mathf.CeilToInt(BaseDamage * (1f + Level * 0.1f) * multiplier), false, contact, ContactPhase.Lightning, context.SimulationTick), out _)) LastLightningContactCount++;
            }
            LastLightningSimulationTick = context.SimulationTick;
            runtime.DamageService.RetireAttack(attack.InstanceId);
            attack = null; marked.Clear(); State = WindThunderFanState.Complete;
        }

        private void AdvanceEchoDelay(float step, in WeaponExecutionContext context)
        {
            if (!IsEvolved)
            {
                echoRemaining -= step;
                if (echoRemaining <= 0f) State = WindThunderFanState.LightningResolve;
                return;
            }
            if (echoRemaining > step)
            {
                echoRemaining -= step;
                return;
            }

            var residual = step - echoRemaining;
            echoRemaining = 0f;
            State = WindThunderFanState.LightningResolve;
            if (IsEvolved) ResolveEvolvedLightning(residual, context);
            else ResolveLightning(context);
        }

        private void ResolveEvolvedLightning(float availableTime, in WeaponExecutionContext context)
        {
            while (lightningIndex < marked.Count && availableTime + 0.00001f >= strikeDueIn)
            {
                availableTime -= strikeDueIn;
                outboundElapsed += strikeDueIn;
                var target = marked[lightningIndex++];
                if (target != null && target.IsAlive && TryGustContact(target, out var contact) &&
                    runtime.DamageService.TryApply(WeaponDamageRequest.Create(attack, WeaponId.WindThunderFan, target, Mathf.CeilToInt(BaseDamage * (1f + Level * 0.1f) * LightningMultiplier(target, contact, false)), false, contact, ContactPhase.Lightning, context.SimulationTick), out _))
                {
                    LastLightningContactCount++;
                    if (successfulOutboundTargetIdSet.Add(target.RuntimeId)) successfulOutboundTargetIds.Add(target.RuntimeId);
                }
                outboundStrikeTimes.Add(outboundElapsed);
                strikeDueIn = LightningStrikeInterval;
            }
            // The phase clock advances even when a frame reaches only part of the
            // next due interval. This keeps telemetry independent of frame slicing.
            if (lightningIndex < marked.Count)
            {
                outboundElapsed += availableTime;
                strikeDueIn -= availableTime;
            }
            LastLightningSimulationTick = context.SimulationTick;
            if (lightningIndex >= marked.Count)
            {
                inboundPauseRemaining = LightningStrikeInterval;
                State = WindThunderFanState.InboundResolve;
                AdvanceInboundPause(availableTime, context);
            }
        }

        private void AdvanceInboundPause(float availableTime, in WeaponExecutionContext context)
        {
            if (inboundPauseRemaining > availableTime + 0.00001f)
            {
                inboundPauseRemaining -= availableTime;
                return;
            }

            var residual = Mathf.Max(0f, availableTime - inboundPauseRemaining);
            inboundPauseRemaining = 0f;
            ResolveInbound(context);
            AdvancePotentialChain(residual, context);
        }

        private void ResolveInbound(in WeaponExecutionContext context)
        {
            for (var index = successfulOutboundTargetIds.Count - 1; index >= 0; index--)
            {
                if (!runtime.Targets.TryGet(successfulOutboundTargetIds[index], out var target) || target == null || !target.IsAlive || !TryGustContact(target, out var contact)) continue;
                var hit = runtime.DamageService.TryApply(WeaponDamageRequest.Create(attack, WeaponId.WindThunderFan, target, Mathf.CeilToInt(BaseDamage * .6f * LightningMultiplier(target, contact, true)), false, contact, ContactPhase.Inbound, context.SimulationTick), out _);
                if (hit) { LastInboundContactCount++; if (Potentials.HasPotential(WeaponPotentialId.FanReturningChain) && !target.IsAlive && TryPotentialContact(WeaponPotentialId.FanReturningChain, target, contact) && !pendingChain.Scheduled) ScheduleChain(target); }
            }
            runtime.DamageService.RetireAttack(attack.InstanceId);
            attack = null; marked.Clear(); State = WindThunderFanState.Complete;
        }

        private bool HasLegalTarget()
        {
            runtime.Targets.CopyTo(targets);
            foreach (var target in targets) if (target != null && target.IsAlive) return true;
            return false;
        }

        private bool TryGustContact(ICombatTarget target, out Float2 contact)
        {
            contact = default;
            return target.HurtMask != null &&
                PixelMaskContactService.TryFindContact(runtime.BladeMask, PixelMaskTransform.Translation(target.WorldPosition.X, target.WorldPosition.Y), target.HurtMask, target.HurtMaskTransform, out contact);
        }

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

        private static int CompareProjection(Float2 direction, ICombatTarget left, ICombatTarget right)
        {
            var leftProjection = left.WorldPosition.X * direction.X + left.WorldPosition.Y * direction.Y;
            var rightProjection = right.WorldPosition.X * direction.X + right.WorldPosition.Y * direction.Y;
            var compared = leftProjection.CompareTo(rightProjection);
            return compared != 0 ? compared : left.RuntimeId.CompareTo(right.RuntimeId);
        }

        private static float DistanceSquared(Float2 left, Float2 right) { var x = left.X - right.X; var y = left.Y - right.Y; return x * x + y * y; }
        private bool TryPotentialContact(WeaponPotentialId potential, ICombatTarget target, Float2 contact) => target != null && target.HurtMask != null && WeaponPotentialVisuals.TryGet(potential, out _, out var mask) && PixelMaskContactService.TryFindContact(mask, PixelMaskTransform.Translation(contact.X, contact.Y), target.HurtMask, target.HurtMaskTransform, out _);
        private float LightningMultiplier(ICombatTarget target, Float2 contact, bool inbound)
        {
            if (!Potentials.HasPotential(WeaponPotentialId.FanDistantThunder) || !TryPotentialContact(WeaponPotentialId.FanDistantThunder, target, contact)) return 1f;
            var projection = ((target.WorldPosition.X - castOrigin.X) * lightningDirection.X + (target.WorldPosition.Y - castOrigin.Y) * lightningDirection.Y);
            var distance = Mathf.Clamp01(projection / Mathf.Max(.01f, Range)); return 1f + distance * .75f;
        }
        private void RefreshBleed(ICombatTarget target, Float2 contact)
        {
            for (var i = 0; i < bleeds.Count; i++) if (bleeds[i].TargetId == target.RuntimeId) { var refreshed = bleeds[i]; refreshed.Remaining = 4; refreshed.Elapsed = 0f; refreshed.Contact = contact; bleeds[i] = refreshed; return; }
            bleeds.Add(new Bleed { TargetId = target.RuntimeId, Contact = contact, Remaining = 4, Attack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.TimedTicks, .4f) });
        }
        private void AdvanceBleeds(float step, in WeaponExecutionContext context)
        {
            for (var i = bleeds.Count - 1; i >= 0; i--)
            {
                var bleed = bleeds[i]; bleed.Elapsed += step;
                while (bleed.Elapsed + .00001f >= .4f && bleed.Remaining > 0)
                {
                    bleed.Elapsed -= .4f; if (!runtime.Targets.TryGet(bleed.TargetId, out var target) || target == null || !target.IsAlive) { bleed.Remaining = 0; break; }
                    runtime.DamageService.TryApply(WeaponDamageRequest.Create(bleed.Attack, WeaponId.WindThunderFan, target, Mathf.CeilToInt(BaseDamage * .15f), false, bleed.Contact, ContactPhase.Bleed, context.SimulationTick), out _); bleed.Remaining--;
                }
                if (bleed.Remaining <= 0) { runtime.DamageService.RetireAttack(bleed.Attack.InstanceId); bleeds.RemoveAt(i); } else bleeds[i] = bleed;
            }
        }
        private void ScheduleChain(ICombatTarget killed)
        {
            ICombatTarget best = null; foreach (var candidate in marked) { if (candidate == null || !candidate.IsAlive || candidate.RuntimeId == killed.RuntimeId) continue; var d = DistanceSquared(candidate.WorldPosition, killed.WorldPosition); if (d > 9f) continue; if (best == null || d < DistanceSquared(best.WorldPosition, killed.WorldPosition) || (Mathf.Approximately(d, DistanceSquared(best.WorldPosition, killed.WorldPosition)) && candidate.RuntimeId < best.RuntimeId)) best = candidate; }
            if (best != null) pendingChain = new PendingChain { Scheduled = true, Remaining = .08f, TargetId = best.RuntimeId, Attack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f) };
        }
        private void AdvancePotentialChain(float step, in WeaponExecutionContext context)
        {
            if (!pendingChain.Scheduled) return; pendingChain.Remaining -= step; if (pendingChain.Remaining > .00001f) return;
            if (runtime.Targets.TryGet(pendingChain.TargetId, out var target) && target != null && target.IsAlive && target.HurtMask != null && WeaponPotentialVisuals.TryGet(WeaponPotentialId.FanReturningChain, out _, out var mask) && PixelMaskContactService.TryFindContact(mask, PixelMaskTransform.Translation(target.WorldPosition.X, target.WorldPosition.Y), target.HurtMask, target.HurtMaskTransform, out var contact)) runtime.DamageService.TryApply(WeaponDamageRequest.Create(pendingChain.Attack, WeaponId.WindThunderFan, target, Mathf.CeilToInt(BaseDamage * .5f), false, contact, ContactPhase.PotentialChain, context.SimulationTick), out _);
            runtime.DamageService.RetireAttack(pendingChain.Attack.InstanceId); pendingChain = default;
        }
        private struct Bleed { public int TargetId; public Float2 Contact; public int Remaining; public float Elapsed; public AttackInstance Attack; }
        private struct PendingChain { public bool Scheduled; public float Remaining; public int TargetId; public AttackInstance Attack; }
        private static readonly Float2[] CardinalDirections = { new Float2(1f, 0f), new Float2(0f, 1f), new Float2(-1f, 0f), new Float2(0f, -1f) };
    }
}
