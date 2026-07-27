using System;
using JoseonHunter.Domain.Geumjul;

namespace JoseonHunter.Runtime.Combat
{
    public readonly struct PixelMaskTransform
    {
        public PixelMaskTransform(Float2 position, int rotationDegrees = 0, bool flipX = false, int scale = 1)
        {
            if (scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
            Position = position; RotationDegrees = rotationDegrees; FlipX = flipX; Scale = scale;
        }
        public Float2 Position { get; }
        public int RotationDegrees { get; }
        public bool FlipX { get; }
        public int Scale { get; }
        public static PixelMaskTransform Identity => new PixelMaskTransform(new Float2(0f, 0f));
        public static PixelMaskTransform Translation(float x, float y) => new PixelMaskTransform(new Float2(x, y));
    }

    public static class PixelMaskContactService
    {
        public static bool TryFindContact(PixelHitMask attack, PixelMaskTransform attackTransform, PixelHitMask enemy, PixelMaskTransform enemyTransform, out Float2 point)
        {
            if (attack == null) throw new ArgumentNullException(nameof(attack));
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));
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

        private static Float2 ToWorld(PixelHitMask mask, PixelMaskTransform transform, int x, int y)
        {
            var localX = (x - mask.PivotPixel.x) * transform.Scale / mask.PixelsPerUnit;
            var localY = (y - mask.PivotPixel.y) * transform.Scale / mask.PixelsPerUnit;
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
            var sourceX = RoundToNearest(x * mask.PixelsPerUnit / transform.Scale + mask.PivotPixel.x);
            var sourceY = RoundToNearest(y * mask.PixelsPerUnit / transform.Scale + mask.PivotPixel.y);
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
    }
}
