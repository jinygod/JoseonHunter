using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit { }
}

namespace JoseonHunter.Editor.AssetProduction
{
    public sealed record CharacterSheetValidationResult(
        IReadOnlyList<string> Errors,
        Vector2Int CellSize,
        Vector2Int FootAnchor,
        Vector2 Pivot,
        int FrameCount);

    public static class CharacterSheetContract
    {
        private const int SheetWidth = 384;
        private const int SheetHeight = 448;
        private const int Frames = 38;

        public static CharacterSheetValidationResult Validate(string sourceRoot, string runtimePath)
        {
            var errors = new List<string>();
            var manifestPath = Path.Combine(sourceRoot ?? string.Empty, "manifest.json");
            var manifest = ReadManifest(manifestPath, errors);
            if (manifest == null)
                return Result(errors, Vector2Int.zero, Vector2Int.zero, Vector2.zero, 0);

            var cellSize = ToVector2Int(manifest.cellSize);
            var footAnchor = ToVector2Int(manifest.footAnchor);
            var pivot = ToVector2(manifest.pivot);
            var frameCount = FrameCount(manifest.animations);
            ValidateManifest(manifest, cellSize, footAnchor, pivot, frameCount, errors);

            var palette = ReadPalette(Path.Combine(sourceRoot, "palette.png"), errors);
            foreach (var layer in manifest.layers ?? Array.Empty<string>())
                ValidateLayer(Path.Combine(sourceRoot, "layers", layer + ".png"), layer, palette, errors);

            return Result(errors, cellSize, footAnchor, pivot, frameCount);
        }

        private static CharacterSheetValidationResult Result(List<string> errors, Vector2Int cellSize, Vector2Int footAnchor, Vector2 pivot, int frames) =>
            new CharacterSheetValidationResult(errors, cellSize, footAnchor, pivot, frames);

        private static CharacterSheetManifest ReadManifest(string path, List<string> errors)
        {
            if (!File.Exists(path))
            {
                errors.Add("missing manifest");
                return null;
            }

            var manifest = JsonUtility.FromJson<CharacterSheetManifest>(File.ReadAllText(path));
            if (manifest == null)
                errors.Add("invalid manifest");
            return manifest;
        }

        private static void ValidateManifest(CharacterSheetManifest manifest, Vector2Int cellSize, Vector2Int footAnchor, Vector2 pivot, int frames, List<string> errors)
        {
            if (manifest.id != "mannequin") errors.Add("invalid id");
            if (cellSize != new Vector2Int(64, 64)) errors.Add("invalid cell size");
            if (footAnchor != new Vector2Int(32, 56)) errors.Add("invalid foot anchor");
            if (pivot != new Vector2(0.5f, 0.125f)) errors.Add("invalid pivot");
            if (manifest.pixelsPerUnit != 32) errors.Add("invalid pixels per unit");
            if (frames != Frames) errors.Add("invalid frame count");
            if (manifest.layers == null || manifest.layers.Length != 13) errors.Add("invalid layer contract");
        }

        private static HashSet<Color32> ReadPalette(string path, List<string> errors)
        {
            var colors = new HashSet<Color32>();
            var texture = LoadPng(path);
            if (texture == null)
            {
                errors.Add("missing palette");
                return colors;
            }

            foreach (var color in texture.GetPixels32())
                if (color.a == byte.MaxValue) colors.Add(color);
            UnityEngine.Object.DestroyImmediate(texture);
            if (colors.Count == 0) errors.Add("empty palette");
            return colors;
        }

        private static void ValidateLayer(string path, string layer, HashSet<Color32> palette, List<string> errors)
        {
            var texture = LoadPng(path);
            if (texture == null)
            {
                errors.Add("missing layer: " + layer);
                return;
            }

            if (texture.width != SheetWidth || texture.height != SheetHeight)
            {
                errors.Add("invalid canvas: " + layer);
                UnityEngine.Object.DestroyImmediate(texture);
                return;
            }

            var pixels = texture.GetPixels32();
            var hasSemiTransparent = false;
            var hasPaletteViolation = false;
            var hasUnusedPixel = false;
            for (var y = 0; y < SheetHeight; y++)
            for (var x = 0; x < SheetWidth; x++)
            {
                var color = pixels[y * SheetWidth + x];
                if (color.a > 0 && color.a < byte.MaxValue) hasSemiTransparent = true;
                if (color.a == byte.MaxValue && !palette.Contains(color)) hasPaletteViolation = true;
                var frame = (y / 64) * 6 + (x / 64);
                if (frame >= Frames && color.a != 0) hasUnusedPixel = true;
            }

            if (hasSemiTransparent) errors.Add("semi-transparent pixel: " + layer);
            if (hasPaletteViolation) errors.Add("color outside palette: " + layer);
            if (hasUnusedPixel) errors.Add("non-transparent unused cell: " + layer);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static Texture2D LoadPng(string path)
        {
            if (!File.Exists(path)) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false) ? texture : null;
        }

        private static int FrameCount(CharacterAnimation[] animations)
        {
            var max = 0;
            foreach (var animation in animations ?? Array.Empty<CharacterAnimation>())
                max = Math.Max(max, animation.start + animation.frames);
            return max;
        }

        private static Vector2Int ToVector2Int(int[] values) => values != null && values.Length == 2 ? new Vector2Int(values[0], values[1]) : Vector2Int.zero;
        private static Vector2 ToVector2(float[] values) => values != null && values.Length == 2 ? new Vector2(values[0], values[1]) : Vector2.zero;

        [Serializable] private sealed class CharacterSheetManifest { public string id; public int[] cellSize; public int[] footAnchor; public float[] pivot; public int pixelsPerUnit; public string[] layers; public CharacterAnimation[] animations; }
        [Serializable] private sealed class CharacterAnimation { public int start; public int frames; }
    }
}
