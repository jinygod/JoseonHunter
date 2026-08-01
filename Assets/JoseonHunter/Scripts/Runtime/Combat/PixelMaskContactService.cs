using System;
using JoseonHunter.Domain.Geumjul;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat
{
    public readonly struct PixelMaskTransform
    {
        public PixelMaskTransform(Float2 position, int rotationDegrees = 0, bool flipX = false, int scale = 1)
            : this(position, rotationDegrees, flipX, new Vector2(scale, scale))
        {
        }

        public PixelMaskTransform(Float2 position, int rotationDegrees, bool flipX, Vector2 scale)
        {
            if (scale.x <= 0f || scale.y <= 0f || float.IsNaN(scale.x) || float.IsNaN(scale.y) ||
                float.IsInfinity(scale.x) || float.IsInfinity(scale.y)) throw new ArgumentOutOfRangeException(nameof(scale));
            Position = position; RotationDegrees = rotationDegrees; FlipX = flipX; Scale = scale;
        }
        public Float2 Position { get; }
        public int RotationDegrees { get; }
        public bool FlipX { get; }
        public Vector2 Scale { get; }
        public static PixelMaskTransform Identity => new PixelMaskTransform(new Float2(0f, 0f));
        public static PixelMaskTransform Translation(float x, float y) => new PixelMaskTransform(new Float2(x, y));
    }

    public static class PixelMaskContactService
    {
        public static bool TryFindContact(PixelHitMask attack, PixelMaskTransform attackTransform, PixelHitMask enemy, PixelMaskTransform enemyTransform, out Float2 point)
        {
            if (attack == null) throw new ArgumentNullException(nameof(attack));
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
            if (!attack.HasActivePixels || !enemy.HasActivePixels ||
                !ActiveBoundsOverlap(attack, attackTransform, enemy, enemyTransform))
            {
                point = default;
                return false;
            }
            for (var y = 0; y < attack.Height; y++)
            for (var x = 0; x < attack.Width; x++)
            {
                if (!attack.IsActive(x, y)) continue;
                var candidate = ToWorld(attack, attackTransform, x, y);
                if (!ContainsWorldPixel(enemy, enemyTransform, candidate)) continue;
                point = candidate;
                return true;
            }
            point = default;
            return false;
        }

        private static bool ActiveBoundsOverlap(
            PixelHitMask first,
            PixelMaskTransform firstTransform,
            PixelHitMask second,
            PixelMaskTransform secondTransform)
        {
            var firstBounds = WorldBounds(first, firstTransform);
            var secondBounds = WorldBounds(second, secondTransform);
            return firstBounds.Overlaps(secondBounds);
        }

        internal static bool ActiveBoundsOverlapSwept(
            PixelHitMask moving,
            PixelMaskTransform from,
            PixelMaskTransform to,
            PixelHitMask target,
            PixelMaskTransform targetTransform)
        {
            if (moving == null) throw new ArgumentNullException(nameof(moving));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (!moving.HasActivePixels || !target.HasActivePixels) return false;
            var sweptBounds = WorldBounds(moving, from);
            sweptBounds.Include(WorldBounds(moving, to));
            return sweptBounds.Overlaps(WorldBounds(target, targetTransform));
        }

        private static MaskWorldBounds WorldBounds(PixelHitMask mask, PixelMaskTransform transform)
        {
            var minimumLocalX = (mask.ActiveMinimumX - mask.PivotPixel.x - .5f) *
                                transform.Scale.x / mask.PixelsPerUnit;
            var maximumLocalX = (mask.ActiveMaximumX - mask.PivotPixel.x + .5f) *
                                transform.Scale.x / mask.PixelsPerUnit;
            var minimumLocalY = (mask.ActiveMinimumY - mask.PivotPixel.y - .5f) *
                                transform.Scale.y / mask.PixelsPerUnit;
            var maximumLocalY = (mask.ActiveMaximumY - mask.PivotPixel.y + .5f) *
                                transform.Scale.y / mask.PixelsPerUnit;
            if (transform.FlipX)
            {
                var flippedMinimum = -maximumLocalX;
                maximumLocalX = -minimumLocalX;
                minimumLocalX = flippedMinimum;
            }

            var result = MaskWorldBounds.Empty;
            IncludeCorner(ref result, transform, minimumLocalX, minimumLocalY);
            IncludeCorner(ref result, transform, minimumLocalX, maximumLocalY);
            IncludeCorner(ref result, transform, maximumLocalX, minimumLocalY);
            IncludeCorner(ref result, transform, maximumLocalX, maximumLocalY);
            return result;
        }

        private static void IncludeCorner(
            ref MaskWorldBounds bounds,
            PixelMaskTransform transform,
            float localX,
            float localY)
        {
            Rotate(localX, localY, transform.RotationDegrees, out var rotatedX, out var rotatedY);
            bounds.Include(transform.Position.X + rotatedX, transform.Position.Y + rotatedY);
        }

        private static Float2 ToWorld(PixelHitMask mask, PixelMaskTransform transform, int x, int y)
        {
            var localX = (x - mask.PivotPixel.x) * transform.Scale.x / mask.PixelsPerUnit;
            var localY = (y - mask.PivotPixel.y) * transform.Scale.y / mask.PixelsPerUnit;
            if (transform.FlipX) localX = -localX;
            Rotate(localX, localY, transform.RotationDegrees, out var rotatedX, out var rotatedY);
            return new Float2(transform.Position.X + rotatedX, transform.Position.Y + rotatedY);
        }

        private static bool ContainsWorldPixel(PixelHitMask mask, PixelMaskTransform transform, Float2 world)
        {
            var x = world.X - transform.Position.X;
            var y = world.Y - transform.Position.Y;
            Rotate(x, y, -transform.RotationDegrees, out x, out y);
            if (transform.FlipX) x = -x;
            var sourceX = RoundToNearest(x * mask.PixelsPerUnit / transform.Scale.x + mask.PivotPixel.x);
            var sourceY = RoundToNearest(y * mask.PixelsPerUnit / transform.Scale.y + mask.PivotPixel.y);
            // Rounding here is the deterministic nearest-neighbor fallback for non-quarter rotations.
            return mask.IsActive(sourceX, sourceY);
        }

        private static int RoundToNearest(float value) => value >= 0f ? (int)Math.Floor(value + 0.5f) : (int)Math.Ceiling(value - 0.5f);
        private static void Rotate(float x, float y, int degrees, out float rotatedX, out float rotatedY)
        {
            var normalized = ((degrees % 360) + 360) % 360;
            switch (normalized)
            {
                case 0: rotatedX = x; rotatedY = y; return;
                case 90: rotatedX = -y; rotatedY = x; return;
                case 180: rotatedX = -x; rotatedY = -y; return;
                case 270: rotatedX = y; rotatedY = -x; return;
                default:
                    var radians = degrees * Math.PI / 180d;
                    var cosine = (float)Math.Cos(radians); var sine = (float)Math.Sin(radians);
                    rotatedX = x * cosine - y * sine; rotatedY = x * sine + y * cosine; return;
            }
        }

        private struct MaskWorldBounds
        {
            public float MinimumX;
            public float MinimumY;
            public float MaximumX;
            public float MaximumY;

            public static MaskWorldBounds Empty => new MaskWorldBounds
            {
                MinimumX = float.PositiveInfinity,
                MinimumY = float.PositiveInfinity,
                MaximumX = float.NegativeInfinity,
                MaximumY = float.NegativeInfinity
            };

            public void Include(float x, float y)
            {
                MinimumX = Math.Min(MinimumX, x);
                MinimumY = Math.Min(MinimumY, y);
                MaximumX = Math.Max(MaximumX, x);
                MaximumY = Math.Max(MaximumY, y);
            }

            public void Include(MaskWorldBounds other)
            {
                MinimumX = Math.Min(MinimumX, other.MinimumX);
                MinimumY = Math.Min(MinimumY, other.MinimumY);
                MaximumX = Math.Max(MaximumX, other.MaximumX);
                MaximumY = Math.Max(MaximumY, other.MaximumY);
            }

            public bool Overlaps(MaskWorldBounds other) =>
                MinimumX <= other.MaximumX && MaximumX >= other.MinimumX &&
                MinimumY <= other.MaximumY && MaximumY >= other.MinimumY;
        }
    }
}
