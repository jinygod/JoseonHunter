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
        private static readonly string[] RequiredDirections = { "down", "right", "up" };
        private static readonly string[] RequiredLayers = { "shadow", "back-equipment", "body", "back-hair", "lower-clothing", "upper-clothing", "armor", "face", "front-hair", "headwear", "left-weapon", "right-prop", "front-overlay" };
        private static readonly string[] RequiredPaletteSlots = { "skin", "primary-cloth", "secondary-cloth", "accent", "metal", "outline" };

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
            ValidateRuntime(sourceRoot, runtimePath, manifest.layers, errors);

            return Result(errors, cellSize, footAnchor, pivot, frameCount);
        }

        public static RectInt[] ActiveFrameBounds(string sourceRoot)
        {
            var pixels = BuildComposite(sourceRoot, ReadManifest(Path.Combine(sourceRoot, "manifest.json"), new List<string>())?.layers);
            var bounds = new RectInt[Frames];
            for (var frame = 0; frame < Frames; frame++)
            {
                var minX = 64; var minY = 64; var maxX = -1; var maxY = -1;
                var originX = (frame % 6) * 64; var originY = (frame / 6) * 64;
                for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++)
                {
                    if (pixels[(originY + y) * SheetWidth + originX + x].a == 0) continue;
                    minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                    var displayY = 63 - y;
                    minY = Math.Min(minY, displayY); maxY = Math.Max(maxY, displayY);
                }
                bounds[frame] = maxX < 0 ? new RectInt() : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            }
            return bounds;
        }

        public static bool HasAnimationVariation(string sourceRoot, int start, int frames)
        {
            var pixels = BuildComposite(sourceRoot, ReadManifest(Path.Combine(sourceRoot, "manifest.json"), new List<string>())?.layers);
            if (frames < 2) return false;
            var first = FrameSignature(pixels, start);
            for (var frame = start + 1; frame < start + frames; frame++)
                if (FrameSignature(pixels, frame) != first) return true;
            return false;
        }

        public static bool FramesDiffer(string sourceRoot, int firstFrame, int secondFrame)
        {
            if (firstFrame < 0 || secondFrame < 0 || firstFrame >= Frames || secondFrame >= Frames)
                return false;
            var pixels = BuildComposite(sourceRoot, ReadManifest(Path.Combine(sourceRoot, "manifest.json"), new List<string>())?.layers);
            return FrameSignature(pixels, firstFrame) != FrameSignature(pixels, secondFrame);
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
            if (string.IsNullOrWhiteSpace(manifest.id)) errors.Add("missing id");
            if (cellSize != new Vector2Int(64, 64)) errors.Add("invalid cell size");
            if (footAnchor != new Vector2Int(32, 56)) errors.Add("invalid foot anchor");
            if (pivot != new Vector2(0.5f, 0.125f)) errors.Add("invalid pivot");
            if (manifest.pixelsPerUnit != 32) errors.Add("invalid pixels per unit");
            if (frames != Frames) errors.Add("invalid frame count");
            if (!Matches(manifest.directions, RequiredDirections)) errors.Add("invalid directions");
            if (manifest.mirrorLeftFrom != "right") errors.Add("invalid mirror source");
            if (!Matches(manifest.layers, RequiredLayers)) errors.Add("invalid layer contract");
            if (!Matches(manifest.paletteSlots, RequiredPaletteSlots)) errors.Add("invalid palette slots");
            if (string.IsNullOrWhiteSpace(manifest.promptRevision)) errors.Add("missing prompt revision");
            if (manifest.animations == null || manifest.animations.Length != 3 ||
                manifest.animations[0].name != "idle" || manifest.animations[1].name != "move" ||
                manifest.animations[2].name != "death") errors.Add("invalid animation contract");
            ValidateAnimation(manifest.animations, "idle", 0, 12, 6, errors);
            ValidateAnimation(manifest.animations, "move", 12, 18, 10, errors);
            ValidateAnimation(manifest.animations, "death", 30, 8, 10, errors);
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

        private static void ValidateRuntime(string sourceRoot, string runtimePath, string[] layers, List<string> errors)
        {
            var runtime = LoadPng(runtimePath);
            if (runtime == null) { errors.Add("missing runtime sheet"); return; }
            if (runtime.width != SheetWidth || runtime.height != SheetHeight) { errors.Add("invalid runtime canvas"); UnityEngine.Object.DestroyImmediate(runtime); return; }
            var expected = BuildComposite(sourceRoot, layers);
            var actual = runtime.GetPixels32();
            for (var index = 0; index < expected.Length; index++)
                if (!expected[index].Equals(actual[index])) { errors.Add("runtime does not match layer composite"); break; }
            UnityEngine.Object.DestroyImmediate(runtime);
        }

        private static Color32[] BuildComposite(string sourceRoot, string[] layers)
        {
            var composite = new Color32[SheetWidth * SheetHeight];
            foreach (var layer in layers ?? Array.Empty<string>())
            {
                var texture = LoadPng(Path.Combine(sourceRoot, "layers", layer + ".png"));
                if (texture == null || texture.width != SheetWidth || texture.height != SheetHeight) { if (texture != null) UnityEngine.Object.DestroyImmediate(texture); continue; }
                var pixels = texture.GetPixels32();
                for (var index = 0; index < pixels.Length; index++) if (pixels[index].a > 0) composite[index] = pixels[index];
                UnityEngine.Object.DestroyImmediate(texture);
            }
            return composite;
        }

        private static int FrameSignature(Color32[] pixels, int frame)
        {
            unchecked
            {
                var hash = 17; var originX = (frame % 6) * 64; var originY = (frame / 6) * 64;
                for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++)
                    hash = hash * 31 + pixels[(originY + y) * SheetWidth + originX + x].GetHashCode();
                return hash;
            }
        }

        private static bool Matches(string[] values, string[] required)
        {
            if (values == null || values.Length != required.Length) return false;
            for (var index = 0; index < required.Length; index++)
                if (values[index] != required[index]) return false;
            return true;
        }

        private static void ValidateAnimation(CharacterAnimation[] animations, string name, int start, int frames, int fps, List<string> errors)
        {
            CharacterAnimation match = null;
            var matches = 0;
            foreach (var animation in animations ?? Array.Empty<CharacterAnimation>())
                if (animation.name == name) { match = animation; matches++; }
            if (matches != 1 || match.start != start || match.frames != frames || match.fps != fps)
                errors.Add("invalid animation: " + name);
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

        [Serializable] private sealed class CharacterSheetManifest { public string id; public int[] cellSize; public int[] footAnchor; public float[] pivot; public int pixelsPerUnit; public string[] directions; public string mirrorLeftFrom; public string[] layers; public string[] paletteSlots; public string promptRevision; public CharacterAnimation[] animations; }
        [Serializable] private sealed class CharacterAnimation { public string name; public int start; public int frames; public int fps; }
    }
}
