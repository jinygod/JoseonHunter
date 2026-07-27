using System;
using UnityEngine;

namespace JoseonHunter.Runtime.Combat
{
    /// <summary>Immutable binary sprite mask. Bit zero is the lower-left source pixel.</summary>
    public sealed class PixelHitMask
    {
        private readonly uint[] bits;

        public PixelHitMask(int width, int height, Vector2 pivotPixel, float pixelsPerUnit, uint[] packedBits)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (pixelsPerUnit <= 0f || float.IsNaN(pixelsPerUnit) || float.IsInfinity(pixelsPerUnit)) throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
            if (packedBits == null) throw new ArgumentNullException(nameof(packedBits));
            if (packedBits.Length != (width * height + 31) / 32) throw new ArgumentException("Packed bit count does not match mask dimensions.", nameof(packedBits));

            Width = width;
            Height = height;
            PivotPixel = pivotPixel;
            PixelsPerUnit = pixelsPerUnit;
            bits = (uint[])packedBits.Clone();
        }

        public int Width { get; }
        public int Height { get; }
        public Vector2 PivotPixel { get; }
        public float PixelsPerUnit { get; }

        public bool IsActive(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
            var index = y * Width + x;
            return (bits[index >> 5] & (1u << (index & 31))) != 0;
        }

        public static PixelHitMask FromTexture(Texture2D texture, Vector2 pivotPixel, float pixelsPerUnit)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            var pixels = texture.GetPixels32();
            var packed = new uint[(pixels.Length + 31) / 32];
            for (var index = 0; index < pixels.Length; index++)
                if (pixels[index].a == byte.MaxValue) packed[index >> 5] |= 1u << (index & 31);
            return new PixelHitMask(texture.width, texture.height, pivotPixel, pixelsPerUnit, packed);
        }

        /// <summary>Builds a mask in the same texture rectangle, pivot, and pixels-per-unit used by a SpriteRenderer.</summary>
        public static PixelHitMask FromSprite(Sprite sprite)
        {
            if (sprite == null) throw new ArgumentNullException(nameof(sprite));
            var rect = sprite.textureRect;
            var width = Mathf.RoundToInt(rect.width);
            var height = Mathf.RoundToInt(rect.height);
            var texturePixels = sprite.texture.GetPixels32();
            var packed = new uint[(width * height + 31) / 32];
            var textureWidth = sprite.texture.width;
            var startX = Mathf.RoundToInt(rect.x);
            var startY = Mathf.RoundToInt(rect.y);
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var index = y * width + x;
                if (texturePixels[(startY + y) * textureWidth + startX + x].a != 0)
                    packed[index >> 5] |= 1u << (index & 31);
            }
            return new PixelHitMask(width, height, sprite.pivot, sprite.pixelsPerUnit, packed);
        }

        public static PixelHitMask OpaqueSpriteRect(Sprite sprite)
        {
            if (sprite == null) throw new ArgumentNullException(nameof(sprite));
            var rect = sprite.textureRect;
            var width = Mathf.RoundToInt(rect.width);
            var height = Mathf.RoundToInt(rect.height);
            var packed = new uint[(width * height + 31) / 32];
            for (var index = 0; index < width * height; index++) packed[index >> 5] |= 1u << (index & 31);
            return new PixelHitMask(width, height, sprite.pivot, sprite.pixelsPerUnit, packed);
        }

        public static PixelHitMask FromRows(params string[] rows)
        {
            if (rows == null || rows.Length == 0 || string.IsNullOrEmpty(rows[0])) throw new ArgumentException("At least one non-empty row is required.", nameof(rows));
            var width = rows[0].Length;
            var packed = new uint[(width * rows.Length + 31) / 32];
            for (var y = 0; y < rows.Length; y++)
            {
                if (rows[y] == null || rows[y].Length != width) throw new ArgumentException("All rows must have the same width.", nameof(rows));
                for (var x = 0; x < width; x++)
                {
                    if (rows[y][x] == '0') continue;
                    if (rows[y][x] != '1') throw new ArgumentException("Rows may contain only '0' and '1'.", nameof(rows));
                    var index = y * width + x;
                    packed[index >> 5] |= 1u << (index & 31);
                }
            }
            return new PixelHitMask(width, rows.Length, Vector2.zero, 1f, packed);
        }
    }
}
