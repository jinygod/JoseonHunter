using System;
using UnityEngine;

namespace JoseonHunter.Runtime.Gameplay
{
    public static class ViewportSpawnGeometry
    {
        public static Vector2 PointOnExpandedPerimeter(Rect bounds, int side, float t, float margin)
        {
            t = Mathf.Clamp01(t);
            margin = Mathf.Max(0f, margin);
            return side switch
            {
                0 => new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, t), bounds.yMax + margin),
                1 => new Vector2(bounds.xMax + margin, Mathf.Lerp(bounds.yMin, bounds.yMax, t)),
                2 => new Vector2(Mathf.Lerp(bounds.xMax, bounds.xMin, t), bounds.yMin - margin),
                3 => new Vector2(bounds.xMin - margin, Mathf.Lerp(bounds.yMax, bounds.yMin, t)),
                _ => throw new ArgumentOutOfRangeException(nameof(side))
            };
        }
    }

    public static class BoundedSpawnGeometry
    {
        private const float DefaultMargin = 1.25f;
        private const float BoundaryInset = .01f;

        public static bool TrySelect(
            Vector2 player,
            Rect battlefieldBounds,
            Camera camera,
            float t,
            out Vector2 position)
        {
            var normalized = Mathf.Repeat(Mathf.Clamp01(t) * 4f, 4f);
            var preferredSide = Mathf.Clamp(Mathf.FloorToInt(normalized), 0, 3);
            var sideT = normalized - preferredSide;
            return TrySelect(player, battlefieldBounds, camera, preferredSide, sideT,
                DefaultMargin, out position);
        }

        public static bool TrySelect(
            Vector2 player,
            Rect battlefieldBounds,
            Camera camera,
            int preferredSide,
            float t,
            float margin,
            out Vector2 position)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (preferredSide < 0 || preferredSide > 3)
                throw new ArgumentOutOfRangeException(nameof(preferredSide));
            if (!IsFinite(player.x) || !IsFinite(player.y))
                throw new ArgumentOutOfRangeException(nameof(player));

            var bottomLeft = camera.ViewportToWorldPoint(Vector3.zero);
            var topRight = camera.ViewportToWorldPoint(Vector3.one);
            var viewport = Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
            var safeMargin = Mathf.Max(0f, margin);
            for (var offset = 0; offset < 4; offset++)
            {
                var side = (preferredSide + offset) & 3;
                var candidate = ViewportSpawnGeometry.PointOnExpandedPerimeter(
                    viewport, side, t, safeMargin);
                candidate.x = Mathf.Clamp(candidate.x,
                    battlefieldBounds.xMin + BoundaryInset, battlefieldBounds.xMax - BoundaryInset);
                candidate.y = Mathf.Clamp(candidate.y,
                    battlefieldBounds.yMin + BoundaryInset, battlefieldBounds.yMax - BoundaryInset);
                if (!ContainsInclusive(battlefieldBounds, candidate) || viewport.Contains(candidate)) continue;
                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        private static bool ContainsInclusive(Rect bounds, Vector2 point) =>
            point.x >= bounds.xMin && point.x <= bounds.xMax &&
            point.y >= bounds.yMin && point.y <= bounds.yMax;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
