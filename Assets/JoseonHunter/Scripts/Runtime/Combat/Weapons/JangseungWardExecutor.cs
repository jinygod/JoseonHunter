using System;
using System.Collections.Generic;
using JoseonHunter.Domain.Combat;
using JoseonHunter.Domain.Geumjul;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat.Weapons
{
    /// <summary>Optional target capability for the short ward debuff owned by one finite post set.</summary>
    public interface IJangseungWardStatusTarget
    {
        void ApplyJangseungWard(int sourceId, float strength);
        void RemoveJangseungWard(int sourceId);
    }

    /// <summary>Finite cardinal ward boundaries. Damage is only produced by a target movement segment crossing a visible segment.</summary>
    public sealed class JangseungWardExecutor : IWeaponExecutor, IWeaponEvolutionProfile
    {
        public const int MaximumWardSets = 4;
        public const float MobileRepositionInterval = 0.35f;
        public const float MaximumMobileStep = 0.75f;
        private const float BoundaryThickness = 0.12f;
        private const float EvolvedPostActivationInterval = 0.1f;
        private readonly WeaponRuntimeController runtime;
        private readonly PixelHitMask segmentMask;
        private readonly Dictionary<int, PixelHitMask> stretchedSegmentMasks = new Dictionary<int, PixelHitMask>();
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly List<WardSet> sets = new List<WardSet>();
        private readonly Dictionary<int, Float2> previousPositions = new Dictionary<int, Float2>();
        private float cooldown;
        private float elapsedSeconds;

        public JangseungWardExecutor(WeaponRuntimeController runtime, PixelHitMask wardSegmentMask, float baseDamage, float cooldownSeconds, float radius, int postCount, int setCapacity, float reentryInterval, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            segmentMask = wardSegmentMask ?? throw new ArgumentNullException(nameof(wardSegmentMask));
            BaseDamage = Mathf.Max(1f, modifiers.ScaleDamage(baseDamage)); CooldownSeconds = Mathf.Max(0.01f, modifiers.ScaleCooldown(cooldownSeconds)); Radius = Mathf.Max(0.05f, modifiers.ScaleArea(radius)); Potentials = modifiers;
            PostCount = Mathf.Clamp(postCount, 2, 4); SetCapacity = Mathf.Clamp(setCapacity, 1, MaximumWardSets);
            ReentryInterval = Mathf.Max(0f, reentryInterval); Level = Mathf.Clamp(level, 1, 5);
            IsEvolved = evolved;
        }

        public JangseungWardExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float radius, int postCount, int setCapacity, float reentryInterval, int level, bool evolved = false, WeaponRuntimeModifiers modifiers = default)
            : this(runtime, PixelHitMask.FromRows("111", "111", "111"), baseDamage, cooldownSeconds, radius, postCount, setCapacity, reentryInterval, level, evolved, modifiers) { }

        public float BaseDamage { get; }
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

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            var step = Mathf.Max(0f, deltaTime);
            var frameStartElapsed = elapsedSeconds;
            elapsedSeconds += step;
            cooldown -= step;
            if (Level == 5 && sets.Count > 0) MoveMobilePosts(step, context.OwnerPosition);
            if (cooldown <= 0f)
            {
                cooldown = CooldownSeconds;
                if (Level == 5 && sets.Count > 0) RequestMobileReposition(context.OwnerPosition);
                else PlaceSet(context.OwnerPosition);
            }
            if (IsEvolved) AdvanceEvolvedPostActivation(step);
            ResolveCrossings(context, frameStartElapsed, step);
            AdvancePotentialCompletions(step, context);
            RememberCurrentTargetPositions();
        }

        public void Reset()
        {
            foreach (var set in sets) Retire(set);
            sets.Clear(); previousPositions.Clear(); stretchedSegmentMasks.Clear(); cooldown = 0f; elapsedSeconds = 0f; EvictedWardSetCount = 0;
        }

        public void Dispose() => Reset();

        private void PlaceSet(Float2 center)
        {
            if (sets.Count >= SetCapacity)
            {
                Retire(sets[0]); sets.RemoveAt(0); EvictedWardSetCount++;
            }
            var count = Level == 5 ? 4 : PostCount;
            var set = new WardSet(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.BoundaryReentry, ReentryInterval), center, Radius, count, IsEvolved);
            sets.Add(set);
        }

        private void AdvanceEvolvedPostActivation(float step)
        {
            foreach (var set in sets)
            {
                if (!set.IsEvolved || set.IsCompleted) continue;
                set.ActivationElapsed += step;
                while (set.ActivationElapsed + 0.0001f >= EvolvedPostActivationInterval && !set.IsCompleted)
                {
                    set.ActivationElapsed -= EvolvedPostActivationInterval;
                    set.ActivateNextPost();
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
                set.PotentialTickStep = set.IsEvolved ? set.CompletionResidual : 0f;
                if (Potentials.HasPotential(WeaponPotentialId.JangseungFourDirectionBarrier) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.JangseungFourDirectionBarrier, out _, out var barrier))
                {
                    set.RotatingAttack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
                    set.RotationMask = barrier; set.RotationRemaining = .8f;
                }
                if (Potentials.HasPotential(WeaponPotentialId.JangseungGuardianDescent) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.JangseungGuardianDescent, out _, out var guardian))
                {
                    set.GuardianMask = guardian; set.GuardianRemaining = 1.2f; set.GuardianAttack = new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.OncePerInstance, 0f);
                    WeaponPotentialVisuals.TryGet(WeaponPotentialId.JangseungGuardianDescent, out var guardianSprite, out _);
                    set.GuardianVisual = new GameObject("Jangseung Guardian"); set.GuardianVisual.transform.SetParent(context.PresentationRoot, false); set.GuardianVisual.transform.position = new Vector3(set.DesiredCenter.X, set.DesiredCenter.Y, 0f); var renderer = set.GuardianVisual.AddComponent<SpriteRenderer>(); renderer.sprite = guardianSprite; renderer.sortingOrder = context.SortingOrder + 1;
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
                        if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(set.RotatingAttack, WeaponId.JangseungWard, target, Mathf.CeilToInt(BaseDamage * .7f), false, contact, ContactPhase.PotentialBlast, context.SimulationTick, elapsedSeconds - set.RotationRemaining), out _)) set.RotatedTargetIds.Add(target.RuntimeId);
                    }
                }
                if (set.RotationRemaining > 0f) continue;
                runtime.DamageService.RetireAttack(set.RotatingAttack.InstanceId); set.RotationMask = null;
            }
            foreach (var set in sets)
            {
                if (set.GuardianRemaining <= 0f || set.GuardianMask == null) continue;
                var potentialStep = set.PotentialTickStep > 0f ? set.PotentialTickStep : step;
                set.GuardianRemaining -= potentialStep; if (!set.GuardianResolved) ResolveGuardian(set, set.GuardianMask, context);
                if (set.GuardianRemaining > 0f) continue;
                runtime.DamageService.RetireAttack(set.GuardianAttack.InstanceId); set.GuardianMask = null;
                if (set.GuardianVisual != null) UnityEngine.Object.Destroy(set.GuardianVisual); set.GuardianVisual = null;
            }
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
            { if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(set.GuardianAttack, WeaponId.JangseungWard, best, Mathf.CeilToInt(BaseDamage * 1.1f), false, contact, ContactPhase.PotentialChain, context.SimulationTick, elapsedSeconds), out _)) set.GuardianResolved = true; }
            // Keep the authored guardian visible for the full lifetime after its one confirmed strike.
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
            }
            set.MarkResolved = true;
        }

        private void RequestMobileReposition(Float2 center)
        {
            var set = sets[sets.Count - 1];
            set.DesiredCenter = center;
            set.HasRequestedMove = true;
        }

        private void MoveMobilePosts(float step, Float2 ownerPosition)
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
            foreach (var segment in Segments(set))
            {
                if (!TrySegmentIntersection(previous, current, segment.Start, segment.End, out var movementT)) continue;
                if (set.TouchingTargetIds.Contains(target.RuntimeId)) continue;
                var atCrossing = Lerp(previous, current, movementT);
                if (!TryConfirmPixelContact(segment, target, atCrossing, out var contact)) continue;
                var crossingTime = frameStartElapsed + step * movementT;
                // Mark the contact before damage so a rejected re-entry interval cannot turn into repeated line damage.
                set.TouchingTargetIds.Add(target.RuntimeId);
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(set.Attack, WeaponId.JangseungWard, target,
                    Mathf.CeilToInt(BaseDamage), false, contact, ContactPhase.BoundaryCrossing, context.SimulationTick, crossingTime), out _))
                {
                    set.MarkedTargetIds.Add(target.RuntimeId);
                    target.ApplyKnockback(OutwardDirection(segment, previous, current), Mathf.Max(0.1f, Level * 0.2f));
                    if (Potentials.HasPotential(WeaponPotentialId.JangseungGhostFace) && WeaponPotentialVisuals.TryGet(WeaponPotentialId.JangseungGhostFace, out _, out var ghostMask) &&
                        PixelMaskContactService.TryFindContact(ghostMask, PixelMaskTransform.Translation(contact.X, contact.Y), target.HurtMask, target.HurtMaskTransform, out _))
                        target.ApplyKnockback(CenterOutward(set.DesiredCenter, atCrossing), 1.25f);
                    if (!set.IsEvolved && target is IJangseungWardStatusTarget status)
                    {
                        set.StatusTargetIds.Add(target.RuntimeId);
                        status.ApplyJangseungWard(set.Attack.InstanceId, Level * 0.1f);
                    }
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

        private void RememberCurrentTargetPositions()
        {
            previousPositions.Clear();
            foreach (var target in targets) if (target != null && target.IsAlive) previousPositions[target.RuntimeId] = target.WorldPosition;
        }

        private void Retire(WardSet set)
        {
            if (set.Retired) return;
            foreach (var targetId in set.StatusTargetIds)
                if (runtime.Targets.TryGet(targetId, out var target) && target is IJangseungWardStatusTarget status) status.RemoveJangseungWard(set.Attack.InstanceId);
            runtime.DamageService.RetireAttack(set.Attack.InstanceId);
            if (set.RotatingAttack != null) runtime.DamageService.RetireAttack(set.RotatingAttack.InstanceId);
            if (set.GuardianAttack != null) runtime.DamageService.RetireAttack(set.GuardianAttack.InstanceId);
            if (set.GuardianVisual != null) UnityEngine.Object.Destroy(set.GuardianVisual);
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
            public WardSet(AttackInstance attack, Float2 center, float radius, int count, bool evolved)
            {
                Attack = attack; DesiredCenter = center; Radius = radius; PostCount = count; IsEvolved = evolved;
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
            public GameObject GuardianVisual { get; set; }
            public float CompletionResidual { get; set; }
            public float PotentialTickStep { get; set; }
            public void ActivateNextPost()
            {
                if (Posts.Count < PostCount) Posts.Add(CardinalPost(DesiredCenter, Radius, CardinalIndex(PostCount, Posts.Count)));
            }
            private static int CardinalIndex(int count, int index) => count == 2 ? index * 2 : index;
        }
    }
}
