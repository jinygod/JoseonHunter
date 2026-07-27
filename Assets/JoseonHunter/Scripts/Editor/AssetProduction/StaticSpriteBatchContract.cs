using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace JoseonHunter.Editor.AssetProduction
{
    public sealed record StaticSpriteBatchValidationResult(IReadOnlyList<string> Errors, int AssetCount);

    public static class StaticSpriteBatchContract
    {
        public const int CanvasSize = 64;
        public const int MaxOpaqueColors = 48;
        public static readonly Vector2Int FootAnchor = new(32, 56);
        public static readonly Vector2 Pivot = new(0.5f, 0.125f);

        private static readonly HashSet<string> ExpectedIds = new(StringComparer.Ordinal)
        {
            "rookie_constable", "shaman", "mountain_hunter", "plague_rat", "vengeful_spirit", "sakkat_specter",
            "dokkaebi", "bandit", "fallen_general", "coin", "experience_spirit_flame", "treasure_chest"
        };
        private static readonly Regex SensitivePattern = new(@"api[_-]?key|token|secret|bearer", RegexOptions.IgnoreCase);
        private static readonly Regex UuidPattern = new(@"\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b", RegexOptions.IgnoreCase);
        private static readonly Regex JsonStringProperty = new("\\\"(?<name>(?:\\\\.|[^\\\"])*)\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"", RegexOptions.Compiled);

        public static StaticSpriteBatchValidationResult Validate(string manifestPath, string sourceRoot, string runtimeRoot, bool requireRuntime)
        {
            var errors = new List<string>();
            if (!File.Exists(manifestPath)) return new StaticSpriteBatchValidationResult(new[] { "missing manifest" }, 0);
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest == null) return new StaticSpriteBatchValidationResult(new[] { "invalid manifest" }, 0);
            if (manifest.schemaVersion != 1) errors.Add("invalid schema version");
            if (string.IsNullOrWhiteSpace(manifest.promptRevision)) errors.Add("missing prompt revision");
            var assets = manifest.assets ?? Array.Empty<Asset>();
            if (assets.Length != ExpectedIds.Count) errors.Add("unexpected asset id");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var asset in assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.id)) { errors.Add("missing id"); continue; }
                if (!seen.Add(asset.id)) errors.Add("duplicate id");
                if (!ExpectedIds.Contains(asset.id)) errors.Add("unexpected asset id");
                ValidateMetadata(asset, errors);
                var sourceDirectory = Path.Combine(sourceRoot ?? string.Empty, SourceDirectory(asset.sourcePath));
                errors.AddRange(ValidateAsset(asset.id, sourceDirectory));
                var sourcePath = Path.Combine(sourceRoot ?? string.Empty, asset.sourcePath ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(asset.sha256) && !string.Equals(asset.sha256.ToLowerInvariant(), Sha256(sourcePath), StringComparison.Ordinal)) errors.Add("source hash mismatch");
                if (requireRuntime && !FilesEqual(sourcePath, Path.Combine(runtimeRoot ?? string.Empty, asset.runtimePath ?? string.Empty))) errors.Add("runtime byte mismatch");
            }
            foreach (var expected in ExpectedIds) if (!seen.Contains(expected)) errors.Add("missing required asset id");
            return new StaticSpriteBatchValidationResult(errors, assets.Length);
        }

        public static IReadOnlyList<string> ValidateAsset(string assetId, string sourceDirectory)
        {
            var errors = new List<string>();
            var spritePath = Path.Combine(sourceDirectory ?? string.Empty, "sprite.png");
            if (!File.Exists(spritePath)) { errors.Add("missing sprite"); return errors; }
            if (!File.Exists(Path.Combine(sourceDirectory, "palette.png"))) errors.Add("missing palette");
            if (!File.Exists(Path.Combine(sourceDirectory, "prompt.md"))) errors.Add("missing prompt");
            var provenancePath = Path.Combine(sourceDirectory, "provenance.json");
            if (!File.Exists(provenancePath)) errors.Add("missing provenance");
            else if (ContainsSensitiveProvenance(File.ReadAllText(provenancePath))) errors.Add("token-like provenance value");
            if (!IsRgbaPng(spritePath)) errors.Add("non-RGBA input");
            var texture = LoadPng(spritePath);
            if (texture == null) { errors.Add("invalid sprite image"); return errors; }
            try { ValidatePixels(texture, errors); } finally { UnityEngine.Object.DestroyImmediate(texture); }
            return errors;
        }

        public static void ValidateFromCommandLine()
        {
            var result = Validate(CommandLineValue("-staticSpriteManifestPath"), CommandLineValue("-staticSpriteSourceRoot"), CommandLineValue("-staticSpriteRuntimeRoot"), HasCommandLineSwitch("-staticSpriteRequireRuntime"));
            if (result.Errors.Count == 0) { Debug.Log("Static sprite batch preflight passed."); return; }
            Debug.LogError("Static sprite batch preflight failed: " + string.Join("; ", result.Errors));
            EditorApplication.Exit(1);
        }

        private static void ValidateMetadata(Asset asset, List<string> errors)
        {
            if (asset.width != CanvasSize || asset.height != CanvasSize) errors.Add("invalid dimensions");
            if (ToVector2Int(asset.footAnchor) != FootAnchor) errors.Add("invalid foot anchor");
            if (ToVector2(asset.pivot) != Pivot) errors.Add("invalid pivot");
            if (asset.pixelsPerUnit != 32) errors.Add("invalid pixels per unit");
            if (string.IsNullOrWhiteSpace(asset.sourcePath) || string.IsNullOrWhiteSpace(asset.runtimePath)) errors.Add("missing asset path");
        }
        private static void ValidatePixels(Texture2D texture, List<string> errors)
        {
            if (texture.width != CanvasSize || texture.height != CanvasSize) { errors.Add("invalid dimensions"); return; }
            var pixels = texture.GetPixels32(); var colors = new HashSet<Color32>(); var minX = CanvasSize; var maxX = -1; var maxY = -1; var semi = false;
            foreach (var pixel in pixels)
            {
                if (pixel.a > 0 && pixel.a < byte.MaxValue) semi = true;
            }
            for (var y = 0; y < CanvasSize; y++) for (var x = 0; x < CanvasSize; x++)
            {
                var pixel = pixels[y * CanvasSize + x]; if (pixel.a != byte.MaxValue) continue;
                colors.Add(pixel); minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
            }
            if (semi) errors.Add("semi-transparent pixel");
            if (pixels[0].a != 0 || pixels[CanvasSize - 1].a != 0 || pixels[CanvasSize * (CanvasSize - 1)].a != 0 || pixels[^1].a != 0) errors.Add("opaque corner");
            if (colors.Count > MaxOpaqueColors) errors.Add("too many opaque colors");
            if (maxY != FootAnchor.y) errors.Add("invalid maximum opaque y");
            if (maxX >= 0 && ((minX + maxX) / 2f < 30f || (minX + maxX) / 2f > 34f)) errors.Add("invalid horizontal center");
        }
        private static bool ContainsSensitiveProvenance(string json)
        {
            foreach (Match match in JsonStringProperty.Matches(json))
            {
                var name = match.Groups["name"].Value; var value = match.Groups["value"].Value;
                if (SensitivePattern.IsMatch(name) || SensitivePattern.IsMatch(value)) return true;
                if (!string.Equals(name, "jobId", StringComparison.OrdinalIgnoreCase) && UuidPattern.IsMatch(value)) return true;
            }
            return false;
        }
        private static bool IsRgbaPng(string path)
        {
            var bytes = File.ReadAllBytes(path);
            return bytes.Length > 25 && bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71 && bytes[25] == 6;
        }
        private static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false)) return texture;
            UnityEngine.Object.DestroyImmediate(texture); return null;
        }
        private static string Sha256(string path)
        {
            if (!File.Exists(path)) return string.Empty;
            using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        private static bool FilesEqual(string first, string second)
        {
            if (!File.Exists(first) || !File.Exists(second)) return false;
            var left = File.ReadAllBytes(first); var right = File.ReadAllBytes(second);
            if (left.Length != right.Length) return false;
            for (var i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }
        private static Vector2Int ToVector2Int(int[] values) => values != null && values.Length == 2 ? new Vector2Int(values[0], values[1]) : Vector2Int.zero;
        private static Vector2 ToVector2(float[] values) => values != null && values.Length == 2 ? new Vector2(values[0], values[1]) : Vector2.zero;
        private static string SourceDirectory(string sourcePath) => string.IsNullOrWhiteSpace(sourcePath) ? string.Empty : Path.GetDirectoryName(sourcePath) ?? string.Empty;
        private static string CommandLineValue(string name) { var arguments = Environment.GetCommandLineArgs(); for (var i = 0; i < arguments.Length - 1; i++) if (arguments[i] == name) return arguments[i + 1]; return string.Empty; }
        private static bool HasCommandLineSwitch(string name) { foreach (var argument in Environment.GetCommandLineArgs()) if (argument == name) return true; return false; }
        [Serializable] private sealed class Manifest { public int schemaVersion; public string promptRevision; public Asset[] assets; }
        [Serializable] private sealed class Asset { public string id; public string role; public string sourcePath; public string runtimePath; public int width; public int height; public int[] footAnchor; public float[] pivot; public int pixelsPerUnit; public string approvalStatus; public string sha256; }
    }
}
