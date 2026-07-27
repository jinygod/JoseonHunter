using System;
using System.Collections.Generic;

namespace JoseonHunter.Domain.Geumjul
{
    public readonly struct SealHit : IEquatable<SealHit>
    {
        public SealHit(int targetId, int damage, float bindSeconds, SealBranch branch) { TargetId = targetId; Damage = damage; BindSeconds = bindSeconds; Branch = branch; }
        public int TargetId { get; }
        public int Damage { get; }
        public float BindSeconds { get; }
        public SealBranch Branch { get; }
        public bool Equals(SealHit other) => TargetId == other.TargetId && Damage == other.Damage && BindSeconds.Equals(other.BindSeconds) && Branch == other.Branch;
        public override bool Equals(object obj) => obj is SealHit other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(TargetId, Damage, BindSeconds, Branch);
    }

    public sealed class SealResolver
    {
        private const float EdgeTolerance = 0.00001f;
        private readonly MasteryState mastery;
        public SealResolver() : this(GeumjulMastery.ForClosures(0)) { }
        public SealResolver(MasteryState mastery) => this.mastery = mastery ?? throw new ArgumentNullException(nameof(mastery));

        public IReadOnlyList<SealHit> Resolve(LoopResult loop, IReadOnlyList<TargetPoint> targets)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (mastery.RequiresBranchChoice) throw new InvalidOperationException("A Fire Mark or Ice Bind branch must be selected before resolving seals.");
            var hits = new List<SealHit>();
            if (!loop.IsValid) return hits.AsReadOnly();
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                if (!IsFinite(target.Position.X) || !IsFinite(target.Position.Y)) throw new ArgumentException("Target positions must be finite.", nameof(targets));
                if (!IsStrictlyInside(loop.Polygon, target.Position)) continue;
                var damage = target.IsBoss ? mastery.BaseDamage * 35 / 100 : mastery.BaseDamage;
                hits.Add(new SealHit(target.TargetId, damage, target.IsBoss ? 0f : 1.2f, mastery.ActiveBranch));
            }
            hits.Sort((first, second) => first.TargetId.CompareTo(second.TargetId));
            return hits.AsReadOnly();
        }

        private static bool IsStrictlyInside(IReadOnlyList<Float2> polygon, Float2 point)
        {
            var inside = false;
            for (int index = 0, previous = polygon.Count - 1; index < polygon.Count; previous = index++)
            {
                var first = polygon[previous];
                var second = polygon[index];
                if (OnSegment(first, second, point)) return false;
                var crosses = (first.Y > point.Y) != (second.Y > point.Y);
                if (crosses && point.X < (second.X - first.X) * (point.Y - first.Y) / (second.Y - first.Y) + first.X) inside = !inside;
            }
            return inside;
        }

        private static bool OnSegment(Float2 first, Float2 second, Float2 point)
        {
            var cross = (second.X - first.X) * (point.Y - first.Y) - (second.Y - first.Y) * (point.X - first.X);
            if (Math.Abs(cross) > EdgeTolerance) return false;
            return point.X >= Math.Min(first.X, second.X) - EdgeTolerance && point.X <= Math.Max(first.X, second.X) + EdgeTolerance && point.Y >= Math.Min(first.Y, second.Y) - EdgeTolerance && point.Y <= Math.Max(first.Y, second.Y) + EdgeTolerance;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
