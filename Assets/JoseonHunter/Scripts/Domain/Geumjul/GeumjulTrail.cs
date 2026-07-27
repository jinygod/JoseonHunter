using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JoseonHunter.Domain.Geumjul
{
    public sealed class GeumjulTrail
    {
        private const float LifetimeSeconds = 4f;
        private readonly List<TrailPoint> points = new List<TrailPoint>();
        private readonly MasteryState mastery;
        private readonly ReadOnlyCollection<TrailPoint> readOnlyPoints;

        public GeumjulTrail() : this(GeumjulMastery.ForClosures(0)) { }
        public GeumjulTrail(MasteryState mastery)
        {
            this.mastery = mastery ?? throw new ArgumentNullException(nameof(mastery));
            readOnlyPoints = points.AsReadOnly();
        }

        public IReadOnlyList<TrailPoint> Points => readOnlyPoints;
        public float Length { get; private set; }

        public void Add(TrailPoint point)
        {
            if (!IsFinite(point.Position.X) || !IsFinite(point.Position.Y) || !IsFinite(point.Time)) throw new ArgumentException("Trail points must contain finite coordinates and time.", nameof(point));
            if (points.Count > 0 && point.Time < points[points.Count - 1].Time) throw new ArgumentException("Trail point time cannot move backwards.", nameof(point));
            points.Add(point);
            TrimExpired(point.Time);
            TrimLength();
            RecalculateLength();
        }

        private void TrimExpired(float currentTime)
        {
            var cutoff = currentTime - LifetimeSeconds;
            while (points.Count > 1 && points[0].Time < cutoff)
            {
                var first = points[0];
                var second = points[1];
                if (second.Time > cutoff)
                {
                    var factor = (cutoff - first.Time) / (second.Time - first.Time);
                    points[0] = new TrailPoint(Lerp(first.Position, second.Position, factor), cutoff);
                    return;
                }
                points.RemoveAt(0);
            }
        }

        private void TrimLength()
        {
            var maxLength = mastery.MaxTrailLength;
            while (points.Count > 1 && CalculateLength() > maxLength)
            {
                var first = points[0];
                var second = points[1];
                var segmentLength = Distance(first.Position, second.Position);
                var excess = CalculateLength() - maxLength;
                if (segmentLength <= 0f) { points.RemoveAt(0); continue; }
                if (excess >= segmentLength) { points.RemoveAt(0); continue; }

                var factor = excess / segmentLength;
                points[0] = new TrailPoint(Lerp(first.Position, second.Position, factor), first.Time + (second.Time - first.Time) * factor);
                break;
            }
        }

        private void RecalculateLength() => Length = CalculateLength();
        private float CalculateLength()
        {
            var length = 0f;
            for (var index = 1; index < points.Count; index++) length += Distance(points[index - 1].Position, points[index].Position);
            return length;
        }

        internal static float Distance(Float2 first, Float2 second)
        {
            var x = first.X - second.X;
            var y = first.Y - second.Y;
            return (float)Math.Sqrt(x * x + y * y);
        }

        private static Float2 Lerp(Float2 first, Float2 second, float factor) => new Float2(first.X + (second.X - first.X) * factor, first.Y + (second.Y - first.Y) * factor);
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
