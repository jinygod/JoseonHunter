using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using JoseonHunter.Runtime.Gameplay;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    /// <summary>Optional target capability for the short ward debuff owned by one finite post set.</summary>
    public interface IJangseungWardStatusTarget
    {
        void ApplyJangseungWard(int sourceId, float strength);
        void RemoveJangseungWard(int sourceId);
    }

    public interface IJangseungContactDamageTarget
    {
        void ApplyJangseungContactProtection(int sourceId, float reduction);
        void RemoveJangseungContactProtection(int sourceId);
    }

    /// <summary>Finite cardinal ward boundaries. Damage is only produced by a target movement segment crossing a visible segment.</summary>
    public sealed class JangseungWardExecutor : IWeaponExecutor, IWeaponEvolutionProfile
    {
        public const int MaximumWardSets = 4;
        public const float MobileRepositionInterval = 0.35f;
        public const float MaximumMobileStep = 0.75f;
        private const float BoundaryThickness = 0.12f;
        private const float EvolvedPostActivationInterval = 0.1f;
        private const float PostRiseDuration = 0.16f;
        private readonly WeaponRuntimeController runtime;
        private readonly PixelHitMask segmentMask;
        private readonly Dictionary<int, PixelHitMask> stretchedSegmentMasks = new Dictionary<int, PixelHitMask>();
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly List<WardSet> sets = new List<WardSet>();
        private readonly List<LegacyPulse> legacyPulses = new List<LegacyPulse>();
        private readonly List<LegacySlam> legacySlams = new List<LegacySlam>();
        private readonly Dictionary<int, Float2> previousPositions = new Dictionary<int, Float2>();
        private WeaponTransientVisualPool transientVisuals;
        private Transform transientVisualRoot;
        private JangseungWardPresenter wardPresenter;
        private Transform wardPresenterRoot;
        private JangseungGuardianDescentPresenter guardianDescentPresenter;
        private Transform guardianDescentRoot;
        private float cooldown;
        private float elapsedSeconds;
#if UNITY_INCLUDE_TESTS
        private readonly List<int> firstPostRiseFrameSequenceForTests = new List<int>();
        private readonly List<int> visibleBoundaryDirectionsForTests = new List<int>();
        private bool boundaryChecksResolvedThisTickForTests;
#endif

        public JangseungWardExecutor(WeaponRuntimeController runtime, PixelHitMask wardSegmentMask, float baseDamage, float cooldownSeconds, float radius, int postCount, int setCapacity, float reentryInterval, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            segmentMask = wardSegmentMask ?? throw new ArgumentNullException(nameof(wardSegmentMask));
            LegacySourceDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage));
            var legacyDamage = modifiers.Legacy.Is(WeaponLegacyPathId.JangseungFourGuardians) ? .7f : 1f;
            BaseDamage = LegacySourceDamage * legacyDamage; CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds)); Radius = Mathf.Max(0.05f, modifiers.ScaleArea(radius)); Potentials = modifiers;
            PostCount = modifiers.Legacy.Is(WeaponLegacyPathId.JangseungFourGuardians)
                ? 4 : Mathf.Clamp(postCount, 2, 4); SetCapacity = Mathf.Clamp(setCapacity, 1, MaximumWardSets);
            ReentryInterval = Mathf.Max(0f, reentryInterval); Level = Mathf.Clamp(level, 1, 5);
            IsEvolved = evolved;
        }

        public JangseungWardExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float radius, int postCount, int setCapacity, float reentryInterval, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
            : this(runtime, PixelHitMask.FromRows("111", "111", "111"), baseDamage, cooldownSeconds, radius, postCount, setCapacity, reentryInterval, level, evolved, modifiers) { }

        public float BaseDamage { get; }
        private float LegacySourceDamage { get; }
        public float CooldownSeconds { get; }
        public float Radius { get; }
        public int PostCount { get; }
        public int SetCapacity { get; }
        public float ReentryInterval { get; }
        public int Level { get; }
        public bool IsEvolved { get; }
        public WeaponRuntimeModifiers Potentials { get; }
        public int ActiveWardSetCount => sets.Count;
        public int ActivePostCount { get { var count = 0; foreach (var set in sets) count += set.Posts.Count; return count; } }
        public int EvictedWardSetCount { get; private set; }
        public int CompletedWardSetCount { get { var count = 0; foreach (var set in sets) if (set.IsCompleted) count++; return count; } }
        public bool HasExactlyOneFlashingBoundaryForCapture => wardPresenter != null && wardPresenter.HasExactlyOneFlashingSegmentForCapture;
#if UNITY_INCLUDE_TESTS
        public float FirstWardVisualRiseForTests => sets.Count > 0 ? sets[0].FirstPostRise : -1f;
        public IReadOnlyList<int> FirstPostRiseFrameSequenceForTests => firstPostRiseFrameSequenceForTests;
        public int FirstPostRiseFramesPlayedThisTickForTests { get; private set; }
        public IReadOnlyList<int> VisibleBoundaryDirectionsForTests => visibleBoundaryDirectionsForTests;
        public int GuardianStrikePresentationCountForTests { get; private set; }
        public bool GuardianStrikeAfterBoundaryChecksForTests { get; private set; } = true;
        public bool EvolvedCompletionAfterBoundaryChecksForTests { get; private set; } = true;
        public int ActiveBarrierCountForTests { get { var count = 0; foreach (var set in sets) if (set.RotationMask != null) count++; return count; } }
        public int ActiveGuardianCountForTests { get { var count = 0; foreach (var set in sets) if (set.GuardianMask != null) count++; return count; } }
        public int GhostFaceApplicationsForTests { get; private set; }
        public int GuardianSpawnsForTests { get; private set; }
        public JangseungWardPresenter WardPresenterForTests => wardPresenter;
        public float LegacyWardLifetimeForTests => Potentials.Legacy.Is(
            WeaponLegacyPathId.JangseungGuardianDescent) ? CooldownSeconds * .6f : 0f;
        public int CompletedPulseCountForTests { get; private set; }
#endif

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            var step = Mathf.Max(0f, deltaTime);
#if UNITY_INCLUDE_TESTS
            FirstPostRiseFramesPlayedThisTickForTests = 0;
            visibleBoundaryDirectionsForTests.Clear();
            boundaryChecksResolvedThisTickForTests = false;
#endif
            EnsureTransientVisuals(context.PresentationRoot);
            transientVisuals?.Tick(step);
            EnsureWardPresenter(context);
            wardPresenter?.Tick(step);
            EnsureGuardianDescentPresenter(context);
            guardianDescentPresenter?.Tick(step);
            var frameStartElapsed = elapsedSeconds;
            elapsedSeconds += step;
            cooldown -= step;
            if (Level == 5 && sets.Count > 0) MoveMobilePosts(step, context.OwnerPosition, context);
            if (cooldown <= 0f)
            {
                cooldown = CooldownSeconds;
                if (Level == 5 && sets.Count > 0) RequestMobileReposition(context.OwnerPosition);
                else PlaceSet(context.OwnerPosition, frameStartElapsed, context);
            }
            if (IsEvolved) AdvanceEvolvedPostActivation(step, context);
            AdvanceWardPresentation(context);
            ResolveCrossings(context, frameStartElapsed, step);
#if UNITY_INCLUDE_TESTS
            boundaryChecksResolvedThisTickForTests = true;
#endif
            PresentEvolvedCompletions(context);
            AdvancePotentialCompletions(step, context);
            AdvanceLegacyCompletions(step, context);
            RetireExpiredLegacySets();
            RememberCurrentTargetPositions();
        }

        public void Reset()
        {
            foreach (var set in sets) Retire(set);
            foreach (var pulse in legacyPulses) runtime.DamageService.RetireAttack(pulse.Attack.InstanceId);
            foreach (var slam in legacySlams) runtime.DamageService.RetireAttack(slam.Attack.InstanceId);
            legacyPulses.Clear(); legacySlams.Clear();
            sets.Clear(); previousPositions.Clear(); stretchedSegmentMasks.Clear(); cooldown = 0f; elapsedSeconds = 0f; EvictedWardSetCount = 0;
            transientVisuals?.Dispose(); transientVisuals = null; transientVisualRoot = null;
            wardPresenter?.Dispose(); wardPresenter = null; wardPresenterRoot = null;
            guardianDescentPresenter?.Dispose(); guardianDescentPresenter = null; guardianDescentRoot = null;
#if UNITY_INCLUDE_TESTS
            firstPostRiseFrameSequenceForTests.Clear(); visibleBoundaryDirectionsForTests.Clear();
            FirstPostRiseFramesPlayedThisTickForTests = 0;
            GhostFaceApplicationsForTests = 0; GuardianSpawnsForTests = 0; GuardianStrikePresentationCountForTests = 0;
            GuardianStrikeAfterBoundaryChecksForTests = true; EvolvedCompletionAfterBoundaryChecksForTests = true;
            boundaryChecksResolvedThisTickForTests = false;
            CompletedPulseCountForTests = 0;
#endif
        }

        public void Dispose() => Reset();

        private void PlaceSet(Float2 center, float createdAt, in WeaponExecutionContext context)
        {
            if (sets.Count >= SetCapacity)
            {
                Retire(sets[0]); sets.RemoveAt(0); EvictedWardSetCount++;
            }
            var count = Level == 5 ? 4 : PostCount;
            var set = new WardSet(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.BoundaryReentry, ReentryInterval), center, Radius, count, IsEvolved, createdAt, Level == 5);
            sets.Add(set);
            wardPresenter?.ShowSet(set.Attack.InstanceId, set.Posts, null);
            if (Potentials.Legacy.Is(WeaponLegacyPathId.JangseungFourGuardians) &&
                Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced)
                MarkEnclosedTargets(set);
        }

        private void AdvanceEvolvedPostActivation(float step, in WeaponExecutionContext context)
        {
            foreach (var set in sets)
            {
                if (!set.IsEvolved || set.IsCompleted) continue;
                set.ActivationElapsed += step;
                while (set.ActivationElapsed + 0.0001f >= EvolvedPostActivationInterval && !set.IsCompleted)
                {
                    set.ActivationElapsed -= EvolvedPostActivationInterval;
                    set.ActivateNextPost();
                    wardPresenter?.UpdateSet(set.Attack.InstanceId, set.Posts, null);
                }
                if (set.IsCompleted && !set.MarkResolved) { set.CompletionResidual = set.ActivationElapsed; MarkEnclosedTargets(set); }
            }
        }

        private void AdvancePotentialCompletions(float step, in WeaponExecutionContext context)
        {
            foreach (var set in sets)
            {
                if (!set.IsCompleted || set.PotentialCompletionStarted) continue;
                set.PotentialCompletionStarted = true;
                set.PotentialStartedThisTick = true;
                set.PotentialCreatedThisTick = true;
                set.PotentialTickStep = set.IsEvolved ? set.CompletionResidual : 0f;
                if (Potentials.HasPotential(WeaponPotentialId.JangseungFourDirectionBarrier) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.JangseungFourDirectionBarrier, out _, out var barrier))
                {
                    set.RotatingAttack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
                    set.RotationMask = barrier; set.RotationRemaining = .8f;
                }
                if (Potentials.HasPotential(WeaponPotentialId.JangseungGuardianDescent) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.JangseungGuardianDescent, out _, out var guardian))
                {
                    set.GuardianMask = guardian; set.GuardianRemaining = 1.2f; set.GuardianAttack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
                }
            }
            foreach (var set in sets)
            {
                var potentialStep = set.PotentialStartedThisTick ? set.PotentialTickStep : step;
                set.PotentialStartedThisTick = false;
                if (set.RotationRemaining <= 0f || set.RotationMask == null) continue;
                var residual = Mathf.Min(potentialStep, set.RotationRemaining);
                while (residual > .00001f)
                {
                    var slice = Mathf.Min(.02f, residual); residual -= slice; set.RotationElapsed += slice; set.RotationRemaining -= slice;
                    runtime.Targets.CopyTo(targets);
                    var degrees = Mathf.RoundToInt(set.RotationElapsed / .8f * 360f);
                    var transform = new PixelMaskTransform(set.DesiredCenter, degrees, false, new Vector2(set.Radius * 2f, BoundaryThickness * 8f));
                    foreach (var target in targets)
                    {
                        if (target == null || !target.IsAlive || target.HurtMask == null || set.RotatedTargetIds.Contains(target.RuntimeId)) continue;
                        if (!PixelMaskContactService.TryFindContact(set.RotationMask, transform, target.HurtMask, target.HurtMaskTransform, out var contact)) continue;
                        if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(set.RotatingAttack, WeaponId.JangseungWard, target, Mathf.CeilToInt(BaseDamage * .7f), false, contact, ContactPhase.PotentialBlast, context.SimulationTick, elapsedSeconds - set.RotationRemaining,
                            true, WeaponHitTrait.Barrier | WeaponHitTrait.Knockback, set.DesiredCenter), out _)) set.RotatedTargetIds.Add(target.RuntimeId);
                    }
                }
                if (set.RotationRemaining > 0f) continue;
                runtime.DamageService.RetireAttack(set.RotatingAttack.InstanceId); set.RotationMask = null;
            }
            foreach (var set in sets)
            {
                if (set.GuardianRemaining <= 0f || set.GuardianMask == null) continue;
                var potentialStep = set.PotentialCreatedThisTick ? set.PotentialTickStep : step;
                set.GuardianRemaining -= potentialStep; if (!set.GuardianResolved) ResolveGuardian(set, set.GuardianMask, context);
                if (set.GuardianRemaining > 0f) continue;
                runtime.DamageService.RetireAttack(set.GuardianAttack.InstanceId); set.GuardianMask = null;
            }
            foreach (var set in sets) { if (!set.PotentialStartedThisTick) set.PotentialTickStep = 0f; set.PotentialCreatedThisTick = false; }
        }

        private void ResolveGuardian(WardSet set, PixelHitMask guardianMask, in WeaponExecutionContext context)
        {
            ICombatTarget best = null;
            foreach (var id in set.MarkedTargetIds)
            {
                if (!runtime.Targets.TryGet(id, out var target) || target == null || !target.IsAlive || target.HurtMask == null) continue;
                if (best == null || target.ThreatScore > best.ThreatScore || (Mathf.Approximately(target.ThreatScore, best.ThreatScore) && target.RuntimeId < best.RuntimeId)) best = target;
            }
            if (best == null) return;
            if (PixelMaskContactService.TryFindContact(guardianMask, PixelMaskTransform.Translation(best.WorldPosition.X, best.WorldPosition.Y), best.HurtMask, best.HurtMaskTransform, out var contact))
            {
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(set.GuardianAttack, WeaponId.JangseungWard, best, Mathf.CeilToInt(BaseDamage * 1.1f), false, contact, ContactPhase.PotentialChain, context.SimulationTick, elapsedSeconds,
                    true, WeaponHitTrait.Heavy | WeaponHitTrait.Explosion, set.DesiredCenter), out _))
                {
                    set.GuardianResolved = true;
                    PlayGuardianStrike(set.Attack.InstanceId, context, contact);
                }
            }
            // Keep the authored guardian visible for the full lifetime after its one confirmed strike.
        }

        private void AdvanceLegacyCompletions(float step, in WeaponExecutionContext context)
        {
            foreach (var set in sets)
            {
                if (!set.IsCompleted || set.LegacyCompletionStarted) continue;
                set.LegacyCompletionStarted = true;
                if (Potentials.Legacy.Is(WeaponLegacyPathId.JangseungFourGuardians) &&
                    Potentials.Legacy.Stage == WeaponLegacyStage.Completed)
                {
                    for (var index = 0; index < 3; index++)
                        legacyPulses.Add(new LegacyPulse(set.Attack.InstanceId,
                            new AttackInstance(runtime.AllocateAttackInstanceId(),
                                RepeatHitPolicy.OncePerPhase, 0f), set.DesiredCenter,
                            .05f + index * .2f, index));
                }
                else if (Potentials.Legacy.Is(WeaponLegacyPathId.JangseungGuardianDescent))
                {
                    legacySlams.Add(new LegacySlam(set.Attack.InstanceId,
                        new AttackInstance(runtime.AllocateAttackInstanceId(),
                            RepeatHitPolicy.OncePerPhase, 0f), set.DesiredCenter, .12f, 1.8f));
                    if (Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced)
                        legacySlams.Add(new LegacySlam(set.Attack.InstanceId,
                            new AttackInstance(runtime.AllocateAttackInstanceId(),
                                RepeatHitPolicy.OncePerPhase, 0f), set.DesiredCenter, .30f,
                            Potentials.Legacy.Stage == WeaponLegacyStage.Completed ? 3.2f : 1.8f));
                }
            }

            for (var index = legacyPulses.Count - 1; index >= 0; index--)
            {
                var pulse = legacyPulses[index]; pulse.Remaining -= step;
                if (pulse.Remaining > 0f) { legacyPulses[index] = pulse; continue; }
                ResolveLegacyPulse(pulse, context);
                runtime.DamageService.RetireAttack(pulse.Attack.InstanceId);
                legacyPulses.RemoveAt(index);
#if UNITY_INCLUDE_TESTS
                CompletedPulseCountForTests++;
#endif
            }

            for (var index = legacySlams.Count - 1; index >= 0; index--)
            {
                var slam = legacySlams[index]; slam.Remaining -= step;
                if (slam.Remaining > 0f) { legacySlams[index] = slam; continue; }
                ResolveLegacySlam(slam, context);
                runtime.DamageService.RetireAttack(slam.Attack.InstanceId);
                legacySlams.RemoveAt(index);
            }
        }

        private void ResolveLegacyPulse(LegacyPulse pulse, in WeaponExecutionContext context)
        {
            runtime.Targets.CopyTo(targets);
            var radius = Radius * (1f + pulse.Ordinal * .25f);
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive ||
                    DistanceSquared(target.WorldPosition, pulse.Center) > radius * radius) continue;
                if (!runtime.DamageService.TryApply(WeaponDamageRequest.Create(pulse.Attack,
                    WeaponId.JangseungWard, target, Mathf.CeilToInt(LegacySourceDamage * .8f),
                    false, target.WorldPosition, ContactPhase.PotentialBlast, context.SimulationTick,
                    true, WeaponHitTrait.Barrier | WeaponHitTrait.Knockback, pulse.Center), out _))
                    continue;
                target.ApplyKnockback(CenterOutward(pulse.Center, target.WorldPosition),
                    .20f + pulse.Ordinal * .08f);
            }
        }

        private void ResolveLegacySlam(LegacySlam slam, in WeaponExecutionContext context)
        {
            runtime.Targets.CopyTo(targets);
            ICombatTarget best = null;
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive ||
                    DistanceSquared(target.WorldPosition, slam.Center) > Radius * Radius) continue;
                if (best == null || target.ThreatScore > best.ThreatScore ||
                    Mathf.Approximately(target.ThreatScore, best.ThreatScore) &&
                    target.RuntimeId < best.RuntimeId) best = target;
            }
            if (best == null) return;
            if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(slam.Attack,
                WeaponId.JangseungWard, best, Mathf.CeilToInt(LegacySourceDamage * slam.Multiplier),
                false, best.WorldPosition, ContactPhase.PotentialChain, context.SimulationTick,
                true, WeaponHitTrait.Heavy | WeaponHitTrait.Explosion, slam.Center), out _))
                PlayGuardianStrike(slam.OwnerId, context, best.WorldPosition);
        }

        private void RetireExpiredLegacySets()
        {
            if (!Potentials.Legacy.Is(WeaponLegacyPathId.JangseungGuardianDescent)) return;
            var lifetime = CooldownSeconds * .6f;
            for (var index = sets.Count - 1; index >= 0; index--)
            {
                if (elapsedSeconds - sets[index].CreatedAt < lifetime) continue;
                Retire(sets[index]); sets.RemoveAt(index);
            }
        }

        private void MarkEnclosedTargets(WardSet set)
        {
            runtime.Targets.CopyTo(targets);
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || !IsInsideCompletedWard(target.WorldPosition, set)) continue;
                set.MarkedTargetIds.Add(target.RuntimeId);
                if (target is IJangseungWardStatusTarget status)
                {
                    set.StatusTargetIds.Add(target.RuntimeId);
                    status.ApplyJangseungWard(set.Attack.InstanceId, Level * 0.1f);
                }
                if (Potentials.Legacy.Is(WeaponLegacyPathId.JangseungFourGuardians) &&
                    Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced &&
                    target is IJangseungContactDamageTarget protectedTarget)
                    protectedTarget.ApplyJangseungContactProtection(set.Attack.InstanceId, .2f);
            }
            set.MarkResolved = true;
        }

        private void RequestMobileReposition(Float2 center)
        {
            var set = sets[sets.Count - 1];
            set.DesiredCenter = center;
            set.HasRequestedMove = true;
        }

        private void MoveMobilePosts(float step, Float2 ownerPosition, in WeaponExecutionContext context)
        {
            foreach (var set in sets)
            {
                set.MobileElapsed += step;
                if (set.MobileElapsed + 0.0001f < MobileRepositionInterval) continue;
                set.MobileElapsed = 0f;
                var center = set.HasRequestedMove ? set.DesiredCenter : ownerPosition;
                set.HasRequestedMove = false;
                for (var index = 0; index < set.Posts.Count; index++)
                {
                    var desired = CardinalPost(center, Radius, index);
                    set.Posts[index] = MoveTowards(set.Posts[index], desired, MaximumMobileStep);
                }
                wardPresenter?.UpdateSet(set.Attack.InstanceId, set.Posts, null);
            }
        }

        private void ResolveCrossings(in WeaponExecutionContext context, float frameStartElapsed, float step)
        {
            runtime.Targets.CopyTo(targets);
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                var current = target.WorldPosition;
                if (!previousPositions.TryGetValue(target.RuntimeId, out var previous)) previous = current;
                foreach (var set in sets) ResolveTargetAgainstSet(target, previous, current, set, context, frameStartElapsed, step);
            }
        }

        private void ResolveTargetAgainstSet(ICombatTarget target, Float2 previous, Float2 current, WardSet set, in WeaponExecutionContext context, float frameStartElapsed, float step)
        {
            if (set.IsEvolved && !set.IsCompleted) return;
            if (set.IsEvolved && !set.MarkedTargetIds.Contains(target.RuntimeId)) return;
            var segments = new List<Segment>(Segments(set));
            for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                var segment = segments[segmentIndex];
                if (!TrySegmentIntersection(previous, current, segment.Start, segment.End, out var movementT)) continue;
                if (set.TouchingTargetIds.Contains(target.RuntimeId)) continue;
                var atCrossing = Lerp(previous, current, movementT);
                if (!TryConfirmPixelContact(segment, target, atCrossing, out var contact)) continue;
                var crossingTime = frameStartElapsed + step * movementT;
                // Mark the contact before damage so a rejected re-entry interval cannot turn into repeated line damage.
                set.TouchingTargetIds.Add(target.RuntimeId);
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(set.Attack, WeaponId.JangseungWard, target,
                    Mathf.CeilToInt(BaseDamage), false, contact, ContactPhase.BoundaryCrossing, context.SimulationTick, crossingTime,
                    true, WeaponHitTrait.Barrier | WeaponHitTrait.Knockback, set.DesiredCenter), out _))
                {
                    wardPresenter?.PlayCrossing(set.Attack.InstanceId, segmentIndex,
                        new Vector2(segment.Start.X, segment.Start.Y), new Vector2(segment.End.X, segment.End.Y),
                        new Vector2(contact.X, contact.Y));
                    set.MarkedTargetIds.Add(target.RuntimeId);
                    target.ApplyKnockback(OutwardDirection(segment, previous, current), Mathf.Max(0.1f, Level * 0.2f));
                    if (Potentials.HasPotential(WeaponPotentialId.JangseungGhostFace) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.JangseungGhostFace, out _, out var ghostMask) &&
                        PixelMaskContactService.TryFindContact(ghostMask, PixelMaskTransform.Translation(contact.X, contact.Y), target.HurtMask, target.HurtMaskTransform, out _))
                    {
                        target.ApplyKnockback(CenterOutward(set.DesiredCenter, atCrossing), 1.25f);
#if UNITY_INCLUDE_TESTS
                        GhostFaceApplicationsForTests++;
#endif
                    }
                    if (!set.IsEvolved && target is IJangseungWardStatusTarget status)
                    {
                        set.StatusTargetIds.Add(target.RuntimeId);
                        status.ApplyJangseungWard(set.Attack.InstanceId, Level * 0.1f);
                    }
                    if (Potentials.Legacy.Is(WeaponLegacyPathId.JangseungFourGuardians) &&
                        Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced &&
                        target is IJangseungContactDamageTarget protectedTarget)
                        protectedTarget.ApplyJangseungContactProtection(set.Attack.InstanceId, .2f);
                }
            }
            if (!IsTouchingBoundary(target, current, set)) set.TouchingTargetIds.Remove(target.RuntimeId);
        }

        private bool IsTouchingBoundary(ICombatTarget target, Float2 position, WardSet set)
        {
            foreach (var segment in Segments(set))
            {
                if (DistanceToSegmentSquared(position, segment) > BoundaryThickness * BoundaryThickness) continue;
                if (TryConfirmPixelContact(segment, target, position, out _)) return true;
            }
            return false;
        }

        private bool TryConfirmPixelContact(Segment segment, ICombatTarget target, Float2 targetAtCrossing, out Float2 contact)
        {
            var dx = segment.End.X - segment.Start.X; var dy = segment.End.Y - segment.Start.Y;
            var length = Mathf.Sqrt(dx * dx + dy * dy);
            var degrees = Mathf.RoundToInt(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
            var mask = StretchedMask(length);
            var midpoint = new Float2((segment.Start.X + segment.End.X) * 0.5f, (segment.Start.Y + segment.End.Y) * 0.5f);
            var intervalCount = Mathf.Max(1, Mathf.CeilToInt(length * segmentMask.PixelsPerUnit));
            var scaleCorrection = length * segmentMask.PixelsPerUnit / intervalCount;
            var transform = new PixelMaskTransform(midpoint, degrees, false, new Vector2(scaleCorrection, 1f));
            var hurt = target.HurtMaskTransform;
            var offset = new Float2(hurt.Position.X - target.WorldPosition.X, hurt.Position.Y - target.WorldPosition.Y);
            var targetTransform = new PixelMaskTransform(new Float2(targetAtCrossing.X + offset.X, targetAtCrossing.Y + offset.Y), hurt.RotationDegrees, hurt.FlipX, hurt.Scale);
            return PixelMaskContactService.TryFindContact(mask, transform, target.HurtMask, targetTransform, out contact);
        }

        private PixelHitMask StretchedMask(float length)
        {
            var pixelsPerUnit = segmentMask.PixelsPerUnit;
            var intervalCount = Mathf.Max(1, Mathf.CeilToInt(length * pixelsPerUnit));
            var width = intervalCount + 1;
            var height = Mathf.Max(1, Mathf.CeilToInt(BoundaryThickness * pixelsPerUnit));
            var key = width * 4099 + height;
            if (stretchedSegmentMasks.TryGetValue(key, out var cached)) return cached;
            var packed = new uint[(width * height + 31) / 32];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var sourceX = Mathf.RoundToInt(x * (segmentMask.Width - 1f) / intervalCount);
                var sourceY = height == 1 ? 0 : Mathf.RoundToInt(y * (segmentMask.Height - 1f) / (height - 1f));
                if (!segmentMask.IsActive(sourceX, sourceY)) continue;
                var bit = y * width + x; packed[bit >> 5] |= 1u << (bit & 31);
            }
            cached = new PixelHitMask(width, height, new Vector2(intervalCount * 0.5f, Mathf.Floor(height * 0.5f)), pixelsPerUnit, packed);
            stretchedSegmentMasks.Add(key, cached);
            return cached;
        }

        private IEnumerable<Segment> Segments(WardSet set)
        {
            for (var index = 0; index < set.Posts.Count; index++)
            {
                if (set.Posts.Count == 2 && index == 1) yield break;
                yield return new Segment(set.Posts[index], set.Posts[(index + 1) % set.Posts.Count]);
            }
        }

        private void AdvanceWardPresentation(in WeaponExecutionContext context)
        {
            foreach (var set in sets)
            {
                for (var postIndex = 0; postIndex < set.Posts.Count; postIndex++)
                {
                    var localElapsed = PostVisualElapsed(set, postIndex);
                    var rise = localElapsed < 0f ? 0f : Mathf.Clamp01(localElapsed / PostRiseDuration);
                    wardPresenter?.SetPostRise(set.Attack.InstanceId, postIndex, rise);
                    if (localElapsed < 0f) continue;
                    if (postIndex == 0) set.FirstPostRise = rise;
                    var currentFrame = Mathf.Min(
                        WeaponVisualPartIndex.Jangseung.WindupFrameCount - 1,
                        Mathf.FloorToInt(rise * WeaponVisualPartIndex.Jangseung.WindupFrameCount));
                    var previousFrame = set.LastRiseFramePlayed[postIndex];
                    if (previousFrame != currentFrame)
                    {
#if UNITY_INCLUDE_TESTS
                        if (postIndex == 0)
                        {
                            firstPostRiseFrameSequenceForTests.Add(currentFrame);
                            FirstPostRiseFramesPlayedThisTickForTests++;
                        }
#endif
                    }
                    set.LastRiseFramePlayed[postIndex] = currentFrame;
                }

                var boundaryStart = 2f / WeaponVisualPartIndex.Jangseung.WindupFrameCount;
                if (set.Posts.Count < 2) continue;
                var segmentCount = set.Posts.Count == 2 ? 1 : set.Posts.Count;
                for (var directionIndex = 0; directionIndex < segmentCount; directionIndex++)
                {
                    var localElapsed = PostVisualElapsed(set, directionIndex);
                    var boundaryRise = localElapsed < 0f ? 0f : Mathf.Clamp01(localElapsed / PostRiseDuration);
                    var alpha = boundaryRise <= boundaryStart ? 0f : Mathf.InverseLerp(boundaryStart, 1f, boundaryRise);
                    wardPresenter?.SetBoundaryAlpha(set.Attack.InstanceId, directionIndex, alpha);
#if UNITY_INCLUDE_TESTS
                    if (alpha > 0f) visibleBoundaryDirectionsForTests.Add(directionIndex);
#endif
                }
            }
        }

        private void PresentEvolvedCompletions(in WeaponExecutionContext context)
        {
            foreach (var set in sets)
            {
                if (!set.IsEvolved || !set.IsCompleted || set.EvolvedCompletionPresented) continue;
#if UNITY_INCLUDE_TESTS
                EvolvedCompletionAfterBoundaryChecksForTests &= boundaryChecksResolvedThisTickForTests;
#endif
                var guardianBurst = context.PresentationSpriteFor(
                    WeaponId.JangseungWard,
                    WeaponVisualPartIndex.Jangseung.Impact +
                    WeaponVisualPartIndex.Jangseung.ImpactFrameCount / 2);
                transientVisuals?.Play(guardianBurst,
                    new Vector3(set.DesiredCenter.X, set.DesiredCenter.Y, 0f),
                    Quaternion.identity, ScaleSpriteToWorldSize(guardianBurst, 1.25f, 1.25f),
                    new Color(.86f, .58f, .18f, .86f), .22f, context.SortingOrder + 3);
                set.EvolvedCompletionPresented = true;
            }
        }

        private static Vector3 ScaleSpriteToWorldSize(Sprite sprite, float worldWidth, float worldHeight)
        {
            if (sprite == null) return Vector3.one;
            return new Vector3(
                worldWidth / Mathf.Max(.01f, sprite.bounds.size.x),
                worldHeight / Mathf.Max(.01f, sprite.bounds.size.y),
                1f);
        }

        private float PostVisualElapsed(WardSet set, int postIndex)
        {
            var delay = set.StaggerPostVisuals ? postIndex * EvolvedPostActivationInterval : 0f;
            return elapsedSeconds - set.CreatedAt - delay;
        }

        private void PlayGuardianStrike(int ownerId, in WeaponExecutionContext context, Float2 contact)
        {
#if UNITY_INCLUDE_TESTS
            GuardianStrikePresentationCountForTests++;
            GuardianStrikeAfterBoundaryChecksForTests &= boundaryChecksResolvedThisTickForTests;
            GuardianSpawnsForTests++;
#endif
            guardianDescentPresenter?.Play(ownerId,
                context.JangseungGeumjulVisualLibrary?.GuardianDescentSprite,
                new Vector2(contact.X, contact.Y),
                context.SortingOrder + 3);
        }

        private void EnsureTransientVisuals(Transform root)
        {
            if (root == null || root == transientVisualRoot) return;
            transientVisuals?.Dispose();
            transientVisualRoot = root;
            transientVisuals = new WeaponTransientVisualPool(root);
        }

        private void EnsureWardPresenter(in WeaponExecutionContext context)
        {
            if (context.PresentationRoot == wardPresenterRoot && wardPresenter != null) return;
            wardPresenter?.Dispose(); wardPresenter = null; wardPresenterRoot = null;
            if (context.PresentationRoot == null || context.JangseungGeumjulVisualLibrary == null) return;
            wardPresenterRoot = context.PresentationRoot;
            wardPresenter = new JangseungWardPresenter(context.JangseungGeumjulVisualLibrary, wardPresenterRoot, context.SortingOrder);
            foreach (var set in sets) wardPresenter.ShowSet(set.Attack.InstanceId, set.Posts, null);
        }

        private void EnsureGuardianDescentPresenter(in WeaponExecutionContext context)
        {
            if (context.PresentationRoot == guardianDescentRoot && guardianDescentPresenter != null) return;
            guardianDescentPresenter?.Dispose();
            guardianDescentPresenter = null;
            guardianDescentRoot = null;
            if (context.PresentationRoot == null ||
                context.JangseungGeumjulVisualLibrary?.GuardianDescentSprite == null) return;
            guardianDescentRoot = context.PresentationRoot;
            guardianDescentPresenter = new JangseungGuardianDescentPresenter(guardianDescentRoot);
        }

        private void RememberCurrentTargetPositions()
        {
            previousPositions.Clear();
            foreach (var target in targets) if (target != null && target.IsAlive) previousPositions[target.RuntimeId] = target.WorldPosition;
        }

        private void Retire(WardSet set)
        {
            if (set.Retired) return;
            for (var index = legacyPulses.Count - 1; index >= 0; index--)
                if (legacyPulses[index].OwnerId == set.Attack.InstanceId)
                {
                    runtime.DamageService.RetireAttack(legacyPulses[index].Attack.InstanceId);
                    legacyPulses.RemoveAt(index);
                }
            for (var index = legacySlams.Count - 1; index >= 0; index--)
                if (legacySlams[index].OwnerId == set.Attack.InstanceId)
                {
                    runtime.DamageService.RetireAttack(legacySlams[index].Attack.InstanceId);
                    legacySlams.RemoveAt(index);
                }
            foreach (var targetId in set.StatusTargetIds)
                if (runtime.Targets.TryGet(targetId, out var target))
                {
                    if (target is IJangseungWardStatusTarget status)
                        status.RemoveJangseungWard(set.Attack.InstanceId);
                    if (target is IJangseungContactDamageTarget protectedTarget)
                        protectedTarget.RemoveJangseungContactProtection(set.Attack.InstanceId);
                }
            runtime.DamageService.RetireAttack(set.Attack.InstanceId);
            if (set.RotatingAttack != null) runtime.DamageService.RetireAttack(set.RotatingAttack.InstanceId);
            if (set.GuardianAttack != null) runtime.DamageService.RetireAttack(set.GuardianAttack.InstanceId);
            guardianDescentPresenter?.Cancel(set.Attack.InstanceId);
            if (set.EvolvedCompletionVisual != null) UnityEngine.Object.Destroy(set.EvolvedCompletionVisual);
            wardPresenter?.RetireSet(set.Attack.InstanceId);
            set.Retired = true;
        }

        private static Float2 CardinalPost(Float2 center, float radius, int index)
        {
            switch (index)
            {
                case 0: return new Float2(center.X + radius, center.Y);
                case 1: return new Float2(center.X, center.Y + radius);
                case 2: return new Float2(center.X - radius, center.Y);
                default: return new Float2(center.X, center.Y - radius);
            }
        }

        private static Float2 MoveTowards(Float2 current, Float2 target, float maximum)
        {
            var x = target.X - current.X; var y = target.Y - current.Y; var length = Mathf.Sqrt(x * x + y * y);
            return length <= maximum || length < 0.0001f ? target : new Float2(current.X + x * maximum / length, current.Y + y * maximum / length);
        }
        private static Float2 Lerp(Float2 left, Float2 right, float t) => new Float2(Mathf.Lerp(left.X, right.X, t), Mathf.Lerp(left.Y, right.Y, t));
        private static float DistanceSquared(Float2 left, Float2 right)
        {
            var x = left.X - right.X; var y = left.Y - right.Y;
            return x * x + y * y;
        }
        private static Float2 OutwardDirection(Segment segment, Float2 previous, Float2 current)
        {
            var x = segment.End.X - segment.Start.X; var y = segment.End.Y - segment.Start.Y;
            var normal = new Float2(-y, x); var movement = new Float2(current.X - previous.X, current.Y - previous.Y);
            return normal.X * movement.X + normal.Y * movement.Y >= 0f ? normal : new Float2(-normal.X, -normal.Y);
        }
        private static Float2 CenterOutward(Float2 center, Float2 point)
        { var x = point.X - center.X; var y = point.Y - center.Y; var length = Mathf.Sqrt(x * x + y * y); return length < .0001f ? new Float2(1f, 0f) : new Float2(x / length, y / length); }
        private static bool TrySegmentIntersection(Float2 a, Float2 b, Float2 c, Float2 d, out float movementT)
        {
            var rX = b.X - a.X; var rY = b.Y - a.Y; var sX = d.X - c.X; var sY = d.Y - c.Y;
            var denominator = rX * sY - rY * sX;
            if (Mathf.Abs(denominator) < 0.0001f) { movementT = 0f; return false; }
            var qX = c.X - a.X; var qY = c.Y - a.Y;
            var t = (qX * sY - qY * sX) / denominator; var u = (qX * rY - qY * rX) / denominator;
            movementT = t;
            return t >= 0f && t <= 1f && u >= 0f && u <= 1f;
        }
        private static float DistanceToSegmentSquared(Float2 point, Segment segment)
        {
            var x = segment.End.X - segment.Start.X; var y = segment.End.Y - segment.Start.Y;
            var lengthSquared = x * x + y * y;
            if (lengthSquared < 0.0001f) return (point.X - segment.Start.X) * (point.X - segment.Start.X) + (point.Y - segment.Start.Y) * (point.Y - segment.Start.Y);
            var t = Mathf.Clamp01(((point.X - segment.Start.X) * x + (point.Y - segment.Start.Y) * y) / lengthSquared);
            var dx = point.X - (segment.Start.X + x * t); var dy = point.Y - (segment.Start.Y + y * t);
            return dx * dx + dy * dy;
        }

        private static bool IsInsideCompletedWard(Float2 point, WardSet set)
        {
            var inside = false;
            for (int current = 0, previous = set.Posts.Count - 1; current < set.Posts.Count; previous = current++)
            {
                var a = set.Posts[current]; var b = set.Posts[previous];
                if ((a.Y > point.Y) == (b.Y > point.Y)) continue;
                if (point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
            }
            return inside;
        }

        private readonly struct Segment { public Segment(Float2 start, Float2 end) { Start = start; End = end; } public Float2 Start { get; } public Float2 End { get; } }
        private sealed class WardSet
        {
            public WardSet(AttackInstance attack, Float2 center, float radius, int count, bool evolved, float createdAt, bool staggerPostVisuals)
            {
                Attack = attack; DesiredCenter = center; Radius = radius; PostCount = count; IsEvolved = evolved;
                CreatedAt = createdAt; StaggerPostVisuals = staggerPostVisuals || evolved;
                for (var index = 0; index < count; index++) LastRiseFramePlayed.Add(-1);
                if (evolved) ActivateNextPost();
                else for (var index = 0; index < count; index++) Posts.Add(CardinalPost(center, radius, CardinalIndex(count, index)));
            }
            public AttackInstance Attack { get; }
            public List<Float2> Posts { get; } = new List<Float2>();
            public HashSet<int> TouchingTargetIds { get; } = new HashSet<int>();
            public HashSet<int> StatusTargetIds { get; } = new HashSet<int>();
            public HashSet<int> MarkedTargetIds { get; } = new HashSet<int>();
            public Float2 DesiredCenter { get; set; }
            public float Radius { get; }
            public int PostCount { get; }
            public bool IsEvolved { get; }
            public float CreatedAt { get; }
            public bool StaggerPostVisuals { get; }
            public List<int> LastRiseFramePlayed { get; } = new List<int>();
            public float FirstPostRise { get; set; }
            public bool IsCompleted => Posts.Count == PostCount;
            public float ActivationElapsed { get; set; }
            public bool MarkResolved { get; set; }
            public float MobileElapsed { get; set; }
            public bool HasRequestedMove { get; set; }
            public bool Retired { get; set; }
            public bool PotentialCompletionStarted { get; set; }
            public AttackInstance RotatingAttack { get; set; }
            public PixelHitMask RotationMask { get; set; }
            public float RotationRemaining { get; set; }
            public float RotationElapsed { get; set; }
            public HashSet<int> RotatedTargetIds { get; } = new HashSet<int>();
            public AttackInstance GuardianAttack { get; set; }
            public PixelHitMask GuardianMask { get; set; }
            public float GuardianRemaining { get; set; }
            public bool GuardianResolved { get; set; }
            public bool PotentialStartedThisTick { get; set; }
            public float CompletionResidual { get; set; }
            public float PotentialTickStep { get; set; }
            public bool PotentialCreatedThisTick { get; set; }
            public bool EvolvedCompletionPresented { get; set; }
            public GameObject EvolvedCompletionVisual { get; set; }
            public bool LegacyCompletionStarted { get; set; }
            public void ActivateNextPost()
            {
                if (Posts.Count < PostCount) Posts.Add(CardinalPost(DesiredCenter, Radius, CardinalIndex(PostCount, Posts.Count)));
            }
            private static int CardinalIndex(int count, int index) => count == 2 ? index * 2 : index;
        }

        private struct LegacyPulse
        {
            public LegacyPulse(int ownerId, AttackInstance attack, Float2 center, float remaining,
                int ordinal)
            { OwnerId = ownerId; Attack = attack; Center = center; Remaining = remaining;
                Ordinal = ordinal; }
            public int OwnerId; public AttackInstance Attack; public Float2 Center;
            public float Remaining; public int Ordinal;
        }

        private struct LegacySlam
        {
            public LegacySlam(int ownerId, AttackInstance attack, Float2 center, float remaining,
                float multiplier)
            { OwnerId = ownerId; Attack = attack; Center = center; Remaining = remaining;
                Multiplier = multiplier; }
            public int OwnerId; public AttackInstance Attack; public Float2 Center;
            public float Remaining; public float Multiplier;
        }
    }
}
