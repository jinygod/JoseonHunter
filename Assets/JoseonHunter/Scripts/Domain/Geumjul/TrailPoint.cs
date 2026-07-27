using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JoseonHunter.Domain.Geumjul
{
    public readonly struct Float2 : IEquatable<Float2>
    {
        public Float2(float x, float y) { X = x; Y = y; }
        public float X { get; }
        public float Y { get; }
        public bool Equals(Float2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is Float2 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(Float2 left, Float2 right) => left.Equals(right);
        public static bool operator !=(Float2 left, Float2 right) => !left.Equals(right);
    }

    public readonly struct TrailPoint : IEquatable<TrailPoint>
    {
        public TrailPoint(Float2 position, float time) { Position = position; Time = time; }
        public Float2 Position { get; }
        public float Time { get; }
        public bool Equals(TrailPoint other) => Position.Equals(other.Position) && Time.Equals(other.Time);
        public override bool Equals(object obj) => obj is TrailPoint other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Position, Time);
    }

    public readonly struct TargetPoint : IEquatable<TargetPoint>
    {
        public TargetPoint(int targetId, Float2 position, bool isBoss) { TargetId = targetId; Position = position; IsBoss = isBoss; }
        public int TargetId { get; }
        public Float2 Position { get; }
        public bool IsBoss { get; }
        public bool Equals(TargetPoint other) => TargetId == other.TargetId && Position.Equals(other.Position) && IsBoss == other.IsBoss;
        public override bool Equals(object obj) => obj is TargetPoint other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(TargetId, Position, IsBoss);
    }

    public readonly struct LoopResult : IEquatable<LoopResult>
    {
        public LoopResult(bool isValid, IReadOnlyList<Float2> polygon, float perimeter, float area)
        {
            IsValid = isValid;
            Polygon = new ReadOnlyCollection<Float2>(new List<Float2>(polygon ?? throw new ArgumentNullException(nameof(polygon))));
            Perimeter = perimeter;
            Area = area;
        }

        public bool IsValid { get; }
        public IReadOnlyList<Float2> Polygon { get; }
        public float Perimeter { get; }
        public float Area { get; }
        public bool Equals(LoopResult other) => IsValid == other.IsValid && Perimeter.Equals(other.Perimeter) && Area.Equals(other.Area) && SamePolygon(Polygon, other.Polygon);
        public override bool Equals(object obj) => obj is LoopResult other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(IsValid, Perimeter, Area);

        private static bool SamePolygon(IReadOnlyList<Float2> first, IReadOnlyList<Float2> second)
        {
            if (ReferenceEquals(first, second)) return true;
            if (first == null || second == null || first.Count != second.Count) return false;
            for (var index = 0; index < first.Count; index++) if (first[index] != second[index]) return false;
            return true;
        }
    }

    public enum SealBranch { None, FireMark, IceBind, FiveColorBarrier }
}
