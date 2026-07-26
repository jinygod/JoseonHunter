using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public sealed record FrontFacingCharacterSheetValidationResult(
        IReadOnlyList<string> Errors,
        Vector2Int CellSize,
        Vector2Int SheetSize,
        Vector2Int FootAnchor,
        Vector2 Pivot,
        int FrameCount,
        float HeadHeightRatio);

    public static class FrontFacingCharacterSheetContract
    {
        public const int Columns = 4;
        public const int Rows = 3;
        public const int Frames = 12;
        public static readonly Vector2Int CellSize = new(64, 64);
        public static readonly Vector2Int SheetSize = new(256, 192);

        public static FrontFacingCharacterSheetValidationResult Validate(string sourceRoot, string runtimePath)
        {
            var errors = new List<string>();
            var manifest = ReadManifest(Path.Combine(sourceRoot ?? string.Empty, "manifest.json"), errors);
            if (manifest == null) return Result(errors, Vector2Int.zero, Vector2Int.zero, Vector2Int.zero, Vector2.zero, 0, 0f);

            var cellSize = ToVector2Int(manifest.cellSize);
            var sheetSize = ToVector2Int(manifest.sheetSize);
            var footAnchor = ToVector2Int(manifest.footAnchor);
            var pivot = ToVector2(manifest.pivot);
            var frameCount = FrameCount(manifest.animations);
            ValidateManifest(manifest, cellSize, sheetSize, footAnchor, pivot, frameCount, errors);

            var flattenedPath = Path.Combine(sourceRoot, "flattened.png");
            var flattened = LoadPng(flattenedPath);
            if (flattened == null) errors.Add("missing flattened sheet");
            else
            {
                ValidateFlattened(flattened, Path.Combine(sourceRoot, "palette.png"), errors);
                if (!HasAnimationVariation(flattened, 2, 4)) errors.Add("identical move frames");
                UnityEngine.Object.DestroyImmediate(flattened);
            }

            if (!FilesEqual(flattenedPath, runtimePath)) errors.Add("runtime does not match flattened source");
            return Result(errors, cellSize, sheetSize, footAnchor, pivot, frameCount, manifest.headHeightRatio);
        }

        public static bool HasAnimationVariation(string sourceRoot, int start, int frames)
        {
            var texture = LoadPng(Path.Combine(sourceRoot ?? string.Empty, "flattened.png"));
            if (texture == null) return false;
            var varied = HasAnimationVariation(texture, start, frames);
            UnityEngine.Object.DestroyImmediate(texture);
            return varied;
        }

        private static bool HasAnimationVariation(Texture2D texture, int start, int frames)
        {
            if (texture.width != SheetSize.x || texture.height != SheetSize.y || frames < 2 || start < 0 || start + frames > Frames) return false;
            var pixels = texture.GetPixels32();
            var signature = FrameSignature(pixels, start);
            for (var frame = start + 1; frame < start + frames; frame++)
                if (FrameSignature(pixels, frame) != signature) return true;
            return false;
        }

        private static FrontFacingCharacterSheetValidationResult Result(List<string> errors, Vector2Int cellSize, Vector2Int sheetSize, Vector2Int footAnchor, Vector2 pivot, int frames, float headHeightRatio) =>
            new FrontFacingCharacterSheetValidationResult(errors, cellSize, sheetSize, footAnchor, pivot, frames, headHeightRatio);

        private static FrontFacingManifest ReadManifest(string path, List<string> errors)
        {
            if (!File.Exists(path)) { errors.Add("missing manifest"); return null; }
            var manifest = JsonUtility.FromJson<FrontFacingManifest>(File.ReadAllText(path));
            if (manifest == null) errors.Add("invalid manifest");
            return manifest;
        }

        private static void ValidateManifest(FrontFacingManifest manifest, Vector2Int cellSize, Vector2Int sheetSize, Vector2Int footAnchor, Vector2 pivot, int frameCount, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(manifest.id)) errors.Add("missing id");
            if (cellSize != CellSize) errors.Add("invalid cell size");
            if (sheetSize != SheetSize) errors.Add("invalid sheet size");
            if (footAnchor != new Vector2Int(32, 56)) errors.Add("invalid foot anchor");
            if (pivot != new Vector2(0.5f, 0.125f)) errors.Add("invalid pivot");
            if (manifest.pixelsPerUnit != 32) errors.Add("invalid pixels per unit");
            if (manifest.view != "front") errors.Add("invalid view");
            if (!Matches(manifest.directions, "front")) errors.Add("invalid directions");
            if (manifest.headHeightRatio < .45f || manifest.headHeightRatio > .55f) errors.Add("invalid head height ratio");
            if (string.IsNullOrWhiteSpace(manifest.promptRevision)) errors.Add("missing prompt revision");
            if (frameCount != Frames) errors.Add("invalid frame count");
            if (manifest.animations == null || manifest.animations.Length != 3 ||
                !AnimationMatches(manifest.animations[0], "idle", 0, 2, 4) ||
                !AnimationMatches(manifest.animations[1], "move", 2, 4, 8) ||
                !AnimationMatches(manifest.animations[2], "death", 6, 6, 8)) errors.Add("invalid animation contract");
        }

        private static void ValidateFlattened(Texture2D texture, string palettePath, List<string> errors)
        {
            if (texture.width != SheetSize.x || texture.height != SheetSize.y) { errors.Add("invalid sheet size"); return; }
            var palette = ReadPalette(palettePath, errors);
            var pixels = texture.GetPixels32();
            if (!IsTransparent(pixels, 0, 0) || !IsTransparent(pixels, SheetSize.x - 1, 0) ||
                !IsTransparent(pixels, 0, SheetSize.y - 1) || !IsTransparent(pixels, SheetSize.x - 1, SheetSize.y - 1)) errors.Add("opaque sheet corner");
            var hasSemiTransparent = false;
            var hasPaletteViolation = false;
            foreach (var pixel in pixels)
            {
                if (pixel.a > 0 && pixel.a < byte.MaxValue) hasSemiTransparent = true;
                if (pixel.a == byte.MaxValue && !palette.Contains(pixel)) hasPaletteViolation = true;
            }
            if (hasSemiTransparent) errors.Add("semi-transparent pixel");
            if (hasPaletteViolation) errors.Add("color outside palette");
        }

        private static HashSet<Color32> ReadPalette(string path, List<string> errors)
        {
            var colors = new HashSet<Color32>();
            var texture = LoadPng(path);
            if (texture == null) { errors.Add("missing palette"); return colors; }
            foreach (var color in texture.GetPixels32()) if (color.a == byte.MaxValue) colors.Add(color);
            UnityEngine.Object.DestroyImmediate(texture);
            if (colors.Count == 0) errors.Add("empty palette");
            return colors;
        }

        private static bool IsTransparent(Color32[] pixels, int x, int y) => pixels[y * SheetSize.x + x].a == 0;
        private static bool FilesEqual(string first, string second)
        {
            if (!File.Exists(first) || !File.Exists(second)) return false;
            var left = File.ReadAllBytes(first);
            var right = File.ReadAllBytes(second);
            if (left.Length != right.Length) return false;
            for (var index = 0; index < left.Length; index++) if (left[index] != right[index]) return false;
            return true;
        }
        private static int FrameSignature(Color32[] pixels, int frame)
        {
            unchecked
            {
                var hash = 17;
                var originX = (frame % Columns) * 64;
                var originY = (frame / Columns) * 64;
                for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++) hash = hash * 31 + pixels[(originY + y) * SheetSize.x + originX + x].GetHashCode();
                return hash;
            }
        }
        private static int FrameCount(FrontFacingAnimation[] animations)
        {
            var max = 0;
            foreach (var animation in animations ?? Array.Empty<FrontFacingAnimation>()) max = Math.Max(max, animation.start + animation.frames);
            return max;
        }
        private static bool Matches(string[] values, string expected) => values != null && values.Length == 1 && values[0] == expected;
        private static bool AnimationMatches(FrontFacingAnimation animation, string name, int start, int frames, int fps) => animation != null && animation.name == name && animation.start == start && animation.frames == frames && animation.fps == fps;
        private static Texture2D LoadPng(string path)
        {
            if (!File.Exists(path)) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false) ? texture : null;
        }
        private static Vector2Int ToVector2Int(int[] values) => values != null && values.Length == 2 ? new Vector2Int(values[0], values[1]) : Vector2Int.zero;
        private static Vector2 ToVector2(float[] values) => values != null && values.Length == 2 ? new Vector2(values[0], values[1]) : Vector2.zero;

        [Serializable] private sealed class FrontFacingManifest { public string id; public int[] cellSize; public int[] sheetSize; public int[] footAnchor; public float[] pivot; public int pixelsPerUnit; public string view; public string[] directions; public float headHeightRatio; public string promptRevision; public FrontFacingAnimation[] animations; }
        [Serializable] private sealed class FrontFacingAnimation { public string name; public int start; public int frames; public int fps; }
    }
}
