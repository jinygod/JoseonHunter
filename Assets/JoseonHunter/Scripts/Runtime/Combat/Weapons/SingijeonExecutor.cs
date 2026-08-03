using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
using JoseonHunter.Runtime.Combat.Weapons.Presentation;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    public sealed class SingijeonExecutor : IWeaponExecutor, IWeaponEvolutionProfile
    {
        private const float BucketDegrees = 30f;
        private const int BucketCount = 12;
        public const int MaxLaneCount = 6;
        private readonly WeaponRuntimeController runtime;
        private readonly LinearProjectileExecutor projectiles;
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private float cooldown;
        private float focusDelay;
        private bool awaitingFocus;
        private Float2 focusPosition;
        private readonly List<string> volleyKinds = new List<string>();
        private readonly HashSet<int> focusAttackIds = new HashSet<int>();
        private readonly HashSet<int> childAttackIds = new HashSet<int>();
        private readonly List<Trail> trails = new List<Trail>();
        private readonly List<FocusedSalvo> focusedSalvos = new List<FocusedSalvo>();
        private readonly List<FireNetField> fireNetFields = new List<FireNetField>();
        private readonly Dictionary<int, FireNetField> fireNetFieldsByProjectile = new Dictionary<int, FireNetField>();
        private readonly Dictionary<int, float> fireNetBurnRemaining = new Dictionary<int, float>();
        private readonly List<ICombatTarget> ignitionTargets = new List<ICombatTarget>(3);
        private readonly List<int> burnTargetIds = new List<int>();
        private FireNetField currentFireNetField;
        private bool resolvingFireNetIgnition;
        private readonly List<PendingLaunch> pendingLaunches = new List<PendingLaunch>();
        private WeaponExecutionContext latestContext;
        private readonly Dictionary<int, Float2> focusDirections = new Dictionary<int, Float2>();
        private int focusLaunchIndex;
        private float nextFocusLaunch;
        private bool focusSequenceActive;
        private bool focusRetargeted;
        private readonly Dictionary<int, PixelMaskTransform> priorTargetTransforms = new Dictionary<int, PixelMaskTransform>();
        private WeaponTransientVisualPool transientVisuals;
        private Transform transientVisualRoot;

        public SingijeonExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float speed, int laneCount, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            projectiles = new LinearProjectileExecutor(runtime);
            LegacySourceDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage));
            var legacyDamage = modifiers.Legacy.Is(WeaponLegacyPathId.SingijeonFireNet) ? .7f : 1f;
            var legacyRange = modifiers.Legacy.Is(WeaponLegacyPathId.SingijeonFireDragon) ? .65f : 1f;
            BaseDamage = LegacySourceDamage * legacyDamage; CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds));
            Range = Mathf.Max(0.01f, modifiers.ScaleArea(range) * legacyRange); Speed = Mathf.Max(0.01f, modifiers.ScaleSpeed(speed)); LaneCount = Mathf.Clamp(laneCount, 1, MaxLaneCount); Level = Mathf.Clamp(level, 1, 5); Potentials = modifiers;
            IsEvolved = evolved;
            runtime.DamageService.DamageConfirmed += OnDamageConfirmed;
            projectiles.ProjectileTravelled += OnProjectileTravelled;
        }

        public float BaseDamage { get; }
        private float LegacySourceDamage { get; }
        public float CooldownSeconds { get; }
        public float Range { get; }
        public float Speed { get; }
        public int LaneCount { get; }
        public int Level { get; }
        public bool IsEvolved { get; }
        public WeaponRuntimeModifiers Potentials { get; }
        public int LastLaunchCount { get; private set; }
        public int ActiveProjectileCount => projectiles.ActiveCount;
        public Float2 LastDirection { get; private set; }
        public int LastDirectionBucket { get; private set; } = -1;
        public IReadOnlyList<string> VolleyKinds => volleyKinds;
        public int ScoutProjectileCount { get; private set; }
        public int FocusProjectileCount { get; private set; }
        public Float2 RecordedFocusPosition => focusPosition;
#if UNITY_INCLUDE_TESTS
        public int ActiveTrailCountForTests => trails.Count + fireNetFields.Count;
        public bool FocusRetargetedForTests => focusRetargeted;
        public int UnlaunchedFocusCountForTests => Mathf.Max(0, FocusProjectileCount - focusLaunchIndex);
        public int FocusRetargetCountForTests { get; private set; }
        public int PendingLaunchCountForTests => pendingLaunches.Count;
        public IReadOnlyList<int> SplitChildAttackIdsForTests => splitChildAttackIds;
        public Func<ICombatTarget, bool> BeforeFocusPotentialCheckForTests { get; set; }
        public bool SuppressNewCastsForTests { get; set; }
        public int LastFocusedTargetRuntimeIdForTests { get; private set; }
        public int LastFocusedSalvoCountForTests { get; private set; }
        public int MaximumConnectedTrailEndpointsForTests { get; private set; }
        public int LastFireNetIgnitionCountForTests { get; private set; }
        public int LastFireNetBurnTargetRuntimeIdForTests { get; private set; }
#endif
        private readonly List<int> splitChildAttackIds = new List<int>();

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            latestContext = context;
            EnsureTransientVisuals(context.PresentationRoot);
            transientVisuals?.Tick(deltaTime);
            AdvanceTrails(Mathf.Max(0f, deltaTime), context);
            AdvanceFireNetFields(Mathf.Max(0f, deltaTime), context);
            TickBurnTracking(Mathf.Max(0f, deltaTime));
#if UNITY_INCLUDE_TESTS
            if (SuppressNewCastsForTests) { RememberTargetTransforms(); return; }
#endif
            if (Potentials.Legacy.Is(WeaponLegacyPathId.SingijeonFireDragon))
            {
                TickFireDragon(Mathf.Max(0f, deltaTime), context);
                RememberTargetTransforms();
                return;
            }
            if (!IsEvolved)
            {
                TickNormal(deltaTime, context);
                RememberTargetTransforms();
                return;
            }

            var remaining = Mathf.Max(0f, deltaTime);
            while (remaining > 0.0001f)
            {
                if (focusSequenceActive)
                {
                    if (nextFocusLaunch <= .00001f) { var zero = 0f; AdvanceFocusSequence(ref zero, context); continue; }
                    var launchSlice = Mathf.Min(remaining, nextFocusLaunch);
                    TickProjectilesAndPending(launchSlice, context); remaining -= launchSlice; nextFocusLaunch -= launchSlice;
                    if (nextFocusLaunch <= .00001f) { var zero = 0f; AdvanceFocusSequence(ref zero, context); }
                    continue;
                }
                if (awaitingFocus)
                {
                    var untilFocus = Mathf.Min(remaining, focusDelay);
                    TickProjectilesAndPending(untilFocus, context);
                    remaining -= untilFocus;
                    focusDelay -= untilFocus;
                    if (focusDelay > 0.0001f) break;
                    focusDelay = 0f;
                    LaunchFocus(context);
                    awaitingFocus = false;
                    cooldown = CooldownSeconds;
                    continue;
                }

                if (cooldown > 0.0001f)
                {
                    var untilReady = Mathf.Min(remaining, cooldown);
                    TickProjectilesAndPending(untilReady, context);
                    remaining -= untilReady;
                    cooldown -= untilReady;
                    if (cooldown > 0.0001f) break;
                    cooldown = 0f;
                    continue;
                }

                if (!TryFindDensestDirection(context.OwnerPosition, out var direction, out var densePosition))
                {
                    TickProjectilesAndPending(remaining, context);
                    break;
                }
                LaunchScout(context, direction, densePosition);
            }
            RememberTargetTransforms();
        }

        private void TickNormal(float deltaTime, in WeaponExecutionContext context)
        {
            cooldown -= deltaTime;
            if (cooldown <= 0f && TryFindDensestDirection(context.OwnerPosition, out var direction, out _))
            {
                cooldown = CooldownSeconds;
                Launch(context, direction);
            }
            TickProjectilesAndPending(deltaTime, context);
        }

        public void Reset()
        {
            foreach (var trail in trails) runtime.DamageService.RetireAttack(trail.Attack.InstanceId);
            foreach (var salvo in focusedSalvos) runtime.DamageService.RetireAttack(salvo.Attack.InstanceId);
            foreach (var field in fireNetFields)
            {
                runtime.DamageService.RetireAttack(field.Attack.InstanceId);
                runtime.DamageService.RetireAttack(field.DetonationAttack.InstanceId);
            }
            cooldown = 0f; focusDelay = 0f; awaitingFocus = false; focusPosition = default; LastLaunchCount = 0; LastDirection = default; LastDirectionBucket = -1; ScoutProjectileCount = 0; FocusProjectileCount = 0; volleyKinds.Clear(); focusAttackIds.Clear(); focusDirections.Clear(); childAttackIds.Clear(); splitChildAttackIds.Clear(); trails.Clear(); pendingLaunches.Clear(); priorTargetTransforms.Clear(); focusLaunchIndex = 0; nextFocusLaunch = 0f; focusSequenceActive = false; focusRetargeted = false;
            focusedSalvos.Clear(); fireNetFields.Clear(); fireNetFieldsByProjectile.Clear();
            fireNetBurnRemaining.Clear(); ignitionTargets.Clear(); burnTargetIds.Clear();
            currentFireNetField = null; resolvingFireNetIgnition = false;
            transientVisuals?.Dispose(); transientVisuals = null; transientVisualRoot = null;
#if UNITY_INCLUDE_TESTS
            FocusRetargetCountForTests = 0;
            LastFocusedTargetRuntimeIdForTests = 0; LastFocusedSalvoCountForTests = 0;
            MaximumConnectedTrailEndpointsForTests = 0;
            LastFireNetIgnitionCountForTests = 0;
            LastFireNetBurnTargetRuntimeIdForTests = 0;
#endif
            projectiles.Reset();
        }

        public void Dispose() { Reset(); runtime.DamageService.DamageConfirmed -= OnDamageConfirmed; projectiles.ProjectileTravelled -= OnProjectileTravelled; projectiles.Dispose(); }

        private void TickFireDragon(float step, in WeaponExecutionContext context)
        {
            cooldown -= step;
            if (cooldown <= 0f && TryFindStrongestTarget(context.OwnerPosition, out var target))
            {
                cooldown = CooldownSeconds;
                var count = Potentials.Legacy.Stage == WeaponLegacyStage.Completed ? 5 :
                    Potentials.Legacy.Stage >= WeaponLegacyStage.Reinforced ? 4 : 1;
                var multiplier = count == 5 ? .32f : count == 4 ? .4f : 1f;
#if UNITY_INCLUDE_TESTS
                LastFocusedTargetRuntimeIdForTests = target.RuntimeId;
                LastFocusedSalvoCountForTests = count;
#endif
                for (var index = 0; index < count; index++)
                    focusedSalvos.Add(new FocusedSalvo(
                        new AttackInstance(runtime.AllocateAttackInstanceId(),
                            RepeatHitPolicy.OncePerPhase, 0f), target.RuntimeId,
                        .05f + index * .10f, multiplier));
            }

            for (var index = focusedSalvos.Count - 1; index >= 0; index--)
            {
                var salvo = focusedSalvos[index]; salvo.Remaining -= step;
                if (salvo.Remaining > 0f) { focusedSalvos[index] = salvo; continue; }
                if (runtime.Targets.TryGet(salvo.TargetRuntimeId, out var salvoTarget) &&
                    salvoTarget != null && salvoTarget.IsAlive)
                {
                    runtime.DamageService.TryApply(WeaponDamageRequest.Create(salvo.Attack,
                        WeaponId.SingijeonVolley, salvoTarget,
                        Mathf.CeilToInt(LegacySourceDamage * salvo.Multiplier), false,
                        salvoTarget.WorldPosition, ContactPhase.PotentialChain, context.SimulationTick,
                        true, WeaponHitTrait.Explosion, context.OwnerPosition), out _);
                    transientVisuals?.Play(context.PresentationSpriteFor(WeaponId.SingijeonVolley,
                            WeaponVisualPartIndex.Singijeon.Detonation),
                        new Vector3(salvoTarget.WorldPosition.X, salvoTarget.WorldPosition.Y, 0f),
                        Quaternion.identity, Vector3.one * .72f,
                        new Color(.86f, .45f, .12f, .9f), .12f, context.SortingOrder + 2);
                }
                runtime.DamageService.RetireAttack(salvo.Attack.InstanceId);
                focusedSalvos.RemoveAt(index);
            }
        }

        private bool TryFindStrongestTarget(Float2 origin, out ICombatTarget selected)
        {
            selected = null;
            runtime.Targets.CopyTo(targets);
            var rangeSquared = Range * Range;
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive ||
                    DistanceSquared(target.WorldPosition, origin) > rangeSquared) continue;
                if (selected == null || target.ThreatScore > selected.ThreatScore ||
                    Mathf.Approximately(target.ThreatScore, selected.ThreatScore) &&
                    target.RuntimeId < selected.RuntimeId) selected = target;
            }
            return selected != null;
        }

        private bool TryFindDensestDirection(Float2 origin, out Float2 direction, out Float2 densePosition)
        {
            targets.Clear(); runtime.Targets.CopyTo(targets);
            var counts = new int[BucketCount];
            var sumX = new float[BucketCount];
            var sumY = new float[BucketCount];
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive) continue;
                var x = target.WorldPosition.X - origin.X; var y = target.WorldPosition.Y - origin.Y;
                if (x * x + y * y < 0.0001f) continue;
                var rawBucket = Mathf.FloorToInt((Mathf.Atan2(y, x) * Mathf.Rad2Deg + BucketDegrees * 0.5f) / BucketDegrees);
                var bucket = ((rawBucket % BucketCount) + BucketCount) % BucketCount;
                counts[bucket]++;
                sumX[bucket] += target.WorldPosition.X;
                sumY[bucket] += target.WorldPosition.Y;
            }
            var selectedBucket = 0; var highestCount = 0;
            for (var bucket = 0; bucket < BucketCount; bucket++)
            {
                if (counts[bucket] > highestCount)
                {
                    selectedBucket = bucket; highestCount = counts[bucket];
                }
            }
            if (highestCount == 0) { direction = default; densePosition = default; return false; }
            LastDirectionBucket = selectedBucket;
            var radians = selectedBucket * BucketDegrees * Mathf.Deg2Rad;
            direction = new Float2(Mathf.Cos(radians), Mathf.Sin(radians));
            densePosition = new Float2(sumX[selectedBucket] / highestCount, sumY[selectedBucket] / highestCount);
            return true;
        }

        private void LaunchScout(in WeaponExecutionContext context, Float2 direction, Float2 densePosition)
        {
            LastDirection = direction; focusPosition = densePosition; awaitingFocus = true; focusDelay = 0.35f;
            volleyKinds.Clear(); volleyKinds.Add("scout"); ScoutProjectileCount = 0; FocusProjectileCount = 0; LastLaunchCount = 3;
            for (var index = -1; index <= 1; index++)
            {
                var radians = index * 10f * Mathf.Deg2Rad;
                var spread = new Float2(direction.X * Mathf.Cos(radians) - direction.Y * Mathf.Sin(radians), direction.X * Mathf.Sin(radians) + direction.Y * Mathf.Cos(radians));
                ScheduleRocket(context.OwnerPosition, spread, "Singijeon Scout Rocket", false, false, (index + 1) * .045f);
                ScoutProjectileCount++;
            }
        }

        private void LaunchFocus(in WeaponExecutionContext context)
        {
            volleyKinds.Add("focus"); FocusProjectileCount = 8; LastLaunchCount = FocusProjectileCount; focusLaunchIndex = 0; nextFocusLaunch = 0f; focusSequenceActive = true; focusRetargeted = false;
            var cue = new WeaponVisualCue(
                WeaponId.SingijeonVolley,
                WeaponVisualStage.Windup,
                Level,
                IsEvolved,
                .9f,
                .14f);
            transientVisuals?.Play(
                context.PresentationSpriteFor(
                    WeaponId.SingijeonVolley,
                    WeaponVisualPartIndex.Singijeon.Windup),
                new Vector3(focusPosition.X, focusPosition.Y, 0f),
                Quaternion.identity,
                Vector3.one * cue.ResolvedScale,
                new Color(1f, .82f, .45f, .9f),
                cue.ResolvedLifetime,
                context.SortingOrder + 1);
            var none = 0f; AdvanceFocusSequence(ref none, context);
        }

        private void AdvanceFocusSequence(ref float available, in WeaponExecutionContext context)
        {
            if (!focusSequenceActive) return;
            while (focusLaunchIndex < FocusProjectileCount && available + .00001f >= nextFocusLaunch)
            {
                available -= nextFocusLaunch; nextFocusLaunch = .035f;
                var radians = focusLaunchIndex * Mathf.PI * 2f / FocusProjectileCount;
                var offset = new Float2(Mathf.Cos(radians) * .3f, Mathf.Sin(radians) * .3f);
                var target = new Float2(focusPosition.X + offset.X, focusPosition.Y + offset.Y);
                LaunchRocket(context, context.OwnerPosition, Normalize(new Float2(target.X - context.OwnerPosition.X, target.Y - context.OwnerPosition.Y)), "Singijeon Focus Rocket", true, false);
                focusLaunchIndex++;
            }
            if (focusLaunchIndex >= FocusProjectileCount) focusSequenceActive = false;
            else nextFocusLaunch -= available;
        }

        private void LaunchRocket(in WeaponExecutionContext context, Float2 position, Float2 direction, string name, bool focus, bool child)
        {
            var attack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
            if (Potentials.Legacy.Is(WeaponLegacyPathId.SingijeonFireNet) && currentFireNetField != null)
            {
                fireNetFieldsByProjectile[attack.InstanceId] = currentFireNetField;
                currentFireNetField.ProjectileAttackIds.Add(attack.InstanceId);
            }
            if (focus) { focusAttackIds.Add(attack.InstanceId); focusDirections[attack.InstanceId] = direction; } if (child) { childAttackIds.Add(attack.InstanceId); splitChildAttackIds.Add(attack.InstanceId); }
            var lifetime = (child ? Range * .55f : Range) / Speed;
            projectiles.Launch(context, new LinearProjectileSpec(
                attack,
                WeaponId.SingijeonVolley,
                position,
                direction,
                Speed,
                lifetime,
                Mathf.CeilToInt(child ? BaseDamage * .35f : BaseDamage),
                1,
                name,
                visualPartStart: WeaponVisualPartIndex.Singijeon.Projectile,
                visualFrameCount: WeaponVisualPartIndex.Singijeon.ProjectileFrameCount,
                visualFrameSeconds: .05f,
                traits: Potentials.Legacy.Is(WeaponLegacyPathId.SingijeonFireNet)
                    ? WeaponHitTrait.Explosion : WeaponHitTrait.None));
        }

        private static Float2 Normalize(Float2 value)
        {
            var length = Mathf.Sqrt(value.X * value.X + value.Y * value.Y);
            return length < 0.0001f ? new Float2(1f, 0f) : new Float2(value.X / length, value.Y / length);
        }

        private void Launch(in WeaponExecutionContext context, Float2 direction)
        {
            LastDirection = direction;
            if (Potentials.Legacy.Is(WeaponLegacyPathId.SingijeonFireNet))
            {
                currentFireNetField = new FireNetField(
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.TimedTicks, .5f),
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerPhase, 0f));
                currentFireNetField.AddEndpoint(context.OwnerPosition);
                fireNetFields.Add(currentFireNetField);
            }
            var rows = Level == 5 ? 3 : 1;
            LastLaunchCount = rows * LaneCount;
            for (var row = 0; row < rows; row++)
            for (var lane = 0; lane < LaneCount; lane++)
            {
                var laneOffset = lane - (LaneCount - 1) * 0.5f;
                var rowOffset = (row - (rows - 1) * 0.5f) * 0.1f;
                var perpendicular = new Float2(-direction.Y, direction.X);
                var position = new Float2(context.OwnerPosition.X + perpendicular.X * laneOffset * 0.12f - direction.X * rowOffset,
                    context.OwnerPosition.Y + perpendicular.Y * laneOffset * 0.12f - direction.Y * rowOffset);
                ScheduleRocket(position, direction, "Singijeon Rocket", false, false, (row * LaneCount + lane) * .045f);
            }
        }

        private void ScheduleRocket(
            Float2 position,
            Float2 direction,
            string name,
            bool focus,
            bool child,
            float delay)
        {
            if (delay <= .00001f)
            {
                LaunchRocket(latestContext, position, direction, name, focus, child);
                return;
            }
            pendingLaunches.Add(new PendingLaunch(position, direction, name, focus, child, delay));
        }

        private void TickProjectilesAndPending(float deltaTime, in WeaponExecutionContext context)
        {
            var remaining = Mathf.Max(0f, deltaTime);
            while (remaining > .00001f)
            {
                var untilLaunch = remaining;
                for (var index = 0; index < pendingLaunches.Count; index++)
                    untilLaunch = Mathf.Min(untilLaunch, pendingLaunches[index].Remaining);

                if (untilLaunch > .00001f)
                {
                    projectiles.Tick(untilLaunch, context);
                    remaining -= untilLaunch;
                    for (var index = 0; index < pendingLaunches.Count; index++)
                    {
                        var pending = pendingLaunches[index];
                        pending.Remaining -= untilLaunch;
                        pendingLaunches[index] = pending;
                    }
                }

                var launched = false;
                for (var index = pendingLaunches.Count - 1; index >= 0; index--)
                {
                    var pending = pendingLaunches[index];
                    if (pending.Remaining > .00001f) continue;
                    pendingLaunches.RemoveAt(index);
                    LaunchRocket(
                        context,
                        pending.Position,
                        pending.Direction,
                        pending.Name,
                        pending.Focus,
                        pending.Child);
                    launched = true;
                }
                if (!launched && untilLaunch <= .00001f) break;
            }
        }

        private void EnsureTransientVisuals(Transform root)
        {
            if (root == null || root == transientVisualRoot) return;
            transientVisuals?.Dispose();
            transientVisualRoot = root;
            transientVisuals = new WeaponTransientVisualPool(root);
        }

        private void OnDamageConfirmed(ConfirmedDamageEvent damage)
        {
            if (Potentials.Legacy.Is(WeaponLegacyPathId.SingijeonFireNet))
            {
                if (damage.WeaponId.Equals(WeaponId.SingijeonVolley) &&
                    damage.Phase == ContactPhase.Direct &&
                    fireNetFieldsByProjectile.TryGetValue(damage.AttackInstanceId, out var field))
                    field.AddEndpoint(damage.ContactPoint);
                TryPropagateFireNetOnDeath(damage);
            }
            if (!damage.WeaponId.Equals(WeaponId.SingijeonVolley)) return;
            if (damage.Phase != ContactPhase.Direct || !focusAttackIds.Contains(damage.AttackInstanceId) ||
                childAttackIds.Contains(damage.AttackInstanceId)) return;
            if (!runtime.Targets.TryGet(damage.TargetRuntimeId, out var target) || target == null || target.HurtMask == null) return;
#if UNITY_INCLUDE_TESTS
            BeforeFocusPotentialCheckForTests?.Invoke(target);
#endif
            if (Potentials.HasPotential(WeaponPotentialId.SingijeonSubmunitionSplit) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.SingijeonSubmunitionSplit, out _, out var split) && PixelMaskContactService.TryFindContact(split, PixelMaskTransform.Translation(damage.ContactPoint.X, damage.ContactPoint.Y), target.HurtMask, target.HurtMaskTransform, out _))
            {
                focusDirections.TryGetValue(damage.AttackInstanceId, out var baseDirection); for (var index = -1; index <= 1; index++) { var rad = index * 30f * Mathf.Deg2Rad; var direction = Normalize(new Float2(baseDirection.X * Mathf.Cos(rad) - baseDirection.Y * Mathf.Sin(rad), baseDirection.X * Mathf.Sin(rad) + baseDirection.Y * Mathf.Cos(rad))); LaunchRocket(latestContext, damage.ContactPoint, direction, "Singijeon Submunition", false, true); }
            }
            if (Potentials.HasPotential(WeaponPotentialId.SingijeonChainIgnition) && !target.IsAlive && WeaponPotentialVisuals.TryGet(WeaponPotentialId.SingijeonChainIgnition, out _, out var chain) && PixelMaskContactService.TryFindContact(chain, PixelMaskTransform.Translation(damage.ContactPoint.X, damage.ContactPoint.Y), target.HurtMask, target.HurtMaskTransform, out _))
            { if (!focusRetargeted && focusSequenceActive && TryFindDensestDirection(latestContext.OwnerPosition, out _, out var centroid)) { focusPosition = centroid; focusRetargeted = true;
#if UNITY_INCLUDE_TESTS
                FocusRetargetCountForTests++;
#endif
            } }
        }

        private void AdvanceTrails(float step, in WeaponExecutionContext context)
        {
            for (var index = trails.Count - 1; index >= 0; index--)
            {
                var trail = trails[index]; trail.Remaining -= step; runtime.Targets.CopyTo(targets);
                foreach (var target in targets)
                {
                    if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                    trail.PreviousTransforms.TryGetValue(target.RuntimeId, out var previousTransform); var hadPrevious = trail.PreviousTransforms.ContainsKey(target.RuntimeId); trail.PreviousTransforms[target.RuntimeId] = target.HurtMaskTransform;
                    var currentContact = PixelMaskContactService.TryFindContact(trail.Mask, PixelMaskTransform.Translation(trail.Position.X, trail.Position.Y), target.HurtMask, target.HurtMaskTransform, out var contact);
                    var previousContact = false;
                    if (hadPrevious) previousContact = PixelMaskContactService.TryFindContact(trail.Mask, PixelMaskTransform.Translation(trail.Position.X, trail.Position.Y), target.HurtMask, previousTransform, out _);
                    if (!trail.Crossed.Contains(target.RuntimeId) && hadPrevious && !previousContact && currentContact) { trail.Crossed.Add(target.RuntimeId); trail.TicksByTarget[target.RuntimeId] = new TrailTicks(contact); }
                }
                var ids = new List<int>(trail.TicksByTarget.Keys);
                foreach (var id in ids)
                {
                    var ticks = trail.TicksByTarget[id]; ticks.Elapsed += step;
                    while (ticks.Elapsed + .00001f >= .3f && ticks.Count < 2) { ticks.Elapsed -= .3f; if (runtime.Targets.TryGet(id, out var target) && target != null && target.IsAlive) runtime.DamageService.TryApply(WeaponDamageRequest.Create(trail.Attack, WeaponId.SingijeonVolley, target, Mathf.CeilToInt(BaseDamage * .15f), false, ticks.Contact, ContactPhase.Burn, context.SimulationTick), out _); ticks.Count++; }
                    trail.TicksByTarget[id] = ticks;
                }
                if (trail.Remaining <= 0f) { runtime.DamageService.RetireAttack(trail.Attack.InstanceId); trails.RemoveAt(index); } else trails[index] = trail;
            }
        }

        private void AdvanceFireNetFields(float step, in WeaponExecutionContext context)
        {
            for (var index = fireNetFields.Count - 1; index >= 0; index--)
            {
                var field = fireNetFields[index];
                field.Remaining -= step;
                field.Elapsed += step;
                field.TickElapsed += step;
                while (field.TickElapsed + .0001f >= .5f && field.Remaining >= -.0001f)
                {
                    field.TickElapsed -= .5f;
                    runtime.Targets.CopyTo(targets);
                    foreach (var target in targets)
                    {
                        if (target == null || !target.IsAlive || !field.IsNearTrail(target.WorldPosition, .34f))
                            continue;
                        fireNetBurnRemaining[target.RuntimeId] = Mathf.Max(0f, field.Remaining);
                        if (!runtime.DamageService.TryApply(WeaponDamageRequest.Create(field.Attack,
                            WeaponId.SingijeonVolley, target,
                            Mathf.RoundToInt(LegacySourceDamage * .3f), false, target.WorldPosition,
                            ContactPhase.Burn, context.SimulationTick, field.Elapsed, true,
                            WeaponHitTrait.Explosion, field.FirstEndpoint), out _)) continue;
                        runtime.AffixStatuses.ApplyTimedStatus(target.RuntimeId, CombatStatusKind.Burn,
                            Mathf.Max(.5f, field.Remaining), 1, WeaponId.SingijeonVolley);
#if UNITY_INCLUDE_TESTS
                        LastFireNetBurnTargetRuntimeIdForTests = target.RuntimeId;
#endif
                    }
                }

#if UNITY_INCLUDE_TESTS
                MaximumConnectedTrailEndpointsForTests = Mathf.Max(
                    MaximumConnectedTrailEndpointsForTests, field.Endpoints.Count);
#endif
                if (field.Remaining > 0f) continue;
                if (Potentials.Legacy.Stage == WeaponLegacyStage.Completed)
                    ResolveConnectedTrailDetonation(field, context);
                runtime.DamageService.RetireAttack(field.Attack.InstanceId);
                runtime.DamageService.RetireAttack(field.DetonationAttack.InstanceId);
                foreach (var attackId in field.ProjectileAttackIds)
                    fireNetFieldsByProjectile.Remove(attackId);
                fireNetFields.RemoveAt(index);
                if (ReferenceEquals(currentFireNetField, field)) currentFireNetField = null;
            }
        }

        private void ResolveConnectedTrailDetonation(FireNetField field,
            in WeaponExecutionContext context)
        {
            runtime.Targets.CopyTo(targets);
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || !field.IsNearTrail(target.WorldPosition, .60f))
                    continue;
                runtime.DamageService.TryApply(WeaponDamageRequest.Create(field.DetonationAttack,
                    WeaponId.SingijeonVolley, target, Mathf.CeilToInt(LegacySourceDamage * 2f),
                    false, target.WorldPosition, ContactPhase.Blast, context.SimulationTick,
                    true, WeaponHitTrait.Explosion, field.FirstEndpoint), out _);
            }
        }

        private void TryPropagateFireNetOnDeath(ConfirmedDamageEvent damage)
        {
            if (resolvingFireNetIgnition || Potentials.Legacy.Stage < WeaponLegacyStage.Reinforced ||
                !fireNetBurnRemaining.TryGetValue(damage.TargetRuntimeId, out var remaining) ||
                remaining <= 0f || !runtime.Targets.TryGet(damage.TargetRuntimeId, out var killed) ||
                killed == null || killed.IsAlive) return;
            fireNetBurnRemaining.Remove(damage.TargetRuntimeId);
            ignitionTargets.Clear(); runtime.Targets.CopyTo(ignitionTargets);
            ignitionTargets.RemoveAll(target => target == null || !target.IsAlive ||
                target.RuntimeId == damage.TargetRuntimeId ||
                DistanceSquared(target.WorldPosition, damage.ContactPoint) > Range * Range);
            ignitionTargets.Sort((left, right) =>
            {
                var distance = DistanceSquared(left.WorldPosition, damage.ContactPoint)
                    .CompareTo(DistanceSquared(right.WorldPosition, damage.ContactPoint));
                return distance != 0 ? distance : left.RuntimeId.CompareTo(right.RuntimeId);
            });
            if (ignitionTargets.Count > 3) ignitionTargets.RemoveRange(3, ignitionTargets.Count - 3);
#if UNITY_INCLUDE_TESTS
            LastFireNetIgnitionCountForTests = ignitionTargets.Count;
#endif
            resolvingFireNetIgnition = true;
            try
            {
                foreach (var target in ignitionTargets)
                {
                    var ticks = Mathf.Max(1, Mathf.CeilToInt(remaining / .5f));
                    if (runtime.AffixStatuses.ApplyOrRefreshPeriodic(new PeriodicEffectRequest(
                        WeaponId.SingijeonVolley, target.RuntimeId, target.WorldPosition,
                        Mathf.RoundToInt(LegacySourceDamage * .3f), ticks,
                        new AttackInstance(runtime.AllocateAttackInstanceId(),
                            RepeatHitPolicy.TimedTicks, .5f), true, ContactPhase.Burn)))
                        fireNetBurnRemaining[target.RuntimeId] = remaining;
                }
            }
            finally { resolvingFireNetIgnition = false; }
        }

        private void TickBurnTracking(float step)
        {
            burnTargetIds.Clear();
            foreach (var pair in fireNetBurnRemaining) burnTargetIds.Add(pair.Key);
            foreach (var targetId in burnTargetIds)
            {
                var remaining = fireNetBurnRemaining[targetId] - step;
                if (remaining <= 0f) fireNetBurnRemaining.Remove(targetId);
                else fireNetBurnRemaining[targetId] = remaining;
            }
        }

        private void OnProjectileTravelled(LinearProjectileTravel travel)
        {
            if (fireNetFieldsByProjectile.TryGetValue(travel.AttackInstanceId, out var fireNet))
                fireNet.AddEndpoint(travel.Current, travel.AttackInstanceId);
            if (!Potentials.HasPotential(WeaponPotentialId.SingijeonPowderTrail) || !WeaponPotentialVisuals.TryGet(WeaponPotentialId.SingijeonPowderTrail, out _, out var mask)) return;
            var dx = travel.Current.X - travel.Previous.X; var dy = travel.Current.Y - travel.Previous.Y; var distance = Mathf.Sqrt(dx * dx + dy * dy); var count = Mathf.Max(1, Mathf.CeilToInt(distance / .35f));
            for (var index = 1; index <= count; index++)
            {
                var t = index / (float)count; var trail = new Trail { Position = new Float2(travel.Previous.X + dx * t, travel.Previous.Y + dy * t), Mask = mask, Remaining = .6f, Attack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.TimedTicks, .3f) };
                runtime.Targets.CopyTo(targets);
                foreach (var target in targets) if (target != null && target.IsAlive && target.HurtMask != null && priorTargetTransforms.TryGetValue(target.RuntimeId, out var prior))
                {
                    var current = PixelMaskContactService.TryFindContact(mask, PixelMaskTransform.Translation(trail.Position.X, trail.Position.Y), target.HurtMask, target.HurtMaskTransform, out var contact);
                    var previous = PixelMaskContactService.TryFindContact(mask, PixelMaskTransform.Translation(trail.Position.X, trail.Position.Y), target.HurtMask, prior, out _);
                    trail.PreviousTransforms[target.RuntimeId] = target.HurtMaskTransform;
                    if (!previous && current) { trail.Crossed.Add(target.RuntimeId); trail.TicksByTarget[target.RuntimeId] = new TrailTicks(contact); }
                }
                trails.Add(trail);
            }
        }
        private void RememberTargetTransforms() { runtime.Targets.CopyTo(targets); priorTargetTransforms.Clear(); foreach (var target in targets) if (target != null && target.IsAlive && target.HurtMask != null) priorTargetTransforms[target.RuntimeId] = target.HurtMaskTransform; }
        private static float DistanceSquared(Float2 left, Float2 right) { var x = left.X - right.X; var y = left.Y - right.Y; return x * x + y * y; }

        private struct FocusedSalvo
        {
            public FocusedSalvo(AttackInstance attack, int targetRuntimeId, float remaining,
                float multiplier)
            { Attack = attack; TargetRuntimeId = targetRuntimeId; Remaining = remaining;
                Multiplier = multiplier; }
            public AttackInstance Attack; public int TargetRuntimeId; public float Remaining;
            public float Multiplier;
        }

        private sealed class FireNetField
        {
            private const int EndpointCapacity = 24;
            public FireNetField(AttackInstance attack, AttackInstance detonationAttack)
            { Attack = attack; DetonationAttack = detonationAttack; }
            public AttackInstance Attack { get; }
            public AttackInstance DetonationAttack { get; }
            public List<Float2> Endpoints { get; } = new List<Float2>(EndpointCapacity);
            public HashSet<int> ProjectileAttackIds { get; } = new HashSet<int>();
            public float Remaining { get; set; } = 3f;
            public float TickElapsed { get; set; }
            public float Elapsed { get; set; }
            public Float2 FirstEndpoint => Endpoints.Count > 0 ? Endpoints[0] : default;

            public void AddEndpoint(Float2 point, int projectileAttackId = 0)
            {
                if (projectileAttackId != 0) ProjectileAttackIds.Add(projectileAttackId);
                if (Endpoints.Count >= EndpointCapacity) return;
                if (Endpoints.Count > 0 && DistanceSquared(Endpoints[Endpoints.Count - 1], point) < .10f * .10f)
                    return;
                Endpoints.Add(point);
            }

            public bool IsNearTrail(Float2 point, float radius)
            {
                var radiusSquared = radius * radius;
                foreach (var endpoint in Endpoints)
                    if (DistanceSquared(endpoint, point) <= radiusSquared) return true;
                return false;
            }
        }

        private struct PendingLaunch
        {
            public PendingLaunch(Float2 position, Float2 direction, string name, bool focus, bool child, float remaining)
            {
                Position = position; Direction = direction; Name = name; Focus = focus; Child = child; Remaining = remaining;
            }
            public Float2 Position;
            public Float2 Direction;
            public string Name;
            public bool Focus;
            public bool Child;
            public float Remaining;
        }
        private sealed class Trail { public Float2 Position; public PixelHitMask Mask; public float Remaining; public AttackInstance Attack; public HashSet<int> Crossed { get; } = new HashSet<int>(); public Dictionary<int, TrailTicks> TicksByTarget { get; } = new Dictionary<int, TrailTicks>(); public Dictionary<int, PixelMaskTransform> PreviousTransforms { get; } = new Dictionary<int, PixelMaskTransform>(); }
        private struct TrailTicks { public TrailTicks(Float2 contact) { Contact = contact; Elapsed = 0f; Count = 0; } public Float2 Contact; public float Elapsed; public int Count; }
    }
}
