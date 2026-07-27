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
    public sealed class JangseungWardExecutor : IWeaponExecutor
    {
        public const int MaximumWardSets = 4;
        public const float MobileRepositionInterval = 0.35f;
        public const float MaximumMobileStep = 0.75f;
        private const float BoundaryThickness = 0.12f;
        private readonly WeaponRuntimeController runtime;
        private readonly PixelHitMask segmentMask;
        private readonly Dictionary<int, PixelHitMask> stretchedSegmentMasks = new Dictionary<int, PixelHitMask>();
        private readonly List<ICombatTarget> targets = new List<ICombatTarget>();
        private readonly List<WardSet> sets = new List<WardSet>();
        private readonly Dictionary<int, Float2> previousPositions = new Dictionary<int, Float2>();
        private float cooldown;
        private float elapsedSeconds;

        public JangseungWardExecutor(WeaponRuntimeController runtime, PixelHitMask wardSegmentMask, float baseDamage, float cooldownSeconds, float radius, int postCount, int setCapacity, float reentryInterval, int level)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            segmentMask = wardSegmentMask ?? throw new ArgumentNullException(nameof(wardSegmentMask));
            BaseDamage = Mathf.Max(1f, baseDamage); CooldownSeconds = Mathf.Max(0.01f, cooldownSeconds); Radius = Mathf.Max(0.05f, radius);
            PostCount = Mathf.Clamp(postCount, 2, 4); SetCapacity = Mathf.Clamp(setCapacity, 1, MaximumWardSets);
            ReentryInterval = Mathf.Max(0f, reentryInterval); Level = Mathf.Clamp(level, 1, 5);
        }

        public JangseungWardExecutor(WeaponRuntimeController runtime, float baseDamage, float cooldownSeconds, float radius, int postCount, int setCapacity, float reentryInterval, int level)
            : this(runtime, PixelHitMask.FromRows("111", "111", "111"), baseDamage, cooldownSeconds, radius, postCount, setCapacity, reentryInterval, level) { }

        public float BaseDamage { get; }
        public float CooldownSeconds { get; }
        public float Radius { get; }
        public int PostCount { get; }
        public int SetCapacity { get; }
        public float ReentryInterval { get; }
        public int Level { get; }
        public int ActiveWardSetCount => sets.Count;
        public int ActivePostCount { get { var count = 0; foreach (var set in sets) count += set.Posts.Count; return count; } }
        public int EvictedWardSetCount { get; private set; }

        public void Tick(float deltaTime, in WeaponExecutionContext context)
        {
            var step = Mathf.Max(0f, deltaTime);
            elapsedSeconds += step;
            cooldown -= step;
            if (Level == 5 && sets.Count > 0) MoveMobilePosts(step, context.OwnerPosition);
            if (cooldown <= 0f)
            {
                cooldown = CooldownSeconds;
                if (Level == 5 && sets.Count > 0) RequestMobileReposition(context.OwnerPosition);
                else PlaceSet(context.OwnerPosition);
            }
            ResolveCrossings(context);
            RememberCurrentTargetPositions();
        }

        public void Reset()
        {
            foreach (var set in sets) Retire(set);
            sets.Clear(); previousPositions.Clear(); stretchedSegmentMasks.Clear(); cooldown = 0f; elapsedSeconds = 0f; EvictedWardSetCount = 0;
        }

        private void PlaceSet(Float2 center)
        {
            if (sets.Count >= SetCapacity)
            {
                Retire(sets[0]); sets.RemoveAt(0); EvictedWardSetCount++;
            }
            var count = Level == 5 ? 4 : PostCount;
            var set = new WardSet(new AttackInstance(runtime.AllocateAttackInstanceId(), RepeatHitPolicy.BoundaryReentry, ReentryInterval), center, Radius, count);
            sets.Add(set);
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

        private void ResolveCrossings(in WeaponExecutionContext context)
        {
            runtime.Targets.CopyTo(targets);
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.HurtMask == null) continue;
                var current = target.WorldPosition;
                if (!previousPositions.TryGetValue(target.RuntimeId, out var previous)) previous = current;
                foreach (var set in sets) ResolveTargetAgainstSet(target, previous, current, set, context);
            }
        }

        private void ResolveTargetAgainstSet(ICombatTarget target, Float2 previous, Float2 current, WardSet set, in WeaponExecutionContext context)
        {
            foreach (var segment in Segments(set))
            {
                if (!TrySegmentIntersection(previous, current, segment.Start, segment.End, out var movementT)) continue;
                if (set.TouchingTargetIds.Contains(target.RuntimeId)) continue;
                var atCrossing = Lerp(previous, current, movementT);
                if (!TryConfirmPixelContact(segment, target, atCrossing, out var contact)) continue;
                // Mark the contact before damage so a rejected re-entry interval cannot turn into repeated line damage.
                set.TouchingTargetIds.Add(target.RuntimeId);
                if (runtime.DamageService.TryApply(WeaponDamageRequest.Create(set.Attack, WeaponId.JangseungWard, target,
                    Mathf.CeilToInt(BaseDamage), false, contact, ContactPhase.BoundaryCrossing, context.SimulationTick, elapsedSeconds), out _))
                {
                    target.ApplyKnockback(OutwardDirection(segment, previous, current), Mathf.Max(0.1f, Level * 0.2f));
                    if (target is IJangseungWardStatusTarget status)
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
            var transform = new PixelMaskTransform(midpoint, degrees, false, Vector2.one);
            var hurt = target.HurtMaskTransform;
            var offset = new Float2(hurt.Position.X - target.WorldPosition.X, hurt.Position.Y - target.WorldPosition.Y);
            var targetTransform = new PixelMaskTransform(new Float2(targetAtCrossing.X + offset.X, targetAtCrossing.Y + offset.Y), hurt.RotationDegrees, hurt.FlipX, hurt.Scale);
            return PixelMaskContactService.TryFindContact(mask, transform, target.HurtMask, targetTransform, out contact);
        }

        private PixelHitMask StretchedMask(float length)
        {
            var pixelsPerUnit = segmentMask.PixelsPerUnit;
            var width = Mathf.Max(1, Mathf.CeilToInt(length * pixelsPerUnit));
            var height = Mathf.Max(1, Mathf.CeilToInt(BoundaryThickness * pixelsPerUnit));
            var key = width * 4099 + height;
            if (stretchedSegmentMasks.TryGetValue(key, out var cached)) return cached;
            var packed = new uint[(width * height + 31) / 32];
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var sourceX = width == 1 ? 0 : Mathf.RoundToInt(x * (segmentMask.Width - 1f) / (width - 1f));
                var sourceY = height == 1 ? 0 : Mathf.RoundToInt(y * (segmentMask.Height - 1f) / (height - 1f));
                if (!segmentMask.IsActive(sourceX, sourceY)) continue;
                var bit = y * width + x; packed[bit >> 5] |= 1u << (bit & 31);
            }
            cached = new PixelHitMask(width, height, new Vector2(width * 0.5f, height * 0.5f), pixelsPerUnit, packed);
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

        private readonly struct Segment { public Segment(Float2 start, Float2 end) { Start = start; End = end; } public Float2 Start { get; } public Float2 End { get; } }
        private sealed class WardSet
        {
            public WardSet(AttackInstance attack, Float2 center, float radius, int count)
            {
                Attack = attack; DesiredCenter = center;
                for (var index = 0; index < count; index++) Posts.Add(CardinalPost(center, radius, CardinalIndex(count, index)));
            }
            public AttackInstance Attack { get; }
            public List<Float2> Posts { get; } = new List<Float2>();
            public HashSet<int> TouchingTargetIds { get; } = new HashSet<int>();
            public HashSet<int> StatusTargetIds { get; } = new HashSet<int>();
            public Float2 DesiredCenter { get; set; }
            public float MobileElapsed { get; set; }
            public bool HasRequestedMove { get; set; }
            public bool Retired { get; set; }
            private static int CardinalIndex(int count, int index) => count == 2 ? index * 2 : index;
        }
    }
}
