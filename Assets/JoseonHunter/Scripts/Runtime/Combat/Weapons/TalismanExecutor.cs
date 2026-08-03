using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
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
        private int elementCastOrdinal;
        private readonly List<GhostFlame> ghostFlames = new List<GhostFlame>();
        private readonly List<LegacyGhostBurst> legacyGhostBursts = new List<LegacyGhostBurst>();
        private readonly List<IceSlow> iceSlows = new List<IceSlow>();
        private bool resolvingHeavenChain;
        private int legacyGhostChainsScheduled;
        private WeaponTransientVisualPool transientVisuals;
        private Transform transientVisualRoot;

        public TalismanExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float speed, int hopCount, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            var legacyDamage = modifiers.Legacy.Is(WeaponLegacyPathId.TalismanHeavenSeal) ? .75f : 1f;
            BaseDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage) * legacyDamage); CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds));
            Range = Mathf.Max(0.01f, modifiers.ScaleArea(range)); Speed = Mathf.Max(0.01f, modifiers.ScaleSpeed(speed)); Potentials = modifiers;
            HopCount = Mathf.Max(1, hopCount); Level = Mathf.Clamp(level, 1, 5);
            IsEvolved = evolved;
            runtime.DamageService.DamageConfirmed += OnDamageConfirmed;
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
        public int TransferCount { get; private set; }
        public int LastGhostSeekTargetRuntimeId { get; private set; }
        public IReadOnlyList<ContactPhase> LastContactPhases => lastContactPhases;
        private readonly List<ContactPhase> lastContactPhases = new List<ContactPhase>();
#if UNITY_INCLUDE_TESTS
        public TalismanState ActiveVisualStageForTests => active.Count == 0 ? TalismanState.Complete : active[0].State;
        public int FirstVisualPartIndexForTests => active.Count == 0 ? -1 : active[0].VisualPartIndex;
#endif

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            EnsureTransientVisuals(context.PresentationRoot);
            transientVisuals?.Tick(deltaTime);
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
                UpdateVisual(cast, context);
                LastState = cast.State;
                if (cast.State != TalismanState.Complete) continue;
                runtime.DamageService.RetireAttack(cast.Attack.InstanceId);
                if (cast.BlastAttack != null) runtime.DamageService.RetireAttack(cast.BlastAttack.InstanceId);
                DestroyVisual(cast);
                active.RemoveAt(index);
            }
            for (var index = iceSlows.Count - 1; index >= 0; index--)
            {
                var slow = iceSlows[index];
                if (slow.CreatedThisTick) { slow.CreatedThisTick = false; iceSlows[index] = slow; continue; }
                slow.Remaining -= Mathf.Max(0f, deltaTime);
                if (slow.Remaining > 0f) { iceSlows[index] = slow; continue; }
                if (slow.Target is IFrostStatusTarget frost) frost.RemoveFrostSlow(slow.SourceAttackId, 0f);
                iceSlows.RemoveAt(index);
            }
            for (var index = ghostFlames.Count - 1; index >= 0; index--)
            {
                var flame = ghostFlames[index]; flame.Remaining -= Mathf.Max(0f, deltaTime);
                if (flame.Remaining > 0f) { ghostFlames[index] = flame; continue; }
                if (TryFindNearestLegal(flame.Position, null, out var ghostTarget) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.TalismanVengefulGhostBurst, out _, out var ghostMask) &&
                    PixelMaskContactService.TryFindContact(ghostMask, PixelMaskTransform.Translation(ghostTarget.WorldPosition.X, ghostTarget.WorldPosition.Y), ghostTarget.HurtMask, ghostTarget.HurtMaskTransform, out var contact))
                {
                    LastGhostSeekTargetRuntimeId = ghostTarget.RuntimeId;
                    runtime.DamageService.TryApply(WeaponDamageRequest.Create(flame.Attack, WeaponId.TalismanThrow, ghostTarget, Mathf.CeilToInt(BaseDamage * .75f), false, contact, ContactPhase.PotentialBlast, context.SimulationTick,
                        true, WeaponHitTrait.Explosion, flame.Position), out _);
                }
                runtime.DamageService.RetireAttack(flame.Attack.InstanceId); ghostFlames.RemoveAt(index);
            }
            TickLegacyGhostBursts(step: Mathf.Max(0f, deltaTime), context);
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
                DestroyVisual(cast);
            }
            active.Clear(); bindingTargets.Clear();
            foreach (var slow in iceSlows) if (slow.Target is IFrostStatusTarget frost) frost.RemoveFrostSlow(slow.SourceAttackId, 0f);
            iceSlows.Clear();
            foreach (var flame in ghostFlames) runtime.DamageService.RetireAttack(flame.Attack.InstanceId);
            ghostFlames.Clear(); elementCastOrdinal = 0;
            foreach (var burst in legacyGhostBursts) runtime.DamageService.RetireAttack(burst.Attack.InstanceId);
            legacyGhostBursts.Clear();
            legacyGhostChainsScheduled = 0;
            if (bindingAttack != null) runtime.DamageService.RetireAttack(bindingAttack.InstanceId);
            bindingAttack = null; cooldown = 0f; LastState = TalismanState.Complete; LastFinalBurstCount = 0;
            LastLaunchCount = 0; TotalLaunchedTalismanCount = 0; lastContactPhases.Clear(); TransferCount = 0; LastGhostSeekTargetRuntimeId = 0;
            transientVisuals?.Dispose(); transientVisuals = null; transientVisualRoot = null;
        }

        public void Dispose()
        {
            runtime.DamageService.DamageConfirmed -= OnDamageConfirmed;
            Reset();
        }

        private void Launch(in WeaponExecutionContext context, ICombatTarget target)
        {
            // Hop count is each talisman's sequential chain length; the master form always starts up to three independent seals.
            var simultaneous = !IsEvolved && Level == 5 ? 3 : 1;
            LastFinalBurstCount = 0; lastContactPhases.Clear();
            if (active.Count == 0) legacyGhostChainsScheduled = 0;
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
                    context.OwnerPosition, target, HopCount, Potentials.HasPotential(WeaponPotentialId.TalismanFiveElementCycle) ? elementCastOrdinal++ % 3 : -1);
                CreateVisual(cast, context);
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
            if (!IsCurrentTargetValid(cast.Target))
            {
                if (cast.SealedConfirmed && cast.SealTransferConfirmed && !cast.HasTransferred && Potentials.HasPotential(WeaponPotentialId.TalismanSealTransfer) &&
                    TryFindNearestLegal(cast.Position, cast.AttemptedTargets, out var transfer) && DistanceSquared(transfer.WorldPosition, cast.Position) <= 16f)
                {
                    cast.Target = transfer; cast.AttemptedTargets.Add(transfer.RuntimeId); cast.ReservedTargets.Add(transfer.RuntimeId);
                    cast.SealedConfirmed = false; cast.HasTransferred = true; cast.SuppressTransferContact = true; TransferCount++;
                    BeginVisualFlight(cast); cast.State = TalismanState.Transferring; return;
                }
                if (cast.SealedConfirmed && Potentials.HasPotential(WeaponPotentialId.TalismanVengefulGhostBurst))
                    ghostFlames.Add(new GhostFlame(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), cast.Position));
                ResolveNoTarget(cast, context); return;
            }
            var delta = Subtract(cast.Target.WorldPosition, cast.Position);
            var distance = Length(delta);
            var step = Mathf.Min(distance, Speed * deltaTime);
            cast.Position = distance <= ArrivalDistance ? cast.Target.WorldPosition : new Float2(cast.Position.X + delta.X / distance * step, cast.Position.Y + delta.Y / distance * step);
            cast.FlightAge += deltaTime;
            if (Length(Subtract(cast.Target.WorldPosition, cast.Position)) > ArrivalDistance) return;
            if (!TryContact(cast.Target, out var contact))
            {
                ResolveFailedContact(cast);
                return;
            }

            if (!cast.SuppressTransferContact)
            {
                Apply(cast, cast.Target, contact, ContactPhase.Direct, context.SimulationTick);
                Apply(cast, cast.Target, contact, ContactPhase.Attach, context.SimulationTick);
                if (!cast.SealTransferEligibilityEvaluated && Potentials.HasPotential(WeaponPotentialId.TalismanSealTransfer) &&
                    WeaponPotentialVisuals.TryGet(WeaponPotentialId.TalismanSealTransfer, out _, out var transferMask) &&
                    PixelMaskContactService.TryFindContact(transferMask, PixelMaskTransform.Translation(contact.X, contact.Y), cast.Target.HurtMask, cast.Target.HurtMaskTransform, out _))
                    cast.SealTransferConfirmed = true;
                cast.SealTransferEligibilityEvaluated = true;
            }
            cast.State = TalismanState.Attached;
            cast.SuppressTransferContact = false;
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
                BeginVisualFlight(cast);
                cast.State = TalismanState.Transferring;
                return;
            }

            cast.State = TalismanState.Complete;
        }

        private void ResolveSeal(TalismanCast cast, in WeaponExecutionContext context)
        {
            if (!IsCurrentTargetValid(cast.Target))
            {
                if (cast.SealedConfirmed && Potentials.HasPotential(WeaponPotentialId.TalismanVengefulGhostBurst))
                    ghostFlames.Add(new GhostFlame(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), cast.Position));
                ResolveNoTarget(cast, context); return;
            }
            // A five-color binding may include only seals that were both pixel-confirmed and accepted by the damage service.
            if (!TryContact(cast.Target, out var contact) || !Apply(cast, cast.Target, contact, ContactPhase.Seal, context.SimulationTick))
            {
                ResolveFailedContact(cast);
                return;
            }
            cast.CompletedHops++;
            cast.SealedConfirmed = true;
            if (Potentials.Legacy.Is(WeaponLegacyPathId.TalismanHeavenSeal))
            {
                runtime.AffixStatuses.ApplyTimedStatus(cast.Target.RuntimeId, CombatStatusKind.Seal,
                    2f, 1, WeaponId.TalismanThrow);
                if (Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced)
                    runtime.AffixStatuses.ApplySealVulnerability(cast.Target.RuntimeId, 2f);
            }
            else if (Potentials.Legacy.Is(WeaponLegacyPathId.TalismanGhostBurst))
                legacyGhostBursts.Add(new LegacyGhostBurst(
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f),
                    cast.Target.WorldPosition));
            PlayConfirmedClosure(context, contact);
            ApplyElement(cast, contact, context);
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
            BeginVisualFlight(cast);
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
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(cast.BlastAttack, WeaponId.TalismanThrow, target, Mathf.CeilToInt(BaseDamage) * 2, false, contact, ContactPhase.Blast, context.SimulationTick,
                    true, WeaponHitTrait.Explosion, contact), out _))
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
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(bindingAttack, WeaponId.TalismanThrow, target, Mathf.CeilToInt(BaseDamage) * 2, false, contact, ContactPhase.Blast, context.SimulationTick,
                    true, WeaponHitTrait.Explosion, contact), out _))
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
            var traits = phase == ContactPhase.Blast ? WeaponHitTrait.Explosion : WeaponHitTrait.None;
            var applied = runtime.DamageService.TryApply(WeaponDamageRequest.Create(cast.Attack, WeaponId.TalismanThrow, target, Mathf.CeilToInt(BaseDamage) * multiplier, false, contact, phase, tick,
                true, traits, contact), out _);
            if (applied) lastContactPhases.Add(phase);
            return applied;
        }

        private void TickLegacyGhostBursts(float step, in WeaponExecutionContext context)
        {
            for (var index = legacyGhostBursts.Count - 1; index >= 0; index--)
            {
                var burst = legacyGhostBursts[index];
                burst.Remaining -= step;
                if (burst.Remaining > 0f)
                {
                    legacyGhostBursts[index] = burst;
                    continue;
                }

                ResolveAreaDamage(burst.Attack, burst.Position,
                    Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced ? Range * .455f : Range * .35f,
                    burst.Phase == 0 ? 2f : burst.Phase == 1 ? 1f : 1.2f,
                    burst.Phase == 0 ? ContactPhase.PotentialBlast : ContactPhase.PotentialChain,
                    burst.Phase == 0 ? int.MaxValue : 1, context.SimulationTick);
                runtime.DamageService.RetireAttack(burst.Attack.InstanceId);
                legacyGhostBursts.RemoveAt(index);

                if (burst.Phase == 0 && Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced)
                    legacyGhostBursts.Add(new LegacyGhostBurst(
                        new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f),
                        burst.Position, .12f, 1));
                if (burst.Phase == 0 && Potentials.Legacy.Stage == WeaponLegacyStage.Completed)
                {
                    var remainingChainBudget = Mathf.Max(0, 3 - legacyGhostChainsScheduled);
                    SelectNearestTargets(burst.Position, Range, remainingChainBudget, 0);
                    foreach (var target in targets)
                    {
                        legacyGhostBursts.Add(new LegacyGhostBurst(
                            new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f),
                            target.WorldPosition, .18f, 2));
                        legacyGhostChainsScheduled++;
                    }
                }
            }
        }

        private int ResolveAreaDamage(AttackInstance attack, Float2 center, float radius,
            float multiplier, ContactPhase phase, int cap, int simulationTick)
        {
            SelectNearestTargets(center, radius, cap, 0);
            var applied = 0;
            foreach (var target in targets)
            {
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(attack,
                    WeaponId.TalismanThrow, target, Mathf.CeilToInt(BaseDamage * multiplier), false,
                    target.WorldPosition, phase, simulationTick, true, WeaponHitTrait.Explosion, center), out _))
                    applied++;
            }
            return applied;
        }

        private void OnDamageConfirmed(ConfirmedDamageEvent damage)
        {
            if (resolvingHeavenChain || !Potentials.Legacy.Is(WeaponLegacyPathId.TalismanHeavenSeal) ||
                Potentials.Legacy.Stage != WeaponLegacyStage.Completed ||
                !runtime.Targets.TryGet(damage.TargetRuntimeId, out var killed) || killed == null || killed.IsAlive ||
                !runtime.AffixStatuses.HasStatus(damage.TargetRuntimeId, CombatStatusKind.Seal)) return;

            resolvingHeavenChain = true;
            try
            {
                SelectNearestTargets(damage.ContactPoint, Range, 4, damage.TargetRuntimeId,
                    requireSeal: true);
                var attack = new AttackInstance(runtime.AllocateAttackInstanceId(),
                    RepeatHitPolicy.OncePerPhase, 0f);
                foreach (var target in targets)
                    runtime.DamageService.TryApply(WeaponDamageRequest.Create(attack,
                        WeaponId.TalismanThrow, target, Mathf.CeilToInt(BaseDamage * 1.6f), false,
                        target.WorldPosition, ContactPhase.PotentialChain, damage.SimulationTick, true,
                        WeaponHitTrait.Explosion, damage.ContactPoint), out _);
                runtime.DamageService.RetireAttack(attack.InstanceId);
            }
            finally { resolvingHeavenChain = false; }
        }

        private void SelectNearestTargets(Float2 center, float radius, int cap, int excludedId,
            bool requireSeal = false)
        {
            targets.Clear();
            runtime.Targets.CopyTo(targets);
            for (var index = targets.Count - 1; index >= 0; index--)
            {
                var candidate = targets[index];
                if (candidate == null || !candidate.IsAlive || candidate.RuntimeId == excludedId ||
                    DistanceSquared(candidate.WorldPosition, center) > radius * radius ||
                    requireSeal && !runtime.AffixStatuses.HasStatus(candidate.RuntimeId, CombatStatusKind.Seal))
                    targets.RemoveAt(index);
            }
            targets.Sort((left, right) =>
            {
                var distance = DistanceSquared(left.WorldPosition, center)
                    .CompareTo(DistanceSquared(right.WorldPosition, center));
                return distance != 0 ? distance : left.RuntimeId.CompareTo(right.RuntimeId);
            });
            if (targets.Count > cap) targets.RemoveRange(cap, targets.Count - cap);
        }

        private void CreateVisual(TalismanCast cast, in WeaponExecutionContext context)
        {
            if (context.PresentationRoot == null) return;
            cast.Visual = new GameObject("Talisman Flight");
            cast.Visual.transform.SetParent(context.PresentationRoot, false);
            var renderer = cast.Visual.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = context.SortingOrder + 1;
            BeginVisualFlight(cast);
            UpdateVisual(cast, context);
        }

        private void UpdateVisual(TalismanCast cast, in WeaponExecutionContext context)
        {
            if (cast.Visual == null) return;
            var renderer = cast.Visual.GetComponent<SpriteRenderer>();
            var partIndex = WeaponVisualPartIndex.Talisman.Projectile;
            var position = cast.Position;
            if (cast.State == TalismanState.Flying || cast.State == TalismanState.Transferring)
            {
                var duration = Mathf.Max(.01f, Length(Subtract(cast.Target.WorldPosition, cast.VisualStart)) / Speed);
                var t = Mathf.Clamp01(cast.FlightAge / duration);
                position = QuadraticBezier(cast.VisualStart, cast.Target.WorldPosition, t);
                partIndex += Mathf.FloorToInt(cast.FlightAge / .05f) % WeaponVisualPartIndex.Talisman.ProjectileFrameCount;
            }
            else if (cast.State == TalismanState.Attached)
            {
                position = cast.Target?.WorldPosition ?? cast.Position;
                partIndex += WeaponVisualPartIndex.Talisman.ProjectileFrameCount - 1;
            }
            else if (cast.State == TalismanState.Sealing)
            {
                position = cast.Target?.WorldPosition ?? cast.Position;
                var progress = 1f - Mathf.Clamp01(cast.SealRemaining / SealDelay);
                partIndex = WeaponVisualPartIndex.Talisman.SealPulse +
                    Mathf.Min(WeaponVisualPartIndex.Talisman.FieldFrameCount - 1,
                        Mathf.FloorToInt(progress * WeaponVisualPartIndex.Talisman.FieldFrameCount));
            }

            cast.VisualPartIndex = partIndex;
            renderer.sprite = context.PresentationSpriteFor(WeaponId.TalismanThrow, partIndex);
            cast.Visual.transform.position = new Vector3(position.X, position.Y, 0f);
        }

        private void PlayConfirmedClosure(in WeaponExecutionContext context, Float2 contact)
        {
            var cue = new WeaponVisualCue(WeaponId.TalismanThrow, WeaponVisualStage.Impact, Level, IsEvolved, .9f, .16f);
            transientVisuals?.Play(
                context.PresentationSpriteFor(WeaponId.TalismanThrow, WeaponVisualPartIndex.Talisman.Binding),
                new Vector3(contact.X, contact.Y, 0f), Quaternion.identity, Vector3.one * cue.ResolvedScale,
                Color.white, cue.ResolvedLifetime, context.SortingOrder + 2);
        }

        private void EnsureTransientVisuals(Transform root)
        {
            if (!Application.isPlaying || root == null || root == transientVisualRoot) return;
            transientVisuals?.Dispose();
            transientVisualRoot = root;
            transientVisuals = new WeaponTransientVisualPool(root);
        }

        private static void DestroyVisual(TalismanCast cast)
        {
            if (cast?.Visual != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(cast.Visual);
                else UnityEngine.Object.DestroyImmediate(cast.Visual);
            }
            if (cast != null) cast.Visual = null;
        }

        private static void BeginVisualFlight(TalismanCast cast)
        {
            cast.VisualStart = cast.Position;
            cast.FlightAge = 0f;
        }

        private static Float2 QuadraticBezier(Float2 start, Float2 end, float t)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = Mathf.Sqrt(dx * dx + dy * dy);
            var bend = Mathf.Min(.35f, length * .2f);
            var control = length <= .0001f
                ? start
                : new Float2((start.X + end.X) * .5f - dy / length * bend, (start.Y + end.Y) * .5f + dx / length * bend);
            var oneMinus = 1f - t;
            return new Float2(
                oneMinus * oneMinus * start.X + 2f * oneMinus * t * control.X + t * t * end.X,
                oneMinus * oneMinus * start.Y + 2f * oneMinus * t * control.Y + t * t * end.Y);
        }

        private void ApplyElement(TalismanCast cast, Float2 contact, in WeaponExecutionContext context)
        {
            if (cast.Element < 0 || !IsCurrentTargetValid(cast.Target) || !WeaponPotentialVisuals.TryGet(WeaponPotentialId.TalismanFiveElementCycle, out _, out var elementMask) ||
                !PixelMaskContactService.TryFindContact(elementMask, PixelMaskTransform.Translation(contact.X, contact.Y), cast.Target.HurtMask, cast.Target.HurtMaskTransform, out _)) return;
            if (cast.Element == 0)
            {
                var burn = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.TimedTicks, .5f);
                runtime.AffixStatuses.ApplyOrRefreshPeriodic(new PeriodicEffectRequest(WeaponId.TalismanThrow, cast.Target.RuntimeId, contact,
                    Mathf.CeilToInt(BaseDamage * .15f), 3, burn, true, ContactPhase.Burn));
                return;
            }
            if (cast.Element == 1)
            {
                if (cast.Target is IFrostStatusTarget frost) frost.ApplyFrostSlow(cast.Attack.InstanceId, .5f);
                iceSlows.Add(new IceSlow(cast.Target, cast.Attack.InstanceId, 1.2f));
                return;
            }
            if (!TryFindNearestLegal(cast.Target.WorldPosition, new HashSet<int> { cast.Target.RuntimeId }, out var other) ||
                DistanceSquared(other.WorldPosition, cast.Target.WorldPosition) > 2.5f * 2.5f ||
                !WeaponPotentialVisuals.TryGet(WeaponPotentialId.TalismanFiveElementCycle, out _, out var chainMask) ||
                !PixelMaskContactService.TryFindContact(chainMask, PixelMaskTransform.Translation(other.WorldPosition.X, other.WorldPosition.Y), other.HurtMask, other.HurtMaskTransform, out var chainContact)) return;
            var attack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
            runtime.DamageService.TryApply(WeaponDamageRequest.Create(attack, WeaponId.TalismanThrow, other, Mathf.CeilToInt(BaseDamage * .60f), false, chainContact, ContactPhase.PotentialChain, context.SimulationTick), out _);
            runtime.DamageService.RetireAttack(attack.InstanceId);
        }

        private static float DistanceSquared(Float2 a, Float2 b) { var x = a.X - b.X; var y = a.Y - b.Y; return x * x + y * y; }

        private static bool IsCurrentTargetValid(ICombatTarget target) => target != null && target.IsAlive;
        private static bool IsTargetAvailable(ICombatTarget target, HashSet<int> reservations) =>
            IsCurrentTargetValid(target) && target.HurtMask != null &&
            (reservations == null || !reservations.Contains(target.RuntimeId));
        private static Float2 Subtract(Float2 left, Float2 right) => new Float2(left.X - right.X, left.Y - right.Y);
        private static float Length(Float2 value) => Mathf.Sqrt(value.X * value.X + value.Y * value.Y);

        private sealed class TalismanCast
        {
            public TalismanCast(AttackInstance attack, AttackInstance blastAttack, Float2 position, ICombatTarget target, int hopLimit, int element)
            { Attack = attack; BlastAttack = blastAttack; Position = position; Target = target; HopLimit = hopLimit; Element = element; }
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
            public int Element { get; }
            public bool SealedConfirmed { get; set; }
            public bool HasTransferred { get; set; }
            public bool SuppressTransferContact { get; set; }
            public bool SealTransferConfirmed { get; set; }
            public bool SealTransferEligibilityEvaluated { get; set; }
            public GameObject Visual { get; set; }
            public Float2 VisualStart { get; set; }
            public float FlightAge { get; set; }
            public int VisualPartIndex { get; set; } = WeaponVisualPartIndex.Talisman.Projectile;

            public void RecordLinkedTarget(ICombatTarget target)
            {
                if (target != null && LinkedTargetIds.Add(target.RuntimeId)) LinkedTargets.Add(target);
            }
        }

        private struct IceSlow { public IceSlow(ICombatTarget target, int sourceAttackId, float remaining) { Target = target; SourceAttackId = sourceAttackId; Remaining = remaining; CreatedThisTick = true; } public ICombatTarget Target; public int SourceAttackId; public float Remaining; public bool CreatedThisTick; }

        private struct GhostFlame
        {
            public GhostFlame(AttackInstance attack, Float2 position) { Attack = attack; Position = position; Remaining = .6f; }
            public AttackInstance Attack; public Float2 Position; public float Remaining;
        }

        private struct LegacyGhostBurst
        {
            public LegacyGhostBurst(AttackInstance attack, Float2 position, float remaining = .6f,
                int phase = 0)
            { Attack = attack; Position = position; Remaining = remaining; Phase = phase; }
            public AttackInstance Attack;
            public Float2 Position;
            public float Remaining;
            public int Phase;
        }
    }
}
