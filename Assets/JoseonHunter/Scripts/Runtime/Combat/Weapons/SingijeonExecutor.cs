using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using JoseonHunter.Domain.Progression;
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
        private WeaponExecutionContext latestContext;
        private readonly Dictionary<int, Float2> focusDirections = new Dictionary<int, Float2>();
        private int focusLaunchIndex;
        private float nextFocusLaunch;
        private bool focusSequenceActive;
        private bool focusRetargeted;
        private readonly Dictionary<int, PixelMaskTransform> priorTargetTransforms = new Dictionary<int, PixelMaskTransform>();

        public SingijeonExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float range, float speed, int laneCount, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            projectiles = new LinearProjectileExecutor(runtime);
            BaseDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage)); CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds));
            Range = Mathf.Max(0.01f, modifiers.ScaleArea(range)); Speed = Mathf.Max(0.01f, modifiers.ScaleSpeed(speed)); LaneCount = Mathf.Clamp(laneCount, 1, MaxLaneCount); Level = Mathf.Clamp(level, 1, 5); Potentials = modifiers;
            IsEvolved = evolved;
            runtime.DamageService.DamageConfirmed += OnDamageConfirmed;
            projectiles.ProjectileTravelled += OnProjectileTravelled;
        }

        public float BaseDamage { get; }
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
        public int ActiveTrailCountForTests => trails.Count;
        public bool FocusRetargetedForTests => focusRetargeted;
        public int UnlaunchedFocusCountForTests => Mathf.Max(0, FocusProjectileCount - focusLaunchIndex);
        public int FocusRetargetCountForTests { get; private set; }
        public IReadOnlyList<int> SplitChildAttackIdsForTests => splitChildAttackIds;
        public Func<ICombatTarget, bool> BeforeFocusPotentialCheckForTests { get; set; }
        public bool SuppressNewCastsForTests { get; set; }
#endif
        private readonly List<int> splitChildAttackIds = new List<int>();

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            latestContext = context; AdvanceTrails(Mathf.Max(0f, deltaTime), context);
#if UNITY_INCLUDE_TESTS
            if (SuppressNewCastsForTests) { RememberTargetTransforms(); return; }
#endif
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
                    projectiles.Tick(launchSlice, context); remaining -= launchSlice; nextFocusLaunch -= launchSlice;
                    if (nextFocusLaunch <= .00001f) { var zero = 0f; AdvanceFocusSequence(ref zero, context); }
                    continue;
                }
                if (awaitingFocus)
                {
                    var untilFocus = Mathf.Min(remaining, focusDelay);
                    projectiles.Tick(untilFocus, context);
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
                    projectiles.Tick(untilReady, context);
                    remaining -= untilReady;
                    cooldown -= untilReady;
                    if (cooldown > 0.0001f) break;
                    cooldown = 0f;
                    continue;
                }

                if (!TryFindDensestDirection(context.OwnerPosition, out var direction, out var densePosition))
                {
                    projectiles.Tick(remaining, context);
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
            projectiles.Tick(deltaTime, context);
        }

        public void Reset()
        {
            foreach (var trail in trails) runtime.DamageService.RetireAttack(trail.Attack.InstanceId);
            cooldown = 0f; focusDelay = 0f; awaitingFocus = false; focusPosition = default; LastLaunchCount = 0; LastDirection = default; LastDirectionBucket = -1; ScoutProjectileCount = 0; FocusProjectileCount = 0; volleyKinds.Clear(); focusAttackIds.Clear(); focusDirections.Clear(); childAttackIds.Clear(); splitChildAttackIds.Clear(); trails.Clear(); priorTargetTransforms.Clear(); focusLaunchIndex = 0; nextFocusLaunch = 0f; focusSequenceActive = false; focusRetargeted = false;
#if UNITY_INCLUDE_TESTS
            FocusRetargetCountForTests = 0;
#endif
            projectiles.Reset();
        }

        public void Dispose() { Reset(); runtime.DamageService.DamageConfirmed -= OnDamageConfirmed; projectiles.ProjectileTravelled -= OnProjectileTravelled; projectiles.Dispose(); }

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
                LaunchRocket(context, context.OwnerPosition, spread, "Singijeon Scout Rocket", false, false);
                ScoutProjectileCount++;
            }
        }

        private void LaunchFocus(in WeaponExecutionContext context)
        {
            volleyKinds.Add("focus"); FocusProjectileCount = 8; LastLaunchCount = FocusProjectileCount; focusLaunchIndex = 0; nextFocusLaunch = 0f; focusSequenceActive = true; focusRetargeted = false;
            var none = 0f; AdvanceFocusSequence(ref none, context);
        }

        private void AdvanceFocusSequence(ref float available, in WeaponExecutionContext context)
        {
            if (!focusSequenceActive) return;
            while (focusLaunchIndex < FocusProjectileCount && available + .00001f >= nextFocusLaunch)
            {
                available -= nextFocusLaunch; nextFocusLaunch = .05f;
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
            if (focus) { focusAttackIds.Add(attack.InstanceId); focusDirections[attack.InstanceId] = direction; } if (child) { childAttackIds.Add(attack.InstanceId); splitChildAttackIds.Add(attack.InstanceId); }
            var lifetime = (child ? Range * .55f : Range) / Speed;
            projectiles.Launch(context, new LinearProjectileSpec(attack, WeaponId.SingijeonVolley, position, direction, Speed, lifetime, Mathf.CeilToInt(child ? BaseDamage * .35f : BaseDamage), 1, name));
        }

        private static Float2 Normalize(Float2 value)
        {
            var length = Mathf.Sqrt(value.X * value.X + value.Y * value.Y);
            return length < 0.0001f ? new Float2(1f, 0f) : new Float2(value.X / length, value.Y / length);
        }

        private void Launch(in WeaponExecutionContext context, Float2 direction)
        {
            LastDirection = direction;
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
                projectiles.Launch(context, new LinearProjectileSpec(
                    new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f), WeaponId.SingijeonVolley,
                    position, direction, Speed, Range / Speed, Mathf.CeilToInt(BaseDamage), 1, "Singijeon Rocket"));
            }
        }

        private void OnDamageConfirmed(ConfirmedDamageEvent damage)
        {
            if (!damage.WeaponId.Equals(WeaponId.SingijeonVolley) || damage.Phase != ContactPhase.Direct || !focusAttackIds.Contains(damage.AttackInstanceId) || childAttackIds.Contains(damage.AttackInstanceId)) return;
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
        private void OnProjectileTravelled(LinearProjectileTravel travel)
        {
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
        private sealed class Trail { public Float2 Position; public PixelHitMask Mask; public float Remaining; public AttackInstance Attack; public HashSet<int> Crossed { get; } = new HashSet<int>(); public Dictionary<int, TrailTicks> TicksByTarget { get; } = new Dictionary<int, TrailTicks>(); public Dictionary<int, PixelMaskTransform> PreviousTransforms { get; } = new Dictionary<int, PixelMaskTransform>(); }
        private struct TrailTicks { public TrailTicks(Float2 contact) { Contact = contact; Elapsed = 0f; Count = 0; } public Float2 Contact; public float Elapsed; public int Count; }
    }
}
