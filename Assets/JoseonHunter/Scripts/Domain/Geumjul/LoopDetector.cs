using System;
using System.Collections.Generic;

namespace JoseonHunter.Domain.Geumjul
{
    public sealed class LoopDetector
    {
        private const float MinimumPerimeter = 2.5f;
        private const float MaximumBaseArea = (float)Math.PI * 3f * 3f;
        private const float Epsilon = 0.00001f;
        private readonly MasteryState mastery;

        public LoopDetector() : this(GeumjulMastery.ForClosures(0)) { }
        public LoopDetector(MasteryState mastery) => this.mastery = mastery ?? throw new ArgumentNullException(nameof(mastery));

        public LoopResult TryClose(IReadOnlyList<TrailPoint> points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count < 4 || HasNonFiniteData(points) || HasZeroLengthSegment(points)) return Invalid();

            var polygon = CloseNearFirstPoint(points) ?? CloseAtIntersection(points);
            if (polygon == null || !HasThreeUniqueVertices(polygon)) return Invalid();

            var perimeter = Perimeter(polygon);
            var area = Area(polygon);
            if (perimeter < MinimumPerimeter || area <= Epsilon || area > MaximumBaseArea * mastery.AreaMultiplier) return Invalid();
            return new LoopResult(true, polygon, perimeter, area);
        }

        private List<Float2> CloseNearFirstPoint(IReadOnlyList<TrailPoint> points)
        {
            var first = points[0].Position;
            var last = points[points.Count - 1].Position;
            var tolerance = mastery.ClosureTolerance;
            var dx = last.X - first.X;
            var dy = last.Y - first.Y;
            if (dx * dx + dy * dy > tolerance * tolerance) return null;

            var polygon = new List<Float2>();
            for (var index = 0; index < points.Count - 1; index++) polygon.Add(points[index].Position);
            if (SquaredDistance(first, last) > Epsilon * Epsilon) polygon.Add(last);
            return polygon;
        }

        private static List<Float2> CloseAtIntersection(IReadOnlyList<TrailPoint> points)
        {
            var finalStart = points[points.Count - 2].Position;
            var finalEnd = points[points.Count - 1].Position;
            var bestIndex = -1;
            var bestFinalFactor = float.PositiveInfinity;
            var bestIntersection = default(Float2);
            for (var index = 0; index < points.Count - 3; index++)
            {
                if (!TryIntersect(points[index].Position, points[index + 1].Position, finalStart, finalEnd, out var intersection, out var finalFactor)) continue;
                if (finalFactor < bestFinalFactor - Epsilon || (Math.Abs(finalFactor - bestFinalFactor) <= Epsilon && index > bestIndex))
                {
                    bestIndex = index;
                    bestFinalFactor = finalFactor;
                    bestIntersection = intersection;
                }
            }
            if (bestIndex < 0) return null;
            var polygon = new List<Float2> { bestIntersection };
            for (var vertex = bestIndex + 1; vertex <= points.Count - 2; vertex++) polygon.Add(points[vertex].Position);
            return polygon;
        }

        private static bool HasNonFiniteData(IReadOnlyList<TrailPoint> points)
        {
            for (var index = 0; index < points.Count; index++)
                if (!IsFinite(points[index].Position.X) || !IsFinite(points[index].Position.Y) || !IsFinite(points[index].Time)) return true;
            return false;
        }

        private static bool HasZeroLengthSegment(IReadOnlyList<TrailPoint> points)
        {
            for (var index = 1; index < points.Count; index++)
                if (SquaredDistance(points[index - 1].Position, points[index].Position) <= Epsilon * Epsilon) return true;
            return false;
        }

        private static bool HasThreeUniqueVertices(IReadOnlyList<Float2> polygon)
        {
            var unique = new List<Float2>();
            for (var index = 0; index < polygon.Count; index++)
            {
                var alreadyExists = false;
                for (var candidate = 0; candidate < unique.Count; candidate++) if (SquaredDistance(polygon[index], unique[candidate]) <= Epsilon * Epsilon) { alreadyExists = true; break; }
                if (!alreadyExists) unique.Add(polygon[index]);
            }
            return unique.Count >= 3;
        }

        private static float Perimeter(IReadOnlyList<Float2> polygon)
        {
            var perimeter = 0f;
            for (var index = 0; index < polygon.Count; index++) perimeter += GeumjulTrail.Distance(polygon[index], polygon[(index + 1) % polygon.Count]);
            return perimeter;
        }

        private static float Area(IReadOnlyList<Float2> polygon)
        {
            var twiceArea = 0f;
            for (var index = 0; index < polygon.Count; index++)
            {
                var next = polygon[(index + 1) % polygon.Count];
                twiceArea += polygon[index].X * next.Y - next.X * polygon[index].Y;
            }
            return Math.Abs(twiceArea) * 0.5f;
        }

        private static float SquaredDistance(Float2 first, Float2 second)
        {
            var dx = first.X - second.X;
            var dy = first.Y - second.Y;
            return dx * dx + dy * dy;
        }

        private static bool TryIntersect(Float2 firstStart, Float2 firstEnd, Float2 secondStart, Float2 secondEnd, out Float2 intersection, out float secondFactor)
        {
            var firstX = firstEnd.X - firstStart.X;
            var firstY = firstEnd.Y - firstStart.Y;
            var secondX = secondEnd.X - secondStart.X;
            var secondY = secondEnd.Y - secondStart.Y;
            var denominator = firstX * secondY - firstY * secondX;
            if (Math.Abs(denominator) <= Epsilon) { intersection = default; secondFactor = 0f; return false; }
            var relativeX = secondStart.X - firstStart.X;
            var relativeY = secondStart.Y - firstStart.Y;
            var firstFactor = (relativeX * secondY - relativeY * secondX) / denominator;
            secondFactor = (relativeX * firstY - relativeY * firstX) / denominator;
            if (firstFactor < -Epsilon || firstFactor > 1f + Epsilon || secondFactor < -Epsilon || secondFactor > 1f + Epsilon) { intersection = default; return false; }
            intersection = new Float2(firstStart.X + firstFactor * firstX, firstStart.Y + firstFactor * firstY);
            return true;
        }

        private static LoopResult Invalid() => new LoopResult(false, Array.Empty<Float2>(), 0f, 0f);
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
