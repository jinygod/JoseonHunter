using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
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
        private readonly List<float> lightningPresentationTimes = new List<float>();
        private readonly List<PendingVisual> pendingVisuals = new List<PendingVisual>();
        private WeaponTransientVisualPool transientVisuals;
        private Transform transientVisualRoot;
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
        private readonly List<VacuumPulse> vacuumPulses = new List<VacuumPulse>();
        private readonly List<HeavenStrike> heavenStrikes = new List<HeavenStrike>();
        private PendingChain pendingChain;
        private Float2 castOrigin;
#if UNITY_INCLUDE_TESTS
        private readonly List<int> gustPresentationPartsForTests = new List<int>();
        private readonly List<int> windMarkPresentationPartsForTests = new List<int>();
        private readonly List<int> outboundLightningPresentationPartsForTests = new List<int>();
        private readonly List<int> inboundPresentationPartsForTests = new List<int>();
#endif

        public WindThunderFanExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float knockback, int markedTargetCap, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            LegacySourceDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage)); BaseDamage = LegacySourceDamage; CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds)); Range = Mathf.Max(0.01f, modifiers.ScaleArea(range)); Potentials = modifiers;
            Knockback = Mathf.Max(0f, knockback) * (modifiers.Legacy.Is(WeaponLegacyPathId.FanVacuum) ? 1.5f : modifiers.Legacy.Is(WeaponLegacyPathId.FanHeavenThunder) ? 0f : 1f); MarkedTargetCap = Mathf.Max(1, markedTargetCap); Level = Mathf.Clamp(level, 1, 5);
            IsEvolved = evolved;
            State = WindThunderFanState.Complete;
        }

        public float BaseDamage { get; }
        private float LegacySourceDamage { get; }
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
#if UNITY_INCLUDE_TESTS
        public float LegacyLightningDamageMultiplierForTests => Potentials.Legacy.Is(WeaponLegacyPathId.FanVacuum) ? .7f : 1f;
        public float LegacyPullMultiplierForTests => Potentials.Legacy.Is(WeaponLegacyPathId.FanVacuum) ? 1.5f : 0f;
        public int MaximumBleedStacksForTests { get; private set; }
        public int MaximumVacuumTargetsQueriedForTests { get; private set; }
        public int LastHeavenThunderBounceCountForTests { get; private set; }
        public IReadOnlyList<float> LightningPresentationTimesForTests => lightningPresentationTimes;
        public IReadOnlyList<int> GustPresentationPartsForTests => gustPresentationPartsForTests;
        public IReadOnlyList<int> WindMarkPresentationPartsForTests => windMarkPresentationPartsForTests;
        public IReadOnlyList<int> OutboundLightningPresentationPartsForTests => outboundLightningPresentationPartsForTests;
        public IReadOnlyList<int> InboundPresentationPartsForTests => inboundPresentationPartsForTests;
        public int ActiveBleedCountForTests => bleeds.Count;
        public bool PendingChainForTests => pendingChain.Scheduled;
        public bool SuppressNewCastsForTests { get; set; }
#endif

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            var step = Mathf.Max(0f, deltaTime);
            EnsureTransientVisuals(context.PresentationRoot);
            transientVisuals?.Tick(step);
            AdvancePendingVisuals(step, context);
            AdvanceBleeds(step, context);
            AdvancePotentialChain(step, context);
            AdvanceVacuumPulses(step, context);
            AdvanceHeavenStrikes(step, context);
            cooldown -= step;
            if (Potentials.Legacy.Is(WeaponLegacyPathId.FanHeavenThunder))
            {
                if (cooldown <= 0f && heavenStrikes.Count == 0 && HasLegalTarget()
#if UNITY_INCLUDE_TESTS
                    && !SuppressNewCastsForTests
#endif
                    ) StartHeavenThunder(context.OwnerPosition);
                return;
            }
            if (State == WindThunderFanState.Complete && cooldown <= 0f && HasLegalTarget()
#if UNITY_INCLUDE_TESTS
                && !SuppressNewCastsForTests
#endif
                ) StartCast(context.OwnerPosition);
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
            foreach (var pulse in vacuumPulses) runtime.DamageService.RetireAttack(pulse.Attack.InstanceId);
            foreach (var strike in heavenStrikes) runtime.DamageService.RetireAttack(strike.Attack.InstanceId);
            if (pendingChain.Attack != null) runtime.DamageService.RetireAttack(pendingChain.Attack.InstanceId);
            attack = null; marked.Clear(); successfulOutboundTargetIds.Clear(); successfulOutboundTargetIdSet.Clear(); outboundStrikeTimes.Clear(); lightningPresentationTimes.Clear(); pendingVisuals.Clear(); bleeds.Clear(); vacuumPulses.Clear(); heavenStrikes.Clear(); pendingChain = default; outboundElapsed = 0f; inboundPauseRemaining = 0f; strikeDueIn = LightningStrikeInterval; cooldown = 0f; State = WindThunderFanState.Complete;
            LastWindContactCount = 0; LastLightningContactCount = 0; LastInboundContactCount = 0; LastLightningSimulationTick = -1;
            transientVisuals?.Dispose(); transientVisuals = null; transientVisualRoot = null;
#if UNITY_INCLUDE_TESTS
            MaximumBleedStacksForTests = 0; MaximumVacuumTargetsQueriedForTests = 0; LastHeavenThunderBounceCountForTests = 0;
            gustPresentationPartsForTests.Clear(); windMarkPresentationPartsForTests.Clear();
            outboundLightningPresentationPartsForTests.Clear(); inboundPresentationPartsForTests.Clear();
#endif
        }

        public void Dispose() => Reset();

        private void StartCast(Float2 origin)
        {
            castOrigin = origin;
            cooldown = CooldownSeconds; marked.Clear(); successfulOutboundTargetIds.Clear(); successfulOutboundTargetIdSet.Clear(); outboundStrikeTimes.Clear(); lightningPresentationTimes.Clear();
#if UNITY_INCLUDE_TESTS
            gustPresentationPartsForTests.Clear(); windMarkPresentationPartsForTests.Clear();
            outboundLightningPresentationPartsForTests.Clear(); inboundPresentationPartsForTests.Clear();
#endif
            gustIndex = 0; lightningIndex = 0; strikeDueIn = LightningStrikeInterval; outboundElapsed = 0f; inboundPauseRemaining = 0f;
            LastWindContactCount = 0; LastLightningContactCount = 0; LastInboundContactCount = 0;
            attack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f);
            if (Potentials.Legacy.Is(WeaponLegacyPathId.FanVacuum) && Potentials.Legacy.Stage == WeaponLegacyStage.Completed)
                for (var index = 0; index < 3; index++) vacuumPulses.Add(new VacuumPulse(.15f + index * .2f,
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f)));
            State = WindThunderFanState.WindActive;
        }

        private void ResolveGust(in WeaponExecutionContext context)
        {
            var ownerPosition = context.OwnerPosition;
            var direction = Level == 5 ? CardinalDirections[gustIndex] : DangerousDirection(ownerPosition);
            if (gustIndex == 0) lightningDirection = direction;
            PlayGustLayers(context, direction);
            runtime.Targets.CopyTo(targets);
            targets.Sort((left, right) => CompareDanger(ownerPosition, left, right));
            foreach (var target in targets)
            {
                if (marked.Count >= MarkedTargetCap || target == null || !target.IsAlive || marked.Contains(target)) continue;
                if (!IsInsideCone(context.OwnerPosition, direction, target.WorldPosition) || !TryGustContact(target, out var contact)) continue;
                // Push is intentionally issued before the confirmed wind damage, so an echo cannot precede the visible gust response.
                var vacuum = Potentials.Legacy.Is(WeaponLegacyPathId.FanVacuum);
                target.ApplyKnockback(vacuum ? new Float2(-direction.X, -direction.Y) : direction, Knockback);
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(attack, WeaponId.WindThunderFan, target, Mathf.CeilToInt(BaseDamage), false, contact, ContactPhase.Wind, context.SimulationTick,
                    traits: vacuum ? WeaponHitTrait.Wind | WeaponHitTrait.Pull : WeaponHitTrait.Wind, attackOrigin: castOrigin), out _))
                {
                    marked.Add(target); LastWindContactCount++;
                    PlayContactSequence(context, contact, WeaponVisualPartIndex.WindThunderFan.Field, WeaponVisualPartIndex.WindThunderFan.FieldFrameCount, false, .62f, PresentationSequenceKind.WindMark);
                    if (Potentials.HasPotential(WeaponPotentialId.FanVacuumEdge) && TryPotentialContact(WeaponPotentialId.FanVacuumEdge, target, contact)) RefreshBleed(target, contact);
                    if (vacuum) AddVacuumBleed(target, contact, context);
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
                var vacuum = Potentials.Legacy.Is(WeaponLegacyPathId.FanVacuum);
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(attack, WeaponId.WindThunderFan, target, Mathf.CeilToInt(BaseDamage * (1f + Level * 0.1f) * multiplier * (vacuum ? .7f : 1f)), false, contact, ContactPhase.Lightning, context.SimulationTick,
                    traits: vacuum ? WeaponHitTrait.Explosion | WeaponHitTrait.Pull : WeaponHitTrait.Explosion, attackOrigin: castOrigin), out _))
                {
                    runtime.AffixStatuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Shock, 2f, 1, WeaponId.WindThunderFan);
                    LastLightningContactCount++;
                    lightningPresentationTimes.Add(0f);
                    PlayContactSequence(context, contact, WeaponVisualPartIndex.WindThunderFan.Impact, WeaponVisualPartIndex.WindThunderFan.ImpactFrameCount, false, .9f, PresentationSequenceKind.OutboundLightning);
                }
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
                    lightningPresentationTimes.Add(outboundElapsed);
                    PlayContactSequence(context, contact, WeaponVisualPartIndex.WindThunderFan.Impact, WeaponVisualPartIndex.WindThunderFan.ImpactFrameCount, false, .9f, PresentationSequenceKind.OutboundLightning);
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
                if (hit)
                {
                    LastInboundContactCount++;
                    PlayContactSequence(context, contact, WeaponVisualPartIndex.WindThunderFan.Impact, WeaponVisualPartIndex.WindThunderFan.ImpactFrameCount, true, .72f, PresentationSequenceKind.Inbound);
                    if (Potentials.HasPotential(WeaponPotentialId.FanReturningChain) && !target.IsAlive && TryPotentialContact(WeaponPotentialId.FanReturningChain, target, contact) && !pendingChain.Scheduled) ScheduleChain(target);
                }
            }
            runtime.DamageService.RetireAttack(attack.InstanceId);
            attack = null; marked.Clear(); State = WindThunderFanState.Complete;
        }

        private void PlayGustLayers(in WeaponExecutionContext context, Float2 direction)
        {
            var degrees = Mathf.Atan2(direction.Y, direction.X) * Mathf.Rad2Deg;
            for (var frame = 0; frame < WeaponVisualPartIndex.WindThunderFan.ProjectileFrameCount; frame++)
            {
                RecordPresentationPartForTests(
                    PresentationSequenceKind.Gust,
                    WeaponVisualPartIndex.WindThunderFan.Projectile + frame);
                var progress = (frame + 1f) / WeaponVisualPartIndex.WindThunderFan.ProjectileFrameCount;
                QueueVisual(
                    context,
                    new PendingVisual(
                        frame * .025f,
                        WeaponVisualPartIndex.WindThunderFan.Projectile + frame,
                        new Vector3(
                            castOrigin.X + direction.X * Range * progress,
                            castOrigin.Y + direction.Y * Range * progress,
                            0f),
                        Quaternion.Euler(0f, 0f, degrees),
                        new Vector3(Mathf.Lerp(.58f, 1.05f, progress), Mathf.Lerp(.42f, .72f, progress), 1f) *
                        WeaponPresentationScale.For(
                            WeaponId.WindThunderFan,
                            WeaponVisualStage.Projectile,
                            1f,
                            Level,
                            IsEvolved),
                        new Color(.72f, .94f, 1f, Mathf.Lerp(.48f, .18f, progress)),
                        .09f,
                        context.SortingOrder));
            }
        }

        private void PlayContactSequence(
            in WeaponExecutionContext context,
            Float2 contact,
            int partStart,
            int frameCount,
            bool reverse,
            float scale,
            PresentationSequenceKind kind)
        {
            for (var sequenceIndex = 0; sequenceIndex < frameCount; sequenceIndex++)
            {
                var frame = reverse ? frameCount - 1 - sequenceIndex : sequenceIndex;
                RecordPresentationPartForTests(kind, partStart + frame);
                QueueVisual(
                    context,
                    new PendingVisual(
                        sequenceIndex * .025f,
                        partStart + frame,
                        new Vector3(contact.X, contact.Y, 0f),
                        Quaternion.identity,
                        Vector3.one * WeaponPresentationScale.For(
                            WeaponId.WindThunderFan,
                            WeaponVisualStage.Impact,
                            scale,
                            Level,
                            IsEvolved),
                        reverse ? new Color(.64f, .82f, 1f, .78f) : Color.white,
                        .08f,
                        context.SortingOrder + 2));
            }
        }

        private void RecordPresentationPartForTests(PresentationSequenceKind kind, int partIndex)
        {
#if UNITY_INCLUDE_TESTS
            switch (kind)
            {
                case PresentationSequenceKind.Gust: gustPresentationPartsForTests.Add(partIndex); break;
                case PresentationSequenceKind.WindMark: windMarkPresentationPartsForTests.Add(partIndex); break;
                case PresentationSequenceKind.OutboundLightning: outboundLightningPresentationPartsForTests.Add(partIndex); break;
                case PresentationSequenceKind.Inbound: inboundPresentationPartsForTests.Add(partIndex); break;
            }
#endif
        }

        private void QueueVisual(in WeaponExecutionContext context, PendingVisual visual)
        {
            if (visual.DueIn <= 0f)
            {
                PlayVisual(context, visual);
                return;
            }
            pendingVisuals.Add(visual);
        }

        private void AdvancePendingVisuals(float step, in WeaponExecutionContext context)
        {
            for (var index = 0; index < pendingVisuals.Count;)
            {
                var visual = pendingVisuals[index];
                visual.DueIn -= step;
                if (visual.DueIn > 0f)
                {
                    pendingVisuals[index] = visual;
                    index++;
                    continue;
                }
                pendingVisuals.RemoveAt(index);
                PlayVisual(context, visual);
            }
        }

        private void PlayVisual(in WeaponExecutionContext context, PendingVisual visual)
        {
            transientVisuals?.Play(
                context.PresentationSpriteFor(WeaponId.WindThunderFan, visual.PartIndex),
                visual.Position,
                visual.Rotation,
                visual.Scale,
                visual.Color,
                visual.Lifetime,
                visual.SortingOrder);
        }

        private void EnsureTransientVisuals(Transform root)
        {
            if (root == null || root == transientVisualRoot) return;
            transientVisuals?.Dispose();
            transientVisualRoot = root;
            transientVisuals = new WeaponTransientVisualPool(root);
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
                if (Potentials.Legacy.Is(WeaponLegacyPathId.FanVacuum) && bleed.Stacks > 0)
                {
                    bleed.RemainingSeconds -= step;
                    while (bleed.Elapsed + .00001f >= .5f && bleed.RemainingSeconds > 0f)
                    {
                        bleed.Elapsed -= .5f;
                        if (!runtime.Targets.TryGet(bleed.TargetId, out var vacuumTarget) || vacuumTarget == null || !vacuumTarget.IsAlive)
                        { bleed.RemainingSeconds = 0f; break; }
                        runtime.DamageService.TryApply(WeaponDamageRequest.Create(bleed.Attack, WeaponId.WindThunderFan,
                            vacuumTarget, Mathf.CeilToInt(LegacySourceDamage * .15f * bleed.Stacks), false,
                            bleed.Contact, ContactPhase.Bleed, context.SimulationTick, traits: WeaponHitTrait.Wind), out _);
                    }
                    if (bleed.RemainingSeconds <= 0f)
                    { runtime.DamageService.RetireAttack(bleed.Attack.InstanceId); bleeds.RemoveAt(i); }
                    else bleeds[i] = bleed;
                    continue;
                }
                while (bleed.Elapsed + .00001f >= .4f && bleed.Remaining > 0)
                {
                    bleed.Elapsed -= .4f; if (!runtime.Targets.TryGet(bleed.TargetId, out var target) || target == null || !target.IsAlive) { bleed.Remaining = 0; break; }
                    runtime.DamageService.TryApply(WeaponDamageRequest.Create(bleed.Attack, WeaponId.WindThunderFan, target, Mathf.CeilToInt(BaseDamage * .15f), false, bleed.Contact, ContactPhase.Bleed, context.SimulationTick), out _); bleed.Remaining--;
                }
                if (bleed.Remaining <= 0) { runtime.DamageService.RetireAttack(bleed.Attack.InstanceId); bleeds.RemoveAt(i); } else bleeds[i] = bleed;
            }
        }

        private void AddVacuumBleed(ICombatTarget target, Float2 contact, in WeaponExecutionContext context)
        {
            var found = -1;
            for (var index = 0; index < bleeds.Count; index++) if (bleeds[index].TargetId == target.RuntimeId) { found = index; break; }
            Bleed bleed;
            if (found >= 0) bleed = bleeds[found];
            else bleed = new Bleed { TargetId = target.RuntimeId, Contact = contact, Attack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.TimedTicks, .5f) };
            bleed.Stacks = Mathf.Min(3, bleed.Stacks + 1); bleed.RemainingSeconds = 2f; bleed.Elapsed = 0f; bleed.Contact = contact;
#if UNITY_INCLUDE_TESTS
            MaximumBleedStacksForTests = Mathf.Max(MaximumBleedStacksForTests, bleed.Stacks);
#endif
            runtime.AffixStatuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Bleed, 2f, bleed.Stacks, WeaponId.WindThunderFan);
            if (found >= 0) bleeds[found] = bleed; else bleeds.Add(bleed);
            if (bleed.Stacks < 3 || Potentials.Legacy.Stage < WeaponLegacyStage.Reinforced) return;
            var rupture = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
            runtime.DamageService.TryApply(WeaponDamageRequest.Create(rupture, WeaponId.WindThunderFan, target,
                Mathf.CeilToInt(LegacySourceDamage), false, contact, ContactPhase.PotentialBlast, context.SimulationTick,
                traits: WeaponHitTrait.Wind | WeaponHitTrait.Pull, attackOrigin: castOrigin), out _);
            runtime.DamageService.RetireAttack(rupture.InstanceId);
        }

        private void AdvanceVacuumPulses(float step, in WeaponExecutionContext context)
        {
            for (var index = vacuumPulses.Count - 1; index >= 0; index--)
            {
                var pulse = vacuumPulses[index]; pulse.Remaining -= step;
                if (pulse.Remaining > .00001f) { vacuumPulses[index] = pulse; continue; }
                runtime.Targets.CopyTo(targets);
                targets.Sort((left, right) => CompareDanger(castOrigin, left, right));
                var queried = Mathf.Min(8, targets.Count);
#if UNITY_INCLUDE_TESTS
                MaximumVacuumTargetsQueriedForTests = Mathf.Max(MaximumVacuumTargetsQueriedForTests, queried);
#endif
                for (var targetIndex = 0; targetIndex < queried; targetIndex++)
                {
                    var target = targets[targetIndex];
                    if (target == null || !target.IsAlive || DistanceSquared(castOrigin, target.WorldPosition) > Range * Range) continue;
                    var dx = castOrigin.X - target.WorldPosition.X; var dy = castOrigin.Y - target.WorldPosition.Y;
                    var length = Mathf.Sqrt(dx * dx + dy * dy);
                    if (length > .0001f) target.ApplyKnockback(new Float2(dx / length, dy / length), Knockback * .35f);
                    AddVacuumBleed(target, target.WorldPosition, context);
                }
                runtime.DamageService.RetireAttack(pulse.Attack.InstanceId);
                vacuumPulses.RemoveAt(index);
            }
        }

        private void StartHeavenThunder(Float2 origin)
        {
            castOrigin = origin; cooldown = CooldownSeconds;
            runtime.Targets.CopyTo(targets);
            targets.RemoveAll(target => target == null || !target.IsAlive || DistanceSquared(origin, target.WorldPosition) > Range * Range);
            targets.Sort((left, right) => CompareDanger(origin, left, right));
            var cap = Potentials.Legacy.Stage == WeaponLegacyStage.Completed ? 7 : 4;
            var count = Mathf.Min(cap, targets.Count);
#if UNITY_INCLUDE_TESTS
            LastHeavenThunderBounceCountForTests = count;
#endif
            for (var index = 0; index < count; index++)
                heavenStrikes.Add(new HeavenStrike(targets[index].RuntimeId, .05f + index * LightningStrikeInterval,
                    LegacySourceDamage * .7f, ContactPhase.PotentialChain, WeaponHitTrait.Explosion,
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), false));
            if (Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced && count > 0)
                heavenStrikes.Add(new HeavenStrike(targets[0].RuntimeId, .08f + count * LightningStrikeInterval,
                    LegacySourceDamage * .8f, ContactPhase.Inbound, WeaponHitTrait.Explosion,
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), false));
            if (Potentials.Legacy.Stage == WeaponLegacyStage.Completed && count > 0)
                heavenStrikes.Add(new HeavenStrike(targets[0].RuntimeId, .16f + count * LightningStrikeInterval,
                    LegacySourceDamage * 2f, ContactPhase.Blast, WeaponHitTrait.Explosion,
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), true));
        }

        private void AdvanceHeavenStrikes(float step, in WeaponExecutionContext context)
        {
            for (var index = heavenStrikes.Count - 1; index >= 0; index--)
            {
                var strike = heavenStrikes[index]; strike.Remaining -= step;
                if (strike.Remaining > .00001f) { heavenStrikes[index] = strike; continue; }
                if (runtime.Targets.TryGet(strike.TargetId, out var center) && center != null && center.IsAlive)
                {
                    if (strike.IsBlast)
                    {
                        runtime.Targets.CopyTo(targets);
                        foreach (var target in targets)
                            if (target != null && target.IsAlive && DistanceSquared(center.WorldPosition, target.WorldPosition) <= 2.25f)
                                ApplyHeavenStrike(strike, target, center.WorldPosition, context);
                    }
                    else ApplyHeavenStrike(strike, center, center.WorldPosition, context);
                }
                runtime.DamageService.RetireAttack(strike.Attack.InstanceId);
                heavenStrikes.RemoveAt(index);
            }
        }

        private void ApplyHeavenStrike(HeavenStrike strike, ICombatTarget target, Float2 origin, in WeaponExecutionContext context)
        {
            if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(strike.Attack, WeaponId.WindThunderFan, target,
                Mathf.CeilToInt(strike.Damage), false, target.WorldPosition, strike.Phase, context.SimulationTick,
                traits: strike.Traits, attackOrigin: origin), out _))
                runtime.AffixStatuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Shock, 2f, 1, WeaponId.WindThunderFan);
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
        private struct Bleed { public int TargetId; public Float2 Contact; public int Remaining; public float RemainingSeconds; public int Stacks; public float Elapsed; public AttackInstance Attack; }
        private struct VacuumPulse { public VacuumPulse(float remaining, AttackInstance attack) { Remaining = remaining; Attack = attack; } public float Remaining; public AttackInstance Attack; }
        private struct HeavenStrike { public HeavenStrike(int targetId, float remaining, float damage, ContactPhase phase, WeaponHitTrait traits, AttackInstance attack, bool isBlast) { TargetId = targetId; Remaining = remaining; Damage = damage; Phase = phase; Traits = traits; Attack = attack; IsBlast = isBlast; } public int TargetId; public float Remaining; public float Damage; public ContactPhase Phase; public WeaponHitTrait Traits; public AttackInstance Attack; public bool IsBlast; }
        private struct PendingChain { public bool Scheduled; public float Remaining; public int TargetId; public AttackInstance Attack; }
        private enum PresentationSequenceKind { Gust, WindMark, OutboundLightning, Inbound }
        private struct PendingVisual
        {
            public PendingVisual(float dueIn, int partIndex, Vector3 position, Quaternion rotation, Vector3 scale, Color color, float lifetime, int sortingOrder)
            {
                DueIn = dueIn;
                PartIndex = partIndex;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                Color = color;
                Lifetime = lifetime;
                SortingOrder = sortingOrder;
            }

            public float DueIn;
            public int PartIndex;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public Color Color;
            public float Lifetime;
            public int SortingOrder;
        }
        private static readonly Float2[] CardinalDirections = { new Float2(1f, 0f), new Float2(0f, 1f), new Float2(-1f, 0f), new Float2(0f, -1f) };
    }
}
